using System;
using System.IO;

namespace SharpBoy.Cartridge
{
    public class ROMOnly : MemoryBankController
    {
        public ROMOnly(Stream fileStream) : base(fileStream)
        {
            // Copy the entire cartidge into ROM space
            Array.Copy(cartridge, data, 0x8000);
        }
    }
}
