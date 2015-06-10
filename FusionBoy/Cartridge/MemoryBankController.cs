/*
 * Copyright 2014 Bryan Davis
 * Licensed under GPLv2
 * Refer to the license.txt file included
 */

using FusionBoy.Core;
using System;
using System.IO;

using System.Diagnostics;

namespace FusionBoy.Cartridge
{
    public abstract class MemoryBankController
    {  
        protected byte[] data;
        protected byte[] cartridge;
        protected byte[] ramBank;
        
        public byte CurrentRomBank { get; set; }
        public byte CurrentRamBank { get; set; }
        protected BankModes BankMode { get; set; }
        public bool ExternalRamEnabled { get; set; }

        public event Action<byte> UpdateTimerHandler;

        protected enum BankModes
        {
            Rom = 0,
            Ram = 1
        }

        protected MemoryBankController(Stream fileStream)
        {
            cartridge = new byte[fileStream.Length];
            fileStream.Read(cartridge, 0x0, cartridge.Length);

            data = new byte[64 * 1024];
            ramBank = new byte[32 * 1024];

            CurrentRomBank = 1;
            CurrentRamBank = 0;

            BankMode = BankModes.Rom;
            ExternalRamEnabled = false;
        }

        // http://problemkaputt.de/pandocs.htm#powerupsequence
        public void Reset()
        {
            data[0xFF05] = 0x00;
            data[0xFF06] = 0x00;
            data[0xFF07] = 0x00;
            data[0xFF10] = 0x80;
            data[0xFF11] = 0xBF;
            data[0xFF12] = 0xF3;
            data[0xFF14] = 0xBF;
            data[0xFF16] = 0x3F;
            data[0xFF17] = 0x00;
            data[0xFF19] = 0xBF;
            data[0xFF1A] = 0x7F;
            data[0xFF1B] = 0xFF;
            data[0xFF1C] = 0x9F;
            data[0xFF1E] = 0xBF;
            data[0xFF20] = 0xFF;
            data[0xFF21] = 0x00;
            data[0xFF22] = 0x00;
            data[0xFF23] = 0xBF;
            data[0xFF24] = 0x77;
            data[0xFF25] = 0xF3;
            data[0xFF26] = 0xF1;
            data[0xFF40] = 0x91;
            data[0xFF42] = 0x00;
            data[0xFF43] = 0x00;
            data[0xFF45] = 0x00;
            data[0xFF47] = 0xFC;
            data[0xFF48] = 0xFF;
            data[0xFF49] = 0xFF;
            data[0xFF4A] = 0x00;
            data[0xFF4B] = 0x00;
            data[0xFFFF] = 0x00;
        }

        // http://problemkaputt.de/pandocs.htm#memorymap
        public virtual byte this[int address] 
        { 
            get
            {
                if (address == 0xFF00)
                {
                    return GetJoypadInput();
                }
                else
                {
                    return data[address];
                }
            }
            set
            {
                if (IsRom(address) || IsUnusableRegion(address))
                {
                    return;
                }
                else if (IsEchoRegion(address))
                {
                    data[address] = value;
                    // Writing to the echo region also writes to an offset of 0xC000 - 0xDDFF
                    data[address - 0x2000] = value;
                }
                else if (address == Util.DividerAddress)
                {
                    data[address] = 0;
                }
                else if (address == Util.TimerControlAddress)
                {
                    data[address] = value;

                    if (UpdateTimerHandler != null)
                        UpdateTimerHandler(value);
                }
                else if (address == Util.ScanlineAddress)
                {
                    data[address] = 0;
                }
                else if (address == Util.DmaAddress)
                {
                    PerformDmaTransfer(value);
                }
                else if (address == Util.InterruptEnableAddress)
                {
                    data[address] = (byte)(value & 0x1F);
                }
                else if (address == Util.InterruptFlagAddress)
                {
                    data[address] = (byte)(value & 0x1F);
                }
                else if (address == Util.TimerControlAddress)
                {
                    data[address] = (byte)(value & 0x07);
                }
                else
                {
                    data[address] = value;
                }
            }
        }

        // Writing directly to the divider address or LCD address will reset 
        // them to 0, so we need dedicated methods to increment them.
        public void IncrementDividerRegister()
        {
            data[Util.DividerAddress]++;
        }

        public void IncrementLcdScanline()
        {
            data[Util.ScanlineAddress]++;
        }

        // http://problemkaputt.de/pandocs.htm#joypadinput
        // Joypad input is odd/inverted because 0 means something is selected 
        // or pressed, instead of 1.
        protected byte GetJoypadInput()
        {
            byte joypad = data[0xFF00];
            // Invert the bits
            joypad ^= 0xFF;

            // TODO: Implement proper joypad state
            byte joypadState = 0xFF;            

            // Looking for direction keys
            if (!Util.IsBitSet(joypad, 4))
            {
                byte buttons = (byte)(joypadState >> 4);
                buttons |= 0xF0;
                joypad &= buttons;
            }
            // Looking for button keys
            else if (!Util.IsBitSet(joypad, 5))
            {
                byte buttons = (byte)(joypadState & 0x0F);
                buttons |= 0xF0;
                joypad &= buttons;
            }

            return joypad;
        }

        // http://problemkaputt.de/pandocs.htm#lcdoamdmatransfers
        protected void PerformDmaTransfer(int sourceAddress)
        {
            ushort address = (ushort)(sourceAddress * 0x100);
            // Copying 160 bytes over
            for (int i = 0; i < 160; i++)
            {
                data[0xFE00 + i] = data[address + i];
            }
        }

        protected byte ReadCartridge(int address)
        {
            if (address >= cartridge.Length)
            {
                address %= cartridge.Length;
            }
            return cartridge[address];
        }

        protected byte ReadRamBank(int address)
        {
            if (address >= ramBank.Length)
            {
                address %= ramBank.Length;
            }
            return ramBank[address];
        }

        protected void WriteRamBank(int address, byte value)
        {
            if (address >= ramBank.Length)
            {
                address %= ramBank.Length;
            }
            ramBank[address] = value;
        }

        protected bool IsRom(int address)
        {
            return address < 0x8000;
        }

        protected bool IsRomBankRegion(int address)
        {
            return 0x4000 <= address && address <= 0x7FFF;
        }

        protected bool IsRamBankRegion(int address)
        {
            return 0xA000 <= address && address <= 0xBFFF;
        }

        protected bool IsRomBankSwitchingRegion(int address)
        {
            return 0x2000 <= address && address <= 0x3FFF;
        }

        protected bool IsUpperBankSwitchingRegion(int address)
        {
            return 0x4000 <= address && address <= 0x5FFF;
        }

        protected bool IsRamEnableRegion(int address)
        {
            return 0x0000 <= address && address <= 0x1FFF;
        }

        protected bool IsUnusableRegion(int address)
        {
            return 0xFEA0 <= address && address <= 0xFEFF;
        }

        protected bool IsEchoRegion(int address)
        {
            return 0xE000 <= address && address <= 0xFDFF;
        }

        protected bool IsRomRamModeRegion(int address)
        {
            return 0x6000 <= address && address <= 0x7FFF;
        }
    }
}
