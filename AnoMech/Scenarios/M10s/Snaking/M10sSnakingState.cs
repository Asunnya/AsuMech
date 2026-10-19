using System;
using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.M10s.Snaking;

public sealed class M10sSnakingState
{
    // Firesnaking takes the four players nearest Red Hot, or the four furthest; Watersnaking the rest.
    public bool FireOnTheNearest { get; }
    // Deep Blue's protean corner on the west wall: south (z +10) or north (z -10).
    public bool DeepBlueSouth { get; }
    public bool DoubleDip { get; }
    // The wall Sick Swell's wave rises at; it later pushes away from it.
    public bool WaveFromNorth { get; }

    // Where each protean helper aimed its first cone, by helper.
    public List<float> FirstCones { get; } = [];

    public M10sSnakingState(Rng rng, M10sSnakingStateOverrides? overrides = null, PartyRole? player = null)
    {
        overrides ??= new M10sSnakingStateOverrides();
        var nearest = rng.NextBool();
        if (player is { } me && overrides.PlayerFire is { } fire) nearest = fire == StartsByRedHot(me);
        FireOnTheNearest = overrides.FireOnTheNearest ?? nearest;
        var south = rng.NextBool();
        DeepBlueSouth = overrides.DeepBlueSouth ?? south;
        var doubleDip = rng.NextBool();
        DoubleDip = overrides.DoubleDip ?? doubleDip;
        var north = rng.NextBool();
        WaveFromNorth = overrides.WaveFromNorth ?? north;
    }

    // Light party two waits by Red Hot, light party one by Deep Blue.
    public static bool StartsByRedHot(PartyRole role) =>
        role is PartyRole.OffTank or PartyRole.ShieldHealer or PartyRole.MeleeDpsB or PartyRole.CasterDps;

    public Vector3 DeepBlueCorner => new(-20f, 0f, DeepBlueSouth ? 10f : -10f);

    public Vector3 WaveWall => new(0f, 0f, WaveFromNorth ? -40f : 40f);

    public float PushHeading => WaveFromNorth ? 0f : MathF.PI;
}
