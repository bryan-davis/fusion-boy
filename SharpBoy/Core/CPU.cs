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

        public byte ReadNextValue()
        {
            byte value = Memory[ProgramCounter++];
            return value;
        }

        // TODO: Verify correct endianess is used
        public ushort ReadNextTwoValues()
        {
            byte value = Memory[ProgramCounter++];
            ushort values = (ushort)(value << 8);
            value = Memory[ProgramCounter++];
            values |= value;
            return values;
        }

        public void ExecuteOpCode(byte opCode)
        {
            switch (opCode)
            {
                case 0x01: break;
                case 0x02: LoadValueToMemory8Bit(RegisterBC.Value, RegisterAF.High);
                    break;
                case 0x03: break;
                case 0x04: break;
                case 0x05: break;
                case 0x06: LoadValueToRegister8Bit(ref RegisterBC.High); 
                    break;
                case 0x08: break;
                case 0x09: break;
                case 0x0A: LoadMemoryToRegister8Bit(ref RegisterAF.High, RegisterBC.Value);
                    break;
                case 0x0B: break;
                case 0x0C: break;
                case 0x0D: break;
                case 0x0E: LoadValueToRegister8Bit(ref RegisterBC.Low); 
                    break;
                case 0x11: break;
                case 0x12: LoadValueToMemory8Bit(RegisterDE.Value, RegisterAF.High);
                    break;
                case 0x13: break;
                case 0x14: break;
                case 0x15: break;
                case 0x16: LoadValueToRegister8Bit(ref RegisterDE.High); 
                    break;
                case 0x18: break;
                case 0x19: break;
                case 0x1A: LoadMemoryToRegister8Bit(ref RegisterAF.High, RegisterDE.Value);
                    break;
                case 0x1B: break;
                case 0x1C: break;
                case 0x1D: break;
                case 0x1E: LoadValueToRegister8Bit(ref RegisterDE.Low); 
                    break;
                case 0x20: break;
                case 0x21: break;
                case 0x22: LoadValueToMemory8Bit(RegisterHL.Value, RegisterAF.High); RegisterHL.Value++;
                    break;
                case 0x23: break;
                case 0x24: break;
                case 0x25: break;
                case 0x26: LoadValueToRegister8Bit(ref RegisterHL.High); 
                    break;
                case 0x28: break;
                case 0x29: break;
                case 0x2A: LoadMemoryToRegister8Bit(ref RegisterAF.High, RegisterHL.Value); RegisterHL.Value++;
                    break;
                case 0x2B: break;
                case 0x2C: break;
                case 0x2D: break;
                case 0x2E: LoadValueToRegister8Bit(ref RegisterHL.Low); 
                    break;
                case 0x30: break;
                case 0x31: break;
                case 0x32: LoadValueToMemory8Bit(RegisterHL.Value, RegisterAF.High); RegisterHL.Value--;
                    break;
                case 0x33: break;
                case 0x34: break;
                case 0x35: break;
                case 0x36: LoadValueToMemory8Bit(RegisterHL.Value, ReadNextValue());
                    break;
                case 0x38: break;
                case 0x39: break;
                case 0x3A: LoadMemoryToRegister8Bit(ref RegisterAF.High, RegisterHL.Value); RegisterHL.Value--;
                    break;
                case 0x3B: break;
                case 0x3C: break;
                case 0x3D: break;
                case 0x3E: LoadValueToRegister8Bit(ref RegisterAF.High);
                    break;
                case 0x40: LoadRegisterToRegister8Bit(ref RegisterBC.High, RegisterBC.High);
                    break;
                case 0x41: LoadRegisterToRegister8Bit(ref RegisterBC.High, RegisterBC.Low); 
                    break;
                case 0x42: LoadRegisterToRegister8Bit(ref RegisterBC.High, RegisterDE.High); 
                    break;
                case 0x43: LoadRegisterToRegister8Bit(ref RegisterBC.High, RegisterDE.Low);
                    break;
                case 0x44: LoadRegisterToRegister8Bit(ref RegisterBC.High, RegisterHL.High);
                    break;
                case 0x45: LoadRegisterToRegister8Bit(ref RegisterBC.High, RegisterHL.Low); 
                    break;
                case 0x46: LoadMemoryToRegister8Bit(ref RegisterBC.High, RegisterHL.Value);
                    break;
                case 0x47: LoadRegisterToRegister8Bit(ref RegisterBC.High, RegisterAF.High);
                    break;
                case 0x48: LoadRegisterToRegister8Bit(ref RegisterBC.Low, RegisterBC.High);
                    break;
                case 0x49: LoadRegisterToRegister8Bit(ref RegisterBC.Low, RegisterBC.Low);
                    break;
                case 0x4A: LoadRegisterToRegister8Bit(ref RegisterBC.Low, RegisterDE.High);
                    break;
                case 0x4B: LoadRegisterToRegister8Bit(ref RegisterBC.Low, RegisterDE.Low);
                    break;
                case 0x4C: LoadRegisterToRegister8Bit(ref RegisterBC.Low, RegisterHL.High);
                    break;
                case 0x4D: LoadRegisterToRegister8Bit(ref RegisterBC.Low, RegisterHL.Low);
                    break;
                case 0x4E: LoadMemoryToRegister8Bit(ref RegisterBC.Low, RegisterHL.Value);
                    break;
                case 0x4F: LoadRegisterToRegister8Bit(ref RegisterBC.Low, RegisterAF.High);
                    break;
                case 0x50: LoadRegisterToRegister8Bit(ref RegisterDE.High, RegisterBC.High);
                    break;
                case 0x51: LoadRegisterToRegister8Bit(ref RegisterDE.High, RegisterBC.Low);
                    break;
                case 0x52: LoadRegisterToRegister8Bit(ref RegisterDE.High, RegisterDE.High);
                    break;
                case 0x53: LoadRegisterToRegister8Bit(ref RegisterDE.High, RegisterDE.Low);
                    break;
                case 0x54: LoadRegisterToRegister8Bit(ref RegisterDE.High, RegisterHL.High);
                    break;
                case 0x55: LoadRegisterToRegister8Bit(ref RegisterDE.High, RegisterHL.Low);
                    break;
                case 0x56: LoadMemoryToRegister8Bit(ref RegisterDE.High, RegisterHL.Value);
                    break;
                case 0x57: LoadRegisterToRegister8Bit(ref RegisterDE.High, RegisterAF.High);
                    break;
                case 0x58: LoadRegisterToRegister8Bit(ref RegisterDE.Low, RegisterBC.High);
                    break;
                case 0x59: LoadRegisterToRegister8Bit(ref RegisterDE.Low, RegisterBC.Low);
                    break;
                case 0x5A: LoadRegisterToRegister8Bit(ref RegisterDE.Low, RegisterDE.High);
                    break;
                case 0x5B: LoadRegisterToRegister8Bit(ref RegisterDE.Low, RegisterDE.Low);
                    break;
                case 0x5C: LoadRegisterToRegister8Bit(ref RegisterDE.Low, RegisterHL.High);
                    break;
                case 0x5D: LoadRegisterToRegister8Bit(ref RegisterDE.Low, RegisterHL.Low);
                    break;
                case 0x5E: LoadMemoryToRegister8Bit(ref RegisterDE.Low, RegisterHL.Value);
                    break;
                case 0x5F: LoadRegisterToRegister8Bit(ref RegisterDE.Low, RegisterAF.High);
                    break;
                case 0x60: LoadRegisterToRegister8Bit(ref RegisterHL.High, RegisterBC.High);
                    break;
                case 0x61: LoadRegisterToRegister8Bit(ref RegisterHL.High, RegisterBC.Low);
                    break;
                case 0x62: LoadRegisterToRegister8Bit(ref RegisterHL.High, RegisterDE.High);
                    break;
                case 0x63: LoadRegisterToRegister8Bit(ref RegisterHL.High, RegisterDE.Low);
                    break;
                case 0x64: LoadRegisterToRegister8Bit(ref RegisterHL.High, RegisterHL.High);
                    break;
                case 0x65: LoadRegisterToRegister8Bit(ref RegisterHL.High, RegisterHL.Low);
                    break;
                case 0x66: LoadMemoryToRegister8Bit(ref RegisterHL.High, RegisterHL.Value);
                    break;
                case 0x67: LoadRegisterToRegister8Bit(ref RegisterHL.High, RegisterAF.High);
                    break;
                case 0x68: LoadRegisterToRegister8Bit(ref RegisterHL.Low, RegisterBC.High);
                    break;
                case 0x69: LoadRegisterToRegister8Bit(ref RegisterHL.Low, RegisterBC.Low);
                    break;
                case 0x6A: LoadRegisterToRegister8Bit(ref RegisterHL.Low, RegisterDE.High);
                    break;
                case 0x6B: LoadRegisterToRegister8Bit(ref RegisterHL.Low, RegisterDE.Low);
                    break;
                case 0x6C: LoadRegisterToRegister8Bit(ref RegisterHL.Low, RegisterHL.High);
                    break;
                case 0x6D: LoadRegisterToRegister8Bit(ref RegisterHL.Low, RegisterHL.Low);
                    break;
                case 0x6E: LoadMemoryToRegister8Bit(ref RegisterHL.Low, RegisterHL.Value);
                    break;
                case 0x6F: LoadRegisterToRegister8Bit(ref RegisterHL.Low, RegisterAF.High);
                    break;
                case 0x70: LoadValueToMemory8Bit(RegisterHL.Value, RegisterBC.High);
                    break;                             
                case 0x71: LoadValueToMemory8Bit(RegisterHL.Value, RegisterBC.Low);
                    break;                             
                case 0x72: LoadValueToMemory8Bit(RegisterHL.Value, RegisterDE.High);
                    break;                             
                case 0x73: LoadValueToMemory8Bit(RegisterHL.Value, RegisterDE.Low);
                    break;                             
                case 0x74: LoadValueToMemory8Bit(RegisterHL.Value, RegisterHL.High);
                    break;                             
                case 0x75: LoadValueToMemory8Bit(RegisterHL.Value, RegisterHL.Low);
                    break;
                case 0x77: LoadValueToMemory8Bit(RegisterHL.Value, RegisterAF.High);
                    break;
                case 0x78: LoadRegisterToRegister8Bit(ref RegisterAF.High, RegisterBC.High); 
                    break;
                case 0x79: LoadRegisterToRegister8Bit(ref RegisterAF.High, RegisterBC.Low); 
                    break;
                case 0x7A: LoadRegisterToRegister8Bit(ref RegisterAF.High, RegisterDE.High); 
                    break;
                case 0x7B: LoadRegisterToRegister8Bit(ref RegisterAF.High, RegisterDE.Low); 
                    break;
                case 0x7C: LoadRegisterToRegister8Bit(ref RegisterAF.High, RegisterHL.High); 
                    break;
                case 0x7D: LoadRegisterToRegister8Bit(ref RegisterAF.High, RegisterHL.Low); 
                    break;
                case 0x7E: LoadMemoryToRegister8Bit(ref RegisterAF.High, RegisterHL.Value);
                    break;
                case 0x7F: LoadRegisterToRegister8Bit(ref RegisterAF.High, RegisterAF.High); 
                    break;
                case 0x80: break;
                case 0x81: break;
                case 0x82: break;
                case 0x83: break;
                case 0x84: break;
                case 0x85: break;
                case 0x86: break;
                case 0x87: break;
                case 0x88: break;
                case 0x89: break;
                case 0x8A: break;
                case 0x8B: break;
                case 0x8C: break;
                case 0x8D: break;
                case 0x8E: break;
                case 0x8F: break;
                case 0x90: break;
                case 0x91: break;
                case 0x92: break;
                case 0x93: break;
                case 0x94: break;
                case 0x95: break;
                case 0x96: break;
                case 0x97: break;
                case 0x98: break;
                case 0x99: break;
                case 0x9A: break;
                case 0x9B: break;
                case 0x9C: break;
                case 0x9D: break;
                case 0x9E: break;
                case 0x9F: break;
                case 0xA0: break;
                case 0xA1: break;
                case 0xA2: break;
                case 0xA3: break;
                case 0xA4: break;
                case 0xA5: break;
                case 0xA6: break;
                case 0xA7: break;
                case 0xA8: break;
                case 0xA9: break;
                case 0xAA: break;
                case 0xAB: break;
                case 0xAC: break;
                case 0xAD: break;
                case 0xAE: break;
                case 0xAF: break;
                case 0xB0: break;
                case 0xB1: break;
                case 0xB2: break;
                case 0xB3: break;
                case 0xB4: break;
                case 0xB5: break;
                case 0xB6: break;
                case 0xB7: break;
                case 0xB8: break;
                case 0xB9: break;
                case 0xBA: break;
                case 0xBB: break;
                case 0xBC: break;
                case 0xBD: break;
                case 0xBE: break;
                case 0xBF: break;
                case 0xC0: break;
                case 0xC1: break;
                case 0xC2: break;
                case 0xC3: break;
                case 0xC4: break;
                case 0xC5: break;
                case 0xC6: break;
                case 0xC7: break;
                case 0xC8: break;
                case 0xC9: break;
                case 0xCA: break;
                case 0xCB: ExecuteExtendedOpCode(opCode); 
                    break;
                case 0xCC: break;
                case 0xCD: break;
                case 0xCE: break;
                case 0xCF: break;
                case 0xD0: break;
                case 0xD1: break;
                case 0xD2: break;
                case 0xD4: break;
                case 0xD5: break;
                case 0xD6: break;
                case 0xD7: break;
                case 0xD8: break;
                case 0xD9: break;
                case 0xDA: break;
                case 0xDC: break;
                case 0xDF: break;
                case 0xE0: LoadValueToMemory8Bit((ushort)(0xFF00 + ReadNextValue()), RegisterAF.High);
                    break;
                case 0xE1: break;
                case 0xE2: LoadValueToMemory8Bit((ushort)(0xFF00 + RegisterBC.Low), RegisterAF.High);
                    break;
                case 0xE5: break;
                case 0xE6: break;
                case 0xE7: break;
                case 0xE8: break;
                case 0xE9: break;
                case 0xEA: LoadValueToMemory8Bit(ReadNextTwoValues(), RegisterAF.High);
                    break;
                case 0xEE: break;
                case 0xEF: break;
                case 0xF0: LoadMemoryToRegister8Bit(ref RegisterAF.High, (ushort)(0xFF00 + ReadNextValue()));
                    break;
                case 0xF1: break;
                case 0xF2: LoadMemoryToRegister8Bit(ref RegisterAF.High, (ushort)(0xFF00 + RegisterBC.Low));
                    break;
                case 0xF5: break;
                case 0xF6: break;
                case 0xF7: break;
                case 0xF8: break;
                case 0xF9: break;
                case 0xFA: LoadMemoryToRegister8Bit(ref RegisterAF.High, ReadNextTwoValues());
                    break;
                case 0xFE: break;
                case 0xFF: break;
            }
        }

        // For all op codes prefixed with 0xCB
        private void ExecuteExtendedOpCode(byte opCode)
        {
            ushort extended = (ushort)(opCode << 8);
            byte next = ReadNextValue();
            extended |= next;

            switch (extended)
            {
                case 0xCB00: break;
                case 0xCB01: break;
                case 0xCB02: break;
                case 0xCB03: break;
                case 0xCB04: break;
                case 0xCB05: break;
                case 0xCB06: break;
                case 0xCB07: break;
                case 0xCB08: break;
                case 0xCB09: break;
                case 0xCB0A: break;
                case 0xCB0B: break;
                case 0xCB0C: break;
                case 0xCB0D: break;
                case 0xCB0E: break;
                case 0xCB0F: break;
                case 0xCB10: break;
                case 0xCB11: break;
                case 0xCB12: break;
                case 0xCB13: break;
                case 0xCB14: break;
                case 0xCB15: break;
                case 0xCB16: break;
                case 0xCB17: break;
                case 0xCB18: break;
                case 0xCB19: break;
                case 0xCB1A: break;
                case 0xCB1B: break;
                case 0xCB1C: break;
                case 0xCB1D: break;
                case 0xCB1E: break;
                case 0xCB1F: break;
                case 0xCB20: break;
                case 0xCB21: break;
                case 0xCB22: break;
                case 0xCB23: break;
                case 0xCB24: break;
                case 0xCB25: break;
                case 0xCB26: break;
                case 0xCB27: break;
                case 0xCB28: break;
                case 0xCB29: break;
                case 0xCB2A: break;
                case 0xCB2B: break;
                case 0xCB2C: break;
                case 0xCB2D: break;
                case 0xCB2E: break;
                case 0xCB2F: break;
                case 0xCB38: break;
                case 0xCB39: break;
                case 0xCB3A: break;
                case 0xCB3B: break;
                case 0xCB3C: break;
                case 0xCB3D: break;
                case 0xCB3E: break;
                case 0xCB3F: break;
                case 0xCB40: break;
                case 0xCB41: break;
                case 0xCB42: break;
                case 0xCB43: break;
                case 0xCB44: break;
                case 0xCB45: break;
                case 0xCB46: break;
                case 0xCB47: break;
                case 0xCB80: break;
                case 0xCB81: break;
                case 0xCB82: break;
                case 0xCB83: break;
                case 0xCB84: break;
                case 0xCB85: break;
                case 0xCB86: break;
                case 0xCB87: break;
                case 0xCBC0: break;
                case 0xCBC1: break;
                case 0xCBC2: break;
                case 0xCBC3: break;
                case 0xCBC4: break;
                case 0xCBC5: break;
                case 0xCBC6: break;
                case 0xCBC7: break;
            }
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

        private void LoadValueToRegister8Bit(ref byte register)
        {
            register = ReadNextValue();
        }

        private void LoadRegisterToRegister8Bit(ref byte toRegister, byte fromRegister)
        {
            toRegister = fromRegister;
        }

        private void LoadMemoryToRegister8Bit(ref byte toRegister, ushort location)
        {
            toRegister = Memory[location];
        }

        private void LoadValueToMemory8Bit(ushort location, byte value)
        {
            Memory[location] = value;
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
