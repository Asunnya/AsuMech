using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.M12s.Act1;

// Where Directed Grotesquerie sprays, turned from its holder's facing.
public enum GrotesquerieArrow
{
    Front,
    Right,
    Back,
    Left,
}

public enum Act1Debuff
{
    Spread,
    Stack,
    Shared,
}

public sealed class M12sAct1State
{
    // The two blood vessel layouts; each leaves one spot on the west wall and one on the east.
    public static readonly Vector3[][] VesselLayouts =
    [
        [new(-17.7f, 0f, 13.5f), new(-6f, 0f, 8.5f), new(12.5f, 0f, 5.7f), new(-11.6f, 0f, -10.8f), new(7f, 0f, -7.8f)],
        [new(9.2f, 0f, 9.6f), new(-9.3f, 0f, 12.1f), new(12.6f, 0f, -11.4f), new(-4.1f, 0f, -8.6f), new(-10.1f, 0f, 0.1f)],
    ];

    // Bursting Grotesquerie (spreads) goes to one role group, the other stacks on Shared Grotesquerie.
    public bool DpsSpread { get; }
    public PartyRole SharedHolder { get; }
    public IReadOnlyDictionary<PartyRole, GrotesquerieArrow> Arrows { get; }
    // The dragon head rises east and Ravenous Reach sweeps west from (10, 0), or the reverse.
    public bool EastSafe { get; }
    public int VesselLayout { get; }
    public PartyRole StackHealer { get; }

    public M12sAct1State(Rng rng, M12sAct1StateOverrides? overrides = null, PartyRole? player = null)
    {
        overrides ??= new M12sAct1StateOverrides();
        var dpsSpread = rng.NextBool();
        var sharedPick = rng.Next(4);
        var arrows = rng.Shuffle(Enum.GetValues<GrotesquerieArrow>().SelectMany(a => new[] { a, a })).ToList();
        var eastSafe = rng.NextBool();
        var layout = rng.Next(VesselLayouts.Length);
        var stackHealer = rng.NextBool() ? PartyRole.RegenHealer : PartyRole.ShieldHealer;

        if (player is { } me && overrides.PlayerDebuff is { } debuff)
            dpsSpread = me.IsDps() == (debuff == Act1Debuff.Spread);
        DpsSpread = dpsSpread;
        var stackers = Enum.GetValues<PartyRole>().Where(r => r.IsDps() != DpsSpread).ToList();
        var shared = stackers[sharedPick];
        if (player is { } holder && overrides.PlayerDebuff is { } own && own != Act1Debuff.Spread)
            shared = own == Act1Debuff.Shared ? holder : stackers.Where(r => r != holder).ElementAt(sharedPick % 3);
        SharedHolder = shared;

        var roles = Enum.GetValues<PartyRole>();
        if (player is { } aimer && overrides.PlayerArrow is { } mine)
        {
            var swap = arrows.IndexOf(mine);
            (arrows[swap], arrows[(int)aimer]) = (arrows[(int)aimer], arrows[swap]);
        }
        Arrows = roles.ToDictionary(r => r, r => arrows[(int)r]);
        EastSafe = overrides.EastSafe ?? eastSafe;
        VesselLayout = overrides.VesselLayout ?? layout;
        StackHealer = overrides.StackHealer ?? stackHealer;
    }

    public bool Spreads(PartyRole role) => role.IsDps() == DpsSpread;

    // Directed Grotesquerie's arrow param, 0x40C front to 0x40F left.
    public static ushort ArrowParam(GrotesquerieArrow arrow) => (ushort)(0x40C + (int)arrow);

    // The arrow's turn from the holder's facing (heading: +π/2 turns from south to east).
    public static float ArrowTurn(GrotesquerieArrow arrow) => arrow switch
    {
        GrotesquerieArrow.Right => -MathF.PI / 2f,
        GrotesquerieArrow.Back => MathF.PI,
        GrotesquerieArrow.Left => MathF.PI / 2f,
        _ => 0f,
    };

    // The vessel layout's safe spot on the east (true) or west wall.
    public static Vector3 VesselSafeSpot(int layout, bool east) => (layout, east) switch
    {
        (0, false) => new(-18.5f, 0f, 0.5f),
        (0, true) => new(19f, 0f, -13.5f),
        (_, false) => new(-18f, 0f, -12.5f),
        _ => new(18.5f, 0f, 0.5f),
    };
}
