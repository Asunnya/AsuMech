using AnoMech.Learn.Sources;

namespace AnoMech.Tests;

public class ModuleSourceTests
{
    private static ModuleSourceBlock Parse(string folder, string name)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Learn", "Sources", "Fixtures", folder);
        var files = Directory.GetFiles(dir, "*.cs").OrderBy(f => f).Select(f => (Path.GetFileName(f), File.ReadAllText(f)));
        return ModuleSource.ParseFolder(name, "https://example/repo.git", "Modules/Tiny", "0123456789abcdef", "BSD-3-Clause", files);
    }

    [Test]
    public void EnumsKeepValuesAndComments()
    {
        var block = Parse("module-bossmod", "bossmod");
        Assert.That(block.Enums.Keys, Is.EquivalentTo(new[] { "OID", "AID", "SID", "IconID", "TetherID" }));
        Assert.That(block.Enum("OID").Select(e => (e.Name, e.Value)), Is.EqualTo(new[] { ("Boss", 0x4AECL), ("Helper", 0x233CL) }));
        Assert.That(block.Enum("IconID").Single(e => e.Name == "Stack").Value, Is.EqualTo(161));
        Assert.That(block.IconName(139), Is.EqualTo("Spread"));
        Assert.That(block.TetherName(356), Is.EqualTo("ForegoneFatality"));
        Assert.That(block.StatusName(2940), Is.EqualTo("PhysicalVulnerabilityUp"));
        Assert.That(block.Enum("SID")[0].Comment, Is.EqualTo("applied by RawSteel"));
    }

    [Test]
    public void ActionCommentsBecomeFacts()
    {
        var block = Parse("module-bossmod", "bossmod");
        var crown = block.Action(46086)!;
        Assert.That((crown.Caster, crown.Target, crown.CastSeconds, crown.CastExtraSeconds), Is.EqualTo(("Boss", "self", 5f, (float?)null)));
        Assert.That(crown.Shape, Is.EqualTo(new ModuleShape("circle", 60f)));
        var axe = block.Action(46114)!;
        Assert.That((axe.CastSeconds, axe.CastExtraSeconds, axe.Shape!.Kind), Is.EqualTo((2f, 4f, "single-target")));
        var heavy = block.Action(46096)!;
        Assert.That(heavy.CastSeconds, Is.EqualTo(0f), "no cast");
        Assert.That(heavy.Shape, Is.EqualTo(new ModuleShape("cone", 60f, Angle: 45f)));
        Assert.That(heavy.Note, Is.EqualTo("party stack"));
        Assert.That(block.Action(46105)!.Shape, Is.EqualTo(new ModuleShape("donut", 60f, Inner: 5f)));
        Assert.That(block.Action(46106)!.Shape, Is.EqualTo(new ModuleShape("cross", 40f, Width: 10f)));
        Assert.That(block.Action(46109)!.Shape, Is.EqualTo(new ModuleShape("rect", 60f, Width: 6f)));
        var mystery = block.Action(46999)!;
        Assert.That(mystery.Shape, Is.Null);
        Assert.That(mystery.Note, Is.EqualTo("something the parser can't read"), "an unreadable comment is kept as the note");
        Assert.That(block.Action(46998)!.Comment(), Is.Null);
        var reborn = Parse("module-reborn", "reborn");
        var unknownAngle = reborn.Action(46096)!;
        Assert.That(unknownAngle.Shape, Is.EqualTo(new ModuleShape("cone", 60f, Angle: null)), "?-degree keeps the cone, drops the angle");
        Assert.That(unknownAngle.Note, Is.EqualTo("Party stack cone for RawSteelTrophy"));
        Assert.That(reborn.Action(46099)!.Target, Is.EqualTo("location"));
    }

    [Test]
    public void CodeShapesBindToTheActionsOfTheirStatement()
    {
        var bossmod = Parse("module-bossmod", "bossmod");
        Assert.That(bossmod.ShapesFor("RawSteelScytheHeavyHitter").ToList(), Is.EqualTo(new[] { new ModuleShape("cone", 60f, Angle: 45f) }), "a field's cone, 22.5° half-angle doubled, bound where the field meets the action");
        Assert.That(bossmod.ShapesFor("AssaultEvolvedScytheAOE").ToList(), Is.EqualTo(new[] { new ModuleShape("donut", 60f, Inner: 5f) }));
        Assert.That(bossmod.ShapesFor("AssaultEvolvedSwordAOE").ToList(), Is.EqualTo(new[] { new ModuleShape("cross", 40f, Width: 10f) }));
        Assert.That(bossmod.ShapesFor("SharpTaste").ToList(), Is.EqualTo(new[] { new ModuleShape("rect", 60f, Width: 6f) }));
        Assert.That(bossmod.ShapesFor("CrownOfArcadia"), Is.Empty, "a raidwide component builds no shape");
        Assert.That(bossmod.Unparsed, Has.Some.Contains("GenericTowers(module, AID.Bare)"), "a tower without a radius is listed, not guessed");

        var reborn = Parse("module-reborn", "reborn");
        Assert.That(reborn.ShapesFor("Cometite").ToList(), Is.EqualTo(new[] { new ModuleShape("circle", 6f) }), "SimpleAOEs' radius");
        Assert.That(reborn.ShapesFor("Flatliner1").ToList(), Is.EqualTo(new[] { new ModuleShape("circle", 60f) }), "an explicit shape wins over the knockback distance");
        Assert.That(reborn.ShapesFor("RawSteel_HeavyHitter"), Is.Empty, "a field's cone is bound only where the field and the action share a statement");
    }

    [Test]
    public void StatesKeepPhaseActionsNumbersNameAndHint()
    {
        var bossmod = Parse("module-bossmod", "bossmod");
        Assert.That(bossmod.States.Select(s => (s.Phase, s.Kind, string.Join("+", s.Actions), string.Join("/", s.Numbers), s.Name, s.Hint)), Is.EqualTo(new[]
        {
            ("SinglePhase", "Cast", "CrownOfArcadia", "5/8", "Boss disappears (enrage)", (string?)null),
            ("TrophyWeapons", "Cast", "CrownOfArcadia", "5", "Raidwide", "Raidwide"),
            ("TrophyWeapons", "Cast", "RawSteelTrophyAxe", "10.5/3", null, null),
            ("TrophyWeapons", "ComponentCondition<RawSteel>", "", "2.2/0", "Weapon 1", null),
            ("TrophyWeapons", "ComponentCondition<RawSteel>", "", "5.2/1", "Weapon 2", null),
        }));
        var reborn = Parse("module-reborn", "reborn");
        Assert.That(reborn.States.Select(s => (s.Kind, string.Join("+", s.Actions), s.Name)), Is.EqualTo(new[]
        {
            ("Cast", "CrownOfArcadia", "Crown of Arcadia"), ("Condition", "Flatliner1", "Majestic Meteor"), ("SimpleState", "", "Final Phase"),
        }));
        Assert.That(reborn.States[0].Numbers, Is.EqualTo(new[] { 0f, 5f }));
        Assert.That(reborn.States[0].Hint, Is.EqualTo("Raidwide"));
    }

    [Test]
    public void JsonRoundTripsAndNamesComeFromTheFirstSourceThatHasThem()
    {
        var module = new ModuleSource { Fight = "tiny", Sources = [Parse("module-reborn", "reborn"), Parse("module-bossmod", "bossmod")] };
        var again = ModuleSource.Parse(module.ToJson());
        Assert.That(again.Fight, Is.EqualTo("tiny"));
        Assert.That(again.Sources.Select(s => s.Name), Is.EqualTo(new[] { "reborn", "bossmod" }));
        Assert.That(again.Sources[1].Actions.Count, Is.EqualTo(module.Sources[1].Actions.Count));
        Assert.That(again.Sources[1].ShapesFor("SharpTaste").ToList(), Is.EqualTo(new[] { new ModuleShape("rect", 60f, Width: 6f) }));
        Assert.That(again.IconName(161), Is.EqualTo("CrushingCometIcon"), "reborn is listed first");
        Assert.That(again.IconName(999), Is.Null);
        Assert.That(again.TetherName(356), Is.EqualTo("ForegoneFatality"), "only bossmod names it");
    }

    [Test]
    public void TheEmbeddedM11sModuleLoadsAndNamesItsMarkers()
    {
        var m11s = ModuleSource.Load("m11s");
        Assert.That(m11s, Is.Not.Null);
        Assert.That(m11s!.Sources.Select(s => s.Name), Is.EquivalentTo(new[] { "bossmod", "reborn" }));
        Assert.That(m11s.Sources.All(s => s.Actions.Count > 90));
        Assert.That(m11s.IconName(161), Is.Not.Null.And.Contains("Comet").Or.Contains("Stack"));
        Assert.That(ModuleSource.LoadForFamily("M11s"), Is.Not.Null);
        Assert.That(ModuleSource.LoadForFamily("Uwu"), Is.Null);
        Assert.That(ModuleSource.Fights(), Does.Contain("m11s"));
        Assert.That(ModuleFights.Load().Fights["m11s"].Paths.Keys, Is.EquivalentTo(new[] { "bossmod", "reborn" }));
    }

    [Test]
    public void CuesTakeTheModulesNameForANamelessMarkerOrTether()
    {
        var m11s = ModuleSource.Load("m11s")!;
        var marker = new AnoMech.Learn.Tools.CueCandidate("marker", "marker 161", "MainTank", 3f, 3, 3);
        Assert.That(AnoMech.Learn.Tools.LessonDraft.NameCue(marker, m11s), Is.EqualTo("Stack"), "bossmod is listed first");
        var tether = new AnoMech.Learn.Tools.CueCandidate("tether", "tether 249", null, 3f, 3, 3);
        Assert.That(AnoMech.Learn.Tools.LessonDraft.NameCue(tether, m11s), Is.EqualTo("Stretched"));
        Assert.That(AnoMech.Learn.Tools.LessonDraft.NameCue(new AnoMech.Learn.Tools.CueCandidate("marker", "marker 9999", null, 3f, 3, 3), m11s), Is.EqualTo("marker 9999"));
        Assert.That(AnoMech.Learn.Tools.LessonDraft.NameCue(marker, null), Is.EqualTo("marker 161"));
        var reborn = new ModuleSource { Fight = "m11s", Sources = [m11s.Sources.Single(s => s.Name == "reborn")] };
        Assert.That(AnoMech.Learn.Tools.LessonDraft.NameCue(marker, reborn), Is.EqualTo("Crushing Comet Icon"), "camel case spaced out");
    }
}

file static class Extensions
{
    public static string? Comment(this ModuleActionFact fact) => fact.Note;
}
