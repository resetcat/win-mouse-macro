using System.Collections.Generic;
using System.Runtime.Serialization;

namespace win_mouse_macro.models {
    // The JSON layout of a saved macro. The names and order here are the file format, so change them with care.
    [DataContract]
    public class MacroFile {
        [DataMember(Name = "version", Order = 0)]
        public int Version { get; set; }

        [DataMember(Name = "loop", Order = 1)]
        public bool Loop { get; set; }

        [DataMember(Name = "loopDelayMs", Order = 2)]
        public int LoopDelayMs { get; set; }

        [DataMember(Name = "actions", Order = 3)]
        public List<MacroFileAction> Actions { get; set; }
    }
}
