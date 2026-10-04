using System.Numerics;
using AnoMech.Core;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
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
    public const uint ArenaWall = SimCharacterDeathExtensions.NoAction;

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

    // The run starts at the first ShouldKill; each call judges the next group of deaths. Groups go in
    // frame order, but within one frame the calls can come in any order. Deaths no call judges are
    // consequences (a stack one short, a tether partner left alone), not what the test broke.
    // Matched on the action the death names, so only Die(actionId, ...) counts.
    public NegativeRun<TScenario> ShouldKill(uint actionId, params PartyRole[] roles)
        => Judge(() => $"{string.Join(", ", roles)} to die to {NameOf(actionId)}", actionId, roles, roles);

    // When the roll decides who is hit.
    public NegativeRun<TScenario> ShouldKillSomeone(uint actionId)
        => Judge(() => $"someone to die to {NameOf(actionId)}", actionId, PerRole.All, []);

    private static string NameOf(uint actionId)
        => actionId == NegativeRun.ArenaWall ? "the arena wall" : $"{ActionLookup.Name(actionId)} ({actionId})";

    private ScenarioRun? run;
    private int runSeed;
    private readonly HashSet<int> judged = [];

    // `expected` is lazy: names resolve only once the run has installed the game data.
    private NegativeRun<TScenario> Judge(Func<string> expected, uint actionId, PartyRole[] allowed, PartyRole[] mustDie)
    {
        if (run is null)
        {
            runSeed = seed ?? Random.Shared.Next();
            run = ScenarioRun.Execute(typeof(TScenario), strat, runSeed, options);
        }
        var message = (judged.Count == 0 ? "expected " : "then expected ") + expected();
        if (GroupMismatch(run, judged, message, actionId, allowed, mustDie, out var group) is not { } problem)
        {
            judged.UnionWith(group);
            return this;
        }

        // Deterministic, so a rerun reproduces the failure with its artifacts on disk.
        var detailed = ScenarioRun.Execute(typeof(TScenario), strat, runSeed, options with { AlwaysWriteArtifacts = true });
        Assert.Fail($"{problem}{Environment.NewLine}{detailed}{Environment.NewLine}  replay: .Seed({runSeed})");
        return this;
    }

    // A group is the unjudged deaths to the expected action in the frame of the first unjudged death.
    // A raidwide they set off (a tether partner gone, a Hello World holder down) can land in that
    // same frame; it stays unjudged.
    private static string? GroupMismatch(
        ScenarioRun run, IReadOnlySet<int> judged, string expected, uint actionId, PartyRole[] allowed, PartyRole[] mustDie,
        out List<int> group)
    {
        group = [];
        if (run.Failure is not null) return $"{expected}, but the run failed.";
        var unjudged = Enumerable.Range(0, run.Deaths.Count).Where(i => !judged.Contains(i)).ToList();
        if (unjudged.Count == 0) return $"{expected}, but nobody {(judged.Count == 0 ? "" : "else ")}died.";
        var first = run.Deaths[unjudged[0]];
        group = unjudged.Where(i => run.Deaths[i].Time <= first.Time + ScenarioRun.FrameSeconds / 2
                                    && run.Deaths[i].ActionId == actionId)
                        .ToList();
        var deaths = group.Select(i => run.Deaths[i]).ToList();
        if (deaths.Count == 0) return $"{expected}, but {Describe([first])} first.";
        var wrong = deaths.Where(d => !allowed.Contains(d.Role)).ToList();
        if (wrong.Count > 0) return $"{expected}, but {Describe(wrong)}.";
        var survived = mustDie.Where(r => deaths.All(d => d.Role != r)).ToList();
        if (survived.Count > 0) return $"{expected}, but {string.Join(", ", survived)} did not die with them.";
        return null;
    }

    private static string Describe(IEnumerable<Death> deaths)
        => string.Join("; ", deaths.Select(d => $"{d.Role} died to \"{d.Cause}\" ({(d.ActionId is { } id ? $"action {id}" : "no action")}) at t={d.Time:F2}"));
}
