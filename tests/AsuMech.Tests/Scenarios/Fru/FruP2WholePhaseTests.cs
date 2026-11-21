using AsuMech.Scenarios.Fru.IntermissionIceVeil;
using AsuMech.Scenarios.Fru.P2DiamondDust;
using AsuMech.Scenarios.Fru.P2LightRampant;
using AsuMech.Scenarios.Fru.P2MirrorMirror;
using AsuMech.Scenarios.Fru.P2QuadrupleSlap;
using static AsuMech.Scenarios.Fru.FruConstants;

namespace AsuMech.Tests;

public class FruP2WholePhaseTests
{
    private static WholePhaseTimeline? recorded;
    private static WholePhaseTimeline Phase => recorded ??= WholePhaseTimeline.Record(typeof(FruIceVeilScenario));

    [Test]
    public void ItClearsStraightThrough()
        => Assert.That(Phase.Run.Passed, Phase.Run.ToString());

    [Test]
    public void DiamondDustComesAsLongAfterTheSecondSlapAsInRetail()
        => Assert.That(Phase.Gap(new FruP2QuadrupleSlapScenario().Name, [ActionId.QuadrupleSlapSecond], [ActionId.MirrorImage]),
            Is.EqualTo(6.5f).Within(1f));

    [Test]
    public void MirrorMirrorComesAsLongAfterTwinStillnessAsInRetail()
        => Assert.That(Phase.Gap(new FruP2DiamondDustScenario().Name, [ActionId.TwinSilenceSecond, ActionId.TwinStillnessSecond],
            [ActionId.HallowedRayCast]), Is.EqualTo(5.76f).Within(0.5f));

    [Test]
    public void LightRampantComesAsLongAfterBanishAsInRetail()
        => Assert.That(Phase.Gap(new FruP2MirrorMirrorScenario().Name, [ActionId.BanishPairs, ActionId.BanishSpread], [ActionId.LightRampant]),
            Is.EqualTo(4.5f).Within(0.5f));

    [Test]
    public void IceVeilComesAsLongAfterTheHouseOfLightAsInRetail()
        => Assert.That(Phase.Gap(new FruP2LightRampantScenario().Name, [ActionId.HouseOfLight], [ActionId.AbsoluteZero]),
            Is.EqualTo(8.4f).Within(0.5f));

    [Test]
    public void TheUsurperCarriesOnInHerLightForm()
    {
        foreach (var name in new[] { new FruP2MirrorMirrorScenario().Name, new FruIceVeilScenario().Name })
        {
            var part = Phase.PartIndex(name);
            var spawn = Phase.Rows.First(r => r.Part == part && r.Kind == "spawn" && r.Id == BNpcBaseId.UsurperP2);
            Assert.That(spawn.Raw["detail"], Does.Contain("model_state=0x4"), name);
            Assert.That(Phase.Rows.Any(r => r.Part == part && r.Kind == "status_gain" && r.Id == TransfigurationStatus
                                            && r.Raw["detail"].Contains($"param={UsurperLightTransformation} on_spawn")), name);
        }
    }

    [TestCase(typeof(FruP2DiamondDustScenario), 23.9f)]
    [TestCase(typeof(FruP2MirrorMirrorScenario), 76.2f)]
    [TestCase(typeof(FruP2LightRampantScenario), 124.8f)]
    [TestCase(typeof(FruIceVeilScenario), 177.3f)]
    public void EachPartStartsTheUsurpersTrackWhereRetailIsThen(Type scenario, float seconds)
        => Assert.That(((AsuMech.Scenarios.IScenario)Activator.CreateInstance(scenario)!).BgmSecondsAtStart, Is.EqualTo(seconds));
}
