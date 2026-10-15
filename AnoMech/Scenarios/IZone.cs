using System;
using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Map;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios;

// A raid territory: the identity every scenario inside it shares. Authored bottom-up —
// scenarios point at their IPhase, phases point here — and ScenarioCatalog derives the downward
// zone -> phase -> scenario view for the menu.
public interface IZone
{
    string Name { get; }                                    // canonical duty name
    uint TerritoryId { get; }
    Vector3 Origin { get; }

    // At least one; [0] is the default.
    IReadOnlyList<WaymarkLayout> WaymarkPresets { get; }

    // From the server's zone-in spawn packet for the EObj. The player spawns at its Position.
    EntranceBarrier EntranceBarrier { get; }

    // Resting state per MapEffect slot, copied from the server's MapEffect snapshot packet in a
    // replay (the same at zone-in and every wipe reset). Sent at the start of every scenario,
    // merged with IPhase.ArenaMapEffectOverrides. Empty = the zone has no MapEffect slots.
    IReadOnlyList<ushort> ArenaMapEffectSnapshot => Array.Empty<ushort>();

    // Zone-wide setup, first in the cascade. Default no-op. Host/solo only -- may create
    // host-only simulation state (arena boundaries, scheduled MapEffect broadcasts) that
    // would conflict if a peer ran it too.
    void Run(SimWorld world) { }

    // Peer-safe subset of zone setup: pure client-side asset/table registration with no
    // simulation side effects (no SimArenaBoundary, no MapEffect broadcasts). Called for
    // BOTH host and peer, same reasoning as IScenario.RunInstanceEvents -- a peer's own
    // client needs this data (e.g. replay-derived RSV/RSF resource paths the server would
    // normally deliver for real duty content) just as much as the host's does. Default no-op.
    void RunClientSetup(SimWorld world) { }
}
