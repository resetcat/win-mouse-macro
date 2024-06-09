using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using win_mouse_macro.models;


namespace win_mouse_macro {

    public partial class MainWindow : Window {

        public List<ClickRecord> clickRecords = new List<ClickRecord>();
        public bool isRecording = false;

        public MainWindow() {
            InitializeComponent();
            Application.Current.MainWindow = this;
            MouseHook.SetHook();
        }

        private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e) {
            if (!isRecording) return;

            var position = e.GetPosition(this);
            var button = e.ChangedButton.ToString();

            ClickRecord record = new ClickRecord {
                Button = button,
                X = (int)position.X,
                Y = (int)position.Y
            };

            clickRecords.Add(record);
            lstRecords.Items.Add(record);
        }


        private void BtnRecord_Click(object sender, RoutedEventArgs e) {
            if (!isRecording) {
                lstRecords.Items.Clear();
                clickRecords.Clear();
                isRecording = true;
                lstRecords.Items.Add("Recording started...");
                lstRecords.Items.Add(" loop delay" + txtLoopDelay.Text);
                lstRecords.Items.Add(" checkbox" + chkRepeatForever.IsChecked);
            } else {
                isRecording = false;
                if (lstRecords.Items.Count > 0) {
                    lstRecords.Items.RemoveAt(lstRecords.Items.Count - 1);
                    clickRecords.RemoveAt(clickRecords.Count - 1);
                }
                lstRecords.Items.Add("Recording stopped.");
            }
        }

        private async void BtnPlay_Click(object sender, RoutedEventArgs e) {

            if (isRecording) {
                lstRecords.Items.Add("Playback blocked: Recording is active.");
                clickRecords.RemoveAt(clickRecords.Count - 1);
                return;
            }
            lstRecords.Items.Add("Playing back actions...");
            await PlayActions();

        }

        private async Task PlayActions(int delay = 500) {
            foreach (var record in new List<ClickRecord>(clickRecords)) {
                MouseOperations.MouseEventFlags flags = record.Button == "Right" ?
                    MouseOperations.MouseEventFlags.RIGHTDOWN | MouseOperations.MouseEventFlags.RIGHTUP :
                    MouseOperations.MouseEventFlags.LEFTDOWN | MouseOperations.MouseEventFlags.LEFTUP;

                MouseOperations.MouseEvent(flags, record.X, record.Y);
                await Task.Delay(500);
            }
        }


        private void BtnReset_Click(object sender, RoutedEventArgs e) {
            lstRecords.Items.Clear();
            clickRecords.Clear();
            isRecording = false;
        }

        protected override void OnClosed(EventArgs e) {
            MouseHook.ReleaseHook();
            base.OnClosed(e);
        }

        private void BtnIncrease_Click(object sender, RoutedEventArgs e) {

        }

        private void BtnDecrease_Click(object sender, RoutedEventArgs e) {

        }
    }


}
