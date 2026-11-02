using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;

namespace AnoMech.Core.Game;

// Solo practice affordances that span runs: the clean-run streak and auto-restart.
public sealed class PracticeLoop
{
    private readonly Game game;
    private RunScenarioParams? lastRun;
    private IScenario? lastScenario;

    public bool AutoRestart { get; set; }
    public int Streak { get; private set; }

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

    private void OnRunStarted(RunScenarioParams p, bool local)
    {
        // Restarting the same scenario is the normal way to extend a streak.
        if (Counting && p.Scenario != lastScenario)
            Streak = 0;
        lastScenario = p.Scenario;
        // A host rerunning on its own would desync the session.
        lastRun = local ? p : null;
    }

    private void OnRunFinished(bool clean)
    {
        if (clean && Counting) Streak++;
        if (AutoRestart && lastRun is { } p)
            game.StartScenario(p);
    }

    private void OnPartyMemberKilled(PartyRole role, string cause, uint? actionId, ushort? statusId) => EndStreak();

    private void EndStreak()
    {
        if (!Counting) return;
        Streak = 0;
        AutoRestart = false;
    }
}
