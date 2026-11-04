using System;
using System.Collections.Generic;
using Dalamud.Interface.ManagedFontAtlas;

namespace AnoMech.Windows.Asu;

// The default font rebuilt per window size; a size still building shows the last one that was ready.
internal static class AsuFonts
{
    private static readonly Dictionary<int, IFontHandle> Handles = new();
    private static IFontHandle? lastReady;

    public static IDisposable? Push(int percent)
    {
        if (percent == 100)
        {
            lastReady = null;
            return null;
        }
        if (!Handles.TryGetValue(percent, out var handle))
        {
            var multiplier = percent / 100f;
            handle = Plugin.PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(
                e => e.OnPreBuild(tk => tk.AddDalamudDefaultFont(-multiplier)));
            Handles[percent] = handle;
        }
        if (handle.Available) lastReady = handle;
        return lastReady?.Push();
    }

    public static void Dispose()
    {
        foreach (var handle in Handles.Values) handle.Dispose();
        Handles.Clear();
        lastReady = null;
    }
}
