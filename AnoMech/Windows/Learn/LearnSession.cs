using System;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Learn;
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

    public LearnSession(Game game, IScenario scenario, IScenarioAi strat, Lesson? lesson, PartyRole role, LearnLevel level)
    {
        Current?.Dispose();
        this.game = game;
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
        Current = this;
    }

    public IScenario Scenario { get; }
    public IScenarioAi Strat { get; }
    public Lesson? Lesson { get; }
    public PartyRole Role { get; }
    public LearnLevel Level { get; }
    public LessonDirector Director { get; }
    public bool Ended { get; private set; }

    // The sim world the run plays in, for the overlay's projection.
    public SimWorld World => game.World;

    // Once per frame, open or closed window. A run that went live and stopped, or never started, ends the lesson.
    public void Tick(float deltaSeconds)
    {
        if (Ended) return;
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
        if (Director.Judging && Director.Results.Count > 0)
        {
            LastReport = new LessonReport(Scenario.Name, Strat.Name, Level, Role, Director.Results);
            ReportReady?.Invoke(LastReport);
        }
        game.MechanicFailed -= OnMechanicFailed;
        if (ReferenceEquals(game.World.Guidance, Director)) game.World.Guidance = null;
        watch?.Dispose();
        game.EventTimeScale = speedBefore;
        Plugin.Practice.Counting = countingBefore;
        if (ReferenceEquals(Current, this)) Current = null;
    }
}
