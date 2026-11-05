using System;
using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using AnoMech.Learn.Mitigation;
using AnoMech.Windows.Asu;

namespace AnoMech.Windows.Learn;

public enum MitAssist { None, Reminder, Strict }

// Plugin-lifetime glue: when the toggle is on and a plan covers the scenario, a solo run gets a
// MitSession fed by the player's resolved actions, with feedback by the Practice assist level or
// the Learn level, and a report when the run ends.
internal sealed class MitTraining : IDisposable
{
    private static readonly TimeSpan FeedbackDuration = TimeSpan.FromSeconds(4);

    private readonly Game game;

    public MitTraining(Game game)
    {
        this.game = game;
        game.RunStarted += OnRunStarted;
        game.RunFinished += OnRunFinished;
        Natives.UserActions.Acted += OnActed;
    }

    public static MitTraining? Instance { get; private set; }
    public static void Install(MitTraining training) => Instance = training;

    public MitSession? Session { get; private set; }
    public string? Column { get; private set; }
    public string? Job { get; private set; }
    public MitPlan? Plan { get; private set; }
    public IReadOnlyList<MitVerdict>? LastReport { get; private set; }
    public IReadOnlyList<MitUse>? LastOffPlan { get; private set; }
    public string? LastReportTitle { get; private set; }
    public readonly List<(string Text, System.Numerics.Vector4 Color, DateTime Until)> Feedback = [];

    // Why training can't run for the selected scenario right now, or null.
    public static string? Refusal(string scenarioType, bool inSession)
    {
        if (inSession) return "Mitigation training is solo only.";
        if (!Plugin.Config.EnableUserActions) return "Needs \"Resolve your own actions\" in Settings: that's where your presses are read.";
        if (!MitTimelines.PlansFor(scenarioType).Any()) return "No mitigation plan covers this scenario yet.";
        return null;
    }

    public static MitPlan? PlanFor(string scenarioType)
    {
        var plans = MitTimelines.PlansFor(scenarioType).ToList();
        if (plans.Count == 0) return null;
        return plans.FirstOrDefault(p => p.Plan == Plugin.Config.SelectedMitPlan) ?? plans[0];
    }

    private void OnRunStarted(RunScenarioParams p, bool local)
    {
        Session = null;
        if (!local || !Plugin.Config.EnableMitigationTraining) return;
        var scenarioType = p.Scenario.GetType().Name;
        if (Refusal(scenarioType, RunState.InSession) != null) return;
        var plan = PlanFor(scenarioType);
        var timeline = MitTimelines.Load(scenarioType);
        if (plan is null || timeline is null) return;
        var job = MitJobs.Abbreviation(Natives.BattleCharas.LocalPlayer.ClassJob);
        var seat = game.World.Party.PlayerRole.ToString();
        if (job is null) return;
        var column = MitPlanResolver.ColumnFor(job, seat);
        if (column is null || !plan.Scenarios[scenarioType].Mechanics.Any(m => m.Columns.ContainsKey(column)))
        {
            Push($"This plan has no column for {job}.", AsuTheme.Muted);
            return;
        }
        Plan = plan;
        Job = job;
        Column = column;
        if (Plugin.Config.BotsFollowMitPlan) ScheduleBots(plan.Scenarios[scenarioType], timeline, game.World.Party.PlayerRole);
        var expectations = MitPlanResolver.Resolve(plan.Scenarios[scenarioType], timeline, column, job);
        Session = new MitSession(expectations, TargetRole, RecastOf);
        Session.ItemResolved += OnItemResolved;
        LastReport = null;
        LastReportTitle = $"{p.Scenario.Name} · {column} ({job})";
    }

    // The bots' columns of the plan, as sim statuses on the world's clock; the player's column is theirs.
    private void ScheduleBots(MitScenario scenario, CastTimeline timeline, PartyRole player)
    {
        if (!Core.UserActions.Mitigation.Required) return;
        var world = game.World;
        var jobs = new Dictionary<PartyRole, string>();
        foreach (var role in Enum.GetValues<PartyRole>())
            if (world.Party.Get(role) is Core.SimObjects.SimPartyNpc bot && MitJobs.Abbreviation(bot.ClassJob) is { } job) jobs[role] = job;
        var planned = PlannedMits.Schedule(scenario, timeline, jobs, player);
        foreach (var mit in planned)
            world.Events.Add(mit.Time, () => Apply(mit));
        if (planned.Count > 0) Push($"Bots follow the plan: {planned.Count} mitigation(s) across {jobs.Count} seat(s).", AsuTheme.Muted);
    }

