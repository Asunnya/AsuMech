using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Core.UserActions;
using AnoMech.Learn.Ghost;
using AnoMech.Learn.Mitigation;
using AnoMech.Learn.Tools;

namespace AnoMech.Tests;

public class GhostTrackTests
{
    private static readonly Vector3 Origin = new(100f, 0f, 100f);

    private static Dictionary<string, string> Row(float t, string kind, string actor = "", string name = "", string id = "", float? x = null, float? z = null, float rot = 0f, string detail = "")
    {
        var row = new Dictionary<string, string> { ["t"] = SimTimeline.F(t, "F3"), ["kind"] = kind, ["actor"] = actor, ["actor_name"] = name, ["id"] = id, ["detail"] = detail };
        if (x is { } px) { row["x"] = SimTimeline.F(px); row["z"] = SimTimeline.F(z!.Value); row["rot"] = SimTimeline.F(rot); }
        return row;
    }

    [Test]
    public void SeatsComeFromJobs()
    {
        var roles = GhostRoles.Assign([
            ("player 1", (byte)JobId.Reaper), ("player 2", (byte)JobId.Warrior), ("player 3", (byte)JobId.Scholar), ("player 4", (byte)JobId.Paladin),
            ("player 5", (byte)JobId.Sage), ("player 6", (byte)JobId.Dancer), ("player 7", (byte)JobId.Pictomancer), ("player 8", (byte)JobId.Monk),
        ]);
        Assert.That(roles["player 2"], Is.EqualTo(PartyRole.MainTank), "the first tank met");
        Assert.That(roles["player 4"], Is.EqualTo(PartyRole.OffTank));
        Assert.That(roles["player 3"], Is.EqualTo(PartyRole.ShieldHealer));
        Assert.That(roles["player 5"], Is.EqualTo(PartyRole.RegenHealer), "a second shield healer takes the free healer seat");
        Assert.That((roles["player 1"], roles["player 8"]), Is.EqualTo((PartyRole.MeleeDpsA, PartyRole.MeleeDpsB)));
        Assert.That((roles["player 6"], roles["player 7"]), Is.EqualTo((PartyRole.PhysRangedDps, PartyRole.CasterDps)));
        Assert.That(GhostRoles.Assign([("player 1", 0)]), Is.Empty, "no job, no seat");
    }

    [Test]
    public void ARecordingWalksOnTheSimsClockOnceAligned()
    {
        var rows = new List<Dictionary<string, string>>
        {
            Row(0f, "recording", "10000001", "player 1", "1325"),
            Row(0.1f, "member", "10000001", "player 1", ((byte)JobId.Warrior).ToString()),
            Row(0.1f, "member", "10000002", "player 2", ((byte)JobId.WhiteMage).ToString()),
            Row(0.2f, "pos", "10000001", "player 1", x: 100f, z: 110f),
            Row(0.2f, "pos", "10000002", "player 2", x: 95f, z: 100f),
            Row(9f, "cast", "40001A2B", "Boss", "46086", detail: "cast=4.70s"),
            Row(10.2f, "pos", "10000001", "player 1", x: 100f, z: 90f, rot: 3.14f),
            Row(10.6f, "pos", "10000001", "player 1", x: 100f, z: 88f, rot: 3.14f),
            Row(12f, "pos", "40001A2B", "Boss", x: 100f, z: 100f),
        };
        var track = GhostTrack.FromRecording(rows, Origin);
        Assert.That(track.Lanes.Select(l => (l.Label, l.Role)), Is.EqualTo(new[] { ("player 1", (PartyRole?)PartyRole.MainTank), ("player 2", PartyRole.RegenHealer) }), "the boss isn't a player");
        Assert.That(track.EnemyCasts, Is.EqualTo(new[] { (9f, 46086u) }));
        var timeline = new CastTimeline("X", [new CastEvent(46086, "Crown of Arcadia", 5f, 10f, 1)]);
        Assert.That(track.Align(timeline));
        Assert.That(track.Offset, Is.EqualTo(4f).Within(0.001f), "the pull's 9 s is the scenario's 5 s");
        Assert.That(track.AlignedOn, Is.EqualTo("Crown of Arcadia"));
        var tank = track.LaneFor(PartyRole.MainTank)!;
        Assert.That(track.At(tank, 1.2f)!.Value.Position, Is.EqualTo(new Vector2(0f, 10f)), "a ten-second gap is standing still, not a walk");
        Assert.That(track.At(tank, 6.4f)!.Value.Position.Y, Is.EqualTo(-11f).Within(0.01f), "between close samples it interpolates");
        Assert.That(track.At(tank, 6.6f)!.Value.Position.Y, Is.EqualTo(-12f).Within(0.01f), "held at the last sample");
        Assert.That(track.At(tank, 6.6f + GhostTrack.HoldSeconds + 0.5f), Is.Null, "faded after the hold");
        Assert.That(track.Align(new CastTimeline("Y", [new CastEvent(1, "Other", 1f, 2f, 1)])), Is.False, "nothing shared, nothing to line up on");
    }

    [Test]
    public void AnActLogGivesJobsFromTheZoneAndPosesFromCombatantMemory()
    {
        string[] lines =
        [
            "01|2026-11-05T21:30:00.0000000-03:00|52D|AAC Heavyweight M3 (Savage)|h",
            "03|2026-11-05T21:30:01.0000000-03:00|10001234|Real Name|15|64|0000|49|World|0|0|200000|200000|10000|10000|||100.00|110.00|0.00|0.00|h",
            "261|2026-11-05T21:30:01.5000000-03:00|Add|10005678|Heading|0.0000|Job|24|Name|Other Name|PosX|95.0000|PosY|100.0000|PosZ|0.0000|h",
            "260|2026-11-05T21:30:05.0000000-03:00|0|1|1|1|h",
            "261|2026-11-05T21:30:06.0000000-03:00|Change|10001234|Heading|3.1400|PosX|100.0000|PosY|105.0000|PosZ|0.0000|h",
            "20|2026-11-05T21:30:10.0000000-03:00|40001A2B|The Tyrant|B406|Crown of Arcadia|40001A2B|The Tyrant|4.700|100.00|90.00|0.00|3.14|h",
            "261|2026-11-05T21:30:12.0000000-03:00|Change|10005678|Heading|1.0000|PosX|96.0000|PosY|101.0000|PosZ|0.0000|h",
            "260|2026-11-05T21:30:20.0000000-03:00|0|0|1|1|h",
        ];
        var pull = ActLog.Pulls(lines, 0x52D).Single();
        Assert.That(pull.Jobs, Is.EquivalentTo(new Dictionary<uint, byte> { [0x10001234] = 0x15, [0x10005678] = 24 }), "jobs from before the pull: hex on 03, decimal under Job");
        var track = GhostTrack.FromActPull(pull, Origin);
        Assert.That(track.Lanes.Select(l => (l.Label, l.Role)), Is.EquivalentTo(new[] { ("player 1", (PartyRole?)PartyRole.MainTank), ("player 2", PartyRole.RegenHealer) }));
        Assert.That(track.Lanes.SelectMany(l => new[] { l.Label }), Has.None.Contains("Name"), "no real name survives");
        var healer = track.LaneFor(PartyRole.RegenHealer)!;
        Assert.That(healer.Samples.Single().Position, Is.EqualTo(new Vector2(-4f, 1f)), "the pre-pull Add isn't a pose of the pull; the in-pull Change is");
        Assert.That(track.EnemyCasts.Single(), Is.EqualTo((5f, 0xB406u)));
    }
}
