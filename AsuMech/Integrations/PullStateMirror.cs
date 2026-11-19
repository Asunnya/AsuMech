using System;
using AsuMech.Core;
using AsuMech.Core.Game;
using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;

namespace AsuMech.Integrations;

// Shows other plugins (Splatoon, BossMod) a stay as a live pull. Casts, effects, VFX, tethers and
// statuses already reach their hooks, since the sim plays them through the game's own packet
// handlers; what the firewall leaves unset is the client state they gate on. Dalamud's territory
// is moved by ZoneControl, around its guard; this holds the rest.
internal sealed unsafe class PullStateMirror : IDisposable
{
    // EnvManager's active scene, which the server advances at phase changes; Splatoon reads this
    // byte for Controller.Scene and layout scene locks. Not named in FFXIVClientStructs.
    private const int EnvSceneOffset = 0x24;

    private readonly PullStateRules rules = new();
    private bool holding;
    private bool? crossWorldBefore;
    private byte? sceneBefore;

    public bool Holding => holding;
    public bool InCombat { get; private set; }
    public byte? Scene { get; private set; }

    // Every frame, after Game.Tick.
    public void Tick(Game game)
    {
        var run = game.LastRun;
        var running = game.Status is SessionStatus.Running or SessionStatus.RunningWithMistake or SessionStatus.Failed or SessionStatus.Frozen;
        if (rules.Tick(Plugin.Config.MirrorPullState, game.Zone != null, run, running) is not { } frame)
        {
            Release();
            return;
        }
        if (!holding)
        {
            holding = true;
            crossWorldBefore = Plugin.Condition[ConditionFlag.ParticipatingInCrossWorldPartyOrAlliance];
            DiagnosticLog.Info("[PullStateMirror] Holding the pull state for other plugins.");
        }
        InCombat = frame.InCombat;
        SetCondition(ConditionFlag.InCombat, frame.InCombat);
        SetCondition(ConditionFlag.BoundByDuty, frame.InDuty);
        // With it set, ECommons' party (Splatoon priority lists, scripts' GetPartyMembers) reads
        // the server's cross-world roster instead of the party list the doppels are written into.
        SetCondition(ConditionFlag.ParticipatingInCrossWorldPartyOrAlliance, false);

        Scene = run?.Scenario.Phase.EnvScene;
        if (Scene is { } scene)
        {
            sceneBefore ??= ReadScene();
            WriteScene(scene);
        }
    }

    // Back to what the inn reads: a start there required both flags off.
    private void Release()
    {
        if (!holding) return;
        holding = false;
        InCombat = false;
        Scene = null;
        SetCondition(ConditionFlag.InCombat, false);
        SetCondition(ConditionFlag.BoundByDuty, false);
        if (crossWorldBefore is { } crossWorld) SetCondition(ConditionFlag.ParticipatingInCrossWorldPartyOrAlliance, crossWorld);
        crossWorldBefore = null;
        if (sceneBefore is { } scene) WriteScene(scene);
        sceneBefore = null;
        DiagnosticLog.Info("[PullStateMirror] Released the pull state.");
    }

    private static void SetCondition(ConditionFlag flag, bool value)
    {
        var address = Plugin.Condition.Address;
        if (address == 0) return;
        ((bool*)address)[(int)flag] = value;
    }

    private static byte? ReadScene()
    {
        var env = EnvManager.Instance();
        return env == null ? null : *((byte*)env + EnvSceneOffset);
    }

    private static void WriteScene(byte scene)
    {
        var env = EnvManager.Instance();
        if (env != null) *((byte*)env + EnvSceneOffset) = scene;
    }

    public void Dispose() => Release();
}
