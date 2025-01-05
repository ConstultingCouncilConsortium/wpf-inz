using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace wpf_inz
{
    public partial class VerificationView : UserControl
    {
        private string userEmail;
        public event Action OnVerificationSuccess;

        public VerificationView()
        {
            InitializeComponent();
        }

        public void SetEmailForVerification(string email)
        {
            userEmail = email;
        }

        private void ConfirmVerificationCode_Click(object sender, RoutedEventArgs e)
        {
            string enteredCode = VerificationCodeTextBox.Text;

            using (var context = new ApplicationDbContext())
            {
                var user = context.Users.SingleOrDefault(u => u.Email == userEmail && u.VerificationToken == enteredCode);
                if (user != null)
                {
                    user.IsEmailVerified = true;
                    user.VerificationToken = null; // Usuwamy token po weryfikacji
                    context.SaveChanges();

                    ((MainWindow)Application.Current.MainWindow).ShowNotification("Adres e-mail został zweryfikowany!");
                    OnVerificationSuccess?.Invoke();
                }
                else
                {
                    ((MainWindow)Application.Current.MainWindow).ShowNotification("Nieprawidłowy kod weryfikacyjny.", "error");
                }
            }
        }

        private void SendNewCodeButton_Click(object sender, RoutedEventArgs e)
        {
            string newVerificationCode = GenerateVerificationCode();

            using (var context = new ApplicationDbContext())
            {
                var user = context.Users.SingleOrDefault(u => u.Email == userEmail);
                if (user != null)
                {
                    user.VerificationToken = newVerificationCode;
                    context.SaveChanges();

                    var emailService = new EmailService();
                    emailService.SendVerificationEmail(userEmail, newVerificationCode);

                    ((MainWindow)Application.Current.MainWindow).ShowNotification("Nowy kod weryfikacyjny został wysłany na Twój adres e-mail.");
                }
                else
                {
                    ((MainWindow)Application.Current.MainWindow).ShowNotification("Nie znaleziono użytkownika o podanym adresie e-mail.");
                }
            }
        }

        private string GenerateVerificationCode()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString(); // Generuje kod 6-cyfrowy
        }
    }

}
