/*
 * Copyright 2014 Bryan Davis
 * Licensed under GPLv2
 * Refer to the license.txt file included
 */

using System;
using System.IO;

namespace SharpBoy.Cartridge
{
    public class MBC2 : MemoryBankController
    {
        public MBC2(Stream fileStream) : base(fileStream)
        {
            // The first 16K (MBC0) is always read into the beginning of memory
            Array.Copy(cartridge, data, 0x4000);
        }

        public override byte this[int address]
        {
            get
            {
                throw new System.NotImplementedException();
            }
            set
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
