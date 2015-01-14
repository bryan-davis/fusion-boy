/*
 * Copyright 2014 Bryan Davis
 * Licensed under GPLv2
 * Refer to the license.txt file included
 */

using SharpBoy.Cartridge;
using System;

namespace SharpBoy.Core
{
    static class Util
    {
        public static bool IsBitSet(byte value, byte bit)
        {
            return (value & (1 << bit)) == (1 << bit);
        }

        public static bool IsBitSet(MemoryBankController memory, ushort address, byte bit)
        {
            return (memory[address] & (1 << bit)) == (1 << bit);
        }

        public static void SetBits(ref byte value, params byte[] bits)
        {
            foreach (var bit in bits)
            {
                value |= (byte)(1 << bit);
            }
        }

        public static void SetBits(MemoryBankController memory, ushort address, params byte[] bits)
        {
            foreach (var bit in bits)
            {
                memory[address] |= (byte)(1 << bit);
            }            
        }

        public static void ClearBits(ref byte value, params byte[] bits)
        {
            foreach (var bit in bits)
            {
                value &= (byte)~(1 << bit);
            }             
        }

        public static void ClearBits(MemoryBankController memory, ushort address, params byte[] bits)
        {
            foreach (var bit in bits)
            {
                memory[address] &= (byte)~(1 << bit);
            }            
        }

        public static int GetBitValue(byte value, byte bit)
        {
            return IsBitSet(value, bit) ? 1 : 0;
        }

        // Converts a 2d screen coordinate to the 1d index
        // that the screen data is actually stored in
        public static int Convert2dTo1d(int x, int y, int width)
        {
            return (y * width) + x;
        }
    }
}
