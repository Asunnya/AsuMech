using System.Numerics;
using AsuMech.Core.Game.Trace;
using AsuMech.Learn.Tools;

namespace AsuMech.Tests.Core.Game.Trace;

// Expected lines come from a real UWU pull's network log; the writer must produce them field for field.
public class NetworkLogWriterTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 18, 17, 56, 57, TimeSpan.FromHours(-3));

    private static (NetworkLogWriter Log, StringWriter Text) Writer(Func<string, string?>? rewrite = null)
    {
        var text = new StringWriter { NewLine = "\n" };
        return (new NetworkLogWriter(text, () => At, rewrite), text);
    }

    // The line without its type, time and hash.
    private static string Body(StringWriter text)
    {
        var line = text.ToString().TrimEnd('\n').Split('\n')[^1];
        var fields = line.Split('|');
        return string.Join("|", fields[2..^1]);
    }

    private static Vector3 Game(float x, float north, float height) => new(x, height, north);

    [Test]
    public void HashesLikeTheParser()
    {
        var seal = new NetworkLogSeal();
        Assert.That(seal.Seal("01|2026-10-18T17:53:52.4770000-03:00|309|the Weapon's Refrain (Ultimate)"),
            Does.EndWith("|0cb1cc5ef6319b34"));
        for (var i = 2; i <= 10; i++) seal.Seal($"filler {i}");
        Assert.That(seal.Seal("03|2026-10-18T17:53:52.4770000-03:00|40003F62|The Ultima Weapon|00|46|0000|00||2137|8734|3746181|3746181|0|10000|||99.92|95.59|0.00|-0.00"),
            Does.EndWith("|57787bcafbc263f4"));
        Assert.That(seal.Reseal("01|2026-10-18T17:53:52.4770000-03:00|309|the Weapon's Refrain (Ultimate)|ffffffffffffffff"),
            Does.EndWith("|0cb1cc5ef6319b34"), "a zone change starts the count over");
    }

    [Test]
    public void AddCombatant()
    {
        var (log, text) = Writer();
        log.AddCombatant(0x40003F62, 0, "The Ultima Weapon", 0, 0x46, 0, "", 2137, 8734,
            new CombatantState(3746181, 3746181, 0, 10000, Game(99.92f, 95.59f, 0f), -0.001f));
        Assert.That(Body(text), Is.EqualTo("40003F62|The Ultima Weapon|00|46|0000|00||2137|8734|3746181|3746181|0|10000|||99.92|95.59|0.00|-0.00"));
    }

    [Test]
    public void StartsCasting()
    {
        var (log, text) = Writer();
        log.StartsCasting(0x40003F68, "Garuda", 0x40003F68, "Garuda", 0x2B53, "Slipstream", 2.2f,
            new CombatantState(1, 1, 0, 0, Game(100.02f, 98.83f, -0.001f), 3.12f));
        Assert.That(Body(text), Is.EqualTo("40003F68|Garuda|2B53|Slipstream|40003F68|Garuda|2.200|100.02|98.83|-0.00|3.12"));
    }

    [Test]
    public void SingleTargetAbility()
    {
        var (log, text) = Writer();
        log.Ability(false, 0x40003F68, "Garuda", 0x366, "attack", 0x100E975F, "Ayra Fal-aiel",
            new CombatantState(1630757, 1664845, 34464, 10000, Game(99.93f, 102.39f, 0f), -2.19f),
            new CombatantState(26068, 26068, 7000, 10000, Game(99.95f, 101.01f, -0.001f), 1.06f),
            [(0x09B90000UL << 32) | 0x710003], 0x8E0, 0, 1, 0, "", 1, 0x366, 0x366, 0.1f, 0xFED1);
        Assert.That(text.ToString(), Does.StartWith("21|"));
        Assert.That(Body(text), Is.EqualTo("40003F68|Garuda|366|attack|100E975F|Ayra Fal-aiel|710003|9B90000|0|0|0|0|0|0|0|0|0|0|0|0|0|0|"
            + "26068|26068|7000|10000|||99.95|101.01|-0.00|1.06|1630757|1664845|34464|10000|||99.93|102.39|0.00|-2.19|000008E0|0|1|00||01|366|366|0.100|FED1"));
    }

    [Test]
    public void AreaAbilityWithAStatus()
    {
        var (log, text) = Writer();
        log.Ability(true, 0x40003F68, "Garuda", 0x2B42, "Mistral Song", 0x1005EAF8, "Arci Do'tigris",
            new CombatantState(1493560, 1664845, 34464, 10000, Game(100.02f, 98.83f, -0.001f), 3.00f),
            new CombatantState(26162, 26162, 10000, 10000, Game(104.02f, 100.08f, 0f), -1.88f),
            [(0x2D2A0000UL << 32) | 0x350003, (0x2B428000UL << 32) | 0x1B], 0x920, 0, 3, 0, "", 1, 0x2B42, 0x2B42, 2.1f, 0xB43D);
        Assert.That(text.ToString(), Does.StartWith("22|"));
        Assert.That(Body(text), Is.EqualTo("40003F68|Garuda|2B42|Mistral Song|1005EAF8|Arci Do'tigris|350003|2D2A0000|1B|2B428000|0|0|0|0|0|0|0|0|0|0|0|0|"
            + "26162|26162|10000|10000|||104.02|100.08|0.00|-1.88|1493560|1664845|34464|10000|||100.02|98.83|-0.00|3.00|00000920|0|3|00||01|2B42|2B42|2.100|B43D"));
    }

    [Test]
    public void StatusesMarkersTethersAndHp()
    {
        var (log, text) = Writer();
        log.StatusAdd(0x4DB, "Dive Ready", 15f, 0x100A9780, "Strilina Dechamberre", 0x100A9780, "Strilina Dechamberre", 0, 19273, 19273);
        Assert.That(Body(text), Is.EqualTo("4DB|Dive Ready|15.00|100A9780|Strilina Dechamberre|100A9780|Strilina Dechamberre|00|19273|19273"));
        log.StatusRemove(0x4DB, "Dive Ready", 0x100A9780, "Strilina Dechamberre", 0x100A9780, "Strilina Dechamberre", 0, 19273, 19273);
        Assert.That(Body(text), Is.EqualTo("4DB|Dive Ready|0.00|100A9780|Strilina Dechamberre|100A9780|Strilina Dechamberre|00|19273|19273"));
        log.TargetIcon(0x100481D3, "Zoey Heartilly", 0x6AD53299, 0, 0x10, 0x100481D3, 0, 0);
        Assert.That(Body(text), Is.EqualTo("100481D3|Zoey Heartilly|6AD53299|0000|0010|100481D3|0000|0000"));
        log.Tether(0x40003F9D, "Spiny Plume", 0x1005EAF8, "Arci Do'tigris", 0, 0, 0x11, 0x1005EAF8, 0xF, 0);
        Assert.That(Body(text), Is.EqualTo("40003F9D|Spiny Plume|1005EAF8|Arci Do'tigris|0000|0000|0011|1005EAF8|000F|0000"));
        log.UpdateHp(0x40003F6B, "Carbuncle", new CombatantState(16222, 16288, 10000, 10000, Game(100.72f, 114.74f, 0f), -0.001f));
        Assert.That(Body(text), Is.EqualTo("40003F6B|Carbuncle|16222|16288|10000|10000|||100.72|114.74|0.00|-0.00"));
        log.Death(0x40003FA1, "Satin Plume", 0x100B0290, "Creatina Whey");
        Assert.That(Body(text), Is.EqualTo("40003FA1|Satin Plume|100B0290|Creatina Whey"));
    }

    [Test]
    public void OverlayPluginLines()
    {
        var (log, text) = Writer();
        log.CastExtra(0x40003F68, 0x2B53, Game(100.023f, 98.833f, 0.076f), 3.130f);
        Assert.That(Body(text), Is.EqualTo("40003F68|2B53|100.023|98.833|0.076|3.130"));
        log.AbilityExtra(0x40003F68, 0x366, 0x8E0, null, 3.113f, 0x100E975F);
        Assert.That(Body(text), Is.EqualTo("40003F68|0366|000008E0|0||||3.113|100E975F"));
        log.ActorMove(0x40003F6B, 0.2214f, 2, 0x14, Game(100.7553f, 114.8851f, 0.0762f));
        Assert.That(Body(text), Is.EqualTo("40003F6B|0.2214|0002|0014|100.7553|114.8851|0.0762"));
        log.ActorSetPos(0x40003F5E, -0.00001f, 0, 0, Game(92.6274f, 104.5443f, 0f));
        Assert.That(Body(text), Is.EqualTo("40003F5E|-0.0000|00|00|92.6274|104.5443|0.0000"));
        log.MapEffect(0x80030068, 0x00020001, 3);
        Assert.That(Body(text), Is.EqualTo("80030068|00020001|03||"));
    }

    [Test]
    public void ARewriteRenamesBeforeTheHashAndCanDropALine()
    {
        var (log, text) = Writer(line => line.Contains("|000E|") ? null : line.Replace("Creatina Whey", "player 1"));
        log.Chat(0x0E, "Creatina Whey", "pull?");
        log.Death(0x40003FA1, "Satin Plume", 0x100B0290, "Creatina Whey");
        var lines = text.ToString().TrimEnd('\n').Split('\n');
        Assert.That(lines, Has.Length.EqualTo(1));
        Assert.That(lines[0], Does.Contain("|100B0290|player 1|"));
        Assert.That(new NetworkLogSeal().Reseal(lines[0]), Is.EqualTo(lines[0]), "the hash covers the renamed line");
        Assert.That(log.Lines, Is.EqualTo(1));
    }

    [Test]
    public void TheLogReaderTakesTheWrittenLog()
    {
        var t = At;
        var text = new StringWriter { NewLine = "\n" };
        var log = new NetworkLogWriter(text, () => t);
        var boss = new CombatantState(100000, 100000, 0, 0, Game(100f, 100f, 0f), 0f);
        log.Zone(777, "the Weapon's Refrain (Ultimate)");
        log.AddCombatant(0x40000001, 0, "Garuda", 0, 0x46, 0, "", 1644, 8722, boss);
        t = t.AddSeconds(1);
        log.InCombat(true);
        t = t.AddSeconds(2);
        log.StartsCasting(0x40000001, "Garuda", 0x40000001, "Garuda", 0x2B53, "Slipstream", 2.2f, boss);
        t = t.AddSeconds(2.2);
        log.Ability(true, 0x40000001, "Garuda", 0x2B53, "Slipstream", 0x10000001, "player 1", boss,
            new CombatantState(1, 1, 0, 0, Game(100f, 95f, 0f), 0f), [(0x1F400000UL << 32) | 0x3], 0x10, 0, 1, 0, "", 1, 0x2B53, 0x2B53, 2.1f, 0);
        log.StatusAdd(0x4DB, "Dive Ready", 15f, 0x40000001, "Garuda", 0x10000001, "player 1", 0, 1, 100000);
        log.TargetIcon(0x10000001, "player 1", 0, 0, 0x10, 0, 0, 0);
        log.Tether(0x40000001, "Garuda", 0x10000001, "player 1", 0, 0, 0x11, 0x10000001, 0xF, 0);
        t = t.AddSeconds(5);
        log.InCombat(false);

        var pull = ActLog.Pulls(text.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries), 777).Single();

        Assert.That(pull.Duration, Is.EqualTo(9.2f).Within(0.01f));
        var cast = pull.Events.OfType<ActCast>().Single();
        Assert.That((cast.ActionId, cast.T, cast.CastTime), Is.EqualTo((0x2B53u, 2f, 2.2f)));
        Assert.That(cast.Position.X, Is.EqualTo(100f).Within(0.01f));
        Assert.That(cast.Position.Z, Is.EqualTo(100f).Within(0.01f));
        var hit = pull.Events.OfType<ActAbility>().Single();
        Assert.That((hit.Damage, hit.TargetCount), Is.EqualTo(((int?)0x1F40, 1)));
        Assert.That(hit.TargetPosition!.Value.Z, Is.EqualTo(95f).Within(0.01f));
        Assert.That(pull.Events.OfType<ActStatus>().Single().StatusId, Is.EqualTo((ushort)0x4DB));
        Assert.That(pull.Events.OfType<ActMarker>().Single().MarkerId, Is.EqualTo(0x10u));
        Assert.That(pull.Events.OfType<ActTether>().Single().TetherId, Is.EqualTo((ushort)0x11));
    }
}
