using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace wpf_inz
{
    public partial class AddDeviceView : UserControl
    {
        private HomeDevice _deviceToEdit;
        private byte[] receiptPath;

        public AddDeviceView(HomeDevice device = null)
        {
            InitializeComponent();

            if (device != null)
            {
                _deviceToEdit = device;
                HeaderTextBlock.Text = "Edytuj Urządzenie";
                ActionButton.Content = "Edytuj urządzenie";
                DeviceNameTextBox.Text = device.Name;
                DeviceModelTextBox.Text = device.Model;
                PurchaseDatePicker.SelectedDate = device.PurchaseDate.ToDateTime(TimeOnly.MinValue);
                WarrantyPeriodTextBox.Text = device.WarrantyPeriodMonths.ToString();
                receiptPath = device.ReceiptImage;

                if (receiptPath != null && receiptPath.Length > 0)
                {
                    Console.WriteLine("Obraz jest dostępny, długość byte[]: " + receiptPath.Length);
                    ReceiptImage.Source = ConvertByteArrayToImage(receiptPath);
                    ReceiptImage.Visibility = Visibility.Visible;
                }
                else
                {
                    Console.WriteLine("Obraz nie jest dostępny.");
                }
            }
            else
            {
                HeaderTextBlock.Text = "Dodaj Urządzenie";
                ActionButton.Content = "Dodaj urządzenie";
            }
        }
        private void WarrantyPeriodTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(WarrantyPeriodTextBox.Text, out int warrantyMonths) && warrantyMonths > 0)
            {
                WarrantyPeriodError.Visibility = Visibility.Collapsed;
            }
            else
            {
                WarrantyPeriodError.Text = "Okres gwarancji musi być liczbą dodatnią.";
                WarrantyPeriodError.Visibility = Visibility.Visible;
            }
        }

        private void AddDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            bool isValid = true;
            int warrantyMonths = 0;

            // Nazwa urządzenia
            if (string.IsNullOrWhiteSpace(DeviceNameTextBox.Text))
            {
                DeviceNameError.Text = "Nazwa urządzenia jest wymagana.";
                DeviceNameError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else
            {
                DeviceNameError.Visibility = Visibility.Collapsed;
            }

            // Model urządzenia
            if (string.IsNullOrWhiteSpace(DeviceModelTextBox.Text))
            {
                DeviceModelError.Text = "Model urządzenia jest wymagana.";
                DeviceModelError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else
            {
                DeviceModelError.Visibility = Visibility.Collapsed;
            }

            // Data zakupu
            if (PurchaseDatePicker.SelectedDate == null)
            {
                PurchaseDateError.Text = "Data zakupu jest wymagana.";
                PurchaseDateError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else
            {
                PurchaseDateError.Visibility = Visibility.Collapsed;
            }


            if (string.IsNullOrWhiteSpace(WarrantyPeriodTextBox.Text) || !int.TryParse(WarrantyPeriodTextBox.Text, out warrantyMonths) || warrantyMonths <= 0)
            {
                WarrantyPeriodError.Text = "Okres gwarancji jest wymagany i musi być liczbą dodatnią.";
                WarrantyPeriodError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else
            {
                WarrantyPeriodError.Visibility = Visibility.Collapsed;
            }

            if (!isValid) return;

            using (var context = new ApplicationDbContext())
            {
                if (_deviceToEdit == null)
                {
                    // Dodanie nowego urządzenia
                    var newDevice = new HomeDevice
                    {
                        Name = DeviceNameTextBox.Text,
                        Model = DeviceModelTextBox.Text,
                        PurchaseDate = DateOnly.FromDateTime(PurchaseDatePicker.SelectedDate.Value),
                        WarrantyPeriodMonths = warrantyMonths,
                        WarrantyEndDate = DateOnly.FromDateTime(PurchaseDatePicker.SelectedDate.Value.AddMonths(warrantyMonths)),
                        ReceiptImage = receiptPath,
                        UserId = Session.CurrentUser.Id
                    };

                    context.HomeDevices.Add(newDevice);
                    ((MainWindow)Application.Current.MainWindow).ShowNotification("Urządzenie zostało pomyślnie dodane!", "success");
                }
                else
                {
                    // Aktualizacja istniejącego urządzenia
                    var device = context.HomeDevices.Find(_deviceToEdit.Id);
                    if (device != null)
                    {
                        device.Name = DeviceNameTextBox.Text;
                        device.Model = DeviceModelTextBox.Text;
                        device.PurchaseDate = DateOnly.FromDateTime(PurchaseDatePicker.SelectedDate.Value);
                        device.WarrantyPeriodMonths = warrantyMonths;
                        device.WarrantyEndDate = DateOnly.FromDateTime(PurchaseDatePicker.SelectedDate.Value.AddMonths(warrantyMonths));
                        if (receiptPath != null)
                        {
                            device.ReceiptImage = receiptPath;
                        }
                        ((MainWindow)Application.Current.MainWindow).ShowNotification("Urządzenie zostało pomyślnie wyedytowane!", "success");
                    }
                }



                context.SaveChanges();
                var mainWindow = (MainWindow)Application.Current.MainWindow;
                var homeView = new HomeView();
                homeView.MainContent.Content = new DeviceListView(); // Osadzamy DeviceListView w HomeView
                mainWindow.MainContent.Content = homeView; // Ustawiamy HomeView jako główny widok

            }
        }



        public static byte[] ConvertImageToByteArray(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
                return null;

            return File.ReadAllBytes(imagePath);
        }

        private void SelectReceiptButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                receiptPath = ConvertImageToByteArray(openFileDialog.FileName); 
                ReceiptImage.Source = new BitmapImage(new Uri(openFileDialog.FileName));
                ReceiptImage.Visibility = Visibility.Visible;
            }
        }


        public static BitmapImage ConvertByteArrayToImage(byte[] imageBytes)
        {
            try
            {
                if (imageBytes == null || imageBytes.Length == 0)
                    return null;

                using (var stream = new MemoryStream(imageBytes))
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.EndInit();
                    image.Freeze(); 
                    return image;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Błąd podczas konwersji obrazu: " + ex.Message);
                return null;
            }
        }



        // Obsługa kliknięcia przycisku "Powrót"
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            var homeView = new HomeView();
            homeView.MainContent.Content = new DeviceListView(); 
            mainWindow.MainContent.Content = homeView;
        }

        // Obsługa zdarzenia LostFocus dla DeviceNameTextBox
        private void DeviceNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (DeviceNameTextBox.IsKeyboardFocused || string.IsNullOrEmpty(DeviceNameTextBox.Text))
            {
                DeviceNameError.Visibility = Visibility.Collapsed;
            }
            else if (string.IsNullOrWhiteSpace(DeviceNameTextBox.Text))
            {
                DeviceNameError.Text = "Nazwa urządzenia jest wymagana.";
                DeviceNameError.Visibility = Visibility.Visible;
            }
        }

        // Obsługa zdarzenia LostFocus dla DeviceModelTextBox
        private void DeviceModelTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (DeviceModelTextBox.IsKeyboardFocused || string.IsNullOrEmpty(DeviceModelTextBox.Text))
            {
                DeviceModelError.Visibility = Visibility.Collapsed;
            }
            else if (string.IsNullOrWhiteSpace(DeviceModelTextBox.Text))
            {
                DeviceModelError.Text = "Model urządzenia jest wymagany.";
                DeviceModelError.Visibility = Visibility.Visible;
            }
        }

        // Obsługa zdarzenia LostFocus dla PurchaseDatePicker
        private void PurchaseDatePicker_LostFocus(object sender, RoutedEventArgs e)
        {
            if (PurchaseDatePicker.IsKeyboardFocused || PurchaseDatePicker.SelectedDate == null)
            {
                PurchaseDateError.Visibility = Visibility.Collapsed;
            }
            else if (PurchaseDatePicker.SelectedDate == null)
            {
                PurchaseDateError.Text = "Data zakupu jest wymagana.";
                PurchaseDateError.Visibility = Visibility.Visible;
            }
        }

        // Obsługa zdarzenia LostFocus dla WarrantyPeriodTextBox
        private void WarrantyPeriodTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (WarrantyPeriodTextBox.IsKeyboardFocused || string.IsNullOrEmpty(WarrantyPeriodTextBox.Text))
            {
                WarrantyPeriodError.Visibility = Visibility.Collapsed;
            }
            else if (string.IsNullOrWhiteSpace(WarrantyPeriodTextBox.Text) || !int.TryParse(WarrantyPeriodTextBox.Text, out int warrantyMonths) || warrantyMonths <= 0)
            {
                WarrantyPeriodError.Text = "Okres gwarancji musi być liczbą dodatnią.";
                WarrantyPeriodError.Visibility = Visibility.Visible;
            }
        }
    }
}
