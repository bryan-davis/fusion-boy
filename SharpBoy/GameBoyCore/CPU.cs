using System;

namespace SharpBoy.GameBoyCore
{
    [Serializable]
    public class CPU
    {
        private const int memorySize = 0x10000; // 65536 - 64KiB

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

        public byte[] Memory { get; private set; }

        public CPU()
        {
            RegisterAF = new Register();
            RegisterBC = new Register();
            RegisterDE = new Register();
            RegisterHL = new Register();
            StackPointer = 0;
            ProgramCounter = 0;
            Memory = new byte[memorySize];
        }

        // http://problemkaputt.de/pandocs.htm#powerupsequence
        public void Reset()
        {
            RegisterAF.Value = 0x01B0;
            RegisterBC.Value = 0x0013;
            RegisterBC.Value = 0x00D8;
            RegisterBC.Value = 0x014D;
            
            StackPointer = 0xFFFE;
            ProgramCounter = 0x100;

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
    }
}
