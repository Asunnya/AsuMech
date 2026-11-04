using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Learn.Toolbox;
using AnoMech.Learn.Tools;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Top;
using AnoMech.Scenarios.Dsr.P2Thordan;
using AnoMech.Scenarios.Dsr.P6DoubleDragons;
using AnoMech.Scenarios.Fru.P3UltimateRelativity;
using AnoMech.Scenarios.Fru.P5FulgentBlade;
using AnoMech.Scenarios.M11s.Meteorain;
using AnoMech.Scenarios.Top.P2PartySynergy;
using AnoMech.Scenarios.Ucob.P2Nael;
using AnoMech.Scenarios.Umad.P2Forsaken;
using AnoMech.Scenarios.Umad.P3BlackHole;
using AnoMech.Scenarios.Uwu.P1Garuda;
using NUnit.Framework;

namespace AnoMech.Tests.Learn.Toolbox;

public class ExampleOverridesTests
{
    [Test]
    public void ValuesLandOnTheMatchingPropertiesAndLiftClearsThem()
    {
        var overrides = new UwuP1GarudaStateOverrides();
        var applied = ExampleOverrides.Apply(overrides, new Dictionary<string, string>
        {
            ["SuparnaSongBearing"] = "270",
            ["mistralSongTarget"] = "ShieldHealer",
            ["SatinPlumesFirst"] = "[(16,-4),(-4,16),(-12,-12),(-16,4)]",
            ["SistersSongTargets"] = "[CasterDps,RegenHealer]",
            ["NoSuchRoll"] = "1",
            ["ChiradaSongBearing"] = "north",
        });
        Assert.That(applied, Is.EquivalentTo(new[] { "SuparnaSongBearing", "MistralSongTarget", "SatinPlumesFirst", "SistersSongTargets" }));
        Assert.That(overrides.SuparnaSongBearing, Is.EqualTo(270f));
        Assert.That(overrides.MistralSongTarget, Is.EqualTo(PartyRole.ShieldHealer));
        Assert.That(overrides.SatinPlumesFirst, Is.EqualTo(new[] { new Vector2(16f, -4f), new Vector2(-4f, 16f), new Vector2(-12f, -12f), new Vector2(-16f, 4f) }));
        Assert.That(overrides.SistersSongTargets, Is.EqualTo(new[] { PartyRole.CasterDps, PartyRole.RegenHealer }));
        Assert.That(overrides.ChiradaSongBearing, Is.Null);
        ExampleOverrides.Lift(overrides, applied);
        Assert.That(overrides.Any, Is.False);
    }

    [Test]
    public void DirectionsByCompassNameOrDegrees()
    {
        var overrides = new TopP2PartySynergyStateOverrides();
        var applied = ExampleOverrides.Apply(overrides, new Dictionary<string, string> { ["NewNorthA"] = "SE", ["NewNorthB"] = "22.5°", ["AttackDir"] = "nw" });
        Assert.That(applied, Is.EquivalentTo(new[] { "NewNorthA", "NewNorthB", "AttackDir" }));
        Assert.That(overrides.NewNorthA, Is.EqualTo(Direction.SE));
        Assert.That(overrides.NewNorthB!.RadiansFromNorth, Is.EqualTo(22.5f * System.MathF.PI / 180f).Within(1e-4f));
        Assert.That(overrides.AttackDir!.Name(), Is.EqualTo("NW"));
        Assert.That(StateSnapshot.Describe(Direction.SE), Is.EqualTo("SE"));
        Assert.That(StateSnapshot.Describe(overrides.NewNorthB), Is.EqualTo("22.5°"));
    }

    [Test]
    public void PerRoleSettingsSeatByRoleOrInOrder()
    {
        var overrides = new TopP2PartySynergyStateOverrides();
        var applied = ExampleOverrides.Apply(overrides, new Dictionary<string, string>
        {
            ["Symbol"] = "[(MainTank,Cross),(CasterDps,Triangle)]",
            ["Stack"] = "[true,false,null,null,null,null,null,true]",
        });
        Assert.That(applied, Is.EquivalentTo(new[] { "Symbol", "Stack" }));
        Assert.That(overrides.Symbol[PartyRole.MainTank], Is.EqualTo(PlaystationSymbol.Cross));
        Assert.That(overrides.Symbol[PartyRole.CasterDps], Is.EqualTo(PlaystationSymbol.Triangle));
        Assert.That(overrides.Symbol[PartyRole.OffTank], Is.Null);
        Assert.That(overrides.Stack[PartyRole.MainTank], Is.True);
        Assert.That(overrides.Stack[PartyRole.OffTank], Is.False);
        Assert.That(overrides.Stack[PartyRole.RegenHealer], Is.Null);
        Assert.That(overrides.Stack[PartyRole.CasterDps], Is.True);
        ExampleOverrides.Lift(overrides, applied);
        Assert.That(overrides.Symbol.AnySet, Is.False);
        Assert.That(overrides.Stack.AnySet, Is.False);
        Assert.That(overrides.Symbol, Is.Not.Null);
    }

