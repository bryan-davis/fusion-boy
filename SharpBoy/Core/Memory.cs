using System;

namespace SharpBoy.Core
{
    public class Memory
    {
        private const int memorySize = 0x10000; // 65536 - 64KiB
        
        public byte[] Data { get; private set; }

        public Memory()
        {
            Data = new byte[memorySize];
        }

        // http://problemkaputt.de/pandocs.htm#powerupsequence
        public void Reset()
        {
            Data[0xFF05] = 0x00;
            Data[0xFF06] = 0x00;
            Data[0xFF07] = 0x00;
            Data[0xFF10] = 0x80;
            Data[0xFF11] = 0xBF;
            Data[0xFF12] = 0xF3;
            Data[0xFF14] = 0xBF;
            Data[0xFF16] = 0x3F;
            Data[0xFF17] = 0x00;
            Data[0xFF19] = 0xBF;
            Data[0xFF1A] = 0x7F;
            Data[0xFF1B] = 0xFF;
            Data[0xFF1C] = 0x9F;
            Data[0xFF1E] = 0xBF;
            Data[0xFF20] = 0xFF;
            Data[0xFF21] = 0x00;
            Data[0xFF22] = 0x00;
            Data[0xFF23] = 0xBF;
            Data[0xFF24] = 0x77;
            Data[0xFF25] = 0xF3;
            Data[0xFF26] = 0xF1;
            Data[0xFF40] = 0x91;
            Data[0xFF42] = 0x00;
            Data[0xFF43] = 0x00;
            Data[0xFF45] = 0x00;
            Data[0xFF47] = 0xFC;
            Data[0xFF48] = 0xFF;
            Data[0xFF49] = 0xFF;
            Data[0xFF4A] = 0x00;
            Data[0xFF4B] = 0x00;
            Data[0xFFFF] = 0x00;
        }

        // http://problemkaputt.de/pandocs.htm#memorymap
        public byte this[int address]
        {
            get { return Data[address]; }
            set
            {
                if (IsROM(address))
                {
                    return;
                }
                else if (IsUnsableRegion(address))
                {
                    return;
                }
            }
        }

        // 0x0000 - 0x7FFF is read-only memory for the cartridge
        private bool IsROM(int address)
        {
            return address < 0x8000;
        }

        private bool IsUnsableRegion(int address)
        {
            return address >= 0xFEA0 && address <= 0xFEFF;
        }
    }
}
