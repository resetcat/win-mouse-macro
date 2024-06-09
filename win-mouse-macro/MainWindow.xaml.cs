using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
                ((TextBlock)((StackPanel)btnRecord.Content).Children[1]).Text = "Recording...";

                lstRecords.Items.Add("Recording started...");
            } else {
                isRecording = false;
                ((TextBlock)((StackPanel)btnRecord.Content).Children[1]).Text = "Record (F5)";
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

            int delay;
            if (!int.TryParse(txtLoopDelay.Text, out delay) || delay < 0) {
                lstRecords.Items.Add("Invalid loop delay. Please enter a positive number.");
                return;
            }

            lstRecords.Items.Add("Playing back actions...");
            if (chkRepeatForever.IsChecked == true) {
                while (chkRepeatForever.IsChecked == true) {
                    await PlayActions(delay);
                }
            } else {
                await PlayActions(delay);
            }

        }

        private async Task PlayActions(int delay = 500) {
            foreach (var record in new List<ClickRecord>(clickRecords)) {
                MouseOperations.MouseEventFlags flags = record.Button == "Right" ?
                    MouseOperations.MouseEventFlags.RIGHTDOWN | MouseOperations.MouseEventFlags.RIGHTUP :
                    MouseOperations.MouseEventFlags.LEFTDOWN | MouseOperations.MouseEventFlags.LEFTUP;

                MouseOperations.MouseEvent(flags, record.X, record.Y);
                await Task.Delay(500);
            }
            await Task.Delay(delay);
        }


        private void BtnReset_Click(object sender, RoutedEventArgs e) {
            lstRecords.Items.Clear();
            clickRecords.Clear();
            isRecording = false;
            chkRepeatForever.IsChecked = false;
            lstRecords.Items.Add("Reset completed.");
        }

        protected override void OnClosed(EventArgs e) {
            MouseHook.ReleaseHook();
            base.OnClosed(e);
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.F5) {
                BtnRecord_Click(this, new RoutedEventArgs());
            } else if (e.Key == Key.F6) {
                BtnPlay_Click(this, new RoutedEventArgs());
            } else if (e.Key == Key.F8) {
                BtnReset_Click(this, new RoutedEventArgs());
            }
        }
    }
}
