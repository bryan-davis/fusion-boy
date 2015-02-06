/*
 * Copyright 2014 Bryan Davis
 * Licensed under GPLv2
 * Refer to the license.txt file included
 */

using System;
using System.IO;
namespace SharpBoy.Cartridge
{
    public class MBC1 : MemoryBankController
    {
        public MBC1(Stream fileStream) : base(fileStream)
        {
            // The first 16K (MBC0) is always read into the beginning of memory
            Array.Copy(cartridge, data, 0x4000);
        }

        public override byte this[int address]
        {
            get
            {
                if (IsRomBankRegion(address))
                {
                    int offsetAddress = address - 0x4000;
                    int bankAddress = offsetAddress + (CurrentRomBank * 0x4000);
                    return cartridge[bankAddress];
                }                
                else
                {
                    return base[address];
                }
            }
            set
            {
                if (IsRamEnableRegion(address))
                {
                    ExternalRamEnabled = (value & 0x0F) == 0x0A;
                }
                else if (IsRomRamModeRegion(address))
                {
                    if (value == 0 || value == 1)
                    {
                        BankMode = (BankModes)value;
                    }
                }
                else
	            {
                    base[address] = value;
	            }
            }
        }
    }
}