    [Test]
    public void EnumLikeRecordsByStaticMemberName()
    {
        var overrides = new TopP2PartySynergyStateOverrides();
        var first = ExampleOverrides.NamedValues(typeof(OmegaAttack)).First();
        Assert.That(ExampleOverrides.Apply(overrides, new Dictionary<string, string> { ["AttackM"] = first.Key.ToLowerInvariant() }), Is.EqualTo(new[] { "AttackM" }));
        Assert.That(overrides.AttackM, Is.SameAs(first.Value));
        Assert.That(StateSnapshot.Describe(overrides.AttackM), Is.EqualTo(first.Key));
    }

    [Test]
    public void DictionariesByRole()
    {
        var overrides = new FruP3UltimateRelativityStateOverrides();
        var applied = ExampleOverrides.Apply(overrides, new Dictionary<string, string> { ["Debuffs"] = "[(MainTank,ShortSupport),(MeleeDpsA,LongDps)]" });
        Assert.That(applied, Is.EqualTo(new[] { "Debuffs" }));
        Assert.That(overrides.Debuffs![PartyRole.MainTank], Is.EqualTo(RelativityDebuff.ShortSupport));
        Assert.That(overrides.Debuffs[PartyRole.MeleeDpsA], Is.EqualTo(RelativityDebuff.LongDps));
        Assert.That(StateSnapshot.Describe(overrides.Debuffs), Is.EqualTo("[(MainTank,ShortSupport),(MeleeDpsA,LongDps)]"));
    }

    [Test]
    public void TuplesNamedAndUnnamedNullableAndInLists()
    {
        var thordan = new DsrP2ThordanStateOverrides();
        Assert.That(ExampleOverrides.Apply(thordan, new Dictionary<string, string> { ["FirstTowers"] = "[(45,12),(225,8.5)]" }), Is.EqualTo(new[] { "FirstTowers" }));
        Assert.That(thordan.FirstTowers, Is.EqualTo(new[] { (45f, 12f), (225f, 8.5f) }));
        Assert.That(StateSnapshot.Describe(thordan.FirstTowers), Is.EqualTo("[(45,12),(225,8.5)]"));

        var dragons = new DsrP6DoubleDragonsStateOverrides();
        Assert.That(ExampleOverrides.Apply(dragons, new Dictionary<string, string> { ["SecondSetCorner"] = "(1, -1)" }), Is.EqualTo(new[] { "SecondSetCorner" }));
        Assert.That(dragons.SecondSetCorner, Is.EqualTo((1, -1)));

        var meteorain = new M11sMeteorainStateOverrides();
        Assert.That(ExampleOverrides.Apply(meteorain, new Dictionary<string, string> { ["ShadeSides"] = "[(0,1),(1,0)]" }), Is.EqualTo(new[] { "ShadeSides" }));
        Assert.That(meteorain.ShadeSides, Is.EqualTo(new[] { (0, 1), (1, 0) }));
    }

