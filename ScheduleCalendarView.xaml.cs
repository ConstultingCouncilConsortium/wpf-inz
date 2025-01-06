using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.Linq;
using MaterialDesignThemes.Wpf;

namespace wpf_inz
{
    public class CalendarEvent
    {
        public DateTime Date { get; set; }
        public string Description { get; set; }
    }

    public partial class ScheduleCalendarView : UserControl
    {
        public ObservableCollection<CalendarEvent> Events { get; set; } = new ObservableCollection<CalendarEvent>();
    }
    public partial class ScheduleCalendarView : UserControl, INotifyPropertyChanged
    {
        private DateTime _currentMonth;
        public DateTime CurrentMonth
        {
            get => _currentMonth;
            set
            {
                if (_currentMonth != value)
                {
                    _currentMonth = value;
                    OnPropertyChanged(); // Notify property change for CurrentMonth
                    OnPropertyChanged(nameof(CurrentMonthDisplay)); // Notify property change for CurrentMonthDisplay
                }
            }
        }

        public string CurrentMonthDisplay => CurrentMonth.ToString("MMMM yyyy");

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ScheduleCalendarView()
        {
            InitializeComponent();
            DataContext = this; // Set DataContext
            CurrentMonth = DateTime.Now;
            InitializeYearComboBox();
            PopulateCalendar();
        }


