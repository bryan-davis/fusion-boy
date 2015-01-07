using SharpBoy.Cartridge;

namespace SharpBoy.Core
{
    class Util
    {
        public static bool IsBitSet(byte value, byte bit)
        {
            return (value & (1 << bit)) == (1 << bit);
        }

        public static void SetBit(ref byte value, byte bit)
        {
            value |= (byte)(1 << bit);
        }

        public static void SetBit(MemoryBankController memory, ushort address, byte bit)
        {
            memory[address] |= (byte)(1 << bit);
        }

        public static void ClearBit(ref byte value, byte bit)
        {
            value &= (byte)~(1 << bit);
        }

        public static void ClearBit(MemoryBankController memory, ushort address, byte bit)
        {
            memory[address] &= (byte)~(1 << bit);
        }
    }
}
