using System;
using System.IO;

namespace SharpBoy.Cartridge
{
    public abstract class MemoryBankController
    {
        protected const int memorySize = 0x10000; // 65536 - 64K
        protected byte[] cartridge;
        
        public byte[] Memory { get; private set; }
        public byte CurrentROMBank { get; set; }
        public byte CurrentRAMBank { get; set; }
        public bool InROMBankMode { get; set; }
        public bool ExternalRAMEnabled { get; set; }

        protected MemoryBankController(Stream fileStream)
        {            
            CurrentROMBank = 1;
            CurrentRAMBank = 0;

            cartridge = new byte[fileStream.Length];
            fileStream.Read(cartridge, 0x0, cartridge.Length);

            Memory = new byte[memorySize];
        }

        // http://problemkaputt.de/pandocs.htm#powerupsequence
        public void Reset()
        {
            Memory[0xFF05] = 0x00;
            Memory[0xFF06] = 0x00;
            Memory[0xFF07] = 0x00;
            Memory[0xFF10] = 0x80;
            Memory[0xFF11] = 0xBF;
            Memory[0xFF12] = 0xF3;
            Memory[0xFF14] = 0xBF;
            Memory[0xFF16] = 0x3F;
            Memory[0xFF17] = 0x00;
            Memory[0xFF19] = 0xBF;
            Memory[0xFF1A] = 0x7F;
            Memory[0xFF1B] = 0xFF;
            Memory[0xFF1C] = 0x9F;
            Memory[0xFF1E] = 0xBF;
            Memory[0xFF20] = 0xFF;
            Memory[0xFF21] = 0x00;
            Memory[0xFF22] = 0x00;
            Memory[0xFF23] = 0xBF;
            Memory[0xFF24] = 0x77;
            Memory[0xFF25] = 0xF3;
            Memory[0xFF26] = 0xF1;
            Memory[0xFF40] = 0x91;
            Memory[0xFF42] = 0x00;
            Memory[0xFF43] = 0x00;
            Memory[0xFF45] = 0x00;
            Memory[0xFF47] = 0xFC;
            Memory[0xFF48] = 0xFF;
            Memory[0xFF49] = 0xFF;
            Memory[0xFF4A] = 0x00;
            Memory[0xFF4B] = 0x00;
            Memory[0xFFFF] = 0x00;
        }

        // http://problemkaputt.de/pandocs.htm#memorymap
        public abstract byte this[int address] { get; set; }

        protected bool IsROM(int address)
        {
            return address < 0x8000;
        }

        protected bool IsROMBankRegion(int address)
        {
            return address >= 0x4000 && address <= 0x7FFF;
        }

        protected bool IsBankSwitchingRegion(int address)
        {
            return address >= 0x2000 && address <= 0x3FFF;
        }

        protected bool IsUnsableRegion(int address)
        {
            return address >= 0xFEA0 && address <= 0xFEFF;
        }

        protected bool IsEchoRegion(int address)
        {
            return address >= 0xE000 && address <= 0xFDFF;
        }
    }
}
