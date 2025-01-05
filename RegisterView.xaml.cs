using System;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace wpf_inz
{
    public partial class RegisterView : UserControl
    {

        public event Action OnBackToLogin;
        public event Action<string> OnVerificationRequired;

        public RegisterView()
        {
            InitializeComponent();
        }

        private void UsernameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(UsernameTextBox.Text))
            {
                if (UsernameTextBox.Text.Length < 3)
                {
                    UsernameError.Text = "Nazwa użytkownika musi mieć co najmniej 3 znaki.";
                    UsernameError.Visibility = Visibility.Visible;
                }
                else
                {
                    UsernameError.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                UsernameError.Visibility = Visibility.Collapsed;
            }
        }

        private void EmailTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                if (!IsValidEmail(EmailTextBox.Text))
                {
                    EmailError.Text = "Niepoprawny format adresu e-mail.";
                    EmailError.Visibility = Visibility.Visible;
                }
                else
                {
                    EmailError.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                EmailError.Visibility = Visibility.Collapsed;
            }
        }

        // Podobnie dodaj kod do obsługi dla innych pól (Password, ConfirmPassword)


        private void PasswordBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // Walidacja hasła tylko, jeśli pole nie jest puste
            if (!string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                if (PasswordBox.Password.Length < 8)
                {
                    PasswordError.Text = "Hasło musi mieć co najmniej 8 znaków.";
                    PasswordError.Visibility = Visibility.Visible;
                }
                else
                {
                    PasswordError.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                // Jeśli pole jest puste, ukryj komunikat o błędzie
                PasswordError.Visibility = Visibility.Collapsed;
            }
        }

        private void ConfirmPasswordBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // Walidacja potwierdzenia hasła tylko, jeśli pole nie jest puste
            if (!string.IsNullOrWhiteSpace(ConfirmPasswordBox.Password))
            {
                if (ConfirmPasswordBox.Password != PasswordBox.Password)
                {
                    ConfirmPasswordError.Text = "Hasła nie są zgodne.";
                    ConfirmPasswordError.Visibility = Visibility.Visible;
                }
                else
                {
                    ConfirmPasswordError.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                // Jeśli pole jest puste, ukryj komunikat o błędzie
                ConfirmPasswordError.Visibility = Visibility.Collapsed;
            }
        }
        private bool ValidateField(Control inputField, TextBlock errorTextBlock, string errorMessage, Func<string, bool> validation = null)
        {
            string value = inputField is TextBox textBox ? textBox.Text : (inputField as PasswordBox)?.Password;

            if (string.IsNullOrWhiteSpace(value))
            {
                errorTextBlock.Text = errorMessage;
                errorTextBlock.Visibility = Visibility.Visible;
                return false;
            }
            else if (validation != null && !validation(value))
            {
                errorTextBlock.Text = errorMessage;
                errorTextBlock.Visibility = Visibility.Visible;
                return false;
            }

            errorTextBlock.Visibility = Visibility.Collapsed;
            return true;
        }


        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string email = EmailTextBox.Text;
            string password = PasswordBox.Password;

            string verificationCode = GenerateVerificationCode();

            bool isUsernameValid = ValidateField(UsernameTextBox, UsernameError, "Nazwa użytkownika jest wymagana.");
            bool isEmailValid = ValidateField(EmailTextBox, EmailError, "Adres e-mail jest wymagany.", IsValidEmail);
            bool isPasswordValid = ValidateField(PasswordBox, PasswordError, "Hasło jest wymagane.", pwd => pwd.Length >= 8);
            bool isConfirmPasswordValid = ValidateField(ConfirmPasswordBox, ConfirmPasswordError, "Potwierdzenie hasła jest wymagane.",
                                                        confirmPwd => confirmPwd == password);

            // Jeśli walidacja nie powiedzie się, zakończ metodę
            if (!isUsernameValid || !isEmailValid || !isPasswordValid || !isConfirmPasswordValid)
            {
                return;
            }

            using (var context = new ApplicationDbContext())
            {
                var existingUser = context.Users.FirstOrDefault(u => u.Email == email);
                if (existingUser != null)
                {
//                    EmailError.Text = "Użytkownik o podanym adresie e-mail już istnieje.";
                    EmailError.Visibility = Visibility.Visible;
                    ((MainWindow)Application.Current.MainWindow).ShowNotification("Użytkownik o podanym adresie e-mail już istnieje.", "error");
                    return;
                }

                // Tworzenie nowego użytkownika, jeśli e-mail jest unikalny
                var newUser = new User
                {
                    Username = username,
                    Email = email,
                    PasswordHash = HashPassword(password),
                    VerificationToken = verificationCode,
                    IsEmailVerified = false
                };

                context.Users.Add(newUser);
                context.SaveChanges();
            }

            var emailService = new EmailService();
            emailService.SendVerificationEmail(email, verificationCode);

            ((MainWindow)Application.Current.MainWindow).ShowNotification("Rejestracja zakończona sukcesem! Sprawdź swoją skrzynkę pocztową, aby odebrać kod weryfikacyjny.");

            OnVerificationRequired?.Invoke(email);
        }



        private bool IsValidEmail(string email)
        {
            var emailRegex = new Regex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$");
            return emailRegex.IsMatch(email);
        }

        private bool IsValidPassword(string password)
        {
            return password.Length >= 8 &&
                   password.Any(char.IsUpper) &&
                   password.Any(char.IsLower) &&
                   password.Any(char.IsDigit) &&
                   password.Any(ch => !char.IsLetterOrDigit(ch));
        }

        private string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return System.Convert.ToBase64String(hash);
            }
        }
        public string GenerateVerificationToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        }


        public void VerifyEmailLocally(string verificationCode)
        {
            using (var context = new ApplicationDbContext())
            {
                var user = context.Users.SingleOrDefault(u => u.VerificationToken == verificationCode);
                if (user != null)
                {
                    user.IsEmailVerified = true;
                    user.VerificationToken = null; // Usuwamy token po weryfikacji
                    context.SaveChanges();
                    MessageBox.Show("Adres e-mail został zweryfikowany!", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Nieprawidłowy kod weryfikacyjny.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public string GenerateVerificationCode()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString(); // Generuje kod 6-cyfrowy
        }

        private void BackToLoginButton_Click(object sender, RoutedEventArgs e)
        {
            OnBackToLogin?.Invoke();
        }
    }
}
