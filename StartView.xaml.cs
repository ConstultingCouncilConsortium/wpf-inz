using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.EntityFrameworkCore;

namespace wpf_inz
{
    public partial class StartView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public ObservableCollection<string> AvailableMonths { get; set; } = new ObservableCollection<string>();

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string UserName { get; set; }
        public SeriesCollection ExpensesSeries { get; set; }
        public List<string> Months { get; set; }
        public Func<double, string> Formatter { get; set; }

        private int _monthOffset;
        public int MonthOffset
        {
            get => _monthOffset;
            set
            {
                _monthOffset = value;
                OnPropertyChanged(nameof(MonthOffset));
                OnPropertyChanged(nameof(CurrentRange));
            }
        }

        public string CurrentRange
        {
            get
            {
                var startDate = new DateTime(DateTime.Now.AddMonths(MonthOffset - 5).Year, DateTime.Now.AddMonths(MonthOffset - 5).Month, 1);
                var endDate = new DateTime(DateTime.Now.AddMonths(MonthOffset).Year, DateTime.Now.AddMonths(MonthOffset).Month, 1).AddMonths(1).AddDays(-1);
                return $"{startDate:MM/yyyy} - {endDate:MM/yyyy}";
            }
        }

        private string _selectedCategory = "Wydatki"; // Domyślnie "Wydatki"
        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged(nameof(SelectedCategory));
                GenerateExpensesReport();
                LoadWarrantyExpirations();
            }
        }
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

            Debug.WriteLine($"LoadExchangeRates() -> _exchangeRates = {_exchangeRates?.Count} elementów");
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
                // Liczba 120 to arbitralnie 10 lat wstecz – do ustalenia wg potrzeb
                var fallbackDate = date.AddMonths(-monthsBack);
                var fallbackKey = (currency, fallbackDate.Year, fallbackDate.Month);
                if (_exchangeRates.TryGetValue(fallbackKey, out var fallbackRate))
                    return fallbackRate;
            }

            // 3) Jeśli dalej nic, a waluta to PLN – użyj 1.0
            if (currency == "PLN") return 1.0m;

            // 4) W innym razie 0 lub rzuć wyjątek – zależy co wolisz
            return 0m;
        }

        public StartView()
        {
            
            if (string.IsNullOrEmpty(Session.SelectedCurrency))
                Session.SelectedCurrency = "PLN";

            LoadExchangeRates();
            InitializeComponent();
            Debug.WriteLine("_exchangeRates is null? " + (_exchangeRates == null));
            foreach (ComboBoxItem comboItem in CurrencyComboBox.Items)
            {
                if (comboItem.Content?.ToString() == Session.SelectedCurrency)
                {
                    CurrencyComboBox.SelectedItem = comboItem;
                    break;
                }
            }
            DataContext = this;

            if (Session.CurrentUser != null)
            {
                UserName = Session.CurrentUser.Username;
            }
            else
            {
                UserName = "Gość";
            }

            // Generuj listę miesięcy
            var currentDate = DateTime.Now;
            for (int i = 0; i < 4; i++)
            {
                var date = currentDate.AddMonths(-i);
                AvailableMonths.Add($"{date.Month:00}/{date.Year}");
            }

            if (AvailableMonths.Any())
            {
                FirstMonthSelector.SelectedIndex = 0;
                var selectedMonth = AvailableMonths[0];
                var monthYear = selectedMonth.Split('/');
                int selectedMonthInt = int.Parse(monthYear[0]);
                int selectedYear = int.Parse(monthYear[1]);

                // Oblicz poprzedni miesiąc
                var previousDate = new DateTime(selectedYear, selectedMonthInt, 1).AddMonths(-1);
                PreviousMonth = $"{previousDate.Month:00}/{previousDate.Year}";
            }

            LoadUpcomingEvents();
            GenerateExpensesReport();
        }


        private void GenerateExpensesReport()
        {
            using (var context = new ApplicationDbContext())
            {
                var userId = Session.CurrentUser?.Id ?? 0;

                // Wyliczamy zakres dat (ostatnie 6 miesięcy)
                var currentDate = DateTime.Now.AddMonths(MonthOffset);
                var startDate = new DateTime(currentDate.AddMonths(-5).Year, currentDate.AddMonths(-5).Month, 1);
                var endDate = new DateTime(currentDate.Year, currentDate.Month, 1).AddMonths(1).AddDays(-1);

                // Pobieramy dane z bazy
                var rawData = context.Budgets
                    .Where(b => b.UserId == userId && b.Date >= startDate && b.Date <= endDate)
                    .ToList();

                // Najpierw dla każdej transakcji wyliczamy kwotę w wybranej walucie:
                //   1) Kwota oryginalna => PLN
                //   2) PLN => Session.SelectedCurrency
                var convertedData = rawData.Select(b =>
                {
                    // A) Oryginalna waluta -> PLN
                    decimal ratePln = GetRateToPLN(b.Currency, b.Date);
                    double amountPln = (double)(b.Amount * ratePln);

                    // B) PLN -> docelowa waluta (jeżeli Session.SelectedCurrency != "PLN")
                    double finalAmount = 0.0;
                    if (Session.SelectedCurrency == "PLN")
                    {
                        finalAmount = amountPln;
                    }
                    else
                    {
                        decimal targetRate = GetRateToPLN(Session.SelectedCurrency, b.Date);
                        finalAmount = (targetRate == 0) ? 0 : (amountPln / (double)targetRate);
                    }

                    return new
                    {
                        b.Category,
                        b.Date,
                        AmountInSelected = finalAmount
                    };
                }).ToList();

                // Teraz w zależności od wybranej kategorii (Wydatki, Przychody, Bilans)
                // grupujemy po (Rok, Miesiąc) i sumujemy 'AmountInSelected'.
                IEnumerable<dynamic> filteredData;

                if (SelectedCategory == "Wydatki")
                {
                    // Bierzemy tylko te, które Category == "Koszty"
                    filteredData = convertedData
                        .Where(x => x.Category == "Koszty")
                        .GroupBy(x => new { x.Date.Year, x.Date.Month })
                        .Select(g => new
                        {
                            Month = $"{g.Key.Month}/{g.Key.Year}",
                            Total = g.Sum(x => x.AmountInSelected)
                        });
                }
                else if (SelectedCategory == "Przychody")
                {
                    filteredData = convertedData
                        .Where(x => x.Category == "Przychody")
                        .GroupBy(x => new { x.Date.Year, x.Date.Month })
                        .Select(g => new
                        {
                            Month = $"{g.Key.Month}/{g.Key.Year}",
                            Total = g.Sum(x => x.AmountInSelected)
                        });
                }
                else // Bilans
                {
                    // Dla bilansu: Przychody - Wydatki w docelowej walucie
                    // W tym celu możemy analogicznie rozdzielić transakcje na "Koszty" i "Przychody"
                    // i potem je odjąć

                    var expensesDict = convertedData
                        .Where(x => x.Category == "Koszty")
                        .GroupBy(x => new { x.Date.Year, x.Date.Month })
                        .ToDictionary(
                            g => $"{g.Key.Month}/{g.Key.Year}",
                            g => g.Sum(xx => xx.AmountInSelected)
                        );

                    var incomesDict = convertedData
                        .Where(x => x.Category == "Przychody")
                        .GroupBy(x => new { x.Date.Year, x.Date.Month })
                        .ToDictionary(
                            g => $"{g.Key.Month}/{g.Key.Year}",
                            g => g.Sum(xx => xx.AmountInSelected)
                        );

                    filteredData = Enumerable.Range(0, 6).Select(i =>
                    {
                        var date = startDate.AddMonths(i);
                        var label = $"{date.Month}/{date.Year}";

                        double income = incomesDict.ContainsKey(label) ? incomesDict[label] : 0.0;
                        double expense = expensesDict.ContainsKey(label) ? expensesDict[label] : 0.0;

                        return new
                        {
                            Month = label,
                            Total = income - expense
                        };
                    });
                }

                // Tworzymy "fullData" na kolejne 6 miesięcy (od startDate do startDate+5m)
                // i dołączamy sumy z 'filteredData'.
                var fullData = Enumerable.Range(0, 6).Select(i =>
                {
                    var date = startDate.AddMonths(i);
                    var label = $"{date.Month}/{date.Year}";
                    // Znajdujemy "Total" w docelowej walucie
                    var total = filteredData.FirstOrDefault(d => d.Month == label)?.Total ?? 0;
                    return new
                    {
                        Month = label,
                        Total = total
                    };
                }).ToList();

                // Ustawiamy Months i budujemy serię do wykresu
                Months = fullData.Select(d => d.Month).ToList();

                ExpensesSeries = new SeriesCollection
        {
            new ColumnSeries
            {
                Title = SelectedCategory,
                Values = new ChartValues<double>(fullData.Select(d => (double)d.Total))
            }
        };

                // Formatter do osi Y, np. dwie cyfry + kod waluty
                Formatter = value => $"{value:N2} {Session.SelectedCurrency}";

                OnPropertyChanged(nameof(Months));
                OnPropertyChanged(nameof(ExpensesSeries));
            }
        }


        private void PreviousMonth_Click(object sender, RoutedEventArgs e)
        {
            MonthOffset--;
            GenerateExpensesReport();
        }

        private void NextMonth_Click(object sender, RoutedEventArgs e)
        {
            MonthOffset++;
            GenerateExpensesReport();
        }

        private void CategorySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var comboBoxItem = e.AddedItems[0] as ComboBoxItem;
                SelectedCategory = comboBoxItem?.Content.ToString();
            }
        }
        private ObservableCollection<string> _upcomingEvents;
        public ObservableCollection<string> UpcomingEvents
        {
            get => _upcomingEvents;
            set
            {
                _upcomingEvents = value;
                OnPropertyChanged(nameof(UpcomingEvents));
            }
        }

        private void LoadUpcomingEvents()
        {
            using (var context = new ApplicationDbContext())
            {
                var tomorrow = DateTime.Now.AddDays(1).Date;

                var events = context.UnifiedEvents
                    .Where(ue =>
                        (ue.EventType == "GeneralNote" && context.GeneralNotes.Any(gn => gn.Id == ue.ReferenceId && gn.Date.Date == tomorrow)) ||
                        (ue.EventType == "ConfirmedAppointment" && context.ConfirmedAppointments.Any(ca => ca.Id == ue.ReferenceId && ca.Date.Date == tomorrow)) ||
                        (ue.EventType == "WasteSchedule" && context.WasteSchedules.Any(ws => ws.Id == ue.ReferenceId && ws.Date.Date == tomorrow)))
                    .ToList()
                    .Select(ue =>
                    {
                        string description = ue.EventType switch
                        {
                            "GeneralNote" => context.GeneralNotes.FirstOrDefault(gn => gn.Id == ue.ReferenceId)?.Note ?? "Brak opisu",
                            "ConfirmedAppointment" => context.ConfirmedAppointments.FirstOrDefault(ca => ca.Id == ue.ReferenceId)?.Note ?? "Brak opisu",
                            "WasteSchedule" => context.WasteSchedules.FirstOrDefault(ws => ws.Id == ue.ReferenceId)?.WasteType ?? "Brak opisu",
                            _ => "Nieznany typ"
                        };

                        string eventType = ue.EventType switch
                        {
                            "GeneralNote" => "Notatka",
                            "ConfirmedAppointment" => "Zaplanowana aktywność",
                            "WasteSchedule" => "Wywóz śmieci",
                            _ => "Nieznany typ"
                        };

                        DateTime date = ue.EventType switch
                        {
                            "GeneralNote" => context.GeneralNotes.FirstOrDefault(gn => gn.Id == ue.ReferenceId)?.Date ?? DateTime.MinValue,
                            "ConfirmedAppointment" => context.ConfirmedAppointments.FirstOrDefault(ca => ca.Id == ue.ReferenceId)?.Date ?? DateTime.MinValue,
                            "WasteSchedule" => context.WasteSchedules.FirstOrDefault(ws => ws.Id == ue.ReferenceId)?.Date ?? DateTime.MinValue,
                            _ => DateTime.MinValue
                        };

                        return new
                        {
                            EventType = eventType,
                            Description = description,
                            Date = date
                        };
                    });

                UpcomingEventsList.ItemsSource = events;
            }
        }
        private void LoadWarrantyExpirations()
        {
            using (var context = new ApplicationDbContext())
            {
                var today = DateTime.Today;
                var upcomingWarrantyEnd = today.AddMonths(2); // Dodaj 6 miesięcy do dzisiejszej daty

                // Pobierz wszystkie urządzenia z bazy danych
                var devices = context.HomeDevices.ToList();

                // Filtrowanie danych w pamięci
                var warrantyData = devices
                    .Where(device =>
                        device.WarrantyEndDateTime >= today && // Gwarancja kończy się po dzisiejszej dacie
                        device.WarrantyEndDateTime <= upcomingWarrantyEnd) // Gwarancja kończy się w ciągu 6 miesięcy
                    .Select(device => new
                    {
                        Name = device.Name,
                        Model = device.Model,
                        WarrantyEndDate = device.WarrantyEndDateTime
                    })
                    .ToList();

                // Przypisz dane do widoku
                WarrantyExpirationsList.ItemsSource = warrantyData;
            }
        }
        private string _previousMonth;
        public string PreviousMonth
        {
            get => _previousMonth;
            set
            {
                _previousMonth = value;
                OnPropertyChanged(nameof(PreviousMonth));
            }
        }
        private void OnMonthSelectorChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FirstMonthSelector.SelectedItem is string selectedMonth)
            {
                CalculateKPI(selectedMonth);

                // Aktualizacja PreviousMonth na podstawie selectedMonth
                var selectedDateParts = selectedMonth.Split('/');
                int selectedMonthNumber = int.Parse(selectedDateParts[0]);
                int selectedYear = int.Parse(selectedDateParts[1]);

                var previousMonthDate = new DateTime(selectedYear, selectedMonthNumber, 1).AddMonths(-1);
                PreviousMonth = $"{previousMonthDate.Month:D2}/{previousMonthDate.Year}";

                // Aktualizacja tekstu "vs PreviousMonth"
                OnPropertyChanged(nameof(PreviousMonth));
            }
        }

        private void CurrencyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CurrencyComboBox.SelectedItem is ComboBoxItem item)
            {
                // 3) Zapisz do Session i odśwież wykres + KPI
                Session.SelectedCurrency = item.Content.ToString(); // np. "USD" / "EUR" / "PLN"

                // Odśwież wykres
                GenerateExpensesReport();

                // Odśwież KPI (jeśli wybrano jakiś miesiąc)
                if (FirstMonthSelector.SelectedItem is string selectedMonth)
                    CalculateKPI(selectedMonth);
            }
        }


        private void CalculateKPI(string selectedMonth)
        {
            using (var context = new ApplicationDbContext())
            {
                var userId = Session.CurrentUser?.Id ?? 0;

                // Pobierz wszystkie dane tego użytkownika
                var allData = context.Budgets
                    .Where(b => b.UserId == userId)
                    .ToList();

                // Rozbij "MM/yyyy"
                var selectedDateParts = selectedMonth.Split('/');
                int selectedMonthNumber = int.Parse(selectedDateParts[0]);
                int selectedYear = int.Parse(selectedDateParts[1]);

                // Poprzedni miesiąc
                var previousMonthDate = new DateTime(selectedYear, selectedMonthNumber, 1).AddMonths(-1);
                int previousMonthNumber = previousMonthDate.Month;
                int previousYear = previousMonthDate.Year;

                // 1) Wyfiltruj transakcje z wybranego miesiąca i poprzedniego
                var selectedData = allData
                    .Where(b => b.Date.Year == selectedYear && b.Date.Month == selectedMonthNumber)
                    .ToList();

                var previousData = allData
                    .Where(b => b.Date.Year == previousYear && b.Date.Month == previousMonthNumber)
                    .ToList();

                // 2) Przeliczamy wszystkie transakcje w each z tych list na Session.SelectedCurrency
                //    identycznie jak w GenerateExpensesReport (A -> PLN -> docelowa).
                double ConvertToSelected(Budget b)
                {
                    // A) Oryginalna waluta -> PLN
                    decimal ratePln = GetRateToPLN(b.Currency, b.Date);
                    double amountPln = (double)(b.Amount * ratePln);

                    // B) PLN -> docelowa
                    if (Session.SelectedCurrency == "PLN") return amountPln;

                    decimal targetRate = GetRateToPLN(Session.SelectedCurrency, b.Date);
                    return (targetRate == 0) ? 0 : amountPln / (double)targetRate;
                }

                double selectedIncome = selectedData
                    .Where(b => b.Category == "Przychody")
                    .Sum(b => ConvertToSelected(b));

                double selectedExpense = selectedData
                    .Where(b => b.Category == "Koszty")
                    .Sum(b => ConvertToSelected(b));

                double previousIncome = previousData
                    .Where(b => b.Category == "Przychody")
                    .Sum(b => ConvertToSelected(b));

                double previousExpense = previousData
                    .Where(b => b.Category == "Koszty")
                    .Sum(b => ConvertToSelected(b));

                // Bilans
                double selectedBalance = selectedIncome - selectedExpense;
                double previousBalance = previousIncome - previousExpense;

                // Zmiany
                double incomeChange = selectedIncome - previousIncome;
                double expenseChange = selectedExpense - previousExpense;
                double balanceChange = selectedBalance - previousBalance;

                double incomePercentChange = (previousIncome == 0) ? 0 : (incomeChange / previousIncome) * 100;
                double expensePercentChange = (previousExpense == 0) ? 0 : (expenseChange / previousExpense) * 100;
                double balancePercentChange = (previousBalance == 0) ? 0 : (balanceChange / Math.Abs(previousBalance)) * 100;

                // 3) Wyświetlanie tekstu w docelowej walucie
                //    np. "200,00 USD (+100,0, 50,0%)"
                IncomeChangeText.Text = $"{selectedIncome:N2} {Session.SelectedCurrency} ({incomeChange:+0.0;-0.0;0.0}, {incomePercentChange:+0.0;-0.0;0.0}%)";
                ExpenseChangeText.Text = $"{selectedExpense:N2} {Session.SelectedCurrency} ({expenseChange:+0.0;-0.0;0.0}, {expensePercentChange:+0.0;-0.0;0.0}%)";
                BalanceChangeText.Text = $"{selectedBalance:N2} {Session.SelectedCurrency} ({balanceChange:+0.0;-0.0;0.0}, {balancePercentChange:+0.0;-0.0;0.0}%)";

                // 4) Kolory
                IncomeChangeText.Foreground = Brushes.Black;
                ExpenseChangeText.Foreground = Brushes.Black;
                BalanceChangeText.Foreground = Brushes.Black;

                if (incomeChange != 0)
                    IncomeChangeText.Foreground = incomeChange > 0 ? Brushes.Green : Brushes.Red;

                if (expenseChange != 0)
                    ExpenseChangeText.Foreground = expenseChange > 0 ? Brushes.Red : Brushes.Green;

                if (balanceChange != 0)
                    BalanceChangeText.Foreground = balanceChange > 0 ? Brushes.Green : Brushes.Red;
            }
        }




    }
}
