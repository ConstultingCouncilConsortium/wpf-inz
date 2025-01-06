using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace wpf_inz
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _notificationTimer;
        public MainWindow()
        {
            InitializeComponent();
            string projectPath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName);
            string iconPath = Path.Combine(projectPath, "Resources", "AppIcon.ico");
            _notificationTimer = new DispatcherTimer();
            _notificationTimer.Interval = TimeSpan.FromSeconds(3); 
            _notificationTimer.Tick += (s, e) =>
            {
                NotificationPanel.Visibility = Visibility.Collapsed;
                _notificationTimer.Stop();
            };
            if (File.Exists(iconPath))
            {
                this.Icon = new BitmapImage(new Uri(iconPath));
            }
            else
            {
                MessageBox.Show("Ikona nie została znaleziona.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            ShowLoginView();
        }
        public void ShowNotification(string message, string type = "success")
        {
            NotificationText.Text = message;

            switch (type.ToLower())
            {
                case "error":
                    NotificationPanel.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3545")); 
                    break;
                case "warning":
                    NotificationPanel.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFC107")); 
                    break;
                default:
                    NotificationPanel.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#28A745")); 
                    break;
            }

            NotificationPanel.Visibility = Visibility.Visible;
            _notificationTimer.Start();
        }

        public void ShowLoginView()
        {
            var loginView = new LoginView();
            loginView.OnLoginSuccess += () =>
            {
                ShowNotification("Logowanie zakończone sukcesem!");
                ShowHomeView(); // Przekierowanie do głównego widoku po zalogowaniu
            };
            loginView.OnRegister += ShowRegisterView;
            loginView.OnVerificationRequired += ShowVerificationView; // Przekierowanie do weryfikacji
            MainContent.Content = loginView;
        }

        private void ShowRegisterView()
        {
            var registerView = new RegisterView();
            registerView.OnBackToLogin += ShowLoginView;
            registerView.OnVerificationRequired += ShowVerificationView; // Obsługa zdarzenia weryfikacji
            MainContent.Content = registerView;
        }


        private void ShowVerificationView(string email)
        {
            var verificationView = new VerificationView();
            verificationView.OnVerificationSuccess += ShowLoginView;
            verificationView.SetEmailForVerification(email); // Ustawiamy e-mail użytkownika w widoku weryfikacji
            MainContent.Content = verificationView;
        }


        public void ShowMainView()
        {
            var homeView = new HomeView();
            MainContent.Content = homeView;
        }
        public void ShowDeviceListView()
        {
            var deviceListView = new DeviceListView();
            MainContent.Content = deviceListView;
        }


        public void ShowAddDeviceView()
        {
            var addDeviceView = new AddDeviceView();
            MainContent.Content = addDeviceView;
        }
        public void ShowHomeView()
        {
            var homeView = new HomeView();
            MainContent.Content = homeView; // Wczytaj `HomeView` jako główny widok
        }

    }


}
