using System.Numerics;
using AsuMech.Core.Game.Party;
using AsuMech.Scenarios.Ucob.P3Heavensfall;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Tests.NegativeRun;

namespace AsuMech.Tests;

// Nael north between Bahamut Prime and Twintania, a tower every other slot.
public class UcobP3HeavensfallTuuflessAiTests
{
    private const int Tuufless = 1;

    private static void NaelNorthTowersEveryOtherSlot(UcobP3HeavensfallStateOverrides o)
    {
        o.NaelStep = 0;
        o.Formation = HeavensfallFormation.NaelBetween;
        o.TowerSlots = [0, 2, 4, 6, 8, 10, 12, 14];
    }

    [Test]
    public void TowersGoClockwiseFromNaelStH2D2D4AndCounterClockwiseMtH1D1D3()
    {
        var expected = new Dictionary<PartyRole, float>
        {
            [OffTank] = 0f, [ShieldHealer] = 45f, [MeleeDpsB] = 90f, [CasterDps] = 135f,
            [MainTank] = 315f, [RegenHealer] = 270f, [MeleeDpsA] = 225f, [PhysRangedDps] = 180f,
        };
        var bearings = BearingsAt(UcobP3HeavensfallScenario.TowersResolve - 0.3f, NaelNorthTowersEveryOtherSlot);
        foreach (var (role, bearing) in expected)
            Assert.That(AngleBetween(bearings[role], bearing), Is.LessThan(10f), $"{role} at {bearings[role]:F0}, its tower at {bearing:F0}");
    }

    [Test]
    public void BeforeTheDivesTanksFaceNaelMeleeAndHealersFlankAndRangedGoOpposite()
    {
        var bearings = BearingsAt(UcobP3HeavensfallScenario.DivesResolve - 0.3f, NaelNorthTowersEveryOtherSlot);
        Assert.That(AngleBetween(bearings[MainTank], 0f), Is.LessThan(30f));
        Assert.That(AngleBetween(bearings[OffTank], 0f), Is.LessThan(30f));
        Assert.That(AngleBetween(bearings[RegenHealer], 270f), Is.LessThan(30f));
        Assert.That(AngleBetween(bearings[MeleeDpsA], 270f), Is.LessThan(30f));
        Assert.That(AngleBetween(bearings[ShieldHealer], 90f), Is.LessThan(30f));
        Assert.That(AngleBetween(bearings[MeleeDpsB], 90f), Is.LessThan(30f));
        Assert.That(AngleBetween(bearings[PhysRangedDps], 180f), Is.LessThan(30f));
        Assert.That(AngleBetween(bearings[CasterDps], 180f), Is.LessThan(30f));
    }

    private static Dictionary<PartyRole, float> BearingsAt(float time, Action<UcobP3HeavensfallStateOverrides> overrides)
    {
        var bearings = new Dictionary<PartyRole, float>();
        Negative<UcobP3HeavensfallScenario>(MainTank, Tuufless)
            .Overrides(overrides)
            .At(time, p =>
            {
                foreach (var role in Enum.GetValues<PartyRole>())
                    if (p.Member(role) is { } member)
                        bearings[role] = BearingOf(new Vector2(member.Position.X, member.Position.Z));
            })
            .ShouldKillNoOneElse();
        return bearings;
    }

    private static float BearingOf(Vector2 at) => (MathF.Atan2(at.X, -at.Y) * 180f / MathF.PI + 360f) % 360f;

    private static float AngleBetween(float a, float b)
    {
        var d = MathF.Abs(a - b) % 360f;
        return d > 180f ? 360f - d : d;
    }
}
