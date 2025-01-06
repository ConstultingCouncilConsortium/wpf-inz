using System;
using System.Windows;
using System.Windows.Controls;

namespace wpf_inz
{
    public partial class ConfirmedAppointmentsView : UserControl
    {
        public ConfirmedAppointmentsView()
        {
            InitializeComponent();

            // Wypełnij ComboBox z godzinami i minutami
            PopulateTimeSelectors();
        }

        private void PopulateTimeSelectors()
        {
            // Dodaj godziny (0-23)
            for (int hour = 0; hour < 24; hour++)
                HoursComboBox.Items.Add(hour.ToString("D2"));

            // Dodaj minuty 
            MinutesComboBox.Items.Add("00");
            MinutesComboBox.Items.Add("15");
            MinutesComboBox.Items.Add("30");
            MinutesComboBox.Items.Add("45");
        }

        private void EventTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedEventType = (EventTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            // Ukryj/wyświetl pola w zależności od typu wydarzenia
            if (selectedEventType == "Przegląd" || selectedEventType == "Spotkanie")
            {
                ContactNamePanel.Visibility = Visibility.Visible;
                ContactPhonePanel.Visibility = Visibility.Visible;
            }
            else
            {
                ContactNamePanel.Visibility = Visibility.Collapsed;
                ContactPhonePanel.Visibility = Visibility.Collapsed;
            }
        }


        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var eventType = (EventTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            var appointmentDate = AppointmentDatePicker.SelectedDate;
            var selectedHour = HoursComboBox.SelectedItem?.ToString();
            var selectedMinute = MinutesComboBox.SelectedItem?.ToString();
            var note = AppointmentNoteTextBox.Text;
            var contactName = ContactNameTextBox.Text;
            var contactPhone = ContactPhoneTextBox.Text;

            if (string.IsNullOrWhiteSpace(eventType))
            {
                ((MainWindow)Application.Current.MainWindow).ShowNotification("Wybierz typ wydarzenia.", "error");
                return;
            }

            if (!appointmentDate.HasValue)
            {
                ((MainWindow)Application.Current.MainWindow).ShowNotification("Wybierz datę wydarzenia.", "error");
                return;
            }

            if (string.IsNullOrWhiteSpace(selectedHour) || string.IsNullOrWhiteSpace(selectedMinute))
            {
                ((MainWindow)Application.Current.MainWindow).ShowNotification("Wybierz godzinę i minutę wydarzenia.", "error");
                return;
            }

            var appointmentDateTime = appointmentDate.Value.AddHours(int.Parse(selectedHour)).AddMinutes(int.Parse(selectedMinute));

            // Tworzenie ConfirmedAppointment
            var appointment = new ConfirmedAppointment
            {
                EventType = eventType,
                Date = appointmentDateTime,
                Note = note,
                ContactName = eventType == "Przegląd" || eventType == "Spotkanie" ? contactName : null,
                ContactPhone = eventType == "Przegląd" || eventType == "Spotkanie" ? contactPhone : null,
                UserId = Session.CurrentUser.Id
            };

            using (var context = new ApplicationDbContext())
            {
                // Dodaj ConfirmedAppointment
                context.ConfirmedAppointments.Add(appointment);
                context.SaveChanges();

                // Dodaj wpis do UnifiedEvents
                var unifiedEvent = new UnifiedEvent
                {
                    ReferenceId = appointment.Id, // Powiązanie z ConfirmedAppointment
                    EventType = "ConfirmedAppointment"
                };
                context.UnifiedEvents.Add(unifiedEvent);

                // Zapisz zmiany
                context.SaveChanges();
            }

     ((MainWindow)Application.Current.MainWindow).ShowNotification($"{eventType} zostało dodane pomyślnie.", "success");
            ReturnToHomeView();
        }


        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            ReturnToHomeView();
        }

        private void ReturnToHomeView()
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            var homeView = new HomeView();
            homeView.MainContent.Content = new ScheduleCalendarView();
            mainWindow.MainContent.Content = homeView;
        }
    }
}
