/*
 * Copyright 2014 Bryan Davis
 * Licensed under GPLv2
 * Refer to the license.txt file included
 */

using System;
using System.IO;
namespace FusionBoy.Cartridge
{
    // http://problemkaputt.de/pandocs.htm#mbc1max2mbyteromandor32kbyteram
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
                    int bankAddress = (address - 0x4000) + (CurrentRomBank * 0x4000);
                    return cartridge[bankAddress];
                }
                else if (IsRamBankRegion(address) && ExternalRamEnabled)
                {
                    int bankAddress = (address - 0xA000) + (CurrentRamBank * 0x2000);
                    return ramBank[bankAddress];
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
                    ExternalRamEnabled = ((value & 0x0F) == 0x0A);
                }
                else if (IsRomBankSwitchingRegion(address))
                {
                    int bankNumber = value & 0x1F;  // Clear the top bits
                    if (bankNumber == 0)
                    {
                        CurrentRomBank = 1;
                    }
                    else
                    {
                        CurrentRomBank &= 0xE0; // Clear the bottom 5 bits
                        CurrentRomBank = (byte)(CurrentRomBank | bankNumber);
                    }
                }
                else if (IsRamBankRegion(address) && ExternalRamEnabled)
                {
                    int bankAddress = (address - 0xA000) + (CurrentRamBank * 0x2000);
                    ramBank[bankAddress] = value;
                }
                else if (IsUpperBankSwitchingRegion(address))
                {
                    if (BankMode == BankModes.Ram)
                    {
                        CurrentRamBank = (byte)(value & 0x03);
                    }
                    else
                    {
                        UpdateRomBank(value & 0x03);
                    }
                }
                else if (IsRomRamModeRegion(address))
                {
                    value &= 0x01;
                    if (value == 0)
                    {
                        BankMode = BankModes.Rom;
                    }
                    else
                    {
                        BankMode = BankModes.Ram;
                        CurrentRamBank = 0;
                    }
                }
                else
	            {
                    base[address] = value;
	            }
            }
        }

        private void UpdateRomBank(int value)
        {
            CurrentRomBank = (byte)(CurrentRomBank | (value << 5));
            if (CurrentRomBank == 0x20 || CurrentRomBank == 0x40 || CurrentRomBank == 0x60)
            {
                CurrentRomBank++;
            }
        }
    }
}
