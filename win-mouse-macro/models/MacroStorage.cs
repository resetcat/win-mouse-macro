using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Input;

namespace win_mouse_macro.models {
    // Saves macros as JSON files and opens them again. MacroFile and MacroFileAction define the layout.
    public static class MacroStorage {
        private const int FormatVersion = 1;
        // Far more than any real macro needs; stops a huge unrelated file from freezing the app.
        private const long MaxFileBytes = 10 * 1024 * 1024;
        private const string ClickType = "click";
        private const string DragType = "drag";
        private const string KeyType = "key";

        public static void Save(string path, Macro macro) {
            MacroFile file = new MacroFile {
                Version = FormatVersion,
                Loop = macro.Loop,
                LoopDelayMs = macro.LoopDelayMs,
                Actions = macro.Records.Select(ToAction).ToList()
            };

            // Build the whole text in memory first, so a failure cannot leave a half-written file behind.
            using (MemoryStream buffer = new MemoryStream()) {
                using (var writer = JsonReaderWriterFactory.CreateJsonWriter(
                    buffer, Encoding.UTF8, ownsStream: false, indent: true, indentChars: "  ")) {
                    new DataContractJsonSerializer(typeof(MacroFile)).WriteObject(writer, file);
                }
                File.WriteAllBytes(path, buffer.ToArray());
            }
        }

        // Throws InvalidDataException when the file is not a usable macro. Its message is written for the
        // user and completes the sentence "Could not open <file>: ...".
        public static Macro Load(string path) {
            if (new FileInfo(path).Length > MaxFileBytes) {
                throw new InvalidDataException("the file is too large to be a macro.");
            }

            MacroFile file = ReadFile(path);
            if (file == null || file.Version < 1) {
                throw new InvalidDataException("it is not a macro file.");
            }
            if (file.Version > FormatVersion) {
                throw new InvalidDataException($"it was saved by a newer version of this app (file format {file.Version}).");
            }
            if (file.Actions == null) {
                throw new InvalidDataException("it has no \"actions\" list.");
            }
            if (file.LoopDelayMs < 0) {
                throw new InvalidDataException("\"loopDelayMs\" must not be negative.");
            }

            Macro macro = new Macro { Loop = file.Loop, LoopDelayMs = file.LoopDelayMs };
            for (int i = 0; i < file.Actions.Count; i++) {
                macro.Records.Add(ToRecord(file.Actions[i], $"action {i + 1}"));
            }
            return macro;
        }

        private static MacroFile ReadFile(string path) {
            // Reading as text first copes with a byte-order mark (Notepad adds one), which the JSON reader rejects.
            byte[] json = Encoding.UTF8.GetBytes(File.ReadAllText(path));
            try {
                using (MemoryStream stream = new MemoryStream(json)) {
                    return (MacroFile)new DataContractJsonSerializer(typeof(MacroFile)).ReadObject(stream);
                }
            } catch (SerializationException exception) {
                throw new InvalidDataException("it is not valid macro JSON.", exception);
            }
        }

        private static MacroFileAction ToAction(MacroRecord record) {
            MacroFileAction action;
            switch (record) {
                case ClickRecord click:
                    action = new MacroFileAction {
                        Type = ClickType,
                        Button = click.Button.ToString(),
                        X = click.X,
                        Y = click.Y,
                        HoldMs = click.IsHold ? click.HoldMs : (int?)null
                    };
                    break;
                case DragRecord drag:
                    action = new MacroFileAction {
                        Type = DragType,
                        Button = drag.Button.ToString(),
                        FromX = drag.FromX,
                        FromY = drag.FromY,
                        ToX = drag.ToX,
                        ToY = drag.ToY
                    };
                    break;
                case KeyRecord key:
                    action = new MacroFileAction {
                        Type = KeyType,
                        Key = KeyboardOperations.GetKeyName(key.VirtualKey),
                        // The usual scan code is left out and worked out again on opening, so a key name edited
                        // by hand cannot end up paired with the scan code of another key.
                        ScanCode = key.ScanCode == KeyboardOperations.GetScanCode(key.VirtualKey) ? (int?)null : key.ScanCode,
                        Extended = key.IsExtended ? true : (bool?)null
                    };
                    break;
                default:
                    throw new NotSupportedException($"Saving a {record.GetType().Name} is not supported.");
            }

            if (record.Modifiers.Count > 0) {
                action.Modifiers = record.Modifiers.Select(KeyboardOperations.GetKeyName).ToList();
            }
            return action;
        }

        private static MacroRecord ToRecord(MacroFileAction action, string label) {
            if (action == null) throw new InvalidDataException($"{label} is empty.");
            if (string.IsNullOrEmpty(action.Type)) throw new InvalidDataException($"{label} has no \"type\".");

            MacroRecord record;
            switch (action.Type) {
                case ClickType:
                    int holdMs = action.HoldMs ?? 0;
                    if (holdMs < 0) throw new InvalidDataException($"{label}: \"holdMs\" must not be negative.");
                    record = new ClickRecord {
                        Button = ParseButton(action.Button, label),
                        X = Require(action.X, "x", label),
                        Y = Require(action.Y, "y", label),
                        HoldMs = holdMs
                    };
                    break;
                case DragType:
                    record = new DragRecord {
                        Button = ParseButton(action.Button, label),
                        FromX = Require(action.FromX, "fromX", label),
                        FromY = Require(action.FromY, "fromY", label),
                        ToX = Require(action.ToX, "toX", label),
                        ToY = Require(action.ToY, "toY", label)
                    };
                    break;
                case KeyType:
                    if (action.Key == null) throw new InvalidDataException($"{label} has no \"key\".");
                    if (action.ScanCode < 0) throw new InvalidDataException($"{label}: \"scanCode\" must not be negative.");
                    int virtualKey = ParseKey(action.Key, label);
                    record = new KeyRecord {
                        VirtualKey = virtualKey,
                        ScanCode = action.ScanCode ?? KeyboardOperations.GetScanCode(virtualKey),
                        IsExtended = action.Extended ?? false
                    };
                    break;
                default:
                    throw new InvalidDataException($"{label} has an unknown type \"{action.Type}\".");
            }

            if (action.Modifiers != null) {
                foreach (string name in action.Modifiers) {
                    int modifier = ParseKey(name, label);
                    if (!KeyboardOperations.IsModifier(modifier)) {
                        throw new InvalidDataException($"{label}: \"{name}\" cannot be used as a modifier.");
                    }
                    record.Modifiers.Add(modifier);
                }
            }
            return record;
        }

        private static MouseButton ParseButton(string name, string label) {
            if (string.Equals(name, MouseButton.Left.ToString(), StringComparison.OrdinalIgnoreCase)) return MouseButton.Left;
            if (string.Equals(name, MouseButton.Right.ToString(), StringComparison.OrdinalIgnoreCase)) return MouseButton.Right;
            throw new InvalidDataException($"{label}: \"button\" must be \"Left\" or \"Right\".");
        }

        private static int ParseKey(string name, string label) {
            if (!KeyboardOperations.TryParseKeyName(name, out int virtualKey)) {
                throw new InvalidDataException($"{label} has an unknown key \"{name}\".");
            }
            return virtualKey;
        }

        private static int Require(int? value, string name, string label) {
            if (value == null) throw new InvalidDataException($"{label} has no \"{name}\".");
            return value.Value;
        }
    }
}
