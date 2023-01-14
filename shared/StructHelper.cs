using System;
using System.Runtime.InteropServices;

public static class StructHelper
{
    // 'struct' to byte[]
    public static byte[] ToByteArray(object structure)
    {
        int size = Marshal.SizeOf(structure);
        byte[] arr = new byte[size];

        IntPtr ptr = IntPtr.Zero;
        try
        {
            ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(structure, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }

        return arr;
    }

    // byte[] to struct<T>
    public static T ToStructure<T>(byte[] arr) where T : struct
    {
        T userCmd = new T();
        int size = Marshal.SizeOf(userCmd);
        IntPtr ptr = IntPtr.Zero;

        try
        {
            ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(arr, 0, ptr, size);
            userCmd = (T)Marshal.PtrToStructure(ptr, userCmd.GetType());
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }

        return userCmd;
    }
}