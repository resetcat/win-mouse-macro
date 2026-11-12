using System.Collections.Generic;

namespace win_mouse_macro.models {
    // What a saved macro holds: the recorded actions and how they are repeated.
    public class Macro {
        public List<MacroRecord> Records { get; set; } = new List<MacroRecord>();
        public bool Loop { get; set; }
        public int LoopDelayMs { get; set; }
    }
}
