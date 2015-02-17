/*
 * Copyright 2014 Bryan Davis
 * Licensed under GPLv2
 * Refer to the license.txt file included
 */

namespace SharpBoy.Core
{
    /*
     * This portion of the class contains all supporting methods used by the
     * op code execution.
     */
    public partial class CPU
    {
        private void LoadValueToRegister8Bit(ref byte register)
        {
            register = ReadNextValue();
        }

        private void LoadRegisterToRegister8Bit(ref byte toRegister, byte fromRegister)
        {
            toRegister = fromRegister;
        }

        private void LoadMemoryToRegister8Bit(ref byte toRegister, ushort address)
        {
            toRegister = Memory[address];
        }

        private void LoadValueToMemory8Bit(ushort address, byte value)
        {
            Memory[address] = value;
        }

        private void LoadValueToRegister16Bit(Register register)
        {
            register.Value = ReadNextTwoValues();
        }

        private void LoadRegisterToRegister16Bit(Register toRegister, Register fromRegister)
        {
            toRegister.Value = fromRegister.Value;
        }

        private void LoadMemoryToRegister16Bit(Register toRegister, ushort address)
        {
            toRegister.Value = Memory[address];
        }

        private void LoadRegisterToMemory(Register register)
        {
            ushort address = ReadNextTwoValues();
            Memory[address] = register.Low;
            Memory[address + 1] = register.High;
        }

        // Reminder: Stack pointer starts in high memory and moves its way down.
        private void PushAddressOntoStack(ushort address)
        {
            // High byte first, then low byte.
            Memory[--StackPointer.Value] = (byte)(address >> 8);
            Memory[--StackPointer.Value] = (byte)(address & 0x00FF);
        }

        private void PopValuesIntoRegister(Register register)
        {
            register.Low = Memory[StackPointer.Value++];
            register.High = Memory[StackPointer.Value++];
        }

        private void AddValueToRegisterA(byte value, bool addCarryFlag = false)
        {
            int result = AF.High + value;
            int halfCarryResult = (AF.High & 0x0F) + (value & 0x0F);
            
            if (addCarryFlag && IsFlagSet(FlagC))
            {
                result++;
                halfCarryResult++;
            }
            
            ClearAllFlags();

            if ((result & 0xFF) == 0)
                SetFlag(FlagZ);            

            if (halfCarryResult > 0x0F)
                SetFlag(FlagH);

            if (result > 0xFF)
                SetFlag(FlagC);

            AF.High = (byte)result;
        }

        private void SubtractValueFromRegisterA(byte value, bool subCarryFlag = false)
        {
            int result = AF.High - value;
            int halfCarryResult = (AF.High & 0x0F) - (value & 0x0F);
            if (subCarryFlag && IsFlagSet(FlagC))
            {
                result--;
                halfCarryResult--;
            }

            ClearAllFlags();

            if ((result & 0xFF) == 0)
                SetFlag(FlagZ);

            SetFlag(FlagN);

            if (halfCarryResult < 0)
                SetFlag(FlagH);

            if (result < 0)
                SetFlag(FlagC);

            AF.High = (byte)result;
        }

        private void AndWithRegisterA(byte value)
        {
            AF.High &= value;
            ClearAllFlags();

            if (AF.High == 0)
                SetFlag(FlagZ);

            SetFlag(FlagH);
        }

        private void OrWithRegisterA(byte value)
        {
            AF.High |= value;
            ClearAllFlags();

            if (AF.High == 0)
                SetFlag(FlagZ);
        }

        private void XorWithRegisterA(byte value)
        {
            AF.High ^= value;
            ClearAllFlags();

            if (AF.High == 0)
                SetFlag(FlagZ);
        }

        private void CompareWithRegisterA(byte value)
        {
            ClearAllFlags();

            if (AF.High == value)
                SetFlag(FlagZ);

            SetFlag(FlagN);

            if ((AF.High & 0x0F) < (value & 0x0F))
                SetFlag(FlagH);

            if (AF.High < value)
                SetFlag(FlagC);
        }

        private void IncrementRegister8Bit(ref byte register)
        {
            register++;
            HandleIncrementFlags(register);
        }

        private void IncrementRegister16Bit(Register register)
        {
            register.Value++;
        }

        private void IncrementMemory(ushort address)
        {
            Memory[address]++;
            HandleIncrementFlags(Memory[address]);
        }

        private void DecrementRegister8Bit(ref byte register)
        {
            register--;
            HandleDecrementFlags(register);
        }

        private void DecrementRegister16Bit(Register register)
        {
            register.Value--;
        }

