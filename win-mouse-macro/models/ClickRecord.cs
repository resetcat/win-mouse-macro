namespace win_mouse_macro.models {
    public class ClickRecord {
        public string Button { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        public override string ToString() {
            return $"{Button} button clicked at ({X}, {Y})";
        }
    }
}
