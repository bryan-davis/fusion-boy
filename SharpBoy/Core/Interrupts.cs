/*
 * Copyright 2014 Bryan Davis
 * Licensed under GPLv2
 * Refer to the license.txt file included
 */

using System;

namespace SharpBoy.Core
{
    [Flags]
    public enum Interrupts : byte
    {
        // Bit positions, not bit values
        vBlank,
        lcdStat,
        timer,
        serial,
        joypad
    }
}
