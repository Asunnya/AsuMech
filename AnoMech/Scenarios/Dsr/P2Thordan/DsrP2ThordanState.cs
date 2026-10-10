using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Dsr.DsrConstants;

namespace AnoMech.Scenarios.Dsr.P2Thordan;

// Bearings are compass degrees from the arena centre (0 = north); "relative" ones are measured
// from where Thordan lands for the second half of Strength of the Ward.
public sealed class DsrP2ThordanState
{
    public const float DashKnightRadius = 23f;
    public const float GuerriqueRadius = 7f;
    public const float GuideKnightRadius = 10f;
    public const float ThordanLeapRadius = 23f;
    public const float TowerRadius = 12f;
    public const float SanctityThordanRadius = 25f;
    public const float EyeRadius = 40f;
    public const float DarkKnightRadius = 15f;
    public const float MeteorPairRadius = 10f;
    public const float OuterTowerRadius = 18f;
    public const float InnerTowerRadius = 6f;

    public static readonly PartyRole[] LightPartyOne = [PartyRole.MainTank, PartyRole.RegenHealer, PartyRole.MeleeDpsA, PartyRole.PhysRangedDps];
    private static readonly PartyRole[] NonTanks = [PartyRole.RegenHealer, PartyRole.ShieldHealer, PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps];

    // Grinnaux's puddles and Hermenost's towers always sit at the same spots around Thordan.
    public static readonly (float Bearing, float Radius)[] PuddleSpots =
        [(0f, 9f), (90f, 9f), (180f, 9f), (270f, 9f), (45f, 18f), (135f, 18f), (225f, 18f), (315f, 18f)];
    public static readonly float[] TowerBearings = [0f, 45f, 90f, 180f, 270f, 315f];
    public static readonly float[] DefamationSpotBearings = [261f, 180f, 99f];
    public static readonly float[] DefamationTowerBearings = [270f, 180f, 90f];
    public static readonly float[] StackerTowerBearings = [315f, 0f, 45f];

    public float ThordanBearing { get; }
    // 0/45/90/135: the line no knight dashes along; its letter waymark is at this bearing.
    public float SafeLineBearing { get; }
    public IReadOnlyList<float> DashBearings { get; }
    public float GuerriqueBearing { get; }
    public IReadOnlyList<PartyRole> Defamations { get; }
    public IReadOnlyList<PartyRole> Stackers { get; }
    public PartyRole StackTarget { get; }
    // The Paladins first tether two non-tanks without a defamation; the tanks take the tethers over.
    public IReadOnlyList<PartyRole> ShieldBashTethers { get; }
    // Who Thordan turns to as each Broad Swing starts; set when it does.
    public float[] BroadSwingFacing { get; } = new float[2];

    // Adelphel starts east and the knights charge clockwise, or west and counter-clockwise.
    public bool SanctityClockwise { get; }
    public float DarkKnightBearing { get; }
    public float SanctityThordanBearing { get; }
    public float EyeBearing { get; }
    // One sword (plunged first and third) and two swords (second and fourth).
    public PartyRole FirstPlungeTarget { get; }
    public PartyRole SecondPlungeTarget { get; }
    public IReadOnlyList<PartyRole> AwayGroup { get; }
    public IReadOnlyList<PartyRole> BehindGroup { get; }
    // Always two supports or two DPS.
    public IReadOnlyList<PartyRole> MeteorTargets { get; }
    public IReadOnlyList<(float Bearing, float Radius)> FirstTowers { get; }
    public IReadOnlyList<bool> BroadSwingRightFirst { get; }

    public DsrP2ThordanState(Rng rng, DsrP2ThordanStateOverrides? overrides = null, PartyRole player = PartyRole.MainTank)
    {
        overrides ??= new DsrP2ThordanStateOverrides();
        ThordanBearing = Pick(45f * rng.Next(8), overrides.ThordanBearing);
        SafeLineBearing = Pick(45f * rng.Next(4), overrides.SafeLineBearing);
        var safeLine = SafeLineBearing;
        DashBearings = rng.Shuffle(new[] { 0f, 45f, 90f, 135f }.Where(line => line != safeLine))
            .Select(line => line + 180f * rng.Next(2))
            .ToList();
        GuerriqueBearing = Pick(90f * rng.Next(4), overrides.GuerriqueBearing);

        var nonTanks = rng.Shuffle(NonTanks).ToList();
        Defamations = (overrides.Defamations ?? nonTanks.Take(3)).OrderBy(r => r).ToList();
        var defamations = Defamations;
        Stackers = NonTanks.Where(r => !defamations.Contains(r)).ToList();
        StackTarget = Stackers[rng.Next(Stackers.Count)];
        ShieldBashTethers = rng.Shuffle(Stackers).Take(2).ToList();

        SanctityClockwise = overrides.SanctityClockwise ?? rng.NextBool();
        DarkKnightBearing = Pick(45f + 90f * rng.Next(4), overrides.DarkKnightBearing);
        SanctityThordanBearing = 45f * rng.Next(8);
        EyeBearing = Normalize(SanctityThordanBearing + 45f * (1 + rng.Next(7)));
        var marked = rng.Shuffle(Enum.GetValues<PartyRole>()).Take(2).ToList();
        FirstPlungeTarget = overrides.FirstPlungeTarget ?? marked[0];
        SecondPlungeTarget = overrides.SecondPlungeTarget ?? marked[1];
        (AwayGroup, BehindGroup) = PlungeGroups(FirstPlungeTarget, SecondPlungeTarget);

        var meteorSupports = rng.NextBool();
        var meteorPool = Enum.GetValues<PartyRole>().Where(r => IsSupport(r) == meteorSupports);
        var meteors = rng.Shuffle(meteorPool).Take(2).OrderBy(r => r).ToList();
        if (overrides.MeteorOnMe == true)
        {
            var partner = rng.Shuffle(Enum.GetValues<PartyRole>().Where(r => r != player && IsSupport(r) == IsSupport(player))).First();
            meteors = new[] { player, partner }.OrderBy(r => r).ToList();
        }
        MeteorTargets = overrides.MeteorTargets ?? meteors;
        var towers = RollFirstTowers(rng);
        FirstTowers = overrides.FirstTowers ?? towers;
        var swings = new[] { rng.NextBool(), rng.NextBool() };
        BroadSwingRightFirst = overrides.BroadSwingRightFirst ?? swings;
    }

