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

        // TODO: Verify correct endianess is used
        private void LoadRegisterToMemory(Register register)
        {
            ushort address = ReadNextTwoValues();
            Memory[address] = register.Low;
            Memory[address + 1] = register.High;
        }

        // Reminder: Stack pointer starts in high memory and moves its way down
        private void PushRegisterOntoStack(Register register)
        {
            Memory[StackPointer.Value--] = register.High;
            Memory[StackPointer.Value--] = register.Low;
        }

        private void PopValuesIntoRegister(Register register)
        {
            register.Low = Memory[StackPointer.Value++];
            register.High = Memory[StackPointer.Value++];
        }

        private void AddValueToRegisterA(byte value, bool addCarryFlag = false)
        {
            byte addend = value;
            if (addCarryFlag && IsFlagSet(FlagC))
                addend++;   // It's okay for this to overflow to 0
            int result = RegisterAF.High + addend;
            ResetAllFlags();

            if (result == 0)
                SetFlag(FlagZ);

            ResetFlag(FlagN);

            int halfCarryResult = (RegisterAF.High & 0x0F) + (addend & 0x0F);
            if (halfCarryResult > 0x0F)
                SetFlag(FlagH);

            if (result > 0xFF)
                SetFlag(FlagC);

            RegisterAF.High = (byte)result;
        }

        private void SubtractValueFromRegisterA(byte value, bool subCarryFlag = false)
        {
            byte subtrahend = value;
            if (subCarryFlag && IsFlagSet(FlagC))
                subtrahend++;   // It's okay for this to overflow to 0
            int result = RegisterAF.High - subtrahend;
            ResetAllFlags();

            if (result == 0)
                SetFlag(FlagZ);

            SetFlag(FlagN);

            int halfCarryResult = (RegisterAF.High & 0x0F) - (subtrahend & 0x0F);
            if (halfCarryResult < 0)
                SetFlag(FlagH);

            if (result < 0)
                SetFlag(FlagC);

            RegisterAF.High = (byte)result;
        }

        private void AndWithRegisterA(byte value)
        {
            RegisterAF.High &= value;
            ResetAllFlags();

            if (RegisterAF.High == 0)
                SetFlag(FlagZ);

            SetFlag(FlagH);
        }

        private void OrWithRegisterA(byte value)
        {
            RegisterAF.High |= value;
            ResetAllFlags();

            if (RegisterAF.High == 0)
                SetFlag(FlagZ);
        }

        private void XorWithRegisterA(byte value)
        {
            RegisterAF.High ^= value;
            ResetAllFlags();

            if (RegisterAF.High == 0)
                SetFlag(FlagZ);
        }

        private void CompareWithRegisterA(byte value)
        {
            int result = RegisterAF.High - value;
            ResetAllFlags();

            if (result == 0)
                SetFlag(FlagZ);

            SetFlag(FlagN);

            int halfCarryResult = (RegisterAF.High & 0x0F) - (value & 0x0F);
            if (halfCarryResult < 0)
                SetFlag(FlagH);

            if (result < 0)
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

        private void LoadStackPointerToRegisterHL()
        {
            sbyte value = (sbyte)ReadNextValue();
            int result = StackPointer.Value + value;
            RegisterHL.Value = (ushort)result;
            ResetAllFlags();

            // Set the carry flag if there was an overflow
            if (result > 0xFFFF)
                SetFlag(FlagC);

            // Set the half carry flag if there was an overflow in the lower 4 bits
            int halfCarryResult = (StackPointer.Value & 0x0F) + (value & 0x0F);
            if (halfCarryResult > 0x0F)
                SetFlag(FlagH);
        }

        private void HandleIncrementFlags(byte value)
        {
            if (value == 0)
                SetFlag(FlagZ);
            else
                ResetFlag(FlagZ);

            ResetFlag(FlagN);

            // If we incremented from 0x0F to 0x10, then we carried from bit 3.
            if ((value & 0x0F) == 0)
                SetFlag(FlagH);
            else
                ResetFlag(FlagH);
        }

        private void HandleDecrementFlags(byte value)
        {
            if (value == 0)
                SetFlag(FlagZ);
            else
                ResetFlag(FlagZ);

            SetFlag(FlagN);

            // If the lower nibble is 0x0F, then we decremented from 0x10 (lower nibble)
            // and thus borrowed from bit 4.
            if ((value & 0x0F) == 0x0F)
                SetFlag(FlagH);
            else
                ResetFlag(FlagH);
        }

        private void AddValueToRegisterHL(Register register)
        {
            int result = RegisterHL.Value + register.Value;

            ResetFlag(FlagN);

            int halfCarryResult = (RegisterHL.Value & 0xFFF) + (register.Value & 0xFFF);
            if (halfCarryResult > 0xFFF)
                SetFlag(FlagH);
            else
                ResetFlag(FlagH);

            if (result > 0xFFFF)
                SetFlag(FlagC);
            else
                ResetFlag(FlagC);

            RegisterHL.Value = (ushort)result;
        }

        private void AddValueToStackPointer()
        {
            sbyte value = (sbyte)ReadNextValue();
            int result = StackPointer.Value + value;
            StackPointer.Value = (ushort)result;
            ResetAllFlags();

            // Set the carry flag if there was an overflow
            if (result > 0xFFFF)
                SetFlag(FlagC);

            // Set the half carry flag if there was an overflow in the lower 4 bits
            int halfCarryResult = (StackPointer.Value & 0x0F) + (value & 0x0F);
            if (halfCarryResult > 0x0F)
                SetFlag(FlagH);
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
            ResetAllFlags();
            if (value == 0)
                SetFlag(FlagZ);
        }

        // This link contains the best explanation of this instruction that I could find.
        // http://www.worldofspectrum.org/faq/reference/z80reference.htm#DAA
        private void DecimalAdjustRegisterA()
        {
            byte value = RegisterAF.High;
            // Correction will result in either 0x00, 0x06, 0x60, or 0x66
            byte correction = 0x00;

            if (value > 0x99 || IsFlagSet(FlagC))
            {
                correction |= (0x06 << 4);
                SetFlag(FlagC);
            }
            else           
                ResetFlag(FlagC);            

            if ((value & 0x0F) > 0x09 || IsFlagSet(FlagH))            
                correction |= 0x06;

            if (IsFlagSet(FlagN))
                RegisterAF.High -= correction;
            else
                RegisterAF.High += correction;

            if (RegisterAF.High == 0)
                SetFlag(FlagZ);
            else
                ResetFlag(FlagZ);

            // Per pandocs and the Game Boy CPU manual, flag H is reset,
            // which differs from typical Z80 operation according to the link above.
            ResetFlag(FlagH);
        }

        private void ComplementRegisterA()
        {
            RegisterAF.High ^= 0xFF;
            SetFlag(FlagN);
            SetFlag(FlagH);
        }

        private void SetCarryFlag()
        {
            ResetFlag(FlagN);
            ResetFlag(FlagH);
            SetFlag(FlagC);
        }

        private void ComplementCarryFlag()
        {
            ResetFlag(FlagN);
            ResetFlag(FlagH);
            if (IsFlagSet(FlagC))
                ResetFlag(FlagC);
            else
                SetFlag(FlagC);
        }

        private void ResetAllFlags()
        {
            RegisterAF.Low = 0;
        }

        private void ResetFlag(byte flag)
        {
            // Bitwise operations like ~ are performed as an int, hence the cast
            RegisterAF.Low &= (byte)~flag;
        }

        private void SetFlag(byte flag)
        {
            RegisterAF.Low |= flag;
        }

        private bool IsFlagSet(byte flag)
        {
            return (RegisterAF.Low & flag) == flag;
        }
    }
}