using System;
using System.IO;

namespace SharpBoy.GameBoyCore
{
    [Serializable]
    public class CPU
    {
        private CartType cartType;
        
        public Register RegisterAF { get; private set; }
        public Register RegisterBC { get; private set; }
        public Register RegisterDE { get; private set; }
        public Register RegisterHL { get; private set; }
        public ushort StackPointer { get; private set; }
        public ushort ProgramCounter { get; private set; }

        // Flags; http://problemkaputt.de/pandocs.htm#cpuregistersandflags
        public const int FlagZ = 1 << 7;    // Zero Flag
        public const int FlagN = 1 << 6;    // Add/Sub-Flag
        public const int FlagH = 1 << 5;    // Half Carry Flag            
        public const int FlagC = 1 << 4;    // Carry Flag

        public Memory Memory { get; private set; }

        public CPU()
        {
            RegisterAF = new Register();
            RegisterBC = new Register();
            RegisterDE = new Register();
            RegisterHL = new Register();
            StackPointer = 0;
            ProgramCounter = 0;
            Memory = new Memory();
        }

        public void LoadRom(string romPath)
        {
            Reset();
            using (Stream fileStream = File.OpenRead(romPath))
            {
                // The first 16K is always read into the beginning of memory
                fileStream.Read(Memory.Data, 0x00, 0x4000);
                cartType = ReadCartType();
                int romSize = ReadRomSize();
                int delete = 0;
            }
        }

        // Power-up sequence, reset registers and memory
        // http://problemkaputt.de/pandocs.htm#powerupsequence
        public void Reset()
        {
            RegisterAF.Value = 0x01B0;
            RegisterBC.Value = 0x0013;
            RegisterBC.Value = 0x00D8;
            RegisterBC.Value = 0x014D;
            
            StackPointer = 0xFFFE;
            ProgramCounter = 0x100;

            Memory.Reset();
        }

        private CartType ReadCartType()
        {
            CartType type;
            // Cart type is locate as 0x147
            // http://problemkaputt.de/pandocs.htm#thecartridgeheader
            byte value = Memory[0x147];
            switch (value)
            {
                case 0x00: 
                    type = CartType.RomOnly; break;
                case 0x01:
                case 0x02:
                case 0x03: 
                    type = CartType.MBC1; break;
                case 0x05:
                case 0x06:
                    type = CartType.MBC2; break;
                case 0x08:
                case 0x09:
                    type = CartType.RomOnly; break;
                case 0x0F:
                case 0x10:
                case 0x11:
                case 0x12:
                case 0x13:
                    type = CartType.MBC3; break;
                case 0xFF:
                    type = CartType.HuC1; break;
                default:
                    type = CartType.Unknown; break;
            }

            return type;
        }

        private int ReadRomSize()
        {
            int romSize = 0;
            const int BankSize = 16384;   // 16K

            switch (Memory[0x148])
            {
                case 0x00: 
                    romSize = 2 * BankSize; break;   // 32K
                case 0x01: 
                    romSize = 4 * BankSize; break;   // 64K
                case 0x02: 
                    romSize = 8 * BankSize; break;   // 128K
                case 0x03: 
                    romSize = 16 * BankSize; break;  // 256K
                case 0x04: 
                    romSize = 32 * BankSize; break;  // 512K
                case 0x05: 
                    romSize = 64 * BankSize; break;  // 1M
                case 0x06: 
                    romSize = 128 * BankSize; break; // 2M
                case 0x52: 
                    romSize = 72 * BankSize; break;  // 1.1M
                case 0x53: 
                    romSize = 80 * BankSize; break;  // 1.2M
                case 0x54: 
                    romSize = 96 * BankSize; break;  // 1.5M
                default:
                    break;
            }

            return romSize;
        }
    }
}
