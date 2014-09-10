namespace SharpBoy.Core
{
    public class Interrupts
    {
        public bool VBlankEnabled { get; set; }
        public bool LCDEnabled { get; set; }
        public bool TimerEnabled { get; set; }
        public bool SerialEnabled { get; set; }
        public bool JoypadEnabled { get; set; }

        public bool VBlankRequested { get; set; }
        public bool LCDRequested { get; set; }
        public bool TimerRequested { get; set; }
        public bool SerialRequested { get; set; }
        public bool JoypadRequested { get; set; }
    }
}
