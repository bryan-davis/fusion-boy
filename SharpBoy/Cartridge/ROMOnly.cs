using System;
using System.IO;

namespace SharpBoy.Cartridge
{
    public class ROMOnly : MemoryBankController
    {
        public ROMOnly(Stream fileStream) : base(fileStream)
        {
            // Copy the entire cartidge into ROM space
            Array.Copy(cartridge, Memory, 0x8000);
        }

        public override byte this[int address]
        {
            get
            {
                return Memory[address];
            }
            set
            {
                if (IsUnsableRegion(address))
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
