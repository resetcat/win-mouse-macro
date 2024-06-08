using System.Windows;

namespace win_mouse_macro {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }


        private void btnRecord_Click(object sender, RoutedEventArgs e) {
            lstRecords.Items.Add("Recording started...");
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e) {
            lstRecords.Items.Add("Playing back actions...");
        }

    }


}
