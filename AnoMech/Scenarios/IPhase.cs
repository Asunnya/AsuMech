using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios;

// A weather + bgm bundle grouping sibling scenarios (e.g. TOP P5's Delta/Sigma/Omega).
// Weather is the initial weather; a scenario may still change it live via world.SetWeather.
public interface IPhase
{
    string Name { get; }
    IZone Zone { get; }
    byte? Weather => null;
    ushort Bgm => 0;

    // Slot -> resting state where the phase's start differs from IZone.ArenaMapEffectSnapshot,
    // aggregated from the replay's single MapEffect packets between the snapshot and the phase
    // start.
    IReadOnlyDictionary<byte, ushort> ArenaMapEffectOverrides => ImmutableDictionary<byte, ushort>.Empty;

    // Optional phase-wide setup, between zone and scenario Run. Default no-op.
    void Run(SimWorld world) { }

    // Phase-wide client-side setup, run on host and peer alike (see IZone.RunClientSetup).
    // Default no-op.
    void RunClientSetup(SimWorld world) { }
}

// Default phase. Pass Init to attach custom phase-wide setup.
public sealed class Phase : IPhase
{
    private readonly Action<SimWorld>? init;
    private readonly Action<SimWorld>? clientSetup;

    public Phase(IZone zone, string name, byte? weather, ushort bgm, Action<SimWorld>? init = null, Action<SimWorld>? clientSetup = null,
        IReadOnlyDictionary<byte, ushort>? arenaMapEffectOverrides = null)
    {
        Zone = zone;
        Name = name;
        Weather = weather;
        Bgm = bgm;
        ArenaMapEffectOverrides = arenaMapEffectOverrides ?? ImmutableDictionary<byte, ushort>.Empty;
        this.init = init;
        this.clientSetup = clientSetup;
    }

    public IZone Zone { get; }
    public string Name { get; }
    public byte? Weather { get; }
    public ushort Bgm { get; }
    public IReadOnlyDictionary<byte, ushort> ArenaMapEffectOverrides { get; }

    public void Run(SimWorld world) => init?.Invoke(world);
    public void RunClientSetup(SimWorld world) => clientSetup?.Invoke(world);
}
