using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Ucob.UcobConstants;

namespace AsuMech.Scenarios.Ucob;

// Bahamut Prime's Tempest Wing tethers: they jump to the two players nearest him until they go off.
// Each holder hosts its tether toward Bahamut, as the game does: a character draws one tether, so
// two drawn from Bahamut would show as one.
public sealed class UcobTempestWing
{
    private const int Holders = 2;

    private readonly List<SimTether> tethers = [];
    private SimWorld? world;
    private SimCharacter? bahamut;

    public void Start(SimWorld on, SimCharacter? boss)
    {
        world = on;
        bahamut = boss;
        Follow();
    }

    public void Tick() => Follow();

    // The players holding a tether as it goes off, in the order they took it.
    public IReadOnlyList<SimCharacter> Release()
    {
        var holders = tethers.Select(t => t.A).OfType<SimCharacter>().Where(m => m.IsAlive()).ToList();
        Clear();
        return holders;
    }

    public void Clear()
    {
        foreach (var tether in tethers) tether.Despawn();
        tethers.Clear();
        world = null;
        bahamut = null;
    }

    private void Follow()
    {
        if (world is not { } on || bahamut is not { } boss) return;
        var nearest = AliveMembers(on).OrderBy(m => Vector3.DistanceSquared(m.Position, boss.Position)).Take(Holders).ToList();
        foreach (var tether in tethers.Where(t => t.A is not { } holder || !nearest.Contains(holder)).ToList())
        {
            tether.Despawn();
            tethers.Remove(tether);
        }
        foreach (var member in nearest.Where(m => tethers.All(t => !ReferenceEquals(t.A, m))))
            tethers.Add(on.Tether(member, boss, TetherId.TempestWing));
    }

    private static IEnumerable<SimCharacter> AliveMembers(SimWorld on)
    {
        for (var slot = 0; slot < 8; slot++)
            if (on.Party.Get(slot) is { } member && member.IsAlive())
                yield return member;
    }
}
