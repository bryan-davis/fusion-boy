/*
 * Copyright 2014 Bryan Davis
 * Licensed under GPLv2
 * Refer to the license.txt file included
 */

using System;

namespace FusionBoy.Core
{
    public enum Interrupts : byte
    {
        // Bit positions, not bit values
        VBlank = 0,
        LcdStat = 1,
        Timer = 2,
        Serial = 3,
        Joypad = 4
    }
}
