using SharpBoy.Cartridge;
using System;
using System.IO;

namespace SharpBoy.Core
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
        public CartridgeInfo CartInfo { get; private set; }

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
                CartInfo = new CartridgeInfo(Memory);
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
    }
}
