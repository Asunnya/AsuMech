using System;

namespace AnoMech.Core.Native.Implementations.Interop;

public static unsafe class GameObjectHelper
{
    // UTF-8 into a name field of `size` bytes, terminator included, trimmed on a character
    // boundary so a peer's display name with non-ASCII letters is never cut mid-sequence.
    public static void WriteName(byte* field, int size, string name)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(name);
        while (bytes.Length > size - 1)
        {
            name = name[..^1];
            bytes = System.Text.Encoding.UTF8.GetBytes(name);
        }
        for (int i = 0; i < size; i++) field[i] = i < bytes.Length ? bytes[i] : (byte)0;
    }
}
