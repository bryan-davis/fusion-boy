using System;
using System.IO;
namespace SharpBoy.Cartridge
{
    public class MBC1 : MemoryBankController
    {
        public MBC1(Stream fileStream) : base(fileStream)
        {
            // The first 16K (MBC0) is always read into the beginning of memory
            Array.Copy(cartridge, data, 0x4000);
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
                    return base[address];
                }
            }
            set
            {
                base[address] = value;
            }
        }
    }
}
