using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.Game;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;

namespace AnoMech.Core.Map;

// The sim's field markers; present only once a run placed some, so Despawn clears the field
// only after the sim wrote to it.
public sealed class SimWaymarks : ISimObject
{
    private readonly Coordinates coordinates;

    internal SimWaymarks(Coordinates coordinates)
    {
        this.coordinates = coordinates;
    }

    public bool IsActive => true;

    // Scenario-local offsets; replaces the whole set.
    public void Place(IReadOnlyList<Waymark> layout)
    {
        var markers = layout.Select(w => (w.Slot, coordinates.ToGlobal(w.Offset))).ToList();
        if (!Natives.Waymarks.Place(markers))
            Plugin.Log.Warning("Waymarks: MarkingController unavailable");
    }

    public void Tick(float deltaSeconds) { }

    public void Despawn() => Natives.Waymarks.ClearAll();
}
