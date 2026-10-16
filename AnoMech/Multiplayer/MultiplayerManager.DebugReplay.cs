using AnoMech.Core;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios;

namespace AnoMech.Multiplayer;

public sealed partial class MultiplayerManager
{
    // ---- Debug: bot-controlled peer replay ----------------------------------

    // Polled from Tick until it succeeds: LastState isn't set on the first tick after Start.
    private void TrySendAiReplayState()
    {
        if (TryResolveScenario() is not IMultiplayerReplayable replayable) return;
        if (replayable.BuildReplayStateMessage() is not { } msg) return;
        aiReplayStateSent = true;
        DiagnosticLog.Info("[Multiplayer] Host: broadcasting AiReplayState for this run.");
        _ = Send(msg);
    }

    private bool IsValidAiIndex(IScenario scenario) =>
        Session.SelectedAi >= 0 && Session.SelectedAi < scenario.AiStrats.Count;

    // Needs both the host's replay message and our own run, which arrive in either order, so
    // Dispatch and OnPeerStartResolved both call it.
    private void TryStartDebugBotReplay()
    {
        if (!debugBotControlled || debugBotReplayStarted) return;
        // peerEnteredInstance outlives the run; the AI drives the real character.
        if (!PeerInRun || !peerRunStarted || Plugin.Game.ActiveScenario is null) return;
        if (MyClaimedRole is not { } myRole) return;
        if (pendingGenericReplayState is not { } msg) return;
        if (TryResolveScenario() is not IMultiplayerReplayable replayable) return;
        var world = Plugin.Game.World;

        debugBotReplayStarted = true;
        if (!IsValidAiIndex(replayable))
        {
            DiagnosticLog.Warn($"[Multiplayer] Peer: SelectedAi {Session.SelectedAi} is out of range for {replayable.Name} ({replayable.AiStrats.Count} strats) -- skipping debug-bot replay.");
            return;
        }
        DiagnosticLog.Info($"[Multiplayer] Peer: starting debug-bot replay for {myRole}.");
        var shadowState = world.Events.FromRunStart(() => replayable.StartReplay(msg, Session.SelectedAi, myRole, world));
        if (shadowState != null)
        {
            debugShadowStateGeneric = shadowState;
            DebugBotControl.Enabled = true;
        }
    }

    // Every frame: the bot reads this client's world, so the handles and the obstacles it steers
    // around follow the host's actors as they reach this client.
    private void TickDebugBotReplay(float deltaSeconds)
    {
        if (debugShadowStateGeneric is not { } shadow || replay is null || TryResolveScenario() is not IMultiplayerReplayable replayable) return;
        var world = Plugin.Game.World;
        var enemies = replay.Enemies;
        replayable.RefreshLiveHandles(shadow, enemies);
        world.Obstacles.Clear();
        replayable.RebuildPeerObstacles(world.Obstacles, enemies, replay.Props, world.Party.Player);
        if (deltaSeconds > 0f) replayable.TickReplay(shadow, deltaSeconds);
    }

    // Clears the run's replay state, not the sticky debugBotControlled toggle.
    private void StopDebugBotReplay()
    {
        if (debugBotReplayStarted) DiagnosticLog.Info("[Multiplayer] Peer: stopping debug-bot replay.");
        DebugBotControl.Enabled = false;
        pendingGenericReplayState = null;
        debugShadowStateGeneric = null;
        debugBotReplayStarted = false;
    }

}