    private static float Pick(float rolled, float? pinned) => pinned ?? rolled;

    public static float Normalize(float bearing) => ((bearing % 360f) + 360f) % 360f;

    private static float AngleBetween(float a, float b) => MathF.Abs(Normalize(a - b + 180f) - 180f);

    public static bool IsSupport(PartyRole role) => role <= PartyRole.ShieldHealer;

    // Tank-tank, healer-healer, melee-melee, physical ranged-caster: always across light parties.
    public static PartyRole Partner(PartyRole role) => (PartyRole)((int)role ^ 1);

    public static bool InLightPartyOne(PartyRole role) => LightPartyOne.Contains(role);

    // The one-sword light party goes away from the Dark Knight; a two-sword in it swaps with its partner.
    public static (IReadOnlyList<PartyRole> Away, IReadOnlyList<PartyRole> Behind) PlungeGroups(PartyRole first, PartyRole second)
    {
        var away = Enum.GetValues<PartyRole>().Where(r => InLightPartyOne(r) == InLightPartyOne(first)).ToHashSet();
        if (away.Contains(second))
        {
            away.Remove(second);
            away.Add(Partner(second));
        }
        return (away.OrderBy(r => r).ToList(), Enum.GetValues<PartyRole>().Where(r => !away.Contains(r)).ToList());
    }

    // Two towers in every cardinal quadrant: both on the wall, or one on the wall and one in the middle.
    private static List<(float Bearing, float Radius)> RollFirstTowers(Rng rng)
    {
        var cardinals = new[] { 0f, 90f, 180f, 270f };
        var singles = rng.Shuffle(cardinals).Take(rng.Next(5) < 3 ? 2 : 3).ToHashSet();
        var towers = new List<(float, float)>();
        foreach (var cardinal in cardinals)
        {
            var slots = rng.Shuffle(new[] { cardinal - 30f, cardinal, cardinal + 30f });
            foreach (var slot in slots.Take(singles.Contains(cardinal) ? 1 : 2))
                towers.Add((Normalize(slot), OuterTowerRadius));
        }
        foreach (var inner in rng.Shuffle(new[] { 45f, 135f, 225f, 315f }).Take(singles.Count))
            towers.Add((inner, InnerTowerRadius));
        return towers;
    }

    private static float HomeCardinal(PartyRole role) => role switch
    {
        PartyRole.MainTank or PartyRole.PhysRangedDps => 0f,
        PartyRole.OffTank or PartyRole.CasterDps => 180f,
        PartyRole.RegenHealer or PartyRole.MeleeDpsA => 270f,
        _ => 90f,
    };

    public bool IsMeteorRole(PartyRole role) => IsSupport(role) == IsSupport(MeteorTargets[0]);

    // Meteors go north or south, rotating clockwise past one already there; the same-role
    // player there takes the spot they left.
    public IReadOnlyDictionary<PartyRole, float> MeteorCardinals()
    {
        var cardinals = Enum.GetValues<PartyRole>().ToDictionary(r => r, HomeCardinal);
        foreach (var meteor in MeteorTargets.OrderBy(r => cardinals[r] is 0f or 180f ? 0 : 1))
        {
            var from = cardinals[meteor];
            if (from is 0f or 180f) continue;
            var to = Normalize(from + 90f);
            if (MeteorTargets.Any(other => other != meteor && cardinals[other] == to)) to = Normalize(to + 180f);
            var displaced = cardinals.First(kv => kv.Value == to && IsSupport(kv.Key) == IsSupport(meteor)).Key;
            cardinals[displaced] = from;
            cardinals[meteor] = to;
        }
        return cardinals;
    }