        private void DecrementMemory(ushort address)
        {
            Memory[address]--;
            HandleDecrementFlags(Memory[address]);
        }

        // Without random forum posts like these, we'd be lost...
        // http://forums.nesdev.com/viewtopic.php?p=42143&sid=c43232c8909ba277476955fd1e11db67#p42143
        private void LoadStackPointerToRegisterHL()
        {
            byte value = ReadNextValue();
            ClearAllFlags();

            // Set the carry flag if there was an overflow on the lower byte
            if ((StackPointer.Low + value) > 0xFF)
                SetFlag(FlagC);

            // Set the half carry flag if there was an overflow in the lower 4 bits
            int halfCarryResult = (StackPointer.Value & 0x0F) + (value & 0x0F);
            if (halfCarryResult > 0x0F)
                SetFlag(FlagH);

            HL.Value = (ushort)(StackPointer.Value + (sbyte)value);
        }

        private void HandleIncrementFlags(byte value)
        {
            if (value == 0)
                SetFlag(FlagZ);
            else
                ClearFlag(FlagZ);

            ClearFlag(FlagN);

            // If we incremented from 0x0F to 0x10, then we carried from bit 3.
            if ((value & 0x0F) == 0)
                SetFlag(FlagH);
            else
                ClearFlag(FlagH);
        }

        private void HandleDecrementFlags(byte value)
        {
            if (value == 0)
                SetFlag(FlagZ);
            else
                ClearFlag(FlagZ);

            SetFlag(FlagN);

            // If the lower nibble is 0x0F, then we decremented from 0x10 (lower nibble)
            // and thus borrowed from bit 4.
            if ((value & 0x0F) == 0x0F)
                SetFlag(FlagH);
            else
                ClearFlag(FlagH);
        }

        private void AddValueToRegisterHL(Register register)
        {
            int result = HL.Value + register.Value;

            ClearFlag(FlagN);

            int halfCarryResult = (HL.Value & 0xFFF) + (register.Value & 0xFFF);
            if (halfCarryResult > 0xFFF)
                SetFlag(FlagH);
            else
                ClearFlag(FlagH);

            if (result > 0xFFFF)
                SetFlag(FlagC);
            else
                ClearFlag(FlagC);

            HL.Value = (ushort)result;
        }

        // Without random forum posts like these, we'd be lost...
        // http://forums.nesdev.com/viewtopic.php?p=42143&sid=c43232c8909ba277476955fd1e11db67#p42143
        private void AddValueToStackPointer()
        {
            byte value = ReadNextValue();
            ClearAllFlags();

            // Set the carry flag if there was an overflow on the lower byte
            if ((StackPointer.Low + value) > 0xFF)
                SetFlag(FlagC);

            // Set the half carry flag if there was an overflow in the lower 4 bits
            int halfCarryResult = (StackPointer.Value & 0x0F) + (value & 0x0F);
            if (halfCarryResult > 0x0F)
                SetFlag(FlagH);
            
            StackPointer.Value = (ushort)(StackPointer.Value + (sbyte)value);
        }

        private void SwapNibbles(ref byte register)
        {
            byte value = register;
            register = GetSwappedByte(value);
            HandleSwapFlags(register);
        }

        private void SwapNibbles(ushort address)
        {
            byte value = Memory[address];
            Memory[address] = GetSwappedByte(value);
            HandleSwapFlags(Memory[address]);
        }

        private byte GetSwappedByte(byte value)
        {
            byte newLowerNibble = (byte)((value & 0xF0) >> 4);
            byte newUpperNibble = (byte)((value & 0x0F) << 4);
            return (byte)(newUpperNibble | newLowerNibble);
        }

        private void HandleSwapFlags(byte value)
        {
            ClearAllFlags();
            if (value == 0)
                SetFlag(FlagZ);
        }

        // This link contains the best explanation of this instruction that I could find.
        // http://www.worldofspectrum.org/faq/reference/z80reference.htm#DAA
        // Unfortunately, the implementation is a bit different from the above link; it was
        // the only way I could get the Blargg 01-special test to pass.
        private void DecimalAdjustRegisterA()
        {
            // Correction will result in either 0x00, 0x06, 0x60, or 0x66
            short value = AF.High;

            if (IsFlagSet(FlagN))
            {
                if (IsFlagSet(FlagH))
                    value -= 0x06;

                if (IsFlagSet(FlagC))
                    value -= 0x60;
            }
            else
            {
                // Lower 4 bits are greater than 9
                if ((value & 0x0F) > 0x09 || IsFlagSet(FlagH))
                    value += 0x06;

                // Upper 4 bits are greater than 9
                if ((value >> 4) > 0x09 || IsFlagSet(FlagC))
                    value += 0x60;
            }

            ClearFlag(FlagH);
            ClearFlag(FlagZ);

            if (value > 0xFF)
                SetFlag(FlagC);

            value &= 0xFF;

            if (value == 0)
                SetFlag(FlagZ);

            AF.High = (byte)value;
        }

