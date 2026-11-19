using System.Numerics;
using AsuMech.Core.Game.Party;
using AsuMech.Scenarios.Fru.P2MirrorMirror;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.Fru.FruConstants;
using static AsuMech.Tests.NegativeRun;

namespace AsuMech.Tests;

// Pinned: Hallowed Ray on MT, the blue mirror W at (-20,0), the reds N and E, Banish as pairs on
// the supports.
//
//    7.75 Hallowed Ray from (0,0) at the stack on MT.
//   25.02 Scythe Kick from (7.5,0); tanks and melee around her, the rest around the blue mirror.
//   31.37 House of Light from the blue mirror, 31.45 from the Usurper, each at its four nearest.
//   41.42 the reds' House of Light: the healers and ranged at N, tanks and melee at E.
//   46.84 Banish pairs: MT+R (-5.1,-5.1), OT+C (5.1,-5.1), RH+M1 (-5.1,5.1), SH+M2 (5.1,5.1).
public class FruP2MirrorMirrorScenarioTests
{
    private static NegativeRun<FruP2MirrorMirrorScenario> MirrorMirror(PartyRole player, Action<FruP2MirrorMirrorStateOverrides>? tweak = null)
        => Negative<FruP2MirrorMirrorScenario>(player)
            .Overrides<FruP2MirrorMirrorStateOverrides>(o =>
            {
                o.RayTarget = MainTank;
                o.BlueOctant = 6;
                o.FirstRedOctant = 0;
                o.Pairs = true;
                o.SupportPairs = true;
                tweak?.Invoke(o);
            });

    [Test]
    public void RayTargetLeavingTheStackDies()
        => MirrorMirror(MainTank)
            .TeleportAt(7.1f, to: new(10, 0))
            .ShouldKill(ActionId.HallowedRay, MainTank);

    [Test]
    public void HallowedRayLandsWellAfterSevenSeconds()
        => MirrorMirror(MainTank)
            .TeleportAt(7.6f, to: new(10, 0))
            .ShouldKill(ActionId.HallowedRay, MainTank);

    [Test]
    public void StandingOutsideTheKickHoleDies()
        => MirrorMirror(OffTank)
            .TeleportAt(30.5f, to: new(3, 5))
            .ShouldKill(ActionId.ScytheKick, OffTank);

    [Test]
    public void MissingTheRedMirrorsHoleDies()
        => MirrorMirror(RegenHealer)
            .TeleportAt(40.5f, to: new(5, -5))
            .ShouldKill(ActionId.ReflectedScytheKickCast, RegenHealer);

    [Test]
    public void TwoBaitsOnOneSpotAtTheUsurperDie()
        => MirrorMirror(MeleeDpsA)
            .TeleportAt(31.2f, to: new(7.118f, 2.167f))
            .ShouldKill(ActionId.HouseOfLightDiamondDust, MainTank, MeleeDpsA);

    [Test]
    public void BanishPairLeftAloneDies()
        => MirrorMirror(MainTank)
            .TeleportAt(46.5f, to: new(10, 10))
            .ShouldKill(ActionId.BanishPairs, MainTank);

    [Test]
    public void BanishSpreadsOverlappingDie()
        => MirrorMirror(MainTank, o => o.Pairs = false)
            .TeleportAt(46.5f, to: new(7.63f, -0.1f))
            .ShouldKill(ActionId.BanishSpread, MainTank, OffTank);
}
