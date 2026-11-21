using System;
using System.Collections.Generic;
using AsuMech.Core.Game.Party;
using AsuMech.Scenarios;
using Dalamud.Game.Text;

namespace AsuMech.Core.Game;

// Solo practice affordances that span runs: the clean-run streak, auto-restart and Run to end.
public sealed class PracticeLoop
{
    private readonly Game game;
    private RunScenarioParams? lastRun;
    private IScenario? lastScenario;
    private bool autoRestart;
    private bool runToEnd;
    private bool wholePhase;

    public bool AutoRestart
    {
        get => autoRestart;
        set { autoRestart = value; if (value) { runToEnd = false; wholePhase = false; } }
    }

    // A Start plays the whole fight from its first scenario, each one going straight on to the next.
    public bool RunToEnd
    {
        get => runToEnd;
        set { runToEnd = value; if (value) { autoRestart = false; wholePhase = false; } }
    }

    // The same, over the parts of the picked scenario's phase.
    public bool WholePhase
    {
        get => wholePhase;
        set { wholePhase = value; if (value) { runToEnd = false; autoRestart = false; } }
    }

    public bool Chaining => runToEnd || wholePhase;

    private IReadOnlyList<IScenario> Chain(IScenario scenario)
        => wholePhase ? game.Catalog.PartsOf(scenario) : game.Catalog.FightOf(scenario.Phase.Zone);
    public int Streak { get; private set; }

    // Run to end is about to start the fight's next scenario, before it is set up.
    public event Action<RunScenarioParams>? Advancing;

    // Off while runs don't count, such as a bot-played Watch: the streak neither grows nor resets.
    public bool Counting { get; set; } = true;

    public PracticeLoop(Game game)
    {
        this.game = game;
        game.RunStarted += OnRunStarted;
        game.RunFinished += OnRunFinished;
        game.PartyMemberKilled += OnPartyMemberKilled;
        game.MechanicFailed += _ => EndStreak();
    }

    // What a Start runs: with Run to end, the fight's first scenario instead of the picked one.
    public RunScenarioParams Starting(RunScenarioParams p)
    {
        if (!Chaining || FightChain.First(Chain(p.Scenario), p) is not { } first) return p;
        Whole(first.Scenario);
        return first;
    }

    private static void Whole(IScenario scenario)
    {
        if (scenario is ISectionedScenario sectioned) sectioned.SelectedSection = 0;
    }

    private void OnRunStarted(RunScenarioParams p, bool local)
    {
        // Restarting the same scenario is the normal way to extend a streak.
        if (Counting && p.Scenario != lastScenario && !p.Continued)
            Streak = 0;
        lastScenario = p.Scenario;
        // A host rerunning on its own would desync the session.
        lastRun = local ? p : null;
    }

    private void OnRunFinished(bool clean)
    {
        if (clean && Counting) Streak++;
        if (Chaining && lastRun is { } played)
        {
            if (FightChain.Next(Chain(played.Scenario), played) is { } next)
            {
                Whole(next.Scenario);
                Advancing?.Invoke(next);
                game.StartScenario(next);
            }
            else
                Plugin.ChatGui.Print(new XivChatEntry { Type = XivChatType.SystemMessage, Message = wholePhase
                    ? $"[AsuMech] Whole phase: {played.Scenario.Phase.Zone.Name} {played.Scenario.Phase.Name} cleared."
                    : $"[AsuMech] Run to end: {played.Scenario.Phase.Zone.Name} cleared." });
            return;
        }
        if (AutoRestart && lastRun is { } p)
            game.StartScenario(p);
    }

    private void OnPartyMemberKilled(PartyRole role, string cause, uint? actionId, ushort? statusId) => EndStreak();

    // A failure the run survives but the player should not get away with (a missed mitigation).
    public void BreakStreak() => EndStreak();

    private void EndStreak()
    {
        if (!Counting) return;
        Streak = 0;
        AutoRestart = false;
    }
}
