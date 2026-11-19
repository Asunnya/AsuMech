using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Fru.FruConstants;

namespace AsuMech.Scenarios.Fru;

// P2's puddles: Diamond Dust's Sinbound Holy and Light Rampant's Luminous Hammer leave the same light EObj,
// the Ice Veil's Hiemal Rays an ice one.
public sealed class FruPuddles(uint eobjId, float radius)
{
    private const uint Arg2Base = 0x50;
    private const float BleedingDuration = 30f;
    private const float BleedingKillsAfter = 1.5f;

    // The puddle lands where its target stood this long before the hit, so it trails a moving target.
    public const float SnapshotBeforeHit = 0.35f;

    private readonly List<Puddle> puddles = [];
    private readonly Dictionary<SimCharacter, Vector3> snapshots = [];
    private uint shown;

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
        shown = 0;
    }

    public void Snapshot(SimCharacter target) => snapshots[target] = target.Position;

    public void Drop(int wave, SimCharacter target, float armedAt) =>
        puddles.Add(new Puddle(wave, snapshots.Remove(target, out var position) ? position : target.Position, armedAt));

    public void Show(SimWorld world, int wave)
    {
        foreach (var puddle in puddles.Where(p => p.Wave == wave))
        {
            puddle.Visual = world.SpawnEventObject(new EventObjectSpawnConfig
            {
                EObjId = eobjId,
                Placement = new Placement(puddle.Position, 0f),
                EventId = DirectorEventId,
                TargetableStatus = 5,
                Arg2 = (Arg2Base + shown++) << 16 | 3,
            });
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

    // Retail's Bleeding is a DoT that kills on its next server tick, 0-3s after the step.
    public void BleedWhoeverStandsIn(SimWorld world, float elapsed, string explanation)
    {
        foreach (var member in world.Party.ActiveMembers().ToList())
            if (!member.HasStatus(StatusId.Bleeding)
                && puddles.Any(p => !p.Faded && elapsed >= p.ArmedAt && Vector3.Distance(p.Position, member.Position) <= radius))
            {
                member.AddStatus(StatusId.Bleeding, BleedingDuration);
                world.Events.Add(BleedingKillsAfter, () => member.DieOfStatus(StatusId.Bleeding, explanation));
            }
    }
}
