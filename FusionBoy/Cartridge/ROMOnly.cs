/*
 * Copyright 2014 Bryan Davis
 * Licensed under GPLv2
 * Refer to the license.txt file included
 */

using System;
using System.IO;

namespace FusionBoy.Cartridge
{
    public class RomOnly : MemoryBankController
    {
        public RomOnly(Stream fileStream) : base(fileStream)
        {
            // Copy the entire cartidge into ROM space
            Array.Copy(cartridge, data, 0x8000);
        }
    }
}
