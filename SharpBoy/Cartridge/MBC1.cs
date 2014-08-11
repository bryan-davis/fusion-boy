using System;
using System.IO;
namespace SharpBoy.Cartridge
{
    public class MBC1 : MemoryBankController
    {
        public MBC1(Stream fileStream) : base(fileStream)
        {
            // The first 16K (MBC0) is always read into the beginning of memory
            Array.Copy(cartridge, Memory, 0x4000);
        }

        public override byte this[int address]
        {
            get
            {
                if (IsROMBankRegion(address))
                {
                    int offsetAddress = address - 0x4000;
                    int bankAddress = offsetAddress + (CurrentROMBank * 0x4000);
                    return cartridge[bankAddress];
                }
                else
                {
                    return Memory[address];
                }
            }
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
                else if (IsEchoRegion(address))
                {
                    Memory[address] = value;
                    // Writing to the echo region also writes to an offset of 0xC000 - 0xDDFF
                    Memory[address - 0x2000] = value;
                }
                else
                {
                    Memory[address] = value;
                }
            }
        }
    }
}
