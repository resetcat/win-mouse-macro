using System.Collections.Generic;
using System.Runtime.Serialization;

namespace win_mouse_macro.models {
    // One action in a saved macro. Members that do not apply to the action's type stay null and are not written.
    [DataContract]
    public class MacroFileAction {
        [DataMember(Name = "type", Order = 0)]
        public string Type { get; set; }

        [DataMember(Name = "button", Order = 1, EmitDefaultValue = false)]
        public string Button { get; set; }

        [DataMember(Name = "x", Order = 2, EmitDefaultValue = false)]
        public int? X { get; set; }

        [DataMember(Name = "y", Order = 3, EmitDefaultValue = false)]
        public int? Y { get; set; }

        [DataMember(Name = "holdMs", Order = 4, EmitDefaultValue = false)]
        public int? HoldMs { get; set; }

        [DataMember(Name = "fromX", Order = 5, EmitDefaultValue = false)]
        public int? FromX { get; set; }

        [DataMember(Name = "fromY", Order = 6, EmitDefaultValue = false)]
        public int? FromY { get; set; }

        [DataMember(Name = "toX", Order = 7, EmitDefaultValue = false)]
        public int? ToX { get; set; }

        [DataMember(Name = "toY", Order = 8, EmitDefaultValue = false)]
        public int? ToY { get; set; }

        [DataMember(Name = "key", Order = 9, EmitDefaultValue = false)]
        public string Key { get; set; }

        [DataMember(Name = "scanCode", Order = 10, EmitDefaultValue = false)]
        public int? ScanCode { get; set; }

        [DataMember(Name = "extended", Order = 11, EmitDefaultValue = false)]
        public bool? Extended { get; set; }

        [DataMember(Name = "modifiers", Order = 12, EmitDefaultValue = false)]
        public List<string> Modifiers { get; set; }
    }
}
