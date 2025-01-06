using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace wpf_inz
{
    public partial class HomeView : UserControl, INotifyPropertyChanged
    {
        public string UserInitials
        {
            get
            {
                if (Session.CurrentUser != null && !string.IsNullOrEmpty(Session.CurrentUser.Username))
                {
                    var initials = Session.CurrentUser.Username.Substring(0, Math.Min(2, Session.CurrentUser.Username.Length)).ToUpper();
                    return initials;
                }

                return "NN"; // Placeholder
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public HomeView()
        {
            InitializeComponent();
            AdjustSidebarButtonSize(); // Adjust size initially
            LoadNotificationCount();
            MainContent.Content = new StartView();
            DataContext = this;
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AdjustSidebarButtonSize();
        }

        private void AdjustSidebarButtonSize()
        {
            double newSize = Math.Max(40, Math.Min(60, this.ActualHeight / 15)); 
            SidebarStackPanel.Tag = newSize; 
        }
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // Wyświetlenie komunikatu potwierdzenia
            var result = MessageBox.Show(
                "Czy na pewno chcesz się wylogować?",
                "Potwierdzenie wylogowania",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            // Jeśli użytkownik potwierdzi, wykonaj wylogowanie
            if (result == MessageBoxResult.Yes)
            {
                // Display custom notification for logout
                ((MainWindow)Application.Current.MainWindow).ShowNotification("Zostałeś wylogowany!");

                // Logic to return to login screen
                ((MainWindow)Application.Current.MainWindow).ShowLoginView();
            }
        }

        private void ShowDeviceListView_Click(object sender, RoutedEventArgs e)
        {
            // Wyświetlenie listy urządzeń
            MainContent.Content = new DeviceListView();
        }
        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            // Przejdź do widoku zarządzania profilem użytkownika
            var userProfileView = new UserProfileView();
            MainContent.Content = userProfileView;
        }

        private void ScheduleCalendarView(object sender, RoutedEventArgs e)
        {
            // Przejdź do widoku zarządzania profilem użytkownika
            
            MainContent.Content = new ScheduleCalendarView();
        }

        private void BackToStartView_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new StartView(); // Powrót do widoku StartView
        }
        private void OpenNotificationView_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            var homeView = new HomeView();
            homeView.MainContent.Content = new NotificationView(Session.CurrentUser.Id);
            mainWindow.MainContent.Content = homeView; // Ustawiamy HomeView jako główny widok
        }
        private int _notificationCount;
        public int NotificationCount
        {
            get => _notificationCount;
            set
            {
                _notificationCount = value;
                NotificationCountVisibility = _notificationCount > 0 ? Visibility.Visible : Visibility.Collapsed;
                OnPropertyChanged(nameof(NotificationCount));
                OnPropertyChanged(nameof(NotificationCountVisibility));
            }
        }

        private Visibility _notificationCountVisibility = Visibility.Collapsed;
        public Visibility NotificationCountVisibility
        {
            get => _notificationCountVisibility;
            set
            {
                _notificationCountVisibility = value;
                OnPropertyChanged(nameof(NotificationCountVisibility));
            }
        }
        public void LoadNotificationCount()
        {
            using (var context = new ApplicationDbContext())
            {
                NotificationCount = context.Notifications
                    .Count(n => n.UserId == Session.CurrentUser.Id && !n.IsRead);
            }
        }
        private void ShowBudgetView_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new BudgetView(); 
        }



    }
}
