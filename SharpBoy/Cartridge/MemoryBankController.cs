﻿/*
 * Copyright 2014 Bryan Davis
 * Licensed under GPLv2
 * Refer to the license.txt file included
 */

using SharpBoy.Core;
using System;
using System.IO;

using System.Diagnostics;

namespace SharpBoy.Cartridge
{
    public abstract class MemoryBankController
    {
        protected const int MemorySize = 0x10000; // 65536 - 64K
        protected const int DividerAddress = 0xFF04;
        protected const int ScanlineAddress = 0xFF44;
        protected byte[] data;
        protected byte[] cartridge;
        
        public byte CurrentROMBank { get; set; }
        public byte CurrentRAMBank { get; set; }
        public bool InROMBankMode { get; set; }
        public bool ExternalRAMEnabled { get; set; }

        public event Action<byte> UpdateTimerHandler;

        protected MemoryBankController(Stream fileStream)
        {            
            CurrentROMBank = 1;
            CurrentRAMBank = 0;

            cartridge = new byte[fileStream.Length];
            fileStream.Read(cartridge, 0x0, cartridge.Length);

            data = new byte[MemorySize];
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
                if (IsROM(address) || IsUnsableRegion(address))
                {
                    return;
                }
                else if (IsEchoRegion(address))
                {
                    data[address] = value;
                    // Writing to the echo region also writes to an offset of 0xC000 - 0xDDFF
                    data[address - 0x2000] = value;
                }
                else if (IsDividerRegister(address))
                {
                    data[address] = 0;
                }
                else if (IsTimerControl(address))
                {
                    data[address] = value;

                    if (UpdateTimerHandler != null)
                        UpdateTimerHandler(value);
                }
                else if (IsLCDRegister(address))
                {
                    data[address] = 0;
                }
                else if (IsDMAAddress(address))
                {
                    PerformDMATransfer(value);
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
            data[DividerAddress]++;
        }

        public void IncrementLCDScanline()
        {
            data[ScanlineAddress]++;
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
        protected void PerformDMATransfer(int sourceAddress)
        {
            ushort address = (ushort)(sourceAddress * 0x100);
            // Copying 160 bytes over
            for (int i = 0; i < 160; i++)
            {
                data[0xFE00 + i] = data[address + i];
            }
        }

        protected bool IsROM(int address)
        {
            return address < 0x8000;
        }

        protected bool IsROMBankRegion(int address)
        {
            return 0x4000 <= address && address <= 0x7FFF;
        }

        protected bool IsBankSwitchingRegion(int address)
        {
            return 0x2000 <= address && address <= 0x3FFF;
        }

        protected bool IsUnsableRegion(int address)
        {
            return 0xFEA0 <= address && address <= 0xFEFF;
        }

        protected bool IsEchoRegion(int address)
        {
            return 0xE000 <= address && address <= 0xFDFF;
        }

        protected bool IsDividerRegister(int address)
        {
            return address == DividerAddress;
        }

        protected bool IsLCDRegister(int address)
        {
            return address == ScanlineAddress;
        }

        protected bool IsTimerControl(int address)
        {
            return address == 0xFF07;
        }

        protected bool IsDMAAddress(int address)
        {
            return address == 0xFF46;
        }
    }
}