    [Test]
    public void NestedListsArraysAndUnsignedMasks()
    {
        var nael = new UcobP2NaelStateOverrides();
        Assert.That(ExampleOverrides.Apply(nael, new Dictionary<string, string> { ["ThunderTargets"] = "[[MainTank,OffTank],[RegenHealer],[]]" }), Is.EqualTo(new[] { "ThunderTargets" }));
        Assert.That(nael.ThunderTargets!.Select(l => l.ToList()), Is.EqualTo(new[] { new List<PartyRole> { PartyRole.MainTank, PartyRole.OffTank }, new List<PartyRole> { PartyRole.RegenHealer }, new List<PartyRole>() }));
        Assert.That(StateSnapshot.Describe(nael.ThunderTargets), Is.EqualTo("[[MainTank,OffTank],[RegenHealer],[]]"));

        var blackHole = new UmadP3BlackHoleStateOverrides();
        Assert.That(ExampleOverrides.Apply(blackHole, new Dictionary<string, string> { ["StackTargets"] = "[CasterDps,PhysRangedDps]", ["ImplosionAttack"] = "12345" }),
            Is.EquivalentTo(new[] { "StackTargets", "ImplosionAttack" }));
        Assert.That(blackHole.StackTargets, Is.EqualTo(new[] { PartyRole.CasterDps, PartyRole.PhysRangedDps }));
        Assert.That(blackHole.ImplosionAttack, Is.EqualTo(12345u));

        var fulgent = new FruP5FulgentBladeStateOverrides();
        Assert.That(ExampleOverrides.Apply(fulgent, new Dictionary<string, string> { ["CardinalDarkInward"] = "[true,false,false,true]" }), Is.EqualTo(new[] { "CardinalDarkInward" }));
        Assert.That(fulgent.CardinalDarkInward, Is.EqualTo(new[] { true, false, false, true }));

        var forsaken = new UmadP2ForsakenStateOverrides();
        Assert.That(ExampleOverrides.Apply(forsaken, new Dictionary<string, string> { ["SupportLockon"] = "x" }), Is.Empty);

        var applied = ExampleOverrides.Apply(blackHole, new Dictionary<string, string> { ["KefkaPositions"] = "[N,null,SW]", ["SlapAttacks"] = "[null,7]" });
        Assert.That(applied, Is.EquivalentTo(new[] { "KefkaPositions", "SlapAttacks" }));
        Assert.That(blackHole.KefkaPositions[0], Is.EqualTo(Direction.N));
        Assert.That(blackHole.KefkaPositions[1], Is.Null);
        Assert.That(blackHole.KefkaPositions[2], Is.EqualTo(Direction.SW));
        Assert.That(blackHole.SlapAttacks[1], Is.EqualTo(7u));
        ExampleOverrides.Lift(blackHole, applied);
        Assert.That(blackHole.KefkaPositions.All(d => d is null) && blackHole.SlapAttacks.All(a => a is null), Is.True);
    }

    [Test]
    public void EveryOverridesPropertyTypeInTheCatalogIsParseable()
    {
        var unsupported = new List<string>();
        foreach (var scenario in ScenarioCatalog.Create().Scenarios)
        {
            if (scenario.SettingsOverrides is not { } overrides) continue;
            foreach (var p in overrides.GetType().GetProperties().Where(p => p.CanWrite || p.PropertyType.IsArray))
            {
                var type = p.PropertyType;
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(PerRoleSetting<>)) continue;
                var probe = Sample(type);
                if (probe is null || ExampleOverrides.Parse(probe, type) is null) unsupported.Add($"{overrides.GetType().Name}.{p.Name}: {type}");
            }
        }
        Assert.That(unsupported, Is.Empty);
    }

    // A value in StateSnapshot spelling for a type, or null when the type has no obvious sample.
    private static string? Sample(System.Type type)
    {
        var t = System.Nullable.GetUnderlyingType(type) ?? type;
        if (t.IsEnum) return System.Enum.GetNames(t).First();
        if (t == typeof(bool)) return "true";
        if (t == typeof(int) || t == typeof(uint) || t == typeof(long) || t == typeof(float) || t == typeof(double)) return "1";
        if (t == typeof(string)) return "x";
        if (t == typeof(Vector2) || t == typeof(Vector3)) return "(1,2)";
        if (t == typeof(Direction)) return "N";
        if (ExampleOverrides.NamedValues(t).FirstOrDefault() is { Key: { } name }) return name;
        if (t.IsArray) return Sample(t.GetElementType()!) is { } e ? $"[{e}]" : null;
        if (t.IsGenericType)
        {
            var args = t.GetGenericArguments();
            if (t.FullName!.StartsWith("System.ValueTuple`")) return "(" + string.Join(",", args.Select(Sample)) + ")";
            if (args.Length == 1) return Sample(args[0]) is { } e ? $"[{e}]" : null;
            if (args.Length == 2) return Sample(args[0]) is { } k && Sample(args[1]) is { } v ? $"[({k},{v})]" : null;
        }
        return null;
    }
}
