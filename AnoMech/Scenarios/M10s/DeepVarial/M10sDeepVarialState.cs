using System;
using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.M10s.DeepVarial;

public sealed class M10sDeepVarialState
{
    public const float LaneOffset = 15f;
    public const int Jumps = 4;

    // Light party two (OT, H2, M2, R2) carries fire when true, else light party one does.
    public bool SecondPartyFire { get; }
    // The big wave rose at the north wall and pushes south; Deep Blue stands at the wall opposite it.
    public bool WaveFromNorth { get; }
    public bool VarialSpread { get; }
    public int Lane { get; }
    public bool TakeoffSpread { get; }

    // Where Hot Aerial landed, in jump order.
    public List<Vector3> AerialLandings { get; } = [];
    public List<Vector3> InfernoPuddles { get; } = [];
    // Set once Cutback Blaze aims: the bearing, from Red Hot, of the slice it spares.
    public float? SafeSliceRotation { get; set; }
    public Vector3 CutbackOrigin { get; set; }

    public M10sDeepVarialState(Rng rng, M10sDeepVarialStateOverrides? overrides = null, PartyRole? player = null)
    {
        overrides ??= new M10sDeepVarialStateOverrides();
        var second = rng.NextBool();
        if (player is { } me && overrides.PlayerFire is { } fire) second = fire == InSecondParty(me);
        SecondPartyFire = second;
        var north = rng.NextBool();
        WaveFromNorth = overrides.WaveFromNorth ?? north;
        var varialSpread = rng.NextBool();
        VarialSpread = overrides.VarialSpread ?? varialSpread;
        var lane = rng.Next(3) - 1;
        Lane = overrides.Lane ?? lane;
        var takeoffSpread = rng.NextBool();
        TakeoffSpread = overrides.TakeoffSpread ?? takeoffSpread;
    }

    public static bool InSecondParty(PartyRole role) =>
        role is PartyRole.OffTank or PartyRole.ShieldHealer or PartyRole.MeleeDpsB or PartyRole.CasterDps;

    public bool IsFire(PartyRole role) => InSecondParty(role) == SecondPartyFire;

    // +1 when the wave pushes south and Deep Blue's wall is the south one, -1 mirrored.
    public float Side => WaveFromNorth ? 1f : -1f;

    public float PushHeading => WaveFromNorth ? 0f : MathF.PI;

    public Vector3 DeepBlueWall => new(0f, 0f, 20f * Side);

    public float LaneX => Lane * LaneOffset;

    public Vector3 LaneStart => new(LaneX, 0f, -25f * Side);
}
