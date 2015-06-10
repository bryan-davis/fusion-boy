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
    public class Mbc1 : MemoryBankController
    {
        public Mbc1(Stream fileStream) : base(fileStream)
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
                    if (BankMode == BankModes.Rom)
                    {
                        int bankAddress = (address & 0x3FFF) + (CurrentRomBank * 0x4000);
                        return ReadCartridge(bankAddress);
                    }
                    else
                    {
                        int bankAddress = (address & 0x3FFF) + ((CurrentRomBank & 0x1F) * 0x4000);
                        return ReadCartridge(bankAddress);
                    }
                }
                else if (IsRamBankRegion(address) && ExternalRamEnabled)
                {
                    if (BankMode == BankModes.Rom)
                    {                        
                        return ReadRamBank(address & 0x1FFF);
                    }
                    else
                    {
                        int bankAddress = (address & 0x1FFF) + (CurrentRamBank * 0x2000);
                        return ReadRamBank(bankAddress);
                    }
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
                    CurrentRomBank &= 0xE0; // Clear the bottom 5 bits
                    CurrentRomBank = (byte)(CurrentRomBank | bankNumber);
                    AdjustRomBank();
                }
                else if (IsRamBankRegion(address) && ExternalRamEnabled)
                {
                    if (BankMode == BankModes.Rom)
                    {
                        WriteRamBank(address & 0x1FFF, value);
                    }
                    else
                    {
                        int bankAddress = (address & 0x1FFF) + (CurrentRamBank * 0x2000);
                        WriteRamBank(bankAddress, value);
                    }
                }
                else if (IsUpperBankSwitchingRegion(address))
                {
                    if (BankMode == BankModes.Ram)
                    {
                        CurrentRamBank = (byte)(value & 0x03);
                    }
                    else
                    {
                        CurrentRomBank = (byte)(CurrentRomBank | (value << 5));
                        AdjustRomBank();
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

        private void AdjustRomBank()
        {
            if (CurrentRomBank == 0x00 || CurrentRomBank == 0x20 || CurrentRomBank == 0x40 || CurrentRomBank == 0x60)
            {
                CurrentRomBank++;
            }
        }
    }
}