        private void PopulateCalendar()
        {
            // Wyczyść zawartość siatki kalendarza
            CalendarGrid.Children.Clear();

            // Dodaj nagłówki dni tygodnia
            var dayNames = new[] { "Pon", "Wto", "Śro", "Czw", "Pią", "Sob", "Nie" };
            foreach (var day in dayNames)
            {
                CalendarGrid.Children.Add(new TextBlock
                {
                    Text = day,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                });
            }

            using (var context = new ApplicationDbContext())
            {
                var unifiedEvents = context.UnifiedEvents.ToList();

                // Pobierz wydarzenia z różnych tabel
                var wasteSchedules = (from ue in unifiedEvents
                                      join ws in context.WasteSchedules on ue.ReferenceId equals ws.Id
                                      where ue.EventType == "WasteSchedule"
                                      select new
                                      {
                                          ue.Id,
                                          ws.Date,
                                          Description = $"{ws.WasteType} - Wywóz śmieci",
                                          Color = ws.WasteType switch
                                          {
                                              "Szkło" => "LightGreen",
                                              "Plastik" => "Yellow",
                                              "Papier" => "LightBlue",
                                              "Mieszane" => "LightGray",
                                              _ => "White"
                                          },
                                          EventType = ue.EventType // Dodaj EventType
                                      }).ToList();

                var generalNotes = (from ue in unifiedEvents
                                    join gn in context.GeneralNotes on ue.ReferenceId equals gn.Id
                                    where ue.EventType == "GeneralNote"
                                    select new
                                    {
                                        ue.Id,
                                        gn.Date,
                                        Description = gn.Note,
                                        Color = "Orange",
                                        EventType = ue.EventType // Dodaj EventType
                                    }).ToList();

                var confirmedAppointments = (from ue in unifiedEvents
                                             join ca in context.ConfirmedAppointments on ue.ReferenceId equals ca.Id
                                             where ue.EventType == "ConfirmedAppointment"
                                             select new
                                             {
                                                 ue.Id,
                                                 ca.Date,
                                                 Description = $"{ca.EventType} - {ca.Note ?? "Brak notatki"}",
                                                 Color = "LightPink",
                                                 EventType = ue.EventType // Dodaj EventType
                                             }).ToList();

                // Połącz wszystkie wydarzenia
                var allEvents = wasteSchedules
                    .Concat(generalNotes)
                    .Concat(confirmedAppointments)
                    .ToList();


                var daysInMonth = DateTime.DaysInMonth(CurrentMonth.Year, CurrentMonth.Month);
                var firstDayOfMonth = new DateTime(CurrentMonth.Year, CurrentMonth.Month, 1);
                var dayOfWeekOffset = ((int)firstDayOfMonth.DayOfWeek - 1 + 7) % 7;

                // Puste komórki przed początkiem miesiąca
                for (int i = 0; i < dayOfWeekOffset; i++)
                {
                    CalendarGrid.Children.Add(new Border
                    {
                        Background = Brushes.LightGray,
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(0.5)
                    });
                }

                // Dodaj dni miesiąca z wydarzeniami
                for (int day = 1; day <= daysInMonth; day++)
                {
                    var date = new DateTime(CurrentMonth.Year, CurrentMonth.Month, day);
                    var dailyEvents = allEvents.Where(e => e.Date.Date == date.Date).ToList();

                    // Tworzenie komórki dnia
                    var border = new Border
                    {
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(1),
                        Margin = new Thickness(1),
                        Background = Brushes.White
                    };

                    // Tworzenie ScrollViewer dla każdej komórki
                    var scrollViewer = new ScrollViewer
                    {
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                        MaxHeight = 100 // Ogranicz wysokość, aby lepiej zarządzać przestrzenią
                    };

                    // Zawartość komórki
                    var stackPanel = new StackPanel();

                    // Numer dnia
                    stackPanel.Children.Add(new TextBlock
                    {
                        Text = day.ToString(),
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 5)
                    });

                    // Dodaj wydarzenia dla danego dnia
                    foreach (var ev in dailyEvents)
                    {
                        var eventGrid = new Grid
                        {
                            Margin = new Thickness(0, 2, 0, 0)
                        };

                        eventGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                        // Dodaj kolumnę na przycisk szczegółów tylko dla EventType == "ConfirmedAppointment"
                        if (ev.EventType == "ConfirmedAppointment")
                        {
                            eventGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                        }
                        eventGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                        var eventText = new TextBlock
                        {
                            Text = ev.Description,
                            Padding = new Thickness(5),
                            TextWrapping = TextWrapping.Wrap,
                            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(ev.Color))
                        };
                        Grid.SetColumn(eventText, 0);
                        eventGrid.Children.Add(eventText);

                        // Dodaj przycisk szczegółów
                        if (ev.EventType == "ConfirmedAppointment")
                        {
                            var detailsButton = new Button
                            {
                                Width = 50,
                                Height = 35,
                                Background = Brushes.Transparent,
                                BorderBrush = Brushes.Transparent,
                                Tag = ev.Id
                            };
                            detailsButton.Content = new MaterialDesignThemes.Wpf.PackIcon
                            {
                                Kind = MaterialDesignThemes.Wpf.PackIconKind.Information,
                                Foreground = Brushes.Blue
                            };
                            detailsButton.Click += DetailsButton_Click;
                            Grid.SetColumn(detailsButton, 1);
                            eventGrid.Children.Add(detailsButton);
                        }

                        // Dodaj przycisk usuwania
                        var deleteButton = new Button
                        {
                            Width = 50,
                            Height = 35,
                            Background = Brushes.Transparent,
                            BorderBrush = Brushes.Transparent,
                            Tag = new UnifiedEvent
                            {
                                Id = ev.Id,
                                EventType = ev.EventType, // Ustawienie odpowiedniego typu wydarzenia
                                ReferenceId = ev.Id // Ustawienie poprawnego ReferenceId
                            }
                        };
                        deleteButton.Content = new MaterialDesignThemes.Wpf.PackIcon
                        {
                            Kind = MaterialDesignThemes.Wpf.PackIconKind.Delete,
                            Foreground = Brushes.Red
                        };

                        int deleteButtonColumnIndex = ev.EventType == "ConfirmedAppointment" ? 2 : 1;
                        Grid.SetColumn(deleteButton, deleteButtonColumnIndex);
                        eventGrid.Children.Add(deleteButton);

                        // Obsługa zdarzenia kliknięcia dla przycisku usuwania
                        deleteButton.Click += DeleteEvent_Click;

                        stackPanel.Children.Add(eventGrid);
                    }

                    scrollViewer.Content = stackPanel;

                   
                    border.Child = scrollViewer;

                    
                    CalendarGrid.Children.Add(border);
                }
            }
        }




        private void DetailsButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int eventId)
            {
                using (var context = new ApplicationDbContext())
                {
                    var appointment = (from ue in context.UnifiedEvents
                                       join ca in context.ConfirmedAppointments on ue.ReferenceId equals ca.Id
                                       where ue.Id == eventId && ue.EventType == "ConfirmedAppointment"
                                       select ca).FirstOrDefault();

                    if (appointment != null)
                    {
                        // Otwórz nowe okno z szczegółami
                        var detailsWindow = new AppointmentDetailsWindow(appointment);
                        detailsWindow.ShowDialog();
                    }
                }
            }
        }




        private void DeleteEvent_Click(object sender, RoutedEventArgs e)
        {
            
            var button = sender as Button;
            var unifiedEvent = button?.Tag as UnifiedEvent;

            if (unifiedEvent != null)
            {
                // Confirm deletion
                var result = MessageBox.Show(
                    $"Czy na pewno chcesz usunąć wybrany wpis?",
                    "Potwierdzenie usunięcia",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    using (var context = new ApplicationDbContext())
                    {
                        // Delete from the specific table based on EventType
                        if (unifiedEvent.EventType == "WasteSchedule")
                        {
                            var wasteSchedule = context.WasteSchedules.FirstOrDefault(ws => ws.Id == unifiedEvent.ReferenceId);
                            if (wasteSchedule != null)
                                context.WasteSchedules.Remove(wasteSchedule);
                        }
                        else if (unifiedEvent.EventType == "GeneralNote")
                        {
                            var generalNote = context.GeneralNotes.FirstOrDefault(gn => gn.Id == unifiedEvent.ReferenceId);
                            if (generalNote != null)
                                context.GeneralNotes.Remove(generalNote);
                        }

                        // Delete from UnifiedEvents
                        var eventToRemove = context.UnifiedEvents.FirstOrDefault(ue => ue.Id == unifiedEvent.Id);
                        if (eventToRemove != null)
                            context.UnifiedEvents.Remove(eventToRemove);

                        // Save changes
                        context.SaveChanges();
                    }

                    // Refresh the calendar
                    PopulateCalendar();
                }
            }
        }


        private List<(string Text, string Color)> GetEventsForDate(DateTime date)
        {
            using (var context = new ApplicationDbContext())
            {
                var wasteSchedules = context.WasteSchedules
                    .Where(ws => ws.Date.Date == date.Date && ws.UserId == Session.CurrentUser.Id)
                    .ToList()
                    .Select(ws =>
                    {
                        string color = ws.WasteType switch
                        {
                            "Szkło" => "LightGreen",
                            "Plastik" => "Yellow",
                            "Papier" => "LightBlue",
                            "Mieszane" => "LightGray",
                            _ => "White"
                        };

                        return (Text: $"{ws.WasteType} - Wywóz śmieci", Color: color);
                    })
                    .ToList();

                var notes = context.GeneralNotes
                    .Where(note => note.Date.Date == date.Date && note.UserId == Session.CurrentUser.Id)
                    .ToList()
                    .Select(note => (Text: note.Note, Color: "Orange"))
                    .ToList();

                var confirmedAppointments = context.ConfirmedAppointments
                    .Where(ca => ca.Date.Date == date.Date && ca.UserId == Session.CurrentUser.Id)
                    .ToList()
                    .Select(ca => (Text: $"{ca.EventType} - {ca.Note}", Color: "LightPink"))
                    .ToList();

                return wasteSchedules
                    .Concat(notes)
                    .Concat(confirmedAppointments)
                    .ToList();
            }
        }



        private void AddNote_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            var homeView = new HomeView();
            homeView.MainContent.Content = new GeneralNotesView(); 
            mainWindow.MainContent.Content = homeView; 
            
            PopulateCalendar();
        }
        private void AddConfirmedAppointment_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            var homeView = new HomeView();
            homeView.MainContent.Content = new ConfirmedAppointmentsView(); 
            mainWindow.MainContent.Content = homeView; 

            
            PopulateCalendar();
        }



        private void ManageWasteSchedule_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            var homeView = new HomeView();
            homeView.MainContent.Content = new WasteScheduleView(); 
            mainWindow.MainContent.Content = homeView;

            
            PopulateCalendar();
        }

        private void PreviousMonth_Click(object sender, RoutedEventArgs e)
        {
            CurrentMonth = CurrentMonth.AddMonths(-1);
            YearComboBox.SelectedItem = CurrentMonth.Year;
            PopulateCalendar();
        }

        private void NextMonth_Click(object sender, RoutedEventArgs e)
        {
            CurrentMonth = CurrentMonth.AddMonths(1);
            YearComboBox.SelectedItem = CurrentMonth.Year;
            PopulateCalendar();
        }

        private void YearComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (YearComboBox.SelectedItem is int selectedYear)
            {
                CurrentMonth = new DateTime(selectedYear, CurrentMonth.Month, 1);
                PopulateCalendar();
            }
        }

        private void InitializeYearComboBox()
        {
            YearComboBox.Items.Clear(); // Clear previous items

            int currentYear = DateTime.Now.Year;
            for (int year = currentYear - 1; year <= currentYear + 1; year++)
            {
                YearComboBox.Items.Add(year);
            }

            YearComboBox.SelectedItem = CurrentMonth.Year; // Set default selection
        }
    }
}
