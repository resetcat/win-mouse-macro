using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using win_mouse_macro.models;


namespace win_mouse_macro {

    public partial class MainWindow : Window {

        // Pause after each played-back action, before timing jitter is applied.
        private const int ActionDelayMs = 500;

        private const string AppName = "win-mouse-macro";
        private const string FileDialogFilter = "Macro files (*.json)|*.json|All files (*.*)|*.*";
        private const string DefaultFileName = "macro.json";

        public List<MacroRecord> records = new List<MacroRecord>();
        public bool isRecording = false;
        public bool isPlaying = false;
        private int loopCounter = 0;
        private CancellationTokenSource playbackCancellation;
        // The file Save writes to; null until the macro has been opened from or saved to one.
        private string currentFilePath;

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
                // A new recording is a new macro: Save must not quietly overwrite the file opened before.
                SetCurrentFile(null);
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

                await record.PlayAsync(cancellation);
                await Humanizer.DelayAsync(ActionDelayMs, cancellation);
            }
            await Humanizer.DelayAsync(delay, cancellation);
        }


        private void BtnReset_Click(object sender, RoutedEventArgs e) {
            lstRecords.Items.Clear();
            records.Clear();
            SetCurrentFile(null);
            isRecording = false;
            // A running playback notices the cancellation and clears isPlaying itself once it has wound down.
            playbackCancellation?.Cancel();
            loopCounter = 0;
            chkRepeatForever.IsChecked = false;
            ((TextBlock)((StackPanel)btnPlay.Content).Children[1]).Text = "Play (F6)";
            lstRecords.Items.Add("Reset completed.");
            lstRecords.ScrollIntoView(lstRecords.Items[lstRecords.Items.Count - 1]);
        }


        private void MenuOpen_Click(object sender, RoutedEventArgs e) {
            if (RefuseWhileBusy()) return;

            var dialog = new OpenFileDialog { Filter = FileDialogFilter };
            if (dialog.ShowDialog(this) != true) return;

            string fileName = Path.GetFileName(dialog.FileName);
            Macro macro;
            try {
                macro = MacroStorage.Load(dialog.FileName);
            } catch (Exception exception) when (exception is InvalidDataException || exception is IOException || exception is UnauthorizedAccessException) {
                // Nothing has been changed yet, so the macro that was open stays as it is.
                AddMessage($"Could not open {fileName}: {exception.Message}");
                return;
            }

            lstRecords.Items.Clear();
            records.Clear();
            foreach (MacroRecord record in macro.Records) {
                AddRecord(record);
            }
            chkRepeatForever.IsChecked = macro.Loop;
            txtLoopDelay.Text = macro.LoopDelayMs.ToString();
            SetCurrentFile(dialog.FileName);
            AddMessage($"Opened {fileName} ({macro.Records.Count} actions).");
        }

        private void MenuSave_Click(object sender, RoutedEventArgs e) {
            if (currentFilePath == null) {
                MenuSaveAs_Click(sender, e);
                return;
            }

            if (TryBuildMacro(out Macro macro)) {
                SaveMacro(currentFilePath, macro);
            }
        }

        private void MenuSaveAs_Click(object sender, RoutedEventArgs e) {
            // Checked before asking for a name, so the dialog never opens for something that cannot be saved.
            if (!TryBuildMacro(out Macro macro)) return;

            var dialog = new SaveFileDialog {
                Filter = FileDialogFilter,
                DefaultExt = ".json",
                FileName = currentFilePath == null ? DefaultFileName : Path.GetFileName(currentFilePath)
            };
            if (dialog.ShowDialog(this) == true) {
                SaveMacro(dialog.FileName, macro);
            }
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e) {
            Close();
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e) {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            MessageBox.Show(
                this,
                $"{AppName}\nVersion {version.ToString(3)}\n\nRecords mouse and keyboard actions and plays them back.\n\nhttps://github.com/resetcat/win-mouse-macro",
                $"About {AppName}",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        // Collects what Save writes. Says why in the list and returns false when there is nothing valid to save.
        private bool TryBuildMacro(out Macro macro) {
            macro = null;
            if (RefuseWhileBusy()) return false;

            if (records.Count == 0) {
                AddMessage("Nothing to save.");
                return false;
            }

            if (!int.TryParse(txtLoopDelay.Text, out int delay) || delay < 0) {
                AddMessage("Invalid loop delay. Please enter a positive number.");
                return false;
            }

            macro = new Macro {
                Records = new List<MacroRecord>(records),
                Loop = chkRepeatForever.IsChecked == true,
                LoopDelayMs = delay
            };
            return true;
        }

        private void SaveMacro(string path, Macro macro) {
            string fileName = Path.GetFileName(path);
            try {
                MacroStorage.Save(path, macro);
            } catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException) {
                AddMessage($"Could not save {fileName}: {exception.Message}");
                return;
            }

            SetCurrentFile(path);
            AddMessage($"Saved {fileName}.");
        }

        private bool RefuseWhileBusy() {
            if (!isRecording && !isPlaying) return false;

            AddMessage("Stop recording and playback first.");
            return true;
        }

        private void SetCurrentFile(string path) {
            currentFilePath = path;
            Title = path == null ? AppName : $"{Path.GetFileName(path)} - {AppName}";
        }

        private void AddMessage(string message) {
            lstRecords.Items.Add(message);
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
            } else if (e.Key == Key.O && Keyboard.Modifiers == ModifierKeys.Control) {
                MenuOpen_Click(this, new RoutedEventArgs());
                e.Handled = true;
            } else if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control) {
                MenuSave_Click(this, new RoutedEventArgs());
                e.Handled = true;
            } else if (e.Key == Key.S && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift)) {
                MenuSaveAs_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        }
    }
}
