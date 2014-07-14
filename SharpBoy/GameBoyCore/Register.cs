using System;
using System.Runtime.InteropServices;

/*
 * Reference: http://problemkaputt.de/pandocs.htm#cpuregistersandflags
 * Using an explicit layout since registers can be used
 * as 16-bit values and 8-bit values. It's syntactically
 * nicer to read register.Low to retrieve the low order byte
 * as opposed to always masking with register & 0x00FF.
 */
[StructLayout(LayoutKind.Explicit)]
[Serializable]
public class Register
{
    [FieldOffset(0)]
    public ushort Value;

    [FieldOffset(0)]
    public byte Low;

    [FieldOffset(1)]
    public byte High;
}
