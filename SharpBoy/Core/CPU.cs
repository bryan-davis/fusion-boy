using SharpBoy.Cartridge;
using System;
using System.Collections.Generic;
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

        public byte ReadNextOpCode()
        {
            byte opCode = Memory[ProgramCounter++];
            return opCode;
        }

        public void ExecuteOpCode(byte opCode)
        {

        }

        // Power-up sequence, reset registers and memory
        // http://problemkaputt.de/pandocs.htm#powerupsequence
        private void Reset()
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

        // TODO: Re-evaluate this after core op codes have been implemented.
        // Maps the cycle count that each op code takes.
        public readonly Dictionary<int, int> CycleMap = new Dictionary<int, int>()
        {
            { 0x01,   12 }, { 0x02,   8  }, { 0x03,   8  }, { 0x04,   4  }, { 0x05,   4  }, 
            { 0x06,   8  }, { 0x08,   20 }, { 0x09,   8  }, { 0x0A,   8  }, { 0x0B,   8  }, 
            { 0x0C,   4  }, { 0x0D,   4  }, { 0x0E,   8  }, { 0x11,   12 }, { 0x12,   8  }, 
            { 0x13,   8  }, { 0x14,   4  }, { 0x15,   4  }, { 0x16,   8  }, { 0x18,   8  }, 
            { 0x19,   8  }, { 0x1A,   8  }, { 0x1B,   8  }, { 0x1C,   4  }, { 0x1D,   4  }, 
            { 0x1E,   8  }, { 0x20,   8  }, { 0x21,   12 }, { 0x22,   8  }, { 0x23,   8  }, 
            { 0x24,   4  }, { 0x25,   4  }, { 0x26,   8  }, { 0x28,   8  }, { 0x29,   8  }, 
            { 0x2A,   8  }, { 0x2B,   8  }, { 0x2C,   4  }, { 0x2D,   4  }, { 0x2E,   8  }, 
            { 0x30,   8  }, { 0x31,   12 }, { 0x32,   8  }, { 0x33,   8  }, { 0x34,   12 }, 
            { 0x35,   12 }, { 0x36,   12 }, { 0x38,   8  }, { 0x39,   8  }, { 0x3A,   8  }, 
            { 0x3B,   8  }, { 0x3C,   4  }, { 0x3D,   4  }, { 0x3E,   8  }, { 0x40,   4  }, 
            { 0x41,   4  }, { 0x42,   4  }, { 0x43,   4  }, { 0x44,   4  }, { 0x45,   4  }, 
            { 0x46,   8  }, { 0x47,   4  }, { 0x48,   4  }, { 0x49,   4  }, { 0x4A,   4  }, 
            { 0x4B,   4  }, { 0x4C,   4  }, { 0x4D,   4  }, { 0x4E,   8  }, { 0x4F,   4  }, 
            { 0x50,   4  }, { 0x51,   4  }, { 0x52,   4  }, { 0x53,   4  }, { 0x54,   4  }, 
            { 0x55,   4  }, { 0x56,   8  }, { 0x57,   4  }, { 0x58,   4  }, { 0x59,   4  }, 
            { 0x5A,   4  }, { 0x5B,   4  }, { 0x5C,   4  }, { 0x5D,   4  }, { 0x5E,   8  }, 
            { 0x5F,   4  }, { 0x60,   4  }, { 0x61,   4  }, { 0x62,   4  }, { 0x63,   4  }, 
            { 0x64,   4  }, { 0x65,   4  }, { 0x66,   8  }, { 0x67,   4  }, { 0x68,   4  }, 
            { 0x69,   4  }, { 0x6A,   4  }, { 0x6B,   4  }, { 0x6C,   4  }, { 0x6D,   4  }, 
            { 0x6E,   8  }, { 0x6F,   4  }, { 0x70,   8  }, { 0x71,   8  }, { 0x72,   8  }, 
            { 0x73,   8  }, { 0x74,   8  }, { 0x75,   8  }, { 0x77,   8  }, { 0x78,   4  }, 
            { 0x79,   4  }, { 0x7A,   4  }, { 0x7B,   4  }, { 0x7C,   4  }, { 0x7D,   4  }, 
            { 0x7E,   8  }, { 0x7F,   4  }, { 0x80,   4  }, { 0x81,   4  }, { 0x82,   4  }, 
            { 0x83,   4  }, { 0x84,   4  }, { 0x85,   4  }, { 0x86,   8  }, { 0x87,   4  }, 
            { 0x88,   4  }, { 0x89,   4  }, { 0x8A,   4  }, { 0x8B,   4  }, { 0x8C,   4  }, 
            { 0x8D,   4  }, { 0x8E,   8  }, { 0x8F,   4  }, { 0x90,   4  }, { 0x91,   4  }, 
            { 0x92,   4  }, { 0x93,   4  }, { 0x94,   4  }, { 0x95,   4  }, { 0x96,   8  }, 
            { 0x97,   4  }, { 0x98,   4  }, { 0x99,   4  }, { 0x9A,   4  }, { 0x9B,   4  }, 
            { 0x9C,   4  }, { 0x9D,   4  }, { 0x9E,   8  }, { 0x9F,   4  }, { 0xA0,   4  }, 
            { 0xA1,   4  }, { 0xA2,   4  }, { 0xA3,   4  }, { 0xA4,   4  }, { 0xA5,   4  }, 
            { 0xA6,   8  }, { 0xA7,   4  }, { 0xA8,   4  }, { 0xA9,   4  }, { 0xAA,   4  }, 
            { 0xAB,   4  }, { 0xAC,   4  }, { 0xAD,   4  }, { 0xAE,   8  }, { 0xAF,   4  }, 
            { 0xB0,   4  }, { 0xB1,   4  }, { 0xB2,   4  }, { 0xB3,   4  }, { 0xB4,   4  }, 
            { 0xB5,   4  }, { 0xB6,   8  }, { 0xB7,   4  }, { 0xB8,   4  }, { 0xB9,   4  }, 
            { 0xBA,   4  }, { 0xBB,   4  }, { 0xBC,   4  }, { 0xBD,   4  }, { 0xBE,   8  }, 
            { 0xBF,   4  }, { 0xC0,   8  }, { 0xC1,   12 }, { 0xC2,   12 }, { 0xC3,   12 }, 
            { 0xC4,   12 }, { 0xC5,   16 }, { 0xC6,   8  }, { 0xC7,   32 }, { 0xC8,   8  }, 
            { 0xC9,   8  }, { 0xCA,   12 }, { 0xCB00, 8  }, { 0xCB01, 8  }, { 0xCB02, 8  }, 
            { 0xCB03, 8  }, { 0xCB04, 8  }, { 0xCB05, 8  }, { 0xCB06, 16 }, { 0xCB07, 8  }, 
            { 0xCB08, 8  }, { 0xCB09, 8  }, { 0xCB0A, 8  }, { 0xCB0B, 8  }, { 0xCB0C, 8  }, 
            { 0xCB0D, 8  }, { 0xCB0E, 16 }, { 0xCB0F, 8  }, { 0xCB10, 8  }, { 0xCB11, 8  }, 
            { 0xCB12, 8  }, { 0xCB13, 8  }, { 0xCB14, 8  }, { 0xCB15, 8  }, { 0xCB16, 16 }, 
            { 0xCB17, 8  }, { 0xCB18, 8  }, { 0xCB19, 8  }, { 0xCB1A, 8  }, { 0xCB1B, 8  }, 
            { 0xCB1C, 8  }, { 0xCB1D, 8  }, { 0xCB1E, 16 }, { 0xCB1F, 8  }, { 0xCB20, 8  }, 
            { 0xCB21, 8  }, { 0xCB22, 8  }, { 0xCB23, 8  }, { 0xCB24, 8  }, { 0xCB25, 8  }, 
            { 0xCB26, 16 }, { 0xCB27, 8  }, { 0xCB28, 8  }, { 0xCB29, 8  }, { 0xCB2A, 8  }, 
            { 0xCB2B, 8  }, { 0xCB2C, 8  }, { 0xCB2D, 8  }, { 0xCB2E, 16 }, { 0xCB2F, 8  }, 
            { 0xCB38, 8  }, { 0xCB39, 8  }, { 0xCB3A, 8  }, { 0xCB3B, 8  }, { 0xCB3C, 8  }, 
            { 0xCB3D, 8  }, { 0xCB3E, 16 }, { 0xCB3F, 8  }, { 0xCB40, 8  }, { 0xCB41, 8  }, 
            { 0xCB42, 8  }, { 0xCB43, 8  }, { 0xCB44, 8  }, { 0xCB45, 8  }, { 0xCB46, 16 }, 
            { 0xCB47, 8  }, { 0xCB80, 8  }, { 0xCB81, 8  }, { 0xCB82, 8  }, { 0xCB83, 8  }, 
            { 0xCB84, 8  }, { 0xCB85, 8  }, { 0xCB86, 16 }, { 0xCB87, 8  }, { 0xCBC0, 8  }, 
            { 0xCBC1, 8  }, { 0xCBC2, 8  }, { 0xCBC3, 8  }, { 0xCBC4, 8  }, { 0xCBC5, 8  }, 
            { 0xCBC6, 16 }, { 0xCBC7, 8  }, { 0xCC,   12 }, { 0xCD,   12 }, { 0xCE,   8  }, 
            { 0xCF,   32 }, { 0xD0,   8  }, { 0xD1,   12 }, { 0xD2,   12 }, { 0xD4,   12 }, 
            { 0xD5,   16 }, { 0xD6,   8  }, { 0xD7,   32 }, { 0xD8,   8  }, { 0xD9,   8  }, 
            { 0xDA,   12 }, { 0xDC,   12 }, { 0xDF,   32 }, { 0xE0,   12 }, { 0xE1,   12 }, 
            { 0xE2,   8  }, { 0xE5,   16 }, { 0xE6,   8  }, { 0xE7,   32 }, { 0xE8,   16 }, 
            { 0xE9,   4  }, { 0xEA,   16 }, { 0xEE,   8  }, { 0xEF,   32 }, { 0xF0,   12 }, 
            { 0xF1,   12 }, { 0xF2,   8  }, { 0xF5,   16 }, { 0xF6,   8  }, { 0xF7,   32 }, 
            { 0xF8,   12 }, { 0xF9,   8  }, { 0xFA,   16 }, { 0xFE,   8  }, { 0xFF,   32 }
        };
    }
}
