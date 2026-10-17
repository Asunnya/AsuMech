using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Fru.FruConstants;

namespace AnoMech.Scenarios.Fru;

// P2's light puddles: Diamond Dust's Sinbound Holy and Light Rampant's Luminous Hammer leave the same EObj.
public sealed class FruHolyPuddles
{
    private const float Radius = 6f;
    private const uint Arg2Base = 0x50;

    // The puddle lands where its target stood this long before the hit, so it trails a moving target.
    public const float SnapshotBeforeHit = 0.35f;

    private readonly List<Puddle> puddles = [];
    private readonly Dictionary<SimCharacter, Vector3> snapshots = [];

    private sealed class Puddle(int wave, Vector3 position, float armedAt)
    {
        public int Wave { get; } = wave;
        public Vector3 Position { get; } = position;
        public float ArmedAt { get; } = armedAt;
        public SimEventObject? Visual { get; set; }
        public bool Faded { get; set; }
    }

    public void Clear()
    {
        puddles.Clear();
        snapshots.Clear();
    }

    public void Snapshot(SimCharacter target) => snapshots[target] = target.Position;

    public void Drop(int wave, SimCharacter target, float armedAt) =>
        puddles.Add(new Puddle(wave, snapshots.Remove(target, out var position) ? position : target.Position, armedAt));

    public void Show(SimWorld world, int wave)
    {
        var i = 0;
        foreach (var puddle in puddles.Where(p => p.Wave == wave))
        {
            puddle.Visual = world.SpawnEventObject(new EventObjectSpawnConfig
            {
                EObjId = EObjId.HolyPuddle,
                Placement = new Placement(puddle.Position, 0f),
                EventId = DirectorEventId,
                TargetableStatus = 5,
                Arg2 = (Arg2Base + (uint)(wave * 2 + i)) << 16 | 3,
            });
            i++;
        }
    }

    // Harmless from the fade on: retail players slide through faded puddles unhurt.
    public void Fade(int wave)
    {
        foreach (var puddle in puddles.Where(p => p.Wave == wave))
        {
            puddle.Visual?.DirectorEObjMod(7);
            puddle.Faded = true;
        }
    }

    public void Remove(int wave)
    {
        foreach (var puddle in puddles.Where(p => p.Wave == wave)) puddle.Visual?.Despawn();
        puddles.RemoveAll(p => p.Wave == wave);
    }

    public void KillWhoeverStandsIn(SimParty party, float elapsed, uint actionId, string explanation)
    {
        foreach (var member in party.ActiveMembers().ToList())
            if (puddles.Any(p => !p.Faded && elapsed >= p.ArmedAt && Vector3.Distance(p.Position, member.Position) <= Radius))
                member.Die(actionId, explanation);
    }
}
