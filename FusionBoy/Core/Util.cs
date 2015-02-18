/*
 * Copyright 2014 Bryan Davis
 * Licensed under GPLv2
 * Refer to the license.txt file included
 */

using FusionBoy.Cartridge;
using System;

namespace FusionBoy.Core
{
    static class Util
    {
        public const ushort InterruptEnableAddress = 0xFFFF;    // IE
        public const ushort InterruptFlagAddress = 0xFF0F;      // IF
        public const ushort DividerAddress = 0xFF04;            // DIV
        public const ushort TimerCounterAddress = 0xFF05;       // TIMA
        public const ushort TimerModuloAddress = 0xFF06;        // TMA
        public const ushort TimerControlAddress = 0xFF07;       // TAC
        public const ushort LcdControlAddress = 0xFF40;         // LCDC
        public const ushort LcdStatAddress = 0xFF41;            // STAT
        public const ushort WindowYAddress = 0xFF4A;            // WY        
        public const ushort WindowXAddress = 0xFF4B;            // WX
        public const ushort ScrollYAddress = 0xFF42;            // SCY        
        public const ushort ScrollXAddress = 0xFF43;            // SCX
        public const ushort ScanlineAddress = 0xFF44;           // LY
        public const ushort DmaAddress = 0xFF46;                // DMA

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
