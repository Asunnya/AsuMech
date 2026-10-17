using System;
using System.Numerics;
using AnoMech.Core.SimObjects;

namespace AnoMech.Core.Map;

// A tower drawn by a MapEffect slot, lit by how many party members stand in it: `occupied[n - 1]`
// for n inside (counts past the array clamp to its last entry), `empty` once nobody is. With no
// `empty` the slot keeps the highest count reached. Tick only while the tower shows.
public sealed class MapEffectTower(MapController map, byte slot, Vector3 position, float radius, uint[] occupied, uint? empty)
{
    private int shown;

    public void Tick(SimParty party)
    {
        var count = Math.Min(party.Find.InsideCircle(position, radius).Count, occupied.Length);
        if (count == shown || (count < shown && empty is null)) return;
        shown = count;
        if (count > 0) map.AddEffect(occupied[count - 1], slot);
        else map.AddEffect(empty!.Value, slot);
    }
}
