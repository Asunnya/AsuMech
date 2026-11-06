using System.Linq;
using System;
using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Learn;
using AnoMech.Learn.Toolbox;
using AnoMech.Scenarios;
using AnoMech.Windows.Asu;

namespace AnoMech.Windows.Learn;

// One lesson run: owns the director, feeds it the sim clock and the player's position, and gives
// back everything it borrowed (speed, bot control, streak counting, the world's guidance hook)
// however the run ends. Observe is a Watch with the lesson text alongside.
internal sealed class LearnSession : IDisposable
{
    public const float MaxSecondsToStart = 60f;

    public static LearnSession? Current { get; private set; }
    // The last judged run, for the report window; Observe leaves it untouched.
    public static LessonReport? LastReport { get; private set; }
    public static event Action<LessonReport>? ReportReady;

    private readonly Game game;
    private readonly WatchSession? watch;
    private readonly float speedBefore;
    private readonly bool countingBefore;
    private bool sawLive;
    private float waited;

    public LearnSession(Game game, IScenario scenario, IScenarioAi strat, Lesson? lesson, PartyRole role, LearnLevel level, DrillWindow? drill = null)
    {
        Current?.Dispose();
        this.game = game;
        Drill = drill;
        if (drill != null) game.RunStarted += OnRunStarted;
        Scenario = scenario;
        Strat = strat;
        Lesson = lesson;
        Role = role;
        Level = level;
        speedBefore = game.EventTimeScale;
        countingBefore = Plugin.Practice.Counting;
        Plugin.Practice.Counting = level == LearnLevel.Exam;
        if (level == LearnLevel.Observe) watch = new WatchSession(new GameWatchTarget());
        Director = new LessonDirector(lesson ?? new Lesson(scenario.GetType().Name, strat.Name, [], []), role, level, SetSpeed, Plugin.Mastery);
        game.World.Guidance = Director;
        game.MechanicFailed += OnMechanicFailed;
        game.World.ActionResolved += OnActionResolved;
        Current = this;
    }

    // The hits the player took this run, with the step they fell in and why they landed.
    public IReadOnlyList<LessonHit> Hits => hits;
    private readonly List<LessonHit> hits = [];

    // A phrase for the banner, every time the player is caught.
    public static event Action<LessonHit>? PlayerHit;

    private void OnActionResolved(ActionResolved resolved)
    {
        if (game.World.Party.Player is not { } player || !resolved.Hits.Contains(player)) return;
        var step = Director.Current;
        var hit = new LessonHit(game.World.Events.Elapsed, step?.Title, step?.Evaluation.Target, Core.Native.Interfaces.GameDataExtensions.ActionName(Core.Native.Interfaces.Natives.Data, resolved.Action.ActionId),
            player.Position, HitExplanation.Phrase(resolved, player), resolved.Killed.Contains(player));
        hits.Add(hit);
        if (Level != LearnLevel.Observe) PlayerHit?.Invoke(hit);
    }

    public IScenario Scenario { get; }
    public IScenarioAi Strat { get; }
    public Lesson? Lesson { get; private set; }
    public PartyRole Role { get; }
    public LearnLevel Level { get; }
    public DrillWindow? Drill { get; }
    public LessonDirector Director { get; }
    public bool Ended { get; private set; }

    // The sim world the run plays in, for the overlay's projection.
    public SimWorld World => game.World;

    // Once per frame, open or closed window. A run that went live and stopped, or never started, ends the lesson.
    public void Tick(float deltaSeconds)
    {
        if (Ended) return;
        if (LessonLoader.Refresh() && Lesson != null && LessonLoader.For(Scenario, Strat) is { } fresh && !ReferenceEquals(fresh, Lesson))
        {
            Lesson = fresh;
            Director.Reload(fresh);
        }
        var live = RunState.Live(game);
        watch?.Tick(live, deltaSeconds);
        if (live)
        {
            sawLive = true;
            if (game.World.Party.Player is { } player)
                Director.Tick(game.World.Events.Elapsed, player.Position);
            return;
        }
        waited += deltaSeconds;
        if (sawLive || waited > MaxSecondsToStart) Dispose();
    }

    private void OnMechanicFailed(string reason) => Director.OnMechanicFailed();

    private void OnRunStarted(RunScenarioParams p, bool local) => ApplyDrill();

    private object? pinned;
    private IReadOnlyList<string> pinnedKeys = [];

    // The toolbox's rolls on the scenario's overrides for this run; lifted again when the session ends.
    public void PinExample(object overrides, Dictionary<string, string> example)
    {
        pinnedKeys = ExampleOverrides.Apply(overrides, example);
        pinned = overrides;
    }

    // Cuts the freshly scheduled timeline down to the drill's slice. Safe to call more than once.
    public void ApplyDrill()
    {
        if (Drill is { } w && !Ended) game.World.Events.KeepWindow(w.From, w.To);
    }

    private void SetSpeed(float scale)
    {
        // Observe runs at the watch speed; the director's slowdown is for someone at the controls.
        if (watch != null) return;
        game.EventTimeScale = scale;
    }

    public void Dispose()
    {
        if (Ended) return;
        Ended = true;
        Director.End();
        if (Director.Judging && Director.Results.Count > 0 && Lesson is { } judged)
        {
            LastReport = new LessonReport(Scenario, Strat, judged, Level, Role, Director.Results, hits.ToList());
            ReportReady?.Invoke(LastReport);
        }
        game.MechanicFailed -= OnMechanicFailed;
        game.World.ActionResolved -= OnActionResolved;
        if (Drill != null) game.RunStarted -= OnRunStarted;
        if (pinned != null) ExampleOverrides.Lift(pinned, pinnedKeys);
        if (ReferenceEquals(game.World.Guidance, Director)) game.World.Guidance = null;
        watch?.Dispose();
        game.EventTimeScale = speedBefore;
        Plugin.Practice.Counting = countingBefore;
        if (ReferenceEquals(Current, this)) Current = null;
    }
}
