/*
 * Copyright 2014 Bryan Davis
 * Licensed under GPLv2
 * Refer to the license.txt file included
 */

using System;

namespace SharpBoy.Core
{
    public enum Interrupts : byte
    {
        // Bit positions, not bit values
        vBlank = 0,
        lcdStat = 1,
        timer = 2,
        serial = 3,
        joypad = 4
    }
}
