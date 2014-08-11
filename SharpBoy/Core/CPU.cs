using SharpBoy.Cartridge;
using System;
using System.IO;

namespace SharpBoy.Core
{
    [Serializable]
    public class CPU
    {
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

        public MemoryBankController Memory { get; private set; }
        public CartridgeInfo CartInfo { get; private set; }

        public CPU()
        {
            RegisterAF = new Register();
            RegisterBC = new Register();
            RegisterDE = new Register();
            RegisterHL = new Register();
            StackPointer = 0;
            ProgramCounter = 0;            
        }

        public void LoadRom(string romPath)
        {
            byte[] cartHeader = new byte[0x150];    // Header is 336 bytes
            using (Stream fileStream = File.OpenRead(romPath))
            {
                fileStream.Read(cartHeader, 0x0, cartHeader.Length);
                CartInfo = new CartridgeInfo(cartHeader);
                fileStream.Seek(0, SeekOrigin.Begin);
                Memory = CreateMBC(CartInfo.CartType, fileStream);
            }
            Reset();
        }

        // Power-up sequence, reset registers and memory
        // http://problemkaputt.de/pandocs.htm#powerupsequence
        public void Reset()
        {
            RegisterAF.Value = 0x01B0;
            RegisterBC.Value = 0x0013;
            RegisterDE.Value = 0x00D8;
            RegisterHL.Value = 0x014D;
            
            StackPointer = 0xFFFE;
            ProgramCounter = 0x100;

            Memory.Reset();
        }

        private MemoryBankController CreateMBC(CartType cartType, Stream fileStream)
        {
            MemoryBankController mbc = null;

            switch (cartType)
            {
                case CartType.RomOnly:
                    mbc = new ROMOnly(fileStream);
                    break;
                case CartType.MBC1:
                    mbc = new MBC1(fileStream);
                    break;
                case CartType.MBC2:
                    mbc = new MBC2(fileStream);
                    break;
            }

            return mbc;
        }
    }
}
