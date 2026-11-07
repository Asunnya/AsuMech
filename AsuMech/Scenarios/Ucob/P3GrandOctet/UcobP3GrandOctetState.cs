using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.Ucob.P3GrandOctet;

public sealed class UcobP3GrandOctetState
{
    public const int SlotCount = 8;
    public const int DragonCount = 5;
    public const int StackCount = 4;
    public const int TwisterCount = 4;
    public const float RimRadius = 24f;

    // UNVERIFIED: that they never move.
    public static readonly Vector2[] TowerSpots = [new(0f, -11f), new(11f, 0f), new(-7.78f, 7.78f), new(-7.78f, -7.78f)];

    // UNVERIFIED: every one of the eight spots can take any of them.
    public int BahamutSlot { get; }
    public int NaelSlot { get; }
    public int TwintaniaSlot { get; }
    // Indexed like UcobActions.Cauterize.
    public IReadOnlyList<int> DragonSlots { get; }

    // UNVERIFIED for an intercardinal Bahamut: only a cardinal one (dragons counterclockwise) was seen.
    public bool Counterclockwise => BahamutSlot % 2 == 0;
    public int Step => Counterclockwise ? -1 : 1;
    // The dragons by the order they dive in, as UcobActions.Cauterize indices.
    public IReadOnlyList<int> DiveOrder { get; }

    // Everyone carries exactly one of the eight boss markers.
    public PartyRole NaelTarget { get; }
    public IReadOnlyList<PartyRole> DragonTargets { get; }
    public PartyRole BahamutTarget { get; }
    public PartyRole TwintaniaTarget { get; }

    // UNVERIFIED: who gets the stacks and the Twisters looks random.
    public IReadOnlyList<PartyRole> StackTargets { get; }
    public IReadOnlyList<PartyRole> TwisterTargets { get; }

    public UcobP3GrandOctetState(Rng rng, UcobP3GrandOctetStateOverrides? overrides = null)
    {
        overrides ??= new UcobP3GrandOctetStateOverrides();
        var slots = overrides.Slots ?? RollSlots(rng, overrides.BahamutSlot);
        BahamutSlot = slots[0];
        NaelSlot = slots[1];
        TwintaniaSlot = slots[2];
        DragonSlots = slots.Skip(3).ToList();
        DiveOrder = Enumerable.Range(0, DragonCount).OrderBy(d => StepsFromBahamut(DragonSlots[d])).ToList();

        var markers = overrides.Markers ?? RollMarkers(rng, overrides.TwintaniaTarget);
        NaelTarget = markers[0];
        DragonTargets = markers.Skip(1).Take(DragonCount).ToList();
        BahamutTarget = markers[DragonCount + 1];
        TwintaniaTarget = markers[DragonCount + 2];

        var everyone = Enum.GetValues<PartyRole>();
        var stacks = rng.Shuffle(everyone).ToList();
        if (overrides.StackInclude is { } stack) SwapInto(stacks, stack, StackCount - 1, at => at >= StackCount);
        StackTargets = (overrides.StackTargets ?? stacks.Take(StackCount)).OrderBy(r => r).ToList();
        var twisters = rng.Shuffle(everyone).Take(TwisterCount);
        TwisterTargets = (overrides.TwisterTargets ?? twisters).OrderBy(r => r).ToList();
    }

    // 1 for the spot next to Bahamut the way the dragons dive, up to 7.
    public int StepsFromBahamut(int slot) => ((slot - BahamutSlot) * Step % SlotCount + SlotCount) % SlotCount;

    public static Vector2 SlotPosition(int slot, float radius = RimRadius) => AtBearing(slot * 45f, radius);

    public static Vector2 AtBearing(float degrees, float radius)
    {
        var radians = degrees * MathF.PI / 180f;
        return new Vector2(MathF.Sin(radians), -MathF.Cos(radians)) * radius;
    }

    // A pin is swapped into place, so it never changes how much is rolled.
    private static IReadOnlyList<int> RollSlots(Rng rng, int? bahamut)
    {
        var slots = rng.Shuffle(Enumerable.Range(0, SlotCount)).ToList();
        if (bahamut is { } slot) SwapInto(slots, ((slot % SlotCount) + SlotCount) % SlotCount, 0, at => at != 0);
        return slots;
    }

    private static IReadOnlyList<PartyRole> RollMarkers(Rng rng, PartyRole? twintania)
    {
        var markers = rng.Shuffle(Enum.GetValues<PartyRole>()).ToList();
        if (twintania is { } role) SwapInto(markers, role, markers.Count - 1, at => at != markers.Count - 1);
        return markers;
    }

    private static void SwapInto<T>(List<T> rolled, T value, int into, Func<int, bool> outOfPlace)
    {
        var at = rolled.IndexOf(value);
        if (at < 0 || !outOfPlace(at)) return;
        (rolled[at], rolled[into]) = (rolled[into], rolled[at]);
    }
}
