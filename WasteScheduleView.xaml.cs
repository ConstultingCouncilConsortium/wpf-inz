using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace wpf_inz
{
    public partial class WasteScheduleView : UserControl
    {
        public WasteScheduleView()
        {
            InitializeComponent();
        }

        private void AddWasteSchedule_Click(object sender, RoutedEventArgs e)
        {
            if (WasteTypeComboBox.SelectedItem == null || FirstWasteDatePicker.SelectedDate == null)
            {
                ((MainWindow)Application.Current.MainWindow).ShowNotification("Wybierz typ odpadu i datę pierwszego wywozu.", "error");
                return;
            }

            using (var context = new ApplicationDbContext())
            {
                // Pobieranie danych z formularza
                var wasteType = WasteTypeComboBox.Text;
                var firstDate = FirstWasteDatePicker.SelectedDate.Value;
                int? frequencyInDays = null;

                // Parsowanie częstotliwości (jeśli podano)
                if (int.TryParse(FrequencyTextBox.Text, out int frequency))
                {
                    frequencyInDays = frequency;
                }

                // Tworzenie pierwszego wpisu harmonogramu
                var wasteSchedule = new WasteSchedule
                {
                    WasteType = wasteType,
                    Date = firstDate,
                    FrequencyInDays = frequencyInDays,
                    UserId = Session.CurrentUser.Id // Powiązanie z użytkownikiem
                };

                context.WasteSchedules.Add(wasteSchedule);
                context.SaveChanges(); // Save to get the ID of the newly created WasteSchedule

                // Dodanie do UnifiedEvents
                var unifiedEvent = new UnifiedEvent
                {
                    ReferenceId = wasteSchedule.Id,
                    EventType = "WasteSchedule"
                };
                context.UnifiedEvents.Add(unifiedEvent);

                // Generowanie kolejnych dat wywozu w ramach tego samego miesiąca
                if (frequencyInDays.HasValue)
                {
                    DateTime nextDate = firstDate.AddDays(frequencyInDays.Value);
                    DateTime endOfMonth = new DateTime(firstDate.Year, firstDate.Month, DateTime.DaysInMonth(firstDate.Year, firstDate.Month));

                    while (nextDate <= endOfMonth)
                    {
                        var recurringSchedule = new WasteSchedule
                        {
                            WasteType = wasteType,
                            Date = nextDate,
                            FrequencyInDays = frequencyInDays,
                            UserId = Session.CurrentUser.Id
                        };

                        context.WasteSchedules.Add(recurringSchedule);
                        context.SaveChanges(); // Save to get the ID of the recurring WasteSchedule

                        // Dodanie do UnifiedEvents
                        var recurringUnifiedEvent = new UnifiedEvent
                        {
                            ReferenceId = recurringSchedule.Id,
                            EventType = "WasteSchedule"
                        };
                        context.UnifiedEvents.Add(recurringUnifiedEvent);

                        nextDate = nextDate.AddDays(frequencyInDays.Value);
                    }
                }

                context.SaveChanges(); // Save all changes
            }

            var mainWindow = (MainWindow)Application.Current.MainWindow;
            var homeView = new HomeView();
            homeView.MainContent.Content = new ScheduleCalendarView(); // Osadzamy DeviceListView w HomeView
            mainWindow.MainContent.Content = homeView; // Ustawiamy HomeView jako główny widok
                                                       // Po zamknięciu okna odśwież kalendarz, aby uwzględnić nowe notatki
            ((MainWindow)Application.Current.MainWindow).ShowNotification("Harmonogram wywozu śmieci został dodany!", "success");
        }

        // Obsługa kliknięcia przycisku "Powrót"
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            var homeView = new HomeView();
            homeView.MainContent.Content = new ScheduleCalendarView(); // Osadzamy DeviceListView w HomeView
            mainWindow.MainContent.Content = homeView; // Ustawiamy HomeView jako główny widok
        }


    }
}
