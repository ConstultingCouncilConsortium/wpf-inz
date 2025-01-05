using System;
using System.Windows;

namespace wpf_inz
{
    public partial class AppointmentDetailsWindow : Window
    {
        public AppointmentDetailsWindow(ConfirmedAppointment appointment)
        {
            InitializeComponent();

            // Ustaw widoczność sekcji kontaktu na podstawie typu wydarzenia
            var contactVisible = appointment.EventType == "Przegląd" || appointment.EventType == "Spotkanie";
            var dataContext = new
            {
                appointment.EventType,
                appointment.Date,
                appointment.Note,
                ContactName = contactVisible ? appointment.ContactName : string.Empty,
                ContactPhone = contactVisible ? appointment.ContactPhone : string.Empty,
                ContactSectionVisibility = contactVisible ? Visibility.Visible : Visibility.Collapsed
            };

            DataContext = dataContext;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
