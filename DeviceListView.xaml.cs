using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.IO;

namespace wpf_inz
{
    public partial class DeviceListView : UserControl
    {
        public DeviceListView()
        {
            InitializeComponent();
            LoadDevices();
        }

        private void LoadDevices()
        {
            using (var context = new ApplicationDbContext())
            {
                DeviceGrid.ItemsSource = context.HomeDevices
                    .Where(d => d.UserId == Session.CurrentUser.Id)
                    .ToList();
            }
        }

        private void EditDevice_Click(object sender, RoutedEventArgs e)
        {
            if (DeviceGrid.SelectedItem is HomeDevice selectedDevice)
            {
                // Tworzymy widok edycji urządzenia
                var addDeviceView = new AddDeviceView(selectedDevice);

                // Pobieramy HomeView i zamieniamy dynamiczną treść
                var mainWindow = (MainWindow)Application.Current.MainWindow;
                if (mainWindow.MainContent.Content is HomeView homeView)
                {
                    homeView.MainContent.Content = addDeviceView;
                }
            }
        }

        private void AddDevice_Click(object sender, RoutedEventArgs e)
        {
            // Przejdź do widoku dodawania urządzenia
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            var homeView = new HomeView();
            homeView.MainContent.Content = new AddDeviceView(); // Osadzamy DeviceListView w HomeView
            mainWindow.MainContent.Content = homeView; // Ustawiamy HomeView jako główny widok
        }

        private void DownloadReceiptImage_Click(object sender, RoutedEventArgs e)
        {
            if (DeviceGrid.SelectedItem is HomeDevice selectedDevice)
            {
                // Sprawdź, czy obraz istnieje
                if (selectedDevice.ReceiptImage != null && selectedDevice.ReceiptImage.Length > 0)
                {
                    // Otwórz dialog, aby wybrać miejsce zapisu pliku
                    var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                    {
                        FileName = $"{selectedDevice.Name}_receipt.jpg", // Domyślna nazwa pliku
                        Filter = "Image files (*.jpg)|*.jpg|All files (*.*)|*.*"
                    };

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        try
                        {
                            // Zapisz obraz na wybranej ścieżce
                            File.WriteAllBytes(saveFileDialog.FileName, selectedDevice.ReceiptImage);
                            ((MainWindow)Application.Current.MainWindow).ShowNotification("Obraz paragonu został zapisany pomyślnie!", "success");
                        }
                        catch (Exception ex)
                        {
                            ((MainWindow)Application.Current.MainWindow).ShowNotification($"Błąd podczas zapisywania obrazu: {ex.Message}", "error");
                        }
                    }
                }
                else
                {
                    ((MainWindow)Application.Current.MainWindow).ShowNotification("Brak obrazu paragonu do pobrania.", "warning");
                }
            }
        }


        private void DeleteDevice_Click(object sender, RoutedEventArgs e)
        {
            if (DeviceGrid.SelectedItem is HomeDevice selectedDevice)
            {
                var result = MessageBox.Show($"Czy na pewno chcesz usunąć urządzenie \"{selectedDevice.Name}\"?",
                                             "Potwierdzenie", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    using (var context = new ApplicationDbContext())
                    {
                        context.HomeDevices.Remove(selectedDevice);
                        context.SaveChanges();
                    }
                    ((MainWindow)Application.Current.MainWindow).ShowNotification("Urządzenie zostało pomyślnie usunięte!", "success");
                    LoadDevices();
                }
            }
        }
    }
}
