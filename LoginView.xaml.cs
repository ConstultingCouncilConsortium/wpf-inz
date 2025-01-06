using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace wpf_inz
{
    public partial class LoginView : UserControl
    {
        public event Action OnLoginSuccess;
        public event Action OnRegister;
        public event Action<string> OnVerificationRequired; 


        public LoginView()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string email = UsernameTextBox.Text;
            string password = PasswordTextBox.Password;

            var viewModel = new LoginViewModel();
            int loginResult = viewModel.VerifyUser(email, password);

            if (loginResult == 1)
            {
                using (var context = new ApplicationDbContext())
                {
                    Session.CurrentUser = context.Users.SingleOrDefault(u => u.Email == email);
                }
                viewModel.GenerateNotificationsForUser(Session.CurrentUser.Id);
                OnLoginSuccess?.Invoke(); // Użytkownik zweryfikowany - przekierowanie do głównego widoku
            }
            else if (loginResult == 2)
            {
                ((MainWindow)Application.Current.MainWindow).ShowNotification("Musisz zweryfikować swój adres e-mail, zanim się zalogujesz.", "warning");

                OnVerificationRequired?.Invoke(email); // Przekierowanie do widoku weryfikacji
            }
            else
            {
                ((MainWindow)Application.Current.MainWindow).ShowNotification("Niepoprawna nazwa użytkownika lub hasło.", "error");

            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            OnRegister?.Invoke(); // Wywołanie eventu do rejestracji
        }
    }
}
