using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace wpf_inz
{
    public partial class UserProfileView : UserControl
    {
        public UserProfileView()
        {
            InitializeComponent();
        }


        private void CurrentPasswordBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // Sprawdź, czy bieżące hasło jest wprowadzone
            if (string.IsNullOrWhiteSpace(CurrentPasswordBox.Password))
            {
                CurrentPasswordError.Text = "Obecne hasło jest wymagane.";
                CurrentPasswordError.Visibility = Visibility.Visible;
            }
            else
            {
                CurrentPasswordError.Visibility = Visibility.Collapsed;
            }
        }

        private void NewPasswordBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // Walidacja nowego hasła tylko, jeśli pole nie jest puste
            if (!string.IsNullOrWhiteSpace(NewPasswordBox.Password))
            {
                if (NewPasswordBox.Password.Length < 8)
                {
                    NewPasswordError.Text = "Hasło musi mieć co najmniej 8 znaków.";
                    NewPasswordError.Visibility = Visibility.Visible;
                }
                else
                {
                    NewPasswordError.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                // Jeśli pole jest puste, ukryj komunikat o błędzie
                NewPasswordError.Visibility = Visibility.Collapsed;
            }
        }

        private void ConfirmNewPasswordBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // Walidacja potwierdzenia hasła tylko, jeśli pole nie jest puste
            if (!string.IsNullOrWhiteSpace(ConfirmNewPasswordBox.Password))
            {
                if (ConfirmNewPasswordBox.Password != NewPasswordBox.Password)
                {
                    ConfirmNewPasswordError.Text = "Hasła nie są zgodne.";
                    ConfirmNewPasswordError.Visibility = Visibility.Visible;
                }
                else
                {
                    ConfirmNewPasswordError.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                // Jeśli pole jest puste, ukryj komunikat o błędzie
                ConfirmNewPasswordError.Visibility = Visibility.Collapsed;
            }
        }

        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            // Sprawdzenie, czy wszystkie pola zostały poprawnie wypełnione
            if (string.IsNullOrWhiteSpace(CurrentPasswordBox.Password))
            {
                ((MainWindow)Application.Current.MainWindow).ShowNotification("Obecne hasło jest wymagane.", "error");
                return;
            }

            if (string.IsNullOrWhiteSpace(NewPasswordBox.Password) || NewPasswordBox.Password.Length < 8)
            {
                ((MainWindow)Application.Current.MainWindow).ShowNotification("Nowe hasło musi mieć co najmniej 8 znaków.", "error");
                return;
            }

            if (NewPasswordBox.Password != ConfirmNewPasswordBox.Password)
            {
                ((MainWindow)Application.Current.MainWindow).ShowNotification("Nowe hasło i jego potwierdzenie nie są zgodne.", "error");
                return;
            }

            using (var context = new ApplicationDbContext())
            {
                var user = context.Users.Find(Session.CurrentUser.Id);
                if (user != null)
                {
                    // Sprawdzenie poprawności obecnego hasła
                    string hashedCurrentPassword = LoginViewModel.HashPassword(CurrentPasswordBox.Password);
                    if (user.PasswordHash != hashedCurrentPassword)
                    {
                        ((MainWindow)Application.Current.MainWindow).ShowNotification("Obecne hasło jest nieprawidłowe.", "error");
                        return;
                    }

                    // Aktualizacja hasła
                    user.PasswordHash = LoginViewModel.HashPassword(NewPasswordBox.Password);
                    context.SaveChanges();

                    // Wyświetlenie powiadomienia o sukcesie
                    ((MainWindow)Application.Current.MainWindow).ShowNotification("Hasło zostało zmienione pomyślnie!", "success");

                    // Wyczyść wszystkie pola haseł
                    CurrentPasswordBox.Clear();
                    NewPasswordBox.Clear();
                    ConfirmNewPasswordBox.Clear();
                }
            }
        }


        private void DeleteAccount_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Czy na pewno chcesz usunąć swoje konto? Ta operacja jest nieodwracalna.", "Potwierdzenie", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                using (var context = new ApplicationDbContext())
                {
                    var user = context.Users.Find(Session.CurrentUser.Id);
                    if (user != null)
                    {
                        context.HomeDevices.RemoveRange(context.HomeDevices.Where(d => d.UserId == user.Id));
                        context.Users.Remove(user);
                        context.SaveChanges();
                    }
                }

                ((MainWindow)Application.Current.MainWindow).ShowNotification("Konto zostało usunięte!", "success");
                ((MainWindow)Application.Current.MainWindow).ShowLoginView();
            }
        }
    }
}
