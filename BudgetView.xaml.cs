using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Globalization;
using System.Collections.Generic;

namespace wpf_inz
{
    public partial class BudgetView : UserControl, INotifyPropertyChanged
    {
        public ObservableCollection<Budget> Budgets { get; set; } = new ObservableCollection<Budget>();
        private Dictionary<(string Currency, int Year, int Month), decimal> _exchangeRates;

        private void LoadExchangeRates()
        {
            using (var context = new ApplicationDbContext())
            {
                var rates = context.ExchangeRates.ToList();
                _exchangeRates = rates.ToDictionary(
                    x => (x.Currency, x.Year, x.Month),
                    x => x.RateToPLN
                );
            }
            CurrencyHelper.SetExchangeRates(_exchangeRates);

        }

        private decimal GetRateToPLN(string currency, DateTime date)
        {
            // 1) Sprawdzamy klucz exact
            var keyExact = (currency, date.Year, date.Month);
            if (_exchangeRates.TryGetValue(keyExact, out var exactRate))
                return exactRate;

            // 2) Jeżeli brak, to poszukajmy wstecz (najświeższy poprzedni)
            for (int monthsBack = 1; monthsBack <= 120; monthsBack++)
            {
                
                var fallbackDate = date.AddMonths(-monthsBack);
                var fallbackKey = (currency, fallbackDate.Year, fallbackDate.Month);
                if (_exchangeRates.TryGetValue(fallbackKey, out var fallbackRate))
                    return fallbackRate;
            }

           
            if (currency == "PLN") return 1.0m;

           
            return 0m;
        }


        public BudgetView()
        {
            InitializeComponent();
            LoadExchangeRates();
            InitializeYearComboBox();
            CurrentMonth = DateTime.Now;
            YearComboBox.SelectedItem = CurrentMonth.Year;

            LoadBudgets();
            DataContext = this;

            BudgetDataGrid.InitializingNewItem += BudgetDataGrid_InitializingNewItem;
            Budgets.CollectionChanged += (s, e) =>
            {
                // Aktualizacja sum przy każdej zmianie w kolekcji Budgets
                OnPropertyChanged(nameof(TotalIncomeInSelectedCurrency));
                OnPropertyChanged(nameof(TotalExpensesInSelectedCurrency));
                OnPropertyChanged(nameof(BalanceInSelectedCurrency));
                OnPropertyChanged(nameof(SelectedCurrency));
            };
        }
        public string SelectedCurrency => Session.SelectedCurrency;


        private void BudgetDataGrid_InitializingNewItem(object sender, InitializingNewItemEventArgs e)
        {
            if (e.NewItem is Budget newBudget)
            {
                newBudget.Category = "Koszty"; // Ustaw domyślną kategorię
                newBudget.Currency = "PLN";   // Ustaw domyślną walutę
                newBudget.Date = new DateTime(CurrentMonth.Year, CurrentMonth.Month, 1); // Domyślna data
            }
        }

        public decimal TotalIncomeInSelectedCurrency
        {
            get
            {
                decimal total = 0;
                foreach (var b in Budgets.Where(x => x.Category == "Przychody"))
                {
                   
                    decimal rateToPLN = GetRateToPLN(b.Currency, b.Date);
                    decimal amountPLN = b.Amount * rateToPLN;

                    
                    decimal amountSelected = CurrencyHelper.ConvertPLNToSelectedCurrency(amountPLN, b.Date);
                    total += amountSelected;
                }
                return total;
            }
        }

        public decimal TotalExpensesInSelectedCurrency
        {
            get
            {
                decimal total = 0;
                foreach (var b in Budgets.Where(x => x.Category == "Koszty"))
                {
                    decimal rateToPLN = GetRateToPLN(b.Currency, b.Date);
                    decimal amountPLN = b.Amount * rateToPLN;
                    decimal amountSelected = CurrencyHelper.ConvertPLNToSelectedCurrency(amountPLN, b.Date);
                    total += amountSelected;
                }
                return total;
            }
        }