    // The meteor role takes the wall tower (the one on the cardinal first); the other goes out too
    // when the quadrant has two wall towers, else in to a middle tower. Each middle tower goes to
    // whoever going in is closest to it counter-clockwise, so the next one clockwise is skipped
    // when someone nearer has prio on it.
    // The north and south meteors take towers straight across from each other when they can.
    public IReadOnlyDictionary<PartyRole, (float Bearing, float Radius)> FirstTowerSoaks()
    {
        var cardinals = MeteorCardinals();
        var soaks = new Dictionary<PartyRole, (float, float)>();
        var freeInner = FirstTowers.Where(t => t.Radius < OuterTowerRadius).ToList();
        var goingIn = new List<(PartyRole Role, float Cardinal)>();
        var acrossPair = WallTowersNear(0f)
            .SelectMany(north => WallTowersNear(180f).Where(south => AngleBetween(north.Bearing, south.Bearing) > 179f).Select(south => (north, south)))
            .Cast<((float Bearing, float Radius) North, (float Bearing, float Radius) South)?>()
            .FirstOrDefault();
        foreach (var cardinal in new[] { 0f, 90f, 180f, 270f })
        {
            var pair = cardinals.Where(kv => kv.Value == cardinal).Select(kv => kv.Key).ToList();
            var meteorRole = pair.First(IsMeteorRole);
            var other = pair.First(r => r != meteorRole);
            var outer = WallTowersNear(cardinal);
            var meteorTower = (cardinal, acrossPair) switch
            {
                (0f, { } across) => across.North,
                (180f, { } across) => across.South,
                _ => outer[0],
            };
            soaks[meteorRole] = meteorTower;
            if (outer.Count > 1)
            {
                soaks[other] = outer.First(t => t != meteorTower);
                continue;
            }
            goingIn.Add((other, cardinal));
        }
        while (goingIn.Count > 0 && freeInner.Count > 0)
        {
            var (goer, tower) = goingIn.SelectMany(g => freeInner.Select(t => (g, t)))
                .MinBy(p => Normalize(p.t.Bearing - p.g.Cardinal));
            soaks[goer.Role] = tower;
            goingIn.Remove(goer);
            freeInner.Remove(tower);
        }
        return soaks;
    }

    // Cardinal first, then clockwise.
    private List<(float Bearing, float Radius)> WallTowersNear(float cardinal) =>
        FirstTowers.Where(t => t.Radius >= OuterTowerRadius && AngleBetween(t.Bearing, cardinal) <= 30f)
            .OrderBy(t => AngleBetween(t.Bearing, cardinal)).ThenBy(t => Normalize(t.Bearing - cardinal)).ToList();

    // Meteors end on the cardinal opposite their start; the rest of the meteor role keep their
    // cardinal and the other role takes the intercardinal clockwise of theirs.
    public float SecondTowerBearing(PartyRole role)
    {
        var cardinal = MeteorCardinals()[role];
        if (MeteorTargets.Contains(role)) return Normalize(cardinal + 180f);
        return IsMeteorRole(role) ? cardinal : Normalize(cardinal + 45f);
    }

    public Vector3 DarkKnightSpot => AtBearing(DarkKnightBearing, DarkKnightRadius);
    public Vector3 SanctityThordanSpot => AtBearing(SanctityThordanBearing, SanctityThordanRadius);
    public Vector3 EyeSpot => AtBearing(EyeBearing, EyeRadius);
    public byte EyeSlot => (byte)(EyeBearing / 45f);
    public float SanctitySign => SanctityClockwise ? 1f : -1f;

    // Each knight charges three times, dropping Brightspheres along the way.
    public IReadOnlyList<Vector3> AdelphelChargePath()
    {
        var s = SanctitySign;
        return [new(5f * s, 0f, 0f), new(0f, 0f, 21f), new(-19.4f * s, 0f, -8f), new(14.8f * s, 0f, -14.8f)];
    }

    public IReadOnlyList<Vector3> JanlenouxChargePath() => AdelphelChargePath().Select(p => -p).ToList();

    // When the last charge ends beside the plunge groups the late dodge is safe, else the early one.
    public bool LateBrightsphereDodge => Normalize(DarkKnightBearing + (SanctityClockwise ? 0f : 90f)) % 180f == 45f;

    public float AdelphelBearing => ThordanBearing + 120f;
    public float JanlenouxBearing => ThordanBearing + 240f;

    public Vector3 GuerriqueSpot => AtBearing(GuerriqueBearing, GuerriqueRadius);
    public Vector3 ThordanLeapSpot => AtBearing(ThordanBearing, ThordanLeapRadius);

    public Vector3 RelativeToThordan(float relativeBearing, float radius) => AtBearing(ThordanBearing + relativeBearing, radius);

    // Light party 1 takes the numbered end of the safe line, light party 2 the lettered end.
    public float SpreadSideBearing(PartyRole role) => LightPartyOne.Contains(role) ? SafeLineBearing + 180f : SafeLineBearing;
}
