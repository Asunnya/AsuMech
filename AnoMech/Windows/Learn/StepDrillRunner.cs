using System;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Learn;
using AnoMech.Scenarios;

namespace AnoMech.Windows.Learn;

// Repeats one lesson step: each run keeps only the timeline slice around it, and the Practice
// auto-restart chains the runs until the player ends the drill or stops the run.
internal sealed class StepDrillRunner : IDisposable
{
    public static StepDrillRunner? Instance { get; private set; }

    public sealed record Spec(IScenario Scenario, IScenarioAi Strat, Lesson Lesson, LessonStep Step, PartyRole Role, LearnLevel Level, DrillWindow Window);

    private readonly Plugin plugin;
    private readonly Game game;

    // Auto-restart chains runs within a frame or two; a longer gap means the player stopped.
    private const float IdleSecondsToEnd = 5f;

    public Spec? Active { get; private set; }
    public int Runs { get; private set; }
    private float idle;

    public StepDrillRunner(Plugin plugin, Game game)
    {
        this.plugin = plugin;
        this.game = game;
        game.RunStarted += OnRunStarted;
        Instance = this;
    }

    public void Start(LessonReport report, LessonStep step)
        => Start(new Spec(report.Scenario, report.Strat, report.Lesson, step, report.Role, report.Level == LearnLevel.Observe ? LearnLevel.Guided : report.Level, StepDrillPlanner.Window(step)));

    public void Start(Spec spec)
    {
        var strat = IndexOf(spec.Scenario, spec.Strat);
        if (strat < 0) return;
        var waymark = plugin.Picker.Scenario?.Phase.Zone == spec.Scenario.Phase.Zone ? plugin.Picker.Waymark : 0;
        var selection = new ScenarioSelection(spec.Scenario, strat, waymark, plugin.Picker.Seed, spec.Role);
        if (plugin.Runs.StartRefusal(selection, solo: false) != null) return;
        Active = spec;
        Runs = 0;
        idle = 0f;
        Plugin.Practice.AutoRestart = true;
        NewSession();
        plugin.Runs.Start(selection, solo: false);
    }

    public void End()
    {
        Active = null;
        Plugin.Practice.AutoRestart = false;
    }

    public void Tick(float deltaSeconds)
    {
        if (Active is null) return;
        idle = Asu.RunState.Live(game) ? 0f : idle + deltaSeconds;
        if (idle > IdleSecondsToEnd) End();
    }

    // Every chained run gets a fresh session; a run the player started by hand ends the drill.
    private void OnRunStarted(RunScenarioParams p, bool local)
    {
        if (Active is not { } spec) return;
        if (!local || p.Scenario != spec.Scenario || !Plugin.Practice.AutoRestart) { End(); return; }
        Runs++;
        if (LearnSession.Current is { Ended: false, Drill: not null } session) session.ApplyDrill();
        else NewSession().ApplyDrill();
    }

    private static int IndexOf(IScenario scenario, IScenarioAi strat)
    {
        for (var i = 0; i < scenario.AiStrats.Count; i++)
            if (ReferenceEquals(scenario.AiStrats[i], strat)) return i;
        return -1;
    }

    private LearnSession NewSession()
    {
        var spec = Active!;
        return new LearnSession(game, spec.Scenario, spec.Strat, spec.Lesson, spec.Role, spec.Level, spec.Window);
    }

    public void Dispose()
    {
        game.RunStarted -= OnRunStarted;
        if (ReferenceEquals(Instance, this)) Instance = null;
    }
}
