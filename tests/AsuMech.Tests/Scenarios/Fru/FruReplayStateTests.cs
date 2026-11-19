using AsuMech.Core.Game.Party;
using AsuMech.Scenarios.Fru.P2DiamondDust;
using AsuMech.Scenarios.Fru.P2LightRampant;
using AsuMech.Scenarios.Fru.P3Apocalypse;
using AsuMech.Scenarios.Fru.P3UltimateRelativity;
using AsuMech.Scenarios.Fru.P4CrystallizeTime;
using AsuMech.Scenarios.Fru.P4DarklitDragonsong;
using AsuMech.Scenarios.Fru.P5FulgentBlade;
using AsuMech.Scenarios.Fru.P5ParadiseRegained;

namespace AsuMech.Tests;

// A bot peer rebuilds the host's roll from its replay message; one no roll could have dealt is
// refused rather than played.
public class FruReplayStateTests
{
    private static readonly PartyRole[] Roles = Enum.GetValues<PartyRole>();

    [Test]
    public void ARollTheHostCouldHaveDealtIsRebuilt()
    {
        Assert.Multiple(() =>
        {
            Assert.That(FruP2DiamondDustState.FromNetworkReplay(3, true, false, 7, true, 0), Is.Not.Null);
            Assert.That(FruP2LightRampantState.FromNetworkReplay(Roles, true, false)?.Slot(7), Is.EqualTo(Roles[7]));
            Assert.That(FruP3ApocalypseState.FromNetworkReplay([3, 0, 1, 2, 0, 1, 2, 3], 2, true)?.WaterDuration(PartyRole.MainTank), Is.EqualTo(3));
            Assert.That(FruP3UltimateRelativityState.FromNetworkReplay(
                [RelativityDebuff.LongSupport, RelativityDebuff.ShortSupport, RelativityDebuff.MediumSupport, RelativityDebuff.LongSupport,
                 RelativityDebuff.ShortDps, RelativityDebuff.LongDps, RelativityDebuff.MediumDps, RelativityDebuff.ShortDps], true, 5, 200)?
                .Debuff(PartyRole.MainTank), Is.EqualTo(RelativityDebuff.LongSupport));
            Assert.That(FruP4CrystallizeTimeState.FromNetworkReplay(
                [CrystallizeDebuff.BlueWater, CrystallizeDebuff.RedAero, CrystallizeDebuff.RedIce, CrystallizeDebuff.BlueIce,
                 CrystallizeDebuff.RedAero, CrystallizeDebuff.BlueEruption, CrystallizeDebuff.RedIce, CrystallizeDebuff.BlueDarkness],
                true, CrystallizeCorner.SE)?.Debuff(PartyRole.MainTank), Is.EqualTo(CrystallizeDebuff.BlueWater));
            Assert.That(FruP4DarklitDragonsongState.FromNetworkReplay(Roles, 1, 2, true, PartyRole.OffTank)?.SomberTank, Is.EqualTo(PartyRole.OffTank));
            Assert.That(FruP5FulgentBladeState.FromNetworkReplay(3, 1, true), Is.Not.Null);
            Assert.That(FruP5ParadiseRegainedState.FromNetworkReplay(2, true), Is.Not.Null);
        });
    }

    [Test]
    public void ARollNoDealCouldGiveIsRefused()
    {
        Assert.Multiple(() =>
        {
            Assert.That(FruP2DiamondDustState.FromNetworkReplay(4, true, false, 7, true, 0), Is.Null, "a fifth icicle");
            Assert.That(FruP2LightRampantState.FromNetworkReplay([.. Roles[..7], Roles[0]], true, false), Is.Null, "a seat twice");
            Assert.That(FruP2LightRampantState.FromNetworkReplay(null, true, false), Is.Null, "no slots");
            Assert.That(FruP3ApocalypseState.FromNetworkReplay([3, 3, 1, 2, 0, 1, 2, 3], 2, true), Is.Null, "three long waters");
            Assert.That(FruP3UltimateRelativityState.FromNetworkReplay(
                [RelativityDebuff.LongDps, RelativityDebuff.ShortSupport, RelativityDebuff.MediumSupport, RelativityDebuff.LongSupport,
                 RelativityDebuff.ShortDps, RelativityDebuff.LongSupport, RelativityDebuff.MediumDps, RelativityDebuff.ShortDps], true, 5, 200),
                Is.Null, "a dps debuff on a support");
            Assert.That(FruP4CrystallizeTimeState.FromNetworkReplay(
                [CrystallizeDebuff.RedAero, CrystallizeDebuff.RedAero, CrystallizeDebuff.RedAero, CrystallizeDebuff.BlueIce,
                 CrystallizeDebuff.RedIce, CrystallizeDebuff.BlueEruption, CrystallizeDebuff.RedIce, CrystallizeDebuff.BlueDarkness],
                true, CrystallizeCorner.SE), Is.Null, "three red aeros");
            Assert.That(FruP4DarklitDragonsongState.FromNetworkReplay(Roles, 1, 2, true, PartyRole.CasterDps), Is.Null, "a dps baiting Somber Dance");
            Assert.That(FruP5FulgentBladeState.FromNetworkReplay(4, 1, true), Is.Null, "a fifth pocket");
            Assert.That(FruP5ParadiseRegainedState.FromNetworkReplay(3, true), Is.Null, "a fourth rotation");
        });
    }
}
