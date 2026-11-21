using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace AsuMech.Core.Native.Implementations;

// The FC chambers' furniture lives in the housing manager, not the territory, so a client-side
// zone change leaves it standing in the fight's arena. Hidden while the stay runs, shown again
// before the chambers reload.
internal static unsafe class ChambersFurniture
{
    private static readonly HashSet<nint> Hidden = [];

    public static void Hide()
    {
        var before = Hidden.Count;
        var array = ObjectArray();
        if (array == null) return;
        foreach (var ptr in array->Objects)
        {
            var obj = ptr.Value;
            if (obj == null || !Hidden.Add((nint)obj)) continue;
            SetShown(obj, false);
        }
        if (Hidden.Count != before) DiagnosticLog.Info($"[ChambersFurniture] Hid {Hidden.Count - before} housing objects ({Hidden.Count} in all).");
    }

    public static void Show()
    {
        if (Hidden.Count == 0) return;
        var shown = 0;
        var array = ObjectArray();
        // Only objects still in the list: the rest were freed and must not be touched.
        if (array != null)
            foreach (var ptr in array->Objects)
                if (ptr.Value != null && Hidden.Contains((nint)ptr.Value)) { SetShown(ptr.Value, true); shown++; }
        DiagnosticLog.Info($"[ChambersFurniture] Showed {shown} of {Hidden.Count} hidden housing objects again.");
        Hidden.Clear();
    }

    private static void SetShown(GameObject* obj, bool shown)
    {
        if (obj->SharedGroupLayoutInstance is var layout && layout != null)
        {
            layout->SetActive(shown);
            layout->SetColliderActive(shown);
        }
        if (obj->DrawObject != null)
        {
            if (shown) obj->EnableDraw();
            else obj->DisableDraw();
        }
    }

    private static HousingObjectManager.HousingObjectArray* ObjectArray()
    {
        var housing = HousingManager.Instance();
        if (housing == null || housing->IndoorTerritory == null) return null;
        return &housing->IndoorTerritory->FurnitureManager.ObjectManager.ObjectArray;
    }
}
