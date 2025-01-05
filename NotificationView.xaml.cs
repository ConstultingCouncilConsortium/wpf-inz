using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace wpf_inz
{
    public partial class NotificationView : UserControl
    {
        private int _userId;

        public NotificationView(int userId)
        {
            InitializeComponent();
            _userId = userId;
            LoadNotifications();
        }

        private void LoadNotifications()
        {
            using (var context = new ApplicationDbContext())
            {
                var notifications = context.Notifications
                    .Where(n => n.UserId == _userId && !n.IsRead)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToList();

                // Generowanie poprawnych danych
                var detailedNotifications = notifications
                    .Select(n => new
                    {
                        n.Id,
                        Message = n.Message, // Ustawiamy tylko podstawowy tekst
                        DetailedInfo = GetDetailedInfo(n.UnifiedEventId), // Szczegóły generowane osobno
                        n.IsRead
                    })
                    .ToList();

                NotificationsList.ItemsSource = detailedNotifications;
            }
        }






        private string GetDetailedInfo(int unifiedEventId)
        {
            using (var context = new ApplicationDbContext())
            {
                var unifiedEvent = context.UnifiedEvents.FirstOrDefault(ue => ue.Id == unifiedEventId);
                if (unifiedEvent == null) return "Szczegóły niedostępne.";

                return unifiedEvent.EventType switch
                {
                    "GeneralNote" => GetGeneralNoteDetails(context, unifiedEvent.ReferenceId),
                    "ConfirmedAppointment" => GetConfirmedAppointmentDetails(context, unifiedEvent.ReferenceId),
                    "WasteSchedule" => GetWasteScheduleDetails(context, unifiedEvent.ReferenceId),
                    _ => "Nieznany typ wydarzenia."
                };
            }
        }

        private string GetGeneralNoteDetails(ApplicationDbContext context, int referenceId)
        {
            var note = context.GeneralNotes.FirstOrDefault(gn => gn.Id == referenceId);
            return note != null
                ? $"Notatka: {note.Note}\nData: {note.Date:dd.MM.yyyy}"
                : "Szczegóły niedostępne.";
        }

        private string GetConfirmedAppointmentDetails(ApplicationDbContext context, int referenceId)
        {
            var appointment = context.ConfirmedAppointments.FirstOrDefault(ca => ca.Id == referenceId);
            return appointment != null
                ? $"Typ: {appointment.EventType}\nSzczegóły: {appointment.Note ?? "Brak notatki"}\nData: {appointment.Date:dd.MM.yyyy}"
                : "Szczegóły niedostępne.";
        }

        private string GetWasteScheduleDetails(ApplicationDbContext context, int referenceId)
        {
            var wasteSchedule = context.WasteSchedules.FirstOrDefault(ws => ws.Id == referenceId);
            return wasteSchedule != null
                ? $"Typ śmieci: {wasteSchedule.WasteType}\nData: {wasteSchedule.Date:dd.MM.yyyy}"
                : "Szczegóły niedostępne.";
        }




        private void MarkAsRead_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var notificationId = (int)button.Tag;

            using (var context = new ApplicationDbContext())
            {
                var notification = context.Notifications.FirstOrDefault(n => n.Id == notificationId);
                if (notification != null)
                {
                    notification.IsRead = true;
                    context.SaveChanges();
                }
            }

            // Odśwież listę powiadomień w widoku powiadomień
            LoadNotifications();

            // Odśwież licznik w HomeView
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null && mainWindow.MainContent.Content is HomeView homeView)
            {
                homeView.LoadNotificationCount();
            }
        }


    }
}