    private void Apply(PlannedMit mit)
    {
        var world = game.World;
        if (world.Party.Get(mit.Role) is not { } bot || !bot.IsAlive()) return;
        var duration = mit.Status.DurationSeconds;
        switch (mit.Target)
        {
            case MitTarget.Enemy:
                var enemies = world.Children.OfType<Core.SimObjects.SimEnemy>().Where(e => e.IsAlive()).ToList();
                var target = enemies.FirstOrDefault(e => e.Casting.ActionId == mit.AnchorActionId) ?? enemies.FirstOrDefault();
                target?.AddStatus(mit.Status.StatusId, duration, sourceObject: bot.GameObjectId);
                break;
            case MitTarget.Party:
                foreach (var role in Enum.GetValues<PartyRole>())
                    if (world.Party.Get(role) is { } member && member.IsAlive()) member.AddStatus(mit.Status.StatusId, duration, sourceObject: bot.GameObjectId);
                break;
            case MitTarget.Ally:
                var ally = mit.Ally switch { "MT" => PartyRole.MainTank, "OT" => PartyRole.OffTank, _ => (PartyRole?)null };
                (ally is { } a ? world.Party.Get(a) : world.Party.Get(PartyRole.MainTank))?.AddStatus(mit.Status.StatusId, duration, sourceObject: bot.GameObjectId);
                break;
            default:
                bot.AddStatus(mit.Status.StatusId, duration, sourceObject: bot.GameObjectId);
                break;
        }
    }

    private void OnRunFinished(bool clean) => Close();

    // Every frame from the overlay; also notices a run stopped by hand.
    public void Tick()
    {
        if (Session is not { Finished: false } session) return;
        if (!RunState.Live(game))
        {
            Close();
            return;
        }
        session.Tick(game.World.Events.Elapsed);
    }

    private void Close()
    {
        if (Session is not { } session) return;
        LastReport = session.Finish();
        LastOffPlan = session.OffPlan().ToList();
        session.ItemResolved -= OnItemResolved;
        Session = null;
    }

    private void OnActed(PlayerAction action)
    {
        if (action.Step != PlayerActionStep.Resolved || Session is null) return;
        Session.Record(action.ActionId, action.TargetId, game.World.Events.Elapsed);
    }

    private void OnItemResolved(MitVerdict v)
    {
        if (!v.IsError) return;
        var assist = LearnSession.Current is { Ended: false } learn
            ? learn.Level switch { AnoMech.Learn.LearnLevel.Observe or AnoMech.Learn.LearnLevel.Exam => MitAssist.None, _ => MitAssist.Reminder }
            : (MitAssist)Plugin.Config.MitPracticeAssist;
        if (assist == MitAssist.None) return;
        var expected = string.Join("/", v.Expectation.AcceptIds.Select(id => MitCatalog.Find(id)?.Name ?? id.ToString()));
        var text = v.Result switch
        {
            MitResult.Missed => $"{expected} missed — {v.Expectation.Label}",
            MitResult.Late => $"{expected} late — {v.Expectation.Label} ({v.Cause})",
            MitResult.TooEarly => $"{expected} too early — {v.Expectation.Label}",
            MitResult.WrongTarget => $"{expected} on the wrong target — {v.Expectation.Label}",
            MitResult.Forbidden => $"{v.Cause} is forbidden at {v.Expectation.Label}",
            MitResult.CooldownConflict => $"{expected} on cooldown for {v.Expectation.Label}: {v.Cause}",
            _ => $"{v.Result} — {v.Expectation.Label}",
        };
        Push(text, AsuTheme.Bad);
        if (assist == MitAssist.Strict) game.World.Fail($"mitigation: {text}");
        else if (Plugin.Config.MitFailureEndsStreak) Plugin.Practice.BreakStreak();
    }

    private void Push(string text, System.Numerics.Vector4 color) => Feedback.Add((text, color, DateTime.UtcNow + FeedbackDuration));

    private string? TargetRole(ulong targetId)
    {
        foreach (var role in Enum.GetValues<PartyRole>())
            if (game.World.Party.Get(role) is { } member && member.GameObjectId.ObjectId == (uint)targetId) return role.ToString();
        return null;
    }

    private static float RecastOf(uint actionId) => Natives.Data.Action(actionId)?.RecastSeconds is { } r and > 0f ? r : 60f;

    public void Dispose()
    {
        game.RunStarted -= OnRunStarted;
        game.RunFinished -= OnRunFinished;
        Natives.UserActions.Acted -= OnActed;
        if (ReferenceEquals(Instance, this)) Instance = null;
    }
}
