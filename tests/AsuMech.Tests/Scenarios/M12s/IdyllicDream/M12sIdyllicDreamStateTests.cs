using System.Numerics;
using AsuMech.Core.Game.Party;
using AsuMech.Scenarios;
using AsuMech.Scenarios.M12s.IdyllicDream;
using State = AsuMech.Scenarios.M12s.IdyllicDream.M12sIdyllicDreamState;

namespace AsuMech.Tests;

public class M12sIdyllicDreamStateTests
{
    [Test]
    public void EachPlatformHasOneTowerOfEachKindAndEachPairOneTriple()
    {
        for (var seed = 0; seed < 300; seed++)
        {
            var state = new State(new Rng(seed));
            Assert.That(state.Towers, Has.Count.EqualTo(8));
            foreach (var side in new[] { -1, 1 })
            {
                var towers = state.Towers.Where(t => t.Side == side).ToList();
                Assert.That(towers.Select(t => t.Kind), Is.EquivalentTo(Enum.GetValues<TowerKind>()));
                foreach (var inner in new[] { true, false })
                {
                    var pair = towers.Where(t => t.Inner == inner).ToList();
                    Assert.That(pair.Select(t => t.ZSign), Is.EquivalentTo(new[] { -1, 1 }));
                    Assert.That(pair.Count(t => t.Triple), Is.EqualTo(1));
                }
            }
        }
    }

    [Test]
    public void EveryPlayerHasTheirOwnCloneAndThePlayerStandsWhereTheyPicked()
    {
        foreach (var player in Enum.GetValues<PartyRole>())
        for (var direction = 0; direction < State.Directions; direction++)
        {
            var state = new State(new Rng(direction * 11 + (int)player), new M12sIdyllicDreamStateOverrides { PlayerCloneDirection = direction }, player);
            Assert.That(state.CloneDirection[player], Is.EqualTo(direction));
            Assert.That(state.CloneDirection.Values, Is.EquivalentTo(Enumerable.Range(0, State.Directions)));
            Assert.That(state.CloneOwner(direction), Is.EqualTo(player));
        }
    }

    [Test]
    public void BossClonesAlternateAndEachResolvingPairSharesItsMechanic()
    {
        foreach (var cardinalsDefa in new[] { true, false })
        {
            var state = new State(new Rng(3), new M12sIdyllicDreamStateOverrides { CardinalBossClonesDefa = cardinalsDefa });
            for (var d = 0; d < State.Directions; d++)
            {
                Assert.That(state.IsDefaClone(d), Is.Not.EqualTo(state.IsDefaClone((d + 1) % State.Directions)));
                Assert.That(State.ResolveSlot(d), Is.EqualTo(State.ResolveSlot((d + 4) % State.Directions)));
                Assert.That(state.IsDefaClone(d), Is.EqualTo(state.IsDefaClone((d + 4) % State.Directions)));
            }
            Assert.That(state.IsDefaClone(0), Is.EqualTo(cardinalsDefa));
        }
    }

    [Test]
    public void TheReenactmentReplaysTheClonesThatShowedFirst()
    {
        foreach (var cardinalsFirst in new[] { true, false })
        {
            var state = new State(new Rng(5), new M12sIdyllicDreamStateOverrides { CardinalClonesFirst = cardinalsFirst });
            Assert.That(state.ReenactedFirst, Has.Exactly(4).Items);
            Assert.That(state.ReenactedFirst.All(d => State.IsCardinal(d) == cardinalsFirst));
        }
    }

    [Test]
    public void TheSideAwayFromTheEastWestConesIsSafeFromTheRememberedCleaves()
    {
        for (var seed = 0; seed < 100; seed++)
        {
            var state = new State(new Rng(seed));
            var z = -MathF.Sign(state.EastWestConesZ) * 3f;
            foreach (var x in new[] { -13f, -11.5f, 11.5f, 13f })
            foreach (var dz in new[] { -0.8f, 0.8f })
                Assert.That(state.InFirstReplay(new Vector2(x, z + dz)), Is.False, $"seed {seed} at ({x}, {z + dz})");
            Assert.That(state.InFirstReplay(Vector2.Zero), Is.True);
        }
    }

    [Test]
    public void ThePlatformsLeaveAGapUnderTheBoss()
    {
        Assert.That(State.OnPlatform(new Vector3(-14f, 0f, 0f)), Is.True);
        Assert.That(State.OnPlatform(new Vector3(14f, 0f, 9f)), Is.True);
        Assert.That(State.OnPlatform(Vector3.Zero), Is.False);
        Assert.That(State.OnPlatform(new Vector3(14f, 0f, 12f)), Is.False);
    }

    [Test]
    public void TheLightSpreadsPickOneRole()
    {
        var supports = new State(new Rng(1), new M12sIdyllicDreamStateOverrides { SpreadOnSupports = true });
        Assert.That(Enum.GetValues<PartyRole>().Where(supports.IsSpreadRole), Is.EquivalentTo(new[] { PartyRole.MainTank, PartyRole.OffTank, PartyRole.RegenHealer, PartyRole.ShieldHealer }));
        var dps = new State(new Rng(1), new M12sIdyllicDreamStateOverrides { SpreadOnSupports = false });
        Assert.That(Enum.GetValues<PartyRole>().Where(dps.IsSpreadRole).All(r => r.IsDps()));
    }
}
