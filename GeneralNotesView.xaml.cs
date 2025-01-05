using System;
using System.Windows;
using System.Windows.Controls;

namespace wpf_inz
{
    public partial class GeneralNotesView : UserControl
    {
        public GeneralNotesView()
        {
            InitializeComponent();
        }

        private void AddNoteButton_Click(object sender, RoutedEventArgs e)
        {
            // Retrieve values from input fields
            string noteText = NoteTextBox.Text;
            DateTime? selectedDate = NoteDatePicker.SelectedDate;

            // Validate input data
            if (string.IsNullOrWhiteSpace(noteText))
            {
                ((MainWindow)Application.Current.MainWindow).ShowNotification("Treść notatki nie może być pusta.", "error");
                return;
            }

            if (!selectedDate.HasValue)
            {
                ((MainWindow)Application.Current.MainWindow).ShowNotification("Musisz wybrać datę przypomnienia.", "error");
                return;
            }

            // Add note to database
            using (var context = new ApplicationDbContext())
            {
                // Create the GeneralNote entry
                var newNote = new GeneralNote
                {
                    Note = noteText,
                    Date = selectedDate.Value,
                    UserId = Session.CurrentUser.Id
                };

                context.GeneralNotes.Add(newNote);
                context.SaveChanges(); // Save to get the ID of the new note

                // Add corresponding UnifiedEvent entry
                var unifiedEvent = new UnifiedEvent
                {
                    ReferenceId = newNote.Id,
                    EventType = "GeneralNote"
                };
                context.UnifiedEvents.Add(unifiedEvent);

                context.SaveChanges(); // Save UnifiedEvent
            }

            // Clear fields after saving
            NoteTextBox.Clear();
            NoteDatePicker.SelectedDate = null;

            // Navigate back to the home view and show success notification
            ReturnToHomeView("Notatka została dodana pomyślnie.", "success");
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Navigate back to the home view
            ReturnToHomeView();
        }

        private void ReturnToHomeView(string message = null, string messageType = null)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            var homeView = new HomeView();
            homeView.MainContent.Content = new ScheduleCalendarView(); // Embed ScheduleCalendarView in HomeView
            mainWindow.MainContent.Content = homeView;

            // Show notification if provided
            if (!string.IsNullOrEmpty(message) && !string.IsNullOrEmpty(messageType))
            {
                mainWindow.ShowNotification(message, messageType);
            }
        }
    }
}