        private void ComplementRegisterA()
        {
            AF.High ^= 0xFF;
            SetFlag(FlagN);
            SetFlag(FlagH);
        }

        private void SetCarryFlag()
        {
            ClearFlag(FlagN);
            ClearFlag(FlagH);
            SetFlag(FlagC);
        }

        private void ComplementCarryFlag()
        {
            ClearFlag(FlagN);
            ClearFlag(FlagH);
            if (IsFlagSet(FlagC))
                ClearFlag(FlagC);
            else
                SetFlag(FlagC);
        }

        // RLCA
        private void RotateALeftNoCarry()
        {
            bool highBitSet = (AF.High & 0x80) == 0x80;
            AF.High = (byte)((AF.High << 1) | (AF.High >> 7));
            ClearAllFlags();
            // If bit 7 was set before the rotate.
            if (highBitSet)
                SetFlag(FlagC);
        }

        // RLA
        private void RotateALeftThroughCarry()
        {
            bool highBitSet = (AF.High & 0x80) == 0x80;
            AF.High <<= 1;
            
            if (IsFlagSet(FlagC))
                AF.High |= 0x01;

            ClearAllFlags();
            if (highBitSet)
                SetFlag(FlagC);
        }

        // RRCA
        private void RotateARightNoCarry()
        {
            bool lowBitSet = (AF.High & 0x01) == 0x01;
            AF.High = (byte)((AF.High << 7) | (AF.High) >> 1);
            ClearAllFlags();
            // If bit 0 was set before the rotate.
            if (lowBitSet)
                SetFlag(FlagC);
        }

        // RRA
        private void RotateARightThroughCarry()
        {
            bool lowBitSet = (AF.High & 0x01) == 0x01;
            AF.High >>= 1;

            if (IsFlagSet(FlagC))
                AF.High |= 0x80;

            ClearAllFlags();
            if (lowBitSet)
                SetFlag(FlagC);
        }

        // RLC
        private void RotateLeftNoCarry(ref byte register)
        {
            bool highBitSet = (register & 0x80) == 0x80;
            register = (byte)((register << 1) | (register >> 7));

            HandleShiftFlags(register, highBitSet);
        }

        // RLC
        private void RotateLeftNoCarry(ushort address)
        {
            byte value = Memory[address];
            RotateLeftNoCarry(ref value);
            Memory[address] = value;
        }        

        // RL
        private void RotateLeftThroughCarry(ref byte register)
        {
            bool highBitSet = (register & 0x80) == 0x80;
            
            register <<= 1;
            if (IsFlagSet(FlagC))
                register |= 0x01;

            HandleShiftFlags(register, highBitSet);
        }

        // RL
        private void RotateLeftThroughCarry(ushort address)
        {
            byte value = Memory[address];
            RotateLeftThroughCarry(ref value);
            Memory[address] = value;
        }

        // RRC
        private void RotateRightNoCarry(ref byte register)
        {
            bool lowBitSet = (register & 0x01) == 0x01;
            register = (byte)((register << 7) | (register) >> 1);

            HandleShiftFlags(register, lowBitSet);
        }

        // RRC
        private void RotateRightNoCarry(ushort address)
        {
            byte value = Memory[address];
            RotateRightNoCarry(ref value);
            Memory[address] = value;
        }

        // RR
        private void RotateRightThroughCarry(ref byte register)
        {
            bool lowBitSet = (register & 0x01) == 0x01;
            register >>= 1;

            if (IsFlagSet(FlagC))
                register |= 0x80;

            HandleShiftFlags(register, lowBitSet);
        }

        // RR
        private void RotateRightThroughCarry(ushort address)
        {
            byte value = Memory[address];
            RotateRightThroughCarry(ref value);
            Memory[address] = value;
        }

        private void LogicalShiftLeft(ref byte register)
        {
            bool highBitSet = (register & 0x80) == 0x80;
            register <<= 1;

            HandleShiftFlags(register, highBitSet);
        }

        private void LogicalShiftLeft(ushort address)
        {
            byte value = Memory[address];
            LogicalShiftLeft(ref value);
            Memory[address] = value;
        }