        public decimal BalanceInSelectedCurrency
        {
            get
            {
                return TotalIncomeInSelectedCurrency - TotalExpensesInSelectedCurrency;
            }
        }



        private DateTime _currentMonth;
        public DateTime CurrentMonth
        {
            get => _currentMonth;
            set
            {
                if (_currentMonth != value)
                {
                    _currentMonth = value;
                    OnPropertyChanged(nameof(CurrentMonth));
                    OnPropertyChanged(nameof(CurrentMonthDisplay));
                }
            }
        }

        public string CurrentMonthDisplay => _currentMonth.ToString("MMMM yyyy", CultureInfo.CreateSpecificCulture("pl-PL"));

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void InitializeYearComboBox()
        {
            YearComboBox.Items.Clear();
            int currentYear = DateTime.Now.Year;
            for (int year = currentYear - 1; year <= currentYear + 1; year++)
            {
                YearComboBox.Items.Add(year);
            }
            YearComboBox.SelectedItem = currentYear;
        }

        private void LoadBudgets()
        {
            using (var context = new ApplicationDbContext())
            {
                var userId = Session.CurrentUser.Id;
                var query = context.Budgets.Where(b => b.UserId == userId);

                query = query.Where(b => b.Date.Year == CurrentMonth.Year && b.Date.Month == CurrentMonth.Month);

                Budgets.Clear();
                foreach (var budget in query.ToList())
                    Budgets.Add(budget);

                // Aktualizacja podsumowania
                OnPropertyChanged(nameof(TotalIncomeInSelectedCurrency));
                OnPropertyChanged(nameof(TotalExpensesInSelectedCurrency));
                OnPropertyChanged(nameof(BalanceInSelectedCurrency));
            }
        }

        private void PreviousMonth_Click(object sender, RoutedEventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(-1);
            OnPropertyChanged(nameof(CurrentMonthDisplay));
            LoadBudgets();
        }

        private void NextMonth_Click(object sender, RoutedEventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(1);
            OnPropertyChanged(nameof(CurrentMonthDisplay));
            LoadBudgets();
        }

        private void YearComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (YearComboBox.SelectedItem is int selectedYear)
            {
                _currentMonth = new DateTime(selectedYear, _currentMonth.Month, 1);
                OnPropertyChanged(nameof(CurrentMonthDisplay));
                LoadBudgets();
            }
        }

        private void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var budget = button?.DataContext as Budget;

            if (budget != null)
            {
                var result = MessageBox.Show($"Czy na pewno chcesz usunąć ten wpis?", "Potwierdzenie",
                                             MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    using (var context = new ApplicationDbContext())
                    {
                        context.Budgets.Remove(context.Budgets.Find(budget.Id));
                        context.SaveChanges();
                    }

                    Budgets.Remove(budget);
                }
            }
        }


        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            using (var context = new ApplicationDbContext())
            {
                foreach (var budget in Budgets)
                {
                    if (budget.Id == 0)
                    {
                        budget.UserId = Session.CurrentUser.Id;
                        if (budget.Date == DateTime.MinValue)
                            budget.Date = new DateTime(CurrentMonth.Year, CurrentMonth.Month, 1);
                        context.Budgets.Add(budget);
                    }
                    else
                    {
                        var existing = context.Budgets.FirstOrDefault(b => b.Id == budget.Id);
                        if (existing != null)
                        {
                            existing.Category = budget.Category;
                            existing.Currency = budget.Currency;
                            existing.Amount = budget.Amount;
                            existing.Description = budget.Description;
                            existing.Date = budget.Date;
                        }
                    }
                }

                context.SaveChanges();
                ((MainWindow)Application.Current.MainWindow).ShowNotification("Zmiany zostały pomyślnie zapisane", "success");
                LoadBudgets();
            }
        }
        

    }
}
