using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using win_mouse_macro.models;


namespace win_mouse_macro {

    public partial class MainWindow : Window {

        // Pause after each played-back action, before timing jitter is applied.
        private const int ActionDelayMs = 500;

        public List<MacroRecord> records = new List<MacroRecord>();
        public bool isRecording = false;
        public bool isPlaying = false;
        private int loopCounter = 0;
        private CancellationTokenSource playbackCancellation;

        public MainWindow() {
            InitializeComponent();
            Application.Current.MainWindow = this;
            MouseHook.SetHook();
            KeyboardHook.SetHook();
        }

        private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e) {
            if (!isRecording) return;

            var position = e.GetPosition(this);
            var button = e.ChangedButton;

            ClickRecord record = new ClickRecord {
                Button = button,
                X = (int)position.X,
                Y = (int)position.Y
            };

            AddRecord(record);
        }

        public void AddRecord(MacroRecord record) {
            records.Add(record);
            lstRecords.Items.Add(record);
        }


        private void BtnRecord_Click(object sender, RoutedEventArgs e) {
            if (!isRecording) {
                lstRecords.Items.Clear();
                records.Clear();
                isRecording = true;
                ((TextBlock)((StackPanel)btnRecord.Content).Children[1]).Text = "Recording...";

                lstRecords.Items.Add("Recording started...");
                lstRecords.ScrollIntoView(lstRecords.Items[lstRecords.Items.Count - 1]);
            } else {
                isRecording = false;
                ((TextBlock)((StackPanel)btnRecord.Content).Children[1]).Text = "Record (F5)";
                if (records.Count > 0) {
                    lstRecords.Items.RemoveAt(lstRecords.Items.Count - 1);
                    records.RemoveAt(records.Count - 1);
                }
                lstRecords.Items.Add("Recording stopped.");
                lstRecords.ScrollIntoView(lstRecords.Items[lstRecords.Items.Count - 1]);
            }
        }


        private async void BtnPlay_Click(object sender, RoutedEventArgs e) {
            if (isPlaying) {
                // The running playback notices the cancellation, finishes the action it is in the middle of
                // (so no button or key is left held down) and then resets the Play button itself.
                playbackCancellation.Cancel();
                lstRecords.Items.Add("Playback stopped.");
                lstRecords.ScrollIntoView(lstRecords.Items[lstRecords.Items.Count - 1]);
                return;
            }

            if (isRecording) {
                lstRecords.Items.Add("Playback blocked: Recording is active.");
                if (records.Count > 0) {
                    records.RemoveAt(records.Count - 1);
                }
                lstRecords.ScrollIntoView(lstRecords.Items[lstRecords.Items.Count - 1]);
                return;
            }

            int delay;
            if (!int.TryParse(txtLoopDelay.Text, out delay) || delay < 0) {
                lstRecords.Items.Add("Invalid loop delay. Please enter a positive number.");
                lstRecords.ScrollIntoView(lstRecords.Items[lstRecords.Items.Count - 1]);
                return;
            }

            isPlaying = true;
            loopCounter = 0;
            ((TextBlock)((StackPanel)btnPlay.Content).Children[1]).Text = "Stop (F6)";
            lstRecords.Items.Add("Playing back actions...");
            lstRecords.ScrollIntoView(lstRecords.Items[lstRecords.Items.Count - 1]);

            var cancellation = new CancellationTokenSource();
            playbackCancellation = cancellation;
            if (chkRepeatForever.IsChecked == true) {
                while (chkRepeatForever.IsChecked == true) {
                    // Times the whole pass: every action plus the loop delay at its end.
                    Stopwatch loopTimer = Stopwatch.StartNew();
                    await PlayActions(delay, cancellation.Token);
                    // A pass cut short by Stop or Reset is not a completed loop.
                    if (cancellation.IsCancellationRequested) break;
                    loopCounter++;
                    lstRecords.Items.Add($"Loop count: {loopCounter} (took {loopTimer.Elapsed.TotalSeconds:0.0} s)");
                    lstRecords.ScrollIntoView(lstRecords.Items[lstRecords.Items.Count - 1]);
                }
            } else {
                await PlayActions(delay, cancellation.Token);
            }
            playbackCancellation = null;
            cancellation.Dispose();

            isPlaying = false;
            ((TextBlock)((StackPanel)btnPlay.Content).Children[1]).Text = "Play (F6)";
            lstRecords.ScrollIntoView(lstRecords.Items[lstRecords.Items.Count - 1]);
        }


        private async Task PlayActions(int delay, CancellationToken cancellation) {
            foreach (var record in new List<MacroRecord>(records)) {
                if (cancellation.IsCancellationRequested) return; // Stop playback once Stop or Reset was pressed

                await record.PlayAsync();
                await Humanizer.DelayAsync(ActionDelayMs, cancellation);
            }
            await Humanizer.DelayAsync(delay, cancellation);
        }


        private void BtnReset_Click(object sender, RoutedEventArgs e) {
            lstRecords.Items.Clear();
            records.Clear();
            isRecording = false;
            // A running playback notices the cancellation and clears isPlaying itself once it has wound down.
            playbackCancellation?.Cancel();
            loopCounter = 0;
            chkRepeatForever.IsChecked = false;
            ((TextBlock)((StackPanel)btnPlay.Content).Children[1]).Text = "Play (F6)";
            lstRecords.Items.Add("Reset completed.");
            lstRecords.ScrollIntoView(lstRecords.Items[lstRecords.Items.Count - 1]);
        }


        protected override void OnClosed(EventArgs e) {
            MouseHook.ReleaseHook();
            KeyboardHook.ReleaseHook();
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
