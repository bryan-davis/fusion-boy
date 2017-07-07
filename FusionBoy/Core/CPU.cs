/*
 * Copyright 2014 Bryan Davis
 * Licensed under GPLv2
 * Refer to the license.txt file included
 */

using FusionBoy.Cartridge;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace FusionBoy.Core
{
    [Serializable]
    public partial class CPU
    {
        public Register AF { get; private set; }
        public Register BC { get; private set; }
        public Register DE { get; private set; }
        public Register HL { get; private set; }
        public Register StackPointer { get; private set; }
        public ushort ProgramCounter { get; private set; }

        // Flags; http://problemkaputt.de/pandocs.htm#cpuregistersandflags
        public readonly byte FlagZ = 1 << 7;    // Zero Flag
        public readonly byte FlagN = 1 << 6;    // Add/Sub Flag
        public readonly byte FlagH = 1 << 5;    // Half Carry Flag            
        public readonly byte FlagC = 1 << 4;    // Carry Flag

        public MemoryBankController Memory { get; private set; }
        public CartridgeInfo CartInfo { get; private set; }
        public bool Halted { get; private set; }
        public bool Stopped { get; private set; }

        public bool TimerEnabled { get; private set; }
        public int CyclesPerTimerIncrement { get; private set; }
        public int TimerCycles { get; private set; }
        public int CyclesPerDividerIncrement { get; private set; }
        public int DividerCycles { get; private set; }
        public int CyclesExecuted { get; set; }

        public Display Display { get; private set; }
        private const int CyclesPerScanline = 456;
        private int scanlineCycleCounter;

        private Queue<bool> interruptQueue;

        public CPU() { }

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
            Memory.UpdateTimerHandler += HandleTimerUpdate;
            Reset();
        }

        public byte ReadNextValue()
        {
            IncrementCycles(4);
            byte value = Memory[ProgramCounter++];
            return value;
        }

        public ushort ReadNextTwoValues()
        {
            byte low = ReadNextValue();
            ushort values = low;
            byte high = ReadNextValue();
            values |= (ushort)(high << 8);
            return values;
        }

        private byte ReadMemory(ushort address)
        {
            IncrementCycles(4);
            return Memory[address];
        }

        private void WriteMemory(int address, byte value)
        {
            IncrementCycles(4);
            Memory[address] = value;
        }

        private void IncrementCycles(int amount)
        {
            CyclesExecuted += amount;
            UpdateTimers(amount);
            UpdateGraphics(amount);
        }

        private void UpdateGraphics(int cycleCount)
        {
            if (!LCDEnabled())
            {
                ResetLCDStatus(cycleCount);
                return;
            }

            scanlineCycleCounter += cycleCount;
            Display.UpdateLcdStatus(scanlineCycleCounter);
            while (scanlineCycleCounter >= CyclesPerScanline)
            {
                Display.RenderScanline();
                scanlineCycleCounter -= CyclesPerScanline;
            }
        }

        private void ResetLCDStatus(int cycleCount)
        {
            scanlineCycleCounter = cycleCount;
            Memory[Util.ScanlineAddress] = 0;

            // Reset the mode to 01 http://problemkaputt.de/pandocs.htm#lcdstatusregister
            Util.ClearBits(Memory, Util.LcdStatAddress, 0, 1);
            Util.SetBits(Memory, Util.LcdStatAddress, 0);
        }

        /*
         * http://problemkaputt.de/pandocs.htm#timeranddividerregisters
         * TimerCycleIncrement is the number of cycles that need to have happened
         * for the TIMA timer counter to increment by one. TimerCycles keeps track
         * of the number of cycles that have occured between each increment, resetting
         * to 0 after an increment has occurred.
         */
        public void UpdateTimers(int cycleCount)
        {
            UpdateDivider(cycleCount);

            if (TimerEnabled)
            {
                TimerCycles += cycleCount;
                while (TimerCycles >= CyclesPerTimerIncrement)
                {
                    if (++Memory[Util.TimerCounterAddress] == 0)
                    {
                        Memory[Util.TimerCounterAddress] = Memory[Util.TimerModuloAddress];
                        Util.SetBits(Memory, Util.InterruptFlagAddress, (byte)Interrupts.Timer);
                    }
                    TimerCycles -= CyclesPerTimerIncrement;
                } 
            }
        }

        /*
         * Bit 2    - Timer Stop  (0=Stop, 1=Start)
         * Bits 1-0 - Input Clock Select
         * 00:   4096 Hz
         * 01: 262144 Hz
         * 10:  65536 Hz
         * 11:  16384 Hz
         */
        public void HandleTimerUpdate(byte value)
        {
            TimerEnabled = Util.IsBitSet(value, 2);
            int cyclesPerSecond = Properties.Settings.Default.cyclesPerSecond;
            byte refreshSetting = (byte)(value & 3);
            switch (refreshSetting)
            {
                case 0: 
                    CyclesPerTimerIncrement = cyclesPerSecond / 4096;
                    break;
                case 1: 
                    CyclesPerTimerIncrement = cyclesPerSecond / 262144; 
                    break;
                case 2:
                    CyclesPerTimerIncrement = cyclesPerSecond / 65536; 
                    break;
                case 3:
                    CyclesPerTimerIncrement = cyclesPerSecond / 16384; 
                    break;
                default:
                    break;
            }
        }

        // http://problemkaputt.de/pandocs.htm#interrupts
        public void ProcessInterrupts()
        {
            if (interruptQueue.Count > 0 && interruptQueue.Dequeue() == true)
            {
                ushort addressJump = 0x0000;
                // Ignore serial interrupts for now
                ushort[] jumpTable = { 0x0040, 0x0048, 0x0050, 0x0058, 0x0060 };

                byte interruptEnable = Memory[Util.InterruptEnableAddress];
                byte interruptFlags = Memory[Util.InterruptFlagAddress];

                for (byte i = (byte)Interrupts.VBlank; i <= (byte)Interrupts.Joypad; i++)
                {
                    if (Util.IsBitSet(interruptEnable, i) && Util.IsBitSet(interruptFlags, i))
                    {
                        addressJump = jumpTable[i];
                        // Based on results from the Blargg test ROMs, interrupt 
                        // flags are only cleared when not in a Halted state.
                        if (!Halted)
                        {
                            Util.ClearBits(Memory, Util.InterruptFlagAddress, i);
                        }
                        break;
                    }
                }

                if (addressJump != 0x0000)
                {
                    PushAddressOntoStack(ProgramCounter);
                    ProgramCounter = addressJump;
                    interruptQueue.Clear();
                    Halted = false;
                    IncrementCycles(4);
                    IncrementCycles(4);
                }
                else
                {
                    // Continue processing interrupts on the next loop,
                    // until interrupts are disabled completely.
                    if (interruptQueue.Count == 0)
                        interruptQueue.Enqueue(true);
                }        
            }
        }

        // http://problemkaputt.de/pandocs.htm#joypadinput
        public void KeyDown(Joypad input)
        {
            
        }

        public void KeyUp(Joypad input)
        {

        }

        private void UpdateDivider(int cycleCount)
        {
            DividerCycles += cycleCount;
            while (DividerCycles >= CyclesPerDividerIncrement)
            {
                Memory.IncrementDividerRegister();
                DividerCycles -= CyclesPerDividerIncrement;
            } 
        }

        public void ExecuteOpCode()
        {
            int opCode;
            if (Halted)
            {
                opCode = 0x76;
                IncrementCycles(4);
            }
            else
            {
                opCode = ReadNextValue();
            }

            switch (opCode)
            {
                case 0x00: // Do nothing
                    break;
                case 0x01: LoadValueToRegister16Bit(BC);
                    break;
                case 0x02: LoadValueToMemory8Bit(BC.Value, AF.High);
                    break;
                case 0x03: IncrementRegister16Bit(BC);
                    break;
                case 0x04: IncrementRegister8Bit(ref BC.High);
                    break;
                case 0x05: DecrementRegister8Bit(ref BC.High);
                    break;
                case 0x06: LoadValueToRegister8Bit(ref BC.High); 
                    break;
                case 0x07: RotateALeftNoCarry();
                    break;
                case 0x08: LoadRegisterToMemory(StackPointer);
                    break;
                case 0x09: AddValueToRegisterHL(BC);
                    break;
                case 0x0A: LoadMemoryToRegister8Bit(ref AF.High, BC.Value);
                    break;
                case 0x0B: DecrementRegister16Bit(BC);
                    break;
                case 0x0C: IncrementRegister8Bit(ref BC.Low);
                    break;
                case 0x0D: DecrementRegister8Bit(ref BC.Low);
                    break;
                case 0x0E: LoadValueToRegister8Bit(ref BC.Low); 
                    break;
                case 0x0F: RotateARightNoCarry();
                    break;
                case 0x10: Stopped = true; ProgramCounter++;
                    break;
                case 0x11: LoadValueToRegister16Bit(DE);
                    break;
                case 0x12: LoadValueToMemory8Bit(DE.Value, AF.High);
                    break;
                case 0x13: IncrementRegister16Bit(DE);
                    break;
                case 0x14: IncrementRegister8Bit(ref DE.High);
                    break;
                case 0x15: DecrementRegister8Bit(ref DE.High);
                    break;
                case 0x16: LoadValueToRegister8Bit(ref DE.High); 
                    break;
                case 0x17: RotateALeftThroughCarry();
                    break;
                case 0x18: Jump((sbyte)ReadNextValue());
                    break;
                case 0x19: AddValueToRegisterHL(DE);
                    break;
                case 0x1A: LoadMemoryToRegister8Bit(ref AF.High, DE.Value);
                    break;
                case 0x1B: DecrementRegister16Bit(DE);
                    break;
                case 0x1C: IncrementRegister8Bit(ref DE.Low);
                    break;
                case 0x1D: DecrementRegister8Bit(ref DE.Low);
                    break;
                case 0x1E: LoadValueToRegister8Bit(ref DE.Low); 
                    break;
                case 0x1F: RotateARightThroughCarry();
                    break;
                case 0x20: ConditionallyJump(!IsFlagSet(FlagZ), (sbyte)ReadNextValue());
                    break;
                case 0x21: LoadValueToRegister16Bit(HL);
                    break;
                case 0x22: LoadValueToMemory8Bit(HL.Value, AF.High); HL.Value++;
                    break;
                case 0x23: IncrementRegister16Bit(HL);
                    break;
                case 0x24: IncrementRegister8Bit(ref HL.High);
                    break;
                case 0x25: DecrementRegister8Bit(ref HL.High);
                    break;
                case 0x26: LoadValueToRegister8Bit(ref HL.High); 
                    break;
                case 0x27: DecimalAdjustRegisterA();
                    break;
                case 0x28: ConditionallyJump(IsFlagSet(FlagZ), (sbyte)ReadNextValue());
                    break;
                case 0x29: AddValueToRegisterHL(HL);
                    break;
                case 0x2A: LoadMemoryToRegister8Bit(ref AF.High, HL.Value); HL.Value++;
                    break;
                case 0x2B: DecrementRegister16Bit(HL);
                    break;
                case 0x2C: IncrementRegister8Bit(ref HL.Low);
                    break;
                case 0x2D: DecrementRegister8Bit(ref HL.Low);
                    break;
                case 0x2E: LoadValueToRegister8Bit(ref HL.Low); 
                    break;
                case 0x2F: ComplementRegisterA();
                    break;
                case 0x30: ConditionallyJump(!IsFlagSet(FlagC), (sbyte)ReadNextValue());
                    break;
                case 0x31: LoadValueToRegister16Bit(StackPointer);
                    break;
                case 0x32: LoadValueToMemory8Bit(HL.Value, AF.High); HL.Value--;
                    break;
                case 0x33: IncrementRegister16Bit(StackPointer);
                    break;
                case 0x34: IncrementMemory(HL.Value);
                    break;
                case 0x35: DecrementMemory(HL.Value);
                    break;
                case 0x36: LoadValueToMemory8Bit(HL.Value, ReadNextValue());
                    break;
                case 0x37: SetCarryFlag();
                    break;
                case 0x38: ConditionallyJump(IsFlagSet(FlagC), (sbyte)ReadNextValue());
                    break;
                case 0x39: AddValueToRegisterHL(StackPointer);
                    break;
                case 0x3A: LoadMemoryToRegister8Bit(ref AF.High, HL.Value); HL.Value--;
                    break;
                case 0x3B: DecrementRegister16Bit(StackPointer);
                    break;
                case 0x3C: IncrementRegister8Bit(ref AF.High);
                    break;
                case 0x3D: DecrementRegister8Bit(ref AF.High);
                    break;
                case 0x3E: LoadValueToRegister8Bit(ref AF.High);
                    break;
                case 0x3F: ComplementCarryFlag();
                    break;
                case 0x40: LoadRegisterToRegister8Bit(ref BC.High, BC.High);
                    break;
                case 0x41: LoadRegisterToRegister8Bit(ref BC.High, BC.Low); 
                    break;
                case 0x42: LoadRegisterToRegister8Bit(ref BC.High, DE.High); 
                    break;
                case 0x43: LoadRegisterToRegister8Bit(ref BC.High, DE.Low);
                    break;
                case 0x44: LoadRegisterToRegister8Bit(ref BC.High, HL.High);
                    break;
                case 0x45: LoadRegisterToRegister8Bit(ref BC.High, HL.Low); 
                    break;
                case 0x46: LoadMemoryToRegister8Bit(ref BC.High, HL.Value);
                    break;
                case 0x47: LoadRegisterToRegister8Bit(ref BC.High, AF.High);
                    break;
                case 0x48: LoadRegisterToRegister8Bit(ref BC.Low, BC.High);
                    break;
                case 0x49: LoadRegisterToRegister8Bit(ref BC.Low, BC.Low);
                    break;
                case 0x4A: LoadRegisterToRegister8Bit(ref BC.Low, DE.High);
                    break;
                case 0x4B: LoadRegisterToRegister8Bit(ref BC.Low, DE.Low);
                    break;
                case 0x4C: LoadRegisterToRegister8Bit(ref BC.Low, HL.High);
                    break;
                case 0x4D: LoadRegisterToRegister8Bit(ref BC.Low, HL.Low);
                    break;
                case 0x4E: LoadMemoryToRegister8Bit(ref BC.Low, HL.Value);
                    break;
                case 0x4F: LoadRegisterToRegister8Bit(ref BC.Low, AF.High);
                    break;
                case 0x50: LoadRegisterToRegister8Bit(ref DE.High, BC.High);
                    break;
                case 0x51: LoadRegisterToRegister8Bit(ref DE.High, BC.Low);
                    break;
                case 0x52: LoadRegisterToRegister8Bit(ref DE.High, DE.High);
                    break;
                case 0x53: LoadRegisterToRegister8Bit(ref DE.High, DE.Low);
                    break;
                case 0x54: LoadRegisterToRegister8Bit(ref DE.High, HL.High);
                    break;
                case 0x55: LoadRegisterToRegister8Bit(ref DE.High, HL.Low);
                    break;
                case 0x56: LoadMemoryToRegister8Bit(ref DE.High, HL.Value);
                    break;
                case 0x57: LoadRegisterToRegister8Bit(ref DE.High, AF.High);
                    break;
                case 0x58: LoadRegisterToRegister8Bit(ref DE.Low, BC.High);
                    break;
                case 0x59: LoadRegisterToRegister8Bit(ref DE.Low, BC.Low);
                    break;
                case 0x5A: LoadRegisterToRegister8Bit(ref DE.Low, DE.High);
                    break;
                case 0x5B: LoadRegisterToRegister8Bit(ref DE.Low, DE.Low);
                    break;
                case 0x5C: LoadRegisterToRegister8Bit(ref DE.Low, HL.High);
                    break;
                case 0x5D: LoadRegisterToRegister8Bit(ref DE.Low, HL.Low);
                    break;
                case 0x5E: LoadMemoryToRegister8Bit(ref DE.Low, HL.Value);
                    break;
                case 0x5F: LoadRegisterToRegister8Bit(ref DE.Low, AF.High);
                    break;
                case 0x60: LoadRegisterToRegister8Bit(ref HL.High, BC.High);
                    break;
                case 0x61: LoadRegisterToRegister8Bit(ref HL.High, BC.Low);
                    break;
                case 0x62: LoadRegisterToRegister8Bit(ref HL.High, DE.High);
                    break;
                case 0x63: LoadRegisterToRegister8Bit(ref HL.High, DE.Low);
                    break;
                case 0x64: LoadRegisterToRegister8Bit(ref HL.High, HL.High);
                    break;
                case 0x65: LoadRegisterToRegister8Bit(ref HL.High, HL.Low);
                    break;
                case 0x66: LoadMemoryToRegister8Bit(ref HL.High, HL.Value);
                    break;
                case 0x67: LoadRegisterToRegister8Bit(ref HL.High, AF.High);
                    break;
                case 0x68: LoadRegisterToRegister8Bit(ref HL.Low, BC.High);
                    break;
                case 0x69: LoadRegisterToRegister8Bit(ref HL.Low, BC.Low);
                    break;
                case 0x6A: LoadRegisterToRegister8Bit(ref HL.Low, DE.High);
                    break;
                case 0x6B: LoadRegisterToRegister8Bit(ref HL.Low, DE.Low);
                    break;
                case 0x6C: LoadRegisterToRegister8Bit(ref HL.Low, HL.High);
                    break;
                case 0x6D: LoadRegisterToRegister8Bit(ref HL.Low, HL.Low);
                    break;
                case 0x6E: LoadMemoryToRegister8Bit(ref HL.Low, HL.Value);
                    break;
                case 0x6F: LoadRegisterToRegister8Bit(ref HL.Low, AF.High);
                    break;
                case 0x70: LoadValueToMemory8Bit(HL.Value, BC.High);
                    break;                             
                case 0x71: LoadValueToMemory8Bit(HL.Value, BC.Low);
                    break;                             
                case 0x72: LoadValueToMemory8Bit(HL.Value, DE.High);
                    break;                             
                case 0x73: LoadValueToMemory8Bit(HL.Value, DE.Low);
                    break;                             
                case 0x74: LoadValueToMemory8Bit(HL.Value, HL.High);
                    break;                             
                case 0x75: LoadValueToMemory8Bit(HL.Value, HL.Low);
                    break;
                case 0x76: Halt();
                    break;
                case 0x77: LoadValueToMemory8Bit(HL.Value, AF.High);
                    break;
                case 0x78: LoadRegisterToRegister8Bit(ref AF.High, BC.High); 
                    break;
                case 0x79: LoadRegisterToRegister8Bit(ref AF.High, BC.Low); 
                    break;
                case 0x7A: LoadRegisterToRegister8Bit(ref AF.High, DE.High); 
                    break;
                case 0x7B: LoadRegisterToRegister8Bit(ref AF.High, DE.Low); 
                    break;
                case 0x7C: LoadRegisterToRegister8Bit(ref AF.High, HL.High); 
                    break;
                case 0x7D: LoadRegisterToRegister8Bit(ref AF.High, HL.Low); 
                    break;
                case 0x7E: LoadMemoryToRegister8Bit(ref AF.High, HL.Value);
                    break;
                case 0x7F: LoadRegisterToRegister8Bit(ref AF.High, AF.High); 
                    break;
                case 0x80: AddValueToRegisterA(BC.High);
                    break;
                case 0x81: AddValueToRegisterA(BC.Low);
                    break;
                case 0x82: AddValueToRegisterA(DE.High);
                    break;
                case 0x83: AddValueToRegisterA(DE.Low);
                    break;
                case 0x84: AddValueToRegisterA(HL.High);
                    break;
                case 0x85: AddValueToRegisterA(HL.Low);
                    break;
                case 0x86: AddValueToRegisterA(ReadMemory(HL.Value));
                    break;
                case 0x87: AddValueToRegisterA(AF.High);
                    break;
                case 0x88: AddValueToRegisterA(BC.High, true);
                    break;
                case 0x89: AddValueToRegisterA(BC.Low, true);
                    break;
                case 0x8A: AddValueToRegisterA(DE.High, true);
                    break;
                case 0x8B: AddValueToRegisterA(DE.Low, true);
                    break;
                case 0x8C: AddValueToRegisterA(HL.High, true);
                    break;
                case 0x8D: AddValueToRegisterA(HL.Low, true);
                    break;
                case 0x8E: AddValueToRegisterA(ReadMemory(HL.Value), true);
                    break;
                case 0x8F: AddValueToRegisterA(AF.High, true);
                    break;
                case 0x90: SubtractValueFromRegisterA(BC.High);
                    break;
                case 0x91: SubtractValueFromRegisterA(BC.Low);
                    break;
                case 0x92: SubtractValueFromRegisterA(DE.High);
                    break;
                case 0x93: SubtractValueFromRegisterA(DE.Low);
                    break;
                case 0x94: SubtractValueFromRegisterA(HL.High);
                    break;
                case 0x95: SubtractValueFromRegisterA(HL.Low);
                    break;
                case 0x96: SubtractValueFromRegisterA(ReadMemory(HL.Value));
                    break;
                case 0x97: SubtractValueFromRegisterA(AF.High);
                    break;
                case 0x98: SubtractValueFromRegisterA(BC.High, true);
                    break;
                case 0x99: SubtractValueFromRegisterA(BC.Low, true);
                    break;
                case 0x9A: SubtractValueFromRegisterA(DE.High, true);
                    break;
                case 0x9B: SubtractValueFromRegisterA(DE.Low, true);
                    break;
                case 0x9C: SubtractValueFromRegisterA(HL.High, true);
                    break;
                case 0x9D: SubtractValueFromRegisterA(HL.Low, true);
                    break;
                case 0x9E: SubtractValueFromRegisterA(ReadMemory(HL.Value), true);
                    break;
                case 0x9F: SubtractValueFromRegisterA(AF.High, true);
                    break;
                case 0xA0: AndWithRegisterA(BC.High);
                    break;
                case 0xA1: AndWithRegisterA(BC.Low);
                    break;
                case 0xA2: AndWithRegisterA(DE.High);
                    break;
                case 0xA3: AndWithRegisterA(DE.Low);
                    break;
                case 0xA4: AndWithRegisterA(HL.High);
                    break;
                case 0xA5: AndWithRegisterA(HL.Low);
                    break;
                case 0xA6: AndWithRegisterA(ReadMemory(HL.Value));
                    break;
                case 0xA7: AndWithRegisterA(AF.High);
                    break;
                case 0xA8: XorWithRegisterA(BC.High);
                    break;
                case 0xA9: XorWithRegisterA(BC.Low);
                    break;
                case 0xAA: XorWithRegisterA(DE.High);
                    break;
                case 0xAB: XorWithRegisterA(DE.Low);
                    break;
                case 0xAC: XorWithRegisterA(HL.High);
                    break;
                case 0xAD: XorWithRegisterA(HL.Low);
                    break;
                case 0xAE: XorWithRegisterA(ReadMemory(HL.Value));
                    break;
                case 0xAF: XorWithRegisterA(AF.High);
                    break;
                case 0xB0: OrWithRegisterA(BC.High);
                    break;
                case 0xB1: OrWithRegisterA(BC.Low);
                    break;
                case 0xB2: OrWithRegisterA(DE.High);
                    break;
                case 0xB3: OrWithRegisterA(DE.Low);
                    break;
                case 0xB4: OrWithRegisterA(HL.High);
                    break;
                case 0xB5: OrWithRegisterA(HL.Low);
                    break;
                case 0xB6: OrWithRegisterA(ReadMemory(HL.Value));
                    break;
                case 0xB7: OrWithRegisterA(AF.High);
                    break;
                case 0xB8: CompareWithRegisterA(BC.High);
                    break;
                case 0xB9: CompareWithRegisterA(BC.Low);
                    break;
                case 0xBA: CompareWithRegisterA(DE.High);
                    break;
                case 0xBB: CompareWithRegisterA(DE.Low);
                    break;
                case 0xBC: CompareWithRegisterA(HL.High);
                    break;
                case 0xBD: CompareWithRegisterA(HL.Low);
                    break;
                case 0xBE: CompareWithRegisterA(ReadMemory(HL.Value));
                    break;
                case 0xBF: CompareWithRegisterA(AF.High);
                    break;
                case 0xC0: ConditionallyReturn(!IsFlagSet(FlagZ));
                    break;
                case 0xC1: PopValuesIntoRegister(BC);
                    break;
                case 0xC2: ConditionallyJump(!IsFlagSet(FlagZ), ReadNextTwoValues());
                    break;
                case 0xC3: Jump(ReadNextTwoValues());
                    break;
                case 0xC4: ConditionallyCall(!IsFlagSet(FlagZ), ReadNextTwoValues());
                    break;
                case 0xC5: PushAddressOntoStack(BC.Value);
                    break;
                case 0xC6: AddValueToRegisterA(ReadNextValue());
                    break;
                case 0xC7: Restart(0x00);
                    break;
                case 0xC8: ConditionallyReturn(IsFlagSet(FlagZ));
                    break;
                case 0xC9: Return();
                    break;
                case 0xCA: ConditionallyJump(IsFlagSet(FlagZ), ReadNextTwoValues());
                    break;
                case 0xCB: ExecuteCBOpCode();
                    break;
                case 0xCC: ConditionallyCall(IsFlagSet(FlagZ), ReadNextTwoValues());
                    break;
                case 0xCD: Call(ReadNextTwoValues());
                    break;
                case 0xCE: AddValueToRegisterA(ReadNextValue(), true);
                    break;
                case 0xCF: Restart(0x08);
                    break;
                case 0xD0: ConditionallyReturn(!IsFlagSet(FlagC));
                    break;
                case 0xD1: PopValuesIntoRegister(DE);
                    break;
                case 0xD2: ConditionallyJump(!IsFlagSet(FlagC), ReadNextTwoValues());
                    break;
                case 0xD4: ConditionallyCall(!IsFlagSet(FlagC), ReadNextTwoValues());
                    break;
                case 0xD5: PushAddressOntoStack(DE.Value);
                    break;
                case 0xD6: SubtractValueFromRegisterA(ReadNextValue());
                    break;
                case 0xD7: Restart(0x10);
                    break;
                case 0xD8: ConditionallyReturn(IsFlagSet(FlagC));
                    break;
                case 0xD9: ReturnAndEnableInterrupts();
                    break;
                case 0xDA: ConditionallyJump(IsFlagSet(FlagC), ReadNextTwoValues());
                    break;
                case 0xDC: ConditionallyCall(IsFlagSet(FlagC), ReadNextTwoValues());
                    break;
                case 0xDE: SubtractValueFromRegisterA(ReadNextValue(), true);
                    break;
                case 0xDF: Restart(0x18);
                    break;
                case 0xE0: LoadValueToMemory8Bit((ushort)(0xFF00 + ReadNextValue()), AF.High);
                    break;
                case 0xE1: PopValuesIntoRegister(HL);
                    break;
                case 0xE2: LoadValueToMemory8Bit((ushort)(0xFF00 + BC.Low), AF.High);
                    break;
                case 0xE5: PushAddressOntoStack(HL.Value);
                    break;
                case 0xE6: AndWithRegisterA(ReadNextValue());
                    break;
                case 0xE7: Restart(0x20);
                    break;
                case 0xE8: AddValueToStackPointer();
                    break;
                case 0xE9: IncrementCycles(-4); Jump(HL.Value);
                    break;
                case 0xEA: LoadValueToMemory8Bit(ReadNextTwoValues(), AF.High);
                    break;
                case 0xEE: XorWithRegisterA(ReadNextValue());
                    break;
                case 0xEF: Restart(0x28);
                    break;
                case 0xF0: LoadMemoryToRegister8Bit(ref AF.High, (ushort)(0xFF00 + ReadNextValue()));
                    break;
                case 0xF1: PopValuesIntoRegister(AF); AF.Low &= 0xF0;   // Bottom 4 bits of flags are never used; clear them
                    break;
                case 0xF2: LoadMemoryToRegister8Bit(ref AF.High, (ushort)(0xFF00 + BC.Low));
                    break;
                case 0xF3: DisableInterrupts();
                    break;
                case 0xF5: PushAddressOntoStack(AF.Value);
                    break;
                case 0xF6: OrWithRegisterA(ReadNextValue());
                    break;
                case 0xF7: Restart(0x30);
                    break;
                case 0xF8: LoadStackPointerToRegisterHL();
                    break;
                case 0xF9: IncrementCycles(4); LoadRegisterToRegister16Bit(StackPointer, HL);
                    break;
                case 0xFA: LoadMemoryToRegister8Bit(ref AF.High, ReadNextTwoValues());
                    break;
                case 0xFB: EnableInterrupts();
                    break;
                case 0xFE: CompareWithRegisterA(ReadNextValue());
                    break;
                case 0xFF: Restart(0x38);
                    break;                
            }
        }

        // For all op codes prefixed with 0xCB
        public void ExecuteCBOpCode()
        {
            int opCode = ReadNextValue();
            switch (opCode)
            {
                case 0x00: RotateLeftNoCarry(ref BC.High);
                    break;
                case 0x01: RotateLeftNoCarry(ref BC.Low);
                    break;
                case 0x02: RotateLeftNoCarry(ref DE.High);
                    break;
                case 0x03: RotateLeftNoCarry(ref DE.Low);
                    break;
                case 0x04: RotateLeftNoCarry(ref HL.High);
                    break;
                case 0x05: RotateLeftNoCarry(ref HL.Low);
                    break;
                case 0x06: RotateLeftNoCarry(HL.Value);
                    break;
                case 0x07: RotateLeftNoCarry(ref AF.High);
                    break;
                case 0x08: RotateRightNoCarry(ref BC.High);
                    break;
                case 0x09: RotateRightNoCarry(ref BC.Low);
                    break;
                case 0x0A: RotateRightNoCarry(ref DE.High);
                    break;
                case 0x0B: RotateRightNoCarry(ref DE.Low);
                    break;
                case 0x0C: RotateRightNoCarry(ref HL.High);
                    break;
                case 0x0D: RotateRightNoCarry(ref HL.Low);
                    break;
                case 0x0E: RotateRightNoCarry(HL.Value);
                    break;
                case 0x0F: RotateRightNoCarry(ref AF.High);
                    break;
                case 0x10: RotateLeftThroughCarry(ref BC.High);
                    break;
                case 0x11: RotateLeftThroughCarry(ref BC.Low);
                    break;
                case 0x12: RotateLeftThroughCarry(ref DE.High);
                    break;
                case 0x13: RotateLeftThroughCarry(ref DE.Low);
                    break;
                case 0x14: RotateLeftThroughCarry(ref HL.High);
                    break;
                case 0x15: RotateLeftThroughCarry(ref HL.Low);
                    break;
                case 0x16: RotateLeftThroughCarry(HL.Value);
                    break;
                case 0x17: RotateLeftThroughCarry(ref AF.High);
                    break;
                case 0x18: RotateRightThroughCarry(ref BC.High);
                    break;
                case 0x19: RotateRightThroughCarry(ref BC.Low);
                    break;
                case 0x1A: RotateRightThroughCarry(ref DE.High);
                    break;
                case 0x1B: RotateRightThroughCarry(ref DE.Low);
                    break;
                case 0x1C: RotateRightThroughCarry(ref HL.High);
                    break;
                case 0x1D: RotateRightThroughCarry(ref HL.Low);
                    break;
                case 0x1E: RotateRightThroughCarry(HL.Value);
                    break;
                case 0x1F: RotateRightThroughCarry(ref AF.High);
                    break;
                case 0x20: LogicalShiftLeft(ref BC.High);
                    break;
                case 0x21: LogicalShiftLeft(ref BC.Low);
                    break;
                case 0x22: LogicalShiftLeft(ref DE.High);
                    break;
                case 0x23: LogicalShiftLeft(ref DE.Low);
                    break;
                case 0x24: LogicalShiftLeft(ref HL.High);
                    break;
                case 0x25: LogicalShiftLeft(ref HL.Low);
                    break;
                case 0x26: LogicalShiftLeft(HL.Value);
                    break;
                case 0x27: LogicalShiftLeft(ref AF.High);
                    break;
                case 0x28: ArithmeticShiftRight(ref BC.High);
                    break;
                case 0x29: ArithmeticShiftRight(ref BC.Low);
                    break;
                case 0x2A: ArithmeticShiftRight(ref DE.High);
                    break;
                case 0x2B: ArithmeticShiftRight(ref DE.Low);
                    break;
                case 0x2C: ArithmeticShiftRight(ref HL.High);
                    break;
                case 0x2D: ArithmeticShiftRight(ref HL.Low);
                    break;
                case 0x2E: ArithmeticShiftRight(HL.Value);
                    break;
                case 0x2F: ArithmeticShiftRight(ref AF.High);
                    break;
                case 0x30: SwapNibbles(ref BC.High);
                    break;
                case 0x31: SwapNibbles(ref BC.Low);
                    break;
                case 0x32: SwapNibbles(ref DE.High);
                    break;
                case 0x33: SwapNibbles(ref DE.Low);
                    break;
                case 0x34: SwapNibbles(ref HL.High);
                    break;
                case 0x35: SwapNibbles(ref HL.Low);
                    break;
                case 0x36: SwapNibbles(HL.Value);
                    break;
                case 0x37: SwapNibbles(ref AF.High);
                    break;
                case 0x38: LogicalShiftRight(ref BC.High);
                    break;
                case 0x39: LogicalShiftRight(ref BC.Low);
                    break;
                case 0x3A: LogicalShiftRight(ref DE.High);
                    break;
                case 0x3B: LogicalShiftRight(ref DE.Low);
                    break;
                case 0x3C: LogicalShiftRight(ref HL.High);
                    break;
                case 0x3D: LogicalShiftRight(ref HL.Low);
                    break;
                case 0x3E: LogicalShiftRight(HL.Value);
                    break;
                case 0x3F: LogicalShiftRight(ref AF.High);
                    break;
                case 0x40: TestBit(BC.High, 0);
                    break;
                case 0x41: TestBit(BC.Low, 0);
                    break;
                case 0x42: TestBit(DE.High, 0);
                    break;
                case 0x43: TestBit(DE.Low, 0);
                    break;
                case 0x44: TestBit(HL.High, 0);
                    break;
                case 0x45: TestBit(HL.Low, 0);
                    break;
                case 0x46: TestBit(ReadMemory(HL.Value), 0);
                    break;
                case 0x47: TestBit(AF.High, 0);
                    break;
                case 0x48: TestBit(BC.High, 1);
                    break;
                case 0x49: TestBit(BC.Low, 1);
                    break;
                case 0x4A: TestBit(DE.High, 1);
                    break;
                case 0x4B: TestBit(DE.Low, 1);
                    break;
                case 0x4C: TestBit(HL.High, 1);
                    break;
                case 0x4D: TestBit(HL.Low, 1);
                    break;
                case 0x4E: TestBit(ReadMemory(HL.Value), 1);
                    break;
                case 0x4F: TestBit(AF.High, 1);
                    break;
                case 0x50: TestBit(BC.High, 2);
                    break;
                case 0x51: TestBit(BC.Low, 2);
                    break;
                case 0x52: TestBit(DE.High, 2);
                    break;
                case 0x53: TestBit(DE.Low, 2);
                    break;
                case 0x54: TestBit(HL.High, 2);
                    break;
                case 0x55: TestBit(HL.Low, 2);
                    break;
                case 0x56: TestBit(ReadMemory(HL.Value), 2);
                    break;
                case 0x57: TestBit(AF.High, 2);
                    break;
                case 0x58: TestBit(BC.High, 3);
                    break;
                case 0x59: TestBit(BC.Low, 3);
                    break;
                case 0x5A: TestBit(DE.High, 3);
                    break;
                case 0x5B: TestBit(DE.Low, 3);
                    break;
                case 0x5C: TestBit(HL.High, 3);
                    break;
                case 0x5D: TestBit(HL.Low, 3);
                    break;
                case 0x5E: TestBit(ReadMemory(HL.Value), 3);
                    break;
                case 0x5F: TestBit(AF.High, 3);
                    break;
                case 0x60: TestBit(BC.High, 4);
                    break;
                case 0x61: TestBit(BC.Low, 4);
                    break;
                case 0x62: TestBit(DE.High, 4);
                    break;
                case 0x63: TestBit(DE.Low, 4);
                    break;
                case 0x64: TestBit(HL.High, 4);
                    break;
                case 0x65: TestBit(HL.Low, 4);
                    break;
                case 0x66: TestBit(ReadMemory(HL.Value), 4);
                    break;
                case 0x67: TestBit(AF.High, 4);
                    break;
                case 0x68: TestBit(BC.High, 5);
                    break;
                case 0x69: TestBit(BC.Low, 5);
                    break;
                case 0x6A: TestBit(DE.High, 5);
                    break;
                case 0x6B: TestBit(DE.Low, 5);
                    break;
                case 0x6C: TestBit(HL.High, 5);
                    break;
                case 0x6D: TestBit(HL.Low, 5);
                    break;
                case 0x6E: TestBit(ReadMemory(HL.Value), 5);
                    break;
                case 0x6F: TestBit(AF.High, 5);
                    break;
                case 0x70: TestBit(BC.High, 6);
                    break;
                case 0x71: TestBit(BC.Low, 6);
                    break;
                case 0x72: TestBit(DE.High, 6);
                    break;
                case 0x73: TestBit(DE.Low, 6);
                    break;
                case 0x74: TestBit(HL.High, 6);
                    break;
                case 0x75: TestBit(HL.Low, 6);
                    break;
                case 0x76: TestBit(ReadMemory(HL.Value), 6);
                    break;
                case 0x77: TestBit(AF.High, 6);
                    break;
                case 0x78: TestBit(BC.High, 7);
                    break;
                case 0x79: TestBit(BC.Low, 7);
                    break;
                case 0x7A: TestBit(DE.High, 7);
                    break;
                case 0x7B: TestBit(DE.Low, 7);
                    break;
                case 0x7C: TestBit(HL.High, 7);
                    break;
                case 0x7D: TestBit(HL.Low, 7);
                    break;
                case 0x7E: TestBit(ReadMemory(HL.Value), 7);
                    break;
                case 0x7F: TestBit(AF.High, 7);
                    break;
                case 0x80: Util.ClearBits(ref BC.High, 0);
                    break;
                case 0x81: Util.ClearBits(ref BC.Low, 0);
                    break;
                case 0x82: Util.ClearBits(ref DE.High, 0);
                    break;
                case 0x83: Util.ClearBits(ref DE.Low, 0);
                    break;
                case 0x84: Util.ClearBits(ref HL.High, 0);
                    break;
                case 0x85: Util.ClearBits(ref HL.Low, 0);
                    break;
                case 0x86: ClearBits(HL.Value, 0);
                    break;
                case 0x87: Util.ClearBits(ref AF.High, 0);
                    break;
                case 0x88: Util.ClearBits(ref BC.High, 1);
                    break;
                case 0x89: Util.ClearBits(ref BC.Low, 1);
                    break;
                case 0x8A: Util.ClearBits(ref DE.High, 1);
                    break;
                case 0x8B: Util.ClearBits(ref DE.Low, 1);
                    break;
                case 0x8C: Util.ClearBits(ref HL.High, 1);
                    break;
                case 0x8D: Util.ClearBits(ref HL.Low, 1);
                    break;
                case 0x8E: ClearBits(HL.Value, 1);
                    break;
                case 0x8F: Util.ClearBits(ref AF.High, 1);
                    break;
                case 0x90: Util.ClearBits(ref BC.High, 2);
                    break;
                case 0x91: Util.ClearBits(ref BC.Low, 2);
                    break;
                case 0x92: Util.ClearBits(ref DE.High, 2);
                    break;
                case 0x93: Util.ClearBits(ref DE.Low, 2);
                    break;
                case 0x94: Util.ClearBits(ref HL.High, 2);
                    break;
                case 0x95: Util.ClearBits(ref HL.Low, 2);
                    break;
                case 0x96: ClearBits(HL.Value, 2);
                    break;
                case 0x97: Util.ClearBits(ref AF.High, 2);
                    break;
                case 0x98: Util.ClearBits(ref BC.High, 3);
                    break;
                case 0x99: Util.ClearBits(ref BC.Low, 3);
                    break;
                case 0x9A: Util.ClearBits(ref DE.High, 3);
                    break;
                case 0x9B: Util.ClearBits(ref DE.Low, 3);
                    break;
                case 0x9C: Util.ClearBits(ref HL.High, 3);
                    break;
                case 0x9D: Util.ClearBits(ref HL.Low, 3);
                    break;
                case 0x9E: ClearBits(HL.Value, 3);
                    break;
                case 0x9F: Util.ClearBits(ref AF.High, 3);
                    break;
                case 0xA0: Util.ClearBits(ref BC.High, 4);
                    break;
                case 0xA1: Util.ClearBits(ref BC.Low, 4);
                    break;
                case 0xA2: Util.ClearBits(ref DE.High, 4);
                    break;
                case 0xA3: Util.ClearBits(ref DE.Low, 4);
                    break;
                case 0xA4: Util.ClearBits(ref HL.High, 4);
                    break;
                case 0xA5: Util.ClearBits(ref HL.Low, 4);
                    break;
                case 0xA6: ClearBits(HL.Value, 4);
                    break;
                case 0xA7: Util.ClearBits(ref AF.High, 4);
                    break;
                case 0xA8: Util.ClearBits(ref BC.High, 5);
                    break;
                case 0xA9: Util.ClearBits(ref BC.Low, 5);
                    break;
                case 0xAA: Util.ClearBits(ref DE.High, 5);
                    break;
                case 0xAB: Util.ClearBits(ref DE.Low, 5);
                    break;
                case 0xAC: Util.ClearBits(ref HL.High, 5);
                    break;
                case 0xAD: Util.ClearBits(ref HL.Low, 5);
                    break;
                case 0xAE: ClearBits(HL.Value, 5);
                    break;
                case 0xAF: Util.ClearBits(ref AF.High, 5);
                    break;
                case 0xB0: Util.ClearBits(ref BC.High, 6);
                    break;
                case 0xB1: Util.ClearBits(ref BC.Low, 6);
                    break;
                case 0xB2: Util.ClearBits(ref DE.High, 6);
                    break;
                case 0xB3: Util.ClearBits(ref DE.Low, 6);
                    break;
                case 0xB4: Util.ClearBits(ref HL.High, 6);
                    break;
                case 0xB5: Util.ClearBits(ref HL.Low, 6);
                    break;
                case 0xB6: ClearBits(HL.Value, 6);
                    break;
                case 0xB7: Util.ClearBits(ref AF.High, 6);
                    break;
                case 0xB8: Util.ClearBits(ref BC.High, 7);
                    break;
                case 0xB9: Util.ClearBits(ref BC.Low, 7);
                    break;
                case 0xBA: Util.ClearBits(ref DE.High, 7);
                    break;
                case 0xBB: Util.ClearBits(ref DE.Low, 7);
                    break;
                case 0xBC: Util.ClearBits(ref HL.High, 7);
                    break;
                case 0xBD: Util.ClearBits(ref HL.Low, 7);
                    break;
                case 0xBE: ClearBits(HL.Value, 7);
                    break;
                case 0xBF: Util.ClearBits(ref AF.High, 7);
                    break;
                case 0xC0: Util.SetBits(ref BC.High, 0);
                    break;
                case 0xC1: Util.SetBits(ref BC.Low, 0);
                    break;
                case 0xC2: Util.SetBits(ref DE.High, 0);
                    break;
                case 0xC3: Util.SetBits(ref DE.Low, 0);
                    break;
                case 0xC4: Util.SetBits(ref HL.High, 0);
                    break;
                case 0xC5: Util.SetBits(ref HL.Low, 0);
                    break;               
                case 0xC6: SetBits(HL.Value, 0);
                    break;
                case 0xC7: Util.SetBits(ref AF.High, 0);
                    break;
                case 0xC8: Util.SetBits(ref BC.High, 1);
                    break;
                case 0xC9: Util.SetBits(ref BC.Low, 1);
                    break;
                case 0xCA: Util.SetBits(ref DE.High, 1);
                    break;
                case 0xCB: Util.SetBits(ref DE.Low, 1);
                    break;
                case 0xCC: Util.SetBits(ref HL.High, 1);
                    break;
                case 0xCD: Util.SetBits(ref HL.Low, 1);
                    break;
                case 0xCE: SetBits(HL.Value, 1);
                    break;
                case 0xCF: Util.SetBits(ref AF.High, 1);
                    break;
                case 0xD0: Util.SetBits(ref BC.High, 2);
                    break;
                case 0xD1: Util.SetBits(ref BC.Low, 2);
                    break;
                case 0xD2: Util.SetBits(ref DE.High, 2);
                    break;
                case 0xD3: Util.SetBits(ref DE.Low, 2);
                    break;
                case 0xD4: Util.SetBits(ref HL.High, 2);
                    break;
                case 0xD5: Util.SetBits(ref HL.Low, 2);
                    break;
                case 0xD6: SetBits(HL.Value, 2);
                    break;
                case 0xD7: Util.SetBits(ref AF.High, 2);
                    break;
                case 0xD8: Util.SetBits(ref BC.High, 3);
                    break;
                case 0xD9: Util.SetBits(ref BC.Low, 3);
                    break;
                case 0xDA: Util.SetBits(ref DE.High, 3);
                    break;
                case 0xDB: Util.SetBits(ref DE.Low, 3);
                    break;
                case 0xDC: Util.SetBits(ref HL.High, 3);
                    break;
                case 0xDD: Util.SetBits(ref HL.Low, 3);
                    break;
                case 0xDE: SetBits(HL.Value, 3);
                    break;
                case 0xDF: Util.SetBits(ref AF.High, 3);
                    break;
                case 0xE0: Util.SetBits(ref BC.High, 4);
                    break;
                case 0xE1: Util.SetBits(ref BC.Low, 4);
                    break;
                case 0xE2: Util.SetBits(ref DE.High, 4);
                    break;
                case 0xE3: Util.SetBits(ref DE.Low, 4);
                    break;
                case 0xE4: Util.SetBits(ref HL.High, 4);
                    break;
                case 0xE5: Util.SetBits(ref HL.Low, 4);
                    break;
                case 0xE6: SetBits(HL.Value, 4);
                    break;
                case 0xE7: Util.SetBits(ref AF.High, 4);
                    break;
                case 0xE8: Util.SetBits(ref BC.High, 5);
                    break;
                case 0xE9: Util.SetBits(ref BC.Low, 5);
                    break;
                case 0xEA: Util.SetBits(ref DE.High, 5);
                    break;
                case 0xEB: Util.SetBits(ref DE.Low, 5);
                    break;
                case 0xEC: Util.SetBits(ref HL.High, 5);
                    break;
                case 0xED: Util.SetBits(ref HL.Low, 5);
                    break;
                case 0xEE: SetBits(HL.Value, 5);
                    break;
                case 0xEF: Util.SetBits(ref AF.High, 5);
                    break;
                case 0xF0: Util.SetBits(ref BC.High, 6);
                    break;
                case 0xF1: Util.SetBits(ref BC.Low, 6);
                    break;
                case 0xF2: Util.SetBits(ref DE.High, 6);
                    break;
                case 0xF3: Util.SetBits(ref DE.Low, 6);
                    break;
                case 0xF4: Util.SetBits(ref HL.High, 6);
                    break;
                case 0xF5: Util.SetBits(ref HL.Low, 6);
                    break;
                case 0xF6: SetBits(HL.Value, 6);
                    break;
                case 0xF7: Util.SetBits(ref AF.High, 6);
                    break;
                case 0xF8: Util.SetBits(ref BC.High, 7);
                    break;
                case 0xF9: Util.SetBits(ref BC.Low, 7);
                    break;
                case 0xFA: Util.SetBits(ref DE.High, 7);
                    break;
                case 0xFB: Util.SetBits(ref DE.Low, 7);
                    break;
                case 0xFC: Util.SetBits(ref HL.High, 7);
                    break;
                case 0xFD: Util.SetBits(ref HL.Low, 7);
                    break;
                case 0xFE: SetBits(HL.Value, 7);
                    break;
                case 0xFF: Util.SetBits(ref AF.High, 7);
                    break;
            }
        }

        // Power-up sequence, reset registers and memory
        // http://problemkaputt.de/pandocs.htm#powerupsequence
        private void Reset()
        {
            AF = new Register();
            BC = new Register();
            DE = new Register();
            HL = new Register();
            StackPointer = new Register();
            ProgramCounter = 0;
            Halted = false;
            Stopped = false;
            TimerEnabled = false;
            TimerCycles = 0;
            DividerCycles = 0;
            scanlineCycleCounter = 0;

            int cyclesPerSecond = Properties.Settings.Default.cyclesPerSecond;
            // The divider increments at rate of 16384Hz
            CyclesPerDividerIncrement = cyclesPerSecond / 16384;
            // The timer increments at rate of 4096Hz
            CyclesPerTimerIncrement = cyclesPerSecond / 4096;

            AF.Value = 0x01B0;
            BC.Value = 0x0013;
            DE.Value = 0x00D8;
            HL.Value = 0x014D;
            
            StackPointer.Value = 0xFFFE;
            ProgramCounter = 0x100;

            Memory.Reset();
            Display = new Display(Memory);
            interruptQueue = new Queue<bool>();
        }

        private MemoryBankController CreateMBC(CartType cartType, Stream fileStream)
        {
            MemoryBankController mbc = null;

            switch (cartType)
            {
                case CartType.RomOnly:
                    mbc = new RomOnly(fileStream);
                    break;
                case CartType.MBC1:
                    mbc = new Mbc1(fileStream);
                    break;
                case CartType.MBC2:
                    mbc = new Mbc2(fileStream);
                    break;
            }

            return mbc;
        }
    }
}
