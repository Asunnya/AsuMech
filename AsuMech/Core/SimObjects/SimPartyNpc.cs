using System;
using System.Collections.Generic;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Geometry;
using AsuMech.Core.Game.Motion;
using AsuMech.Core.Game.Party;
using AsuMech.Core.Native.Interfaces;
using AsuMech.Core.UserActions;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace AsuMech.Core.SimObjects;

public sealed class SimPartyNpc : SimNpc, ISimPartyMember
{
    public PartyRole Role { get; set; }
    public byte ClassJob { get; }
    public string DisplayName { get; }

    internal SimPartyNpc(IBattleCharaProxy proxy, Coordinates coordinates, PartyRole role, byte classJob, string name, ObstacleField obstacles)
        : base(proxy, coordinates,
            self => new SimulatedMotion(() => self.Proxy, coordinates, self.ActorControl, obstacles, turnSpeed: null, npc: false))
    {
        Role = role;
        ClassJob = classJob;
        DisplayName = name;
    }

    // A bot's button press: the animation, then the same JobActions effects a player's press applies.
    // Bots press only party-wide mitigation, never their own, and only while mitigation is required.
    private bool UseAction(uint actionId)
    {
        if (!Mitigation.Required) return false;
        PlayAction(actionId);
        JobActions.ApplyEffects(this, actionId, (ulong)GameObjectId, Random.Shared);
        return true;
    }

    // level 1-3. The action id used; 0 if KO'd, mitigation is not required, or the job has no limit
    // break at that level.
    internal uint UseLimitBreak(int level)
    {
        if (!this.IsAlive() || ActionsLocked) return 0;
        var actionId = LimitBreakHandler.ActionId(ClassJob, level);
        if (actionId == 0 || !UseAction(actionId)) return 0;
        DiagnosticLog.Info($"[SimPartyNpc] {Role} (job {ClassJob}) uses LB{level} {Natives.Data.ActionName(actionId)}.");
        return actionId;
    }

    public void UseSprint(float duration)
    {
        if (!ActionsLocked) SprintHandler.Apply(this, duration);
    }

    public override void Despawn()
    {
        base.Despawn();
    }

    protected override void OnPacketSpawned(IBattleCharaProxy chara) => ActorControl.ZoneIn();

    public void OnKilled(bool byEnvironment)
    {
        GoDown(byEnvironment);
    }
}