        private void ArithmeticShiftRight(ref byte register)
        {
            bool highBitSet = (register & 0x80) == 0x80;
            bool lowBitSet = (register & 0x01) == 0x01;
            register >>= 1;
            // Keep the original MSB value
            if (highBitSet)
                register |= 0x80;

            HandleShiftFlags(register, lowBitSet);
        }

        private void ArithmeticShiftRight(ushort address)
        {
            byte value = Memory[address];
            ArithmeticShiftRight(ref value);
            Memory[address] = value;
        }

        private void LogicalShiftRight(ref byte register)
        {
            bool lowBitSet = (register & 0x01) == 0x01;
            register >>= 1;

            HandleShiftFlags(register, lowBitSet);
        }

        private void LogicalShiftRight(ushort address)
        {
            byte value = Memory[address];
            LogicalShiftRight(ref value);
            Memory[address] = value;
        }

        // Support for all registers except A when rotating/circular shifting.
        // Support for all registers when shifting.
        private void HandleShiftFlags(byte value, bool bitSet)
        {
            ClearAllFlags();

            if (value == 0)
                SetFlag(FlagZ);

            // If bit was set before the rotate/shift.
            if (bitSet)
                SetFlag(FlagC);
        }

        // http://www.pastraiser.com/cpu/gameboy/gameboy_opcodes.html
        private void TestBit(byte value, byte bit)
        {
            if (Util.IsBitSet(value, bit))
                ClearFlag(FlagZ);
            else
                SetFlag(FlagZ);

            ClearFlag(FlagN);
            SetFlag(FlagH);
        }        

        private void Jump(ushort address)
        {            
            ProgramCounter = address;
        }

        private void Jump(sbyte offset)
        {
            ProgramCounter = (ushort)(ProgramCounter + offset);
        }

        private void ConditionallyJump(bool condition, ushort address)
        {
            if (condition)
            {
                ProgramCounter = address;
                ConditionExecuted = true;
            }
            else
            {
                ConditionExecuted = false;
            }
        }

        private void ConditionallyJump(bool condition, sbyte offset)
        {
            if (condition)
            {
                ProgramCounter = (ushort)(ProgramCounter + offset);
                ConditionExecuted = true;
            }
            else
            {
                ConditionExecuted = false;
            }
        }

        private void Call(ushort address)
        {
            // MSByte first.
            PushAddressOntoStack(ProgramCounter);
            ProgramCounter = address;
        }

        private void ConditionallyCall(bool condition, ushort address)
        {
            if (condition)
            {
                Call(address);
                ConditionExecuted = true;
            }
            else
            {
                ConditionExecuted = false;
            }
        }

        private void Restart(byte offset)
        {
            PushAddressOntoStack(ProgramCounter);
            ushort address = (ushort)(0x0000 + offset);
            ProgramCounter = address;
        }

        private void Return()
        {
            byte low = Memory[StackPointer.Value++];            
            byte high = Memory[StackPointer.Value++];
            ProgramCounter = (ushort)((high << 8) | low);
        }

        private void ConditionallyReturn(bool condition)
        {
            if (condition)
            {
                Return();
                ConditionExecuted = true;
            }
            else
            {
                ConditionExecuted = false;
            }
        }

        private void ReturnAndEnableInterrupts()
        {
            Return();
            interruptQueue.Clear();
            interruptQueue.Enqueue(true);
        }

        private void EnableInterrupts()
        {
            interruptQueue.Clear();
            // Interrupts aren't processed until after the next op code is processed,
            // which is why we push false and then true.
            interruptQueue.Enqueue(false);
            interruptQueue.Enqueue(true);
        }

        private void DisableInterrupts()
        {
            interruptQueue.Clear();
            // Interrupts aren't disabled until after the next op code is processed,
            // which is why we push true and then false.
            interruptQueue.Enqueue(true);
            interruptQueue.Enqueue(false);
        }

        private void ClearAllFlags()
        {
            AF.Low = 0;
        }

        private void ClearFlag(byte flag)
        {
            // Bitwise operations like ~ are performed as an int, hence the cast
            AF.Low &= (byte)~flag;
        }

        private void SetFlag(byte flag)
        {
            AF.Low |= flag;
        }

        private bool IsFlagSet(byte flag)
        {
            return (AF.Low & flag) == flag;
        }

        private bool LCDEnabled()
        {
            return Util.IsBitSet(Memory[Util.LcdControlAddress], 7);
        }


        private void Halt()
        {
            Halted = true;
            if (interruptQueue.Count == 0)
                interruptQueue.Enqueue(true);
        }
    }
}