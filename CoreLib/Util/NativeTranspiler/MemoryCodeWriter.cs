using System;
using System.Runtime.InteropServices;
using Iced.Intel;

namespace CoreLib.Util;

public sealed class MemoryCodeWriter : CodeWriter
{
    private IntPtr startAddress;
    private int byteIndex;
        
    public MemoryCodeWriter(IntPtr startAddress)
    {
        this.startAddress = startAddress;
        byteIndex = 0;
    }
        
    public override void WriteByte(byte value)
    {
        IntPtr ptr = startAddress + byteIndex;
        Marshal.WriteByte(ptr, value);
        byteIndex++;
    }
}