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
        foreach (var obj in Objects())
        {
            if (!Hidden.Add((nint)obj)) continue;
            SetShown(obj, false);
        }
        if (Hidden.Count != before) DiagnosticLog.Info($"[ChambersFurniture] Hid {Hidden.Count - before} housing objects ({Hidden.Count} in all).");
    }

    public static void Show()
    {
        if (Hidden.Count == 0) return;
        // Only objects still in the list: the rest were freed and must not be touched.
        var shown = 0;
        foreach (var obj in Objects())
            if (Hidden.Contains((nint)obj)) { SetShown(obj, true); shown++; }
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

    private static List<nint> RawObjects()
    {
        var found = new List<nint>();
        var housing = HousingManager.Instance();
        if (housing == null || housing->IndoorTerritory == null) return found;
        foreach (var ptr in housing->IndoorTerritory->FurnitureManager.ObjectManager.ObjectArray.Objects)
            if (ptr.Value != null) found.Add((nint)ptr.Value);
        return found;
    }

    private static GameObject*[] Objects()
    {
        var raw = RawObjects();
        var objects = new GameObject*[raw.Count];
        for (var i = 0; i < raw.Count; i++) objects[i] = (GameObject*)raw[i];
        return objects;
    }
}
