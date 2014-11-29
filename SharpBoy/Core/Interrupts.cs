/*
 * Copyright 2014 Bryan Davis
 * Licensed under GPLv2
 * Refer to the license.txt file included
 */

namespace SharpBoy.Core
{
    public class Interrupts
    {
        // TODO: Re-evaluate
        // This whole interrupts implementation may cause funniness
        // depending on how games are reading and writing the interrupt
        // addesses 0xFF0F and 0xFFFF, given that I don't currently reset
        // the bits in memory after interrupts have been processed.
        public bool VBlankEnabled { get; set; }
        public bool LCDEnabled { get; set; }
        public bool TimerEnabled { get; set; }
        public bool JoypadEnabled { get; set; }

        public bool VBlankRequested { get; set; }
        public bool LCDRequested { get; set; }
        public bool TimerRequested { get; set; }
        public bool JoypadRequested { get; set; }

        public override string ToString()
        {
            return string.Format("VBlankEnabled = {0}, LCDEnabled = {1}, TimerEnabled = {2}, JoypadEnabled = {3}\n" +
                "VBlankRequested = {4}, LCDRequested = {5}, TimerRequested = {6}, JoypadRequested = {7}", VBlankEnabled,
                LCDEnabled, TimerEnabled, JoypadEnabled, VBlankRequested, LCDRequested, TimerRequested, JoypadRequested);            
        }
    }
}
