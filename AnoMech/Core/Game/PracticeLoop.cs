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

    public PracticeLoop(Game game)
    {
        this.game = game;
        game.RunStarted += OnRunStarted;
        game.RunFinished += OnRunFinished;
        game.PartyMemberKilled += OnPartyMemberKilled;
    }

    private void OnRunStarted(RunScenarioParams p, bool local)
    {
        // Restarting the same scenario is the normal way to extend a streak.
        if (p.Scenario != lastScenario)
            Streak = 0;
        lastScenario = p.Scenario;
        // A host rerunning on its own would desync the session.
        lastRun = local ? p : null;
    }

    private void OnRunFinished(bool clean)
    {
        if (clean) Streak++;
        if (AutoRestart && lastRun is { } p)
            game.StartScenario(p);
    }

    private void OnPartyMemberKilled(PartyRole role, string cause, uint? actionId)
    {
        Streak = 0;
        AutoRestart = false;
    }
}
