using System.Numerics;
using AnoMech.Core;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;

namespace AnoMech.Tests;

// A run where the player (seated in the role under test) breaks the strat on purpose, and the
// mechanic must kill exactly the expected roles:
//
//     Negative<TopP2PartySynergyScenario>(PartyRole.MeleeDpsA)
//         .Overrides<TopP2PartySynergyStateOverrides>(o => o.AttackM = OmegaAttack.Shield)
//         .TeleportAt(12f, to: new(0, 0))
//         .ShouldKill(ActionId.BeyondStrength, PartyRole.MeleeDpsA);
internal static class NegativeRun
{
    public const uint ArenaWall = 0;

    public static NegativeRun<TScenario> Negative<TScenario>(PartyRole role, int strat = 0) where TScenario : IScenario
        => new(role, strat);

    public static PartyRole[] AllBut(params PartyRole[] roles) => PerRole.All.Except(roles).ToArray();
}

internal sealed class NegativeRun<TScenario>(PartyRole role, int strat) where TScenario : IScenario
{
    private ScenarioRunOptions options = new() { PlayerRole = role, WriteArtifactsOnFailure = false };
    private int? seed;

    public NegativeRun<TScenario> Overrides<TOverrides>(Action<TOverrides> set)
    {
        options = options with { Overrides = o => set((TOverrides)o) };
        return this;
    }

    // The first player step takes the player off the AI for good; later ones move them again.
    public NegativeRun<TScenario> TeleportAt(float time, Vector2 to, float? facing = null)
        => Add(new Takeover(time, to, facing));

    public NegativeRun<TScenario> FreezeAt(float time) => Add(new Takeover(time, null));

    // The bot stays on its AI, which moves it on at its next step.
    public NegativeRun<TScenario> MoveBotAt(float time, PartyRole bot, Vector2 to) => Add(new Takeover(time, to, Bot: bot));

    private NegativeRun<TScenario> Add(Takeover takeover)
    {
        options = options with { Takeovers = options.Takeovers.Append(takeover).OrderBy(t => t.At).ToList() };
        return this;
    }

    public NegativeRun<TScenario> Seed(int value)
    {
        seed = value;
        return this;
    }

    // The run starts at the first ShouldKill; each call judges the next group of deaths, in order.
    // Deaths no call judges are consequences (a stack one short, a tether partner left alone), not
    // what the test broke. `actionId` 0 = the arena wall.
    public NegativeRun<TScenario> ShouldKill(uint actionId, params PartyRole[] roles)
        => Judge(() =>
        {
            var cause = CauseOf(actionId);
            return ($"{string.Join(", ", roles)} to die to {cause}", c => c.Contains(cause, StringComparison.Ordinal), roles, roles);
        });

    // When the roll decides who is hit.
    public NegativeRun<TScenario> ShouldKillSomeone(uint actionId)
        => Judge(() =>
        {
            var cause = CauseOf(actionId);
            return ($"someone to die to {cause}", c => c.Contains(cause, StringComparison.Ordinal), PerRole.All, []);
        });

    private static string CauseOf(uint actionId) => actionId == NegativeRun.ArenaWall ? ArenaWallCause : ActionLookup.Name(actionId);

    private const string ArenaWallCause = "Walked out of arena";

    private ScenarioRun? run;
    private int runSeed;
    private int judgedDeaths;

    // Names resolve only once the run has installed the game data.
    private NegativeRun<TScenario> Judge(Func<(string Expected, Func<string, bool> CauseMatches, PartyRole[] Allowed, PartyRole[] MustDie)> group)
    {
        if (run is null)
        {
            runSeed = seed ?? Random.Shared.Next();
            run = ScenarioRun.Execute(typeof(TScenario), strat, runSeed, options);
        }
        var (expected, causeMatches, allowed, mustDie) = group();
        expected = (judgedDeaths == 0 ? "expected " : "then expected ") + expected;
        if (GroupMismatch(run, judgedDeaths, expected, causeMatches, allowed, mustDie, out var count) is not { } problem)
        {
            judgedDeaths += count;
            return this;
        }

        // Deterministic, so a rerun reproduces the failure with its artifacts on disk.
        var detailed = ScenarioRun.Execute(typeof(TScenario), strat, runSeed, options with { AlwaysWriteArtifacts = true });
        Assert.Fail($"{problem}{Environment.NewLine}{detailed}{Environment.NewLine}  replay: .Seed({runSeed})");
        return this;
    }

    // A group is the deaths from `start` on, in one frame, to the expected cause. A raidwide they set
    // off (a tether partner gone, a Hello World holder down) can land in that same frame, so this
    // goes by order rather than by time alone.
    private static string? GroupMismatch(
        ScenarioRun run, int start, string expected, Func<string, bool> causeMatches, PartyRole[] allowed, PartyRole[] mustDie,
        out int count)
    {
        count = 0;
        if (run.Failure is not null) return $"{expected}, but the run failed.";
        if (run.Deaths.Count <= start) return $"{expected}, but nobody {(start == 0 ? "" : "else ")}died.";
        var time = run.Deaths[start].Time;
        var group = run.Deaths.Skip(start)
                       .TakeWhile(d => d.Time <= time + ScenarioRun.FrameSeconds / 2 && causeMatches(d.Cause))
                       .ToList();
        if (group.Count == 0) return $"{expected}, but {Describe([run.Deaths[start]])} first.";
        var wrong = group.Where(d => !allowed.Contains(d.Role)).ToList();
        if (wrong.Count > 0) return $"{expected}, but {Describe(wrong)}.";
        var survived = mustDie.Where(r => group.All(d => d.Role != r)).ToList();
        if (survived.Count > 0) return $"{expected}, but {string.Join(", ", survived)} did not die with them.";
        count = group.Count;
        return null;
    }

    private static string Describe(IEnumerable<Death> deaths)
        => string.Join("; ", deaths.Select(d => $"{d.Role} died to \"{d.Cause}\" at t={d.Time:F2}"));
}
