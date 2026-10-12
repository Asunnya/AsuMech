using AnoMech.Core.Native.Implementations;
using AnoMech.Core.Native.Interfaces;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace AnoMech.Core.UserActions;

// Linear combos (the 1-2-3 where using one action lights up the next) are driven by
// the server's ActionEffect carrying a Type-0x1B effect; the sim firewall eats it, so
// we replay that effect and let the client advance its own combo. Combo routes
// (Gnashing Fang etc.) use a separate system and are out of scope. Dedup of
// queued/re-entrant executions is UserActions' job, so OnAction only sees actual,
// once-per-execution calls.
internal sealed unsafe class ComboHandler : IUserActionHandler
{
    public void OnAction(ActionType actionType, uint actionId)
    {
        if (actionType != ActionType.Action) return;
        var role = PlayerCombo.RoleOf(actionId);
        if (role == ComboRole.None) return;

        var player = BattleCharaProxy.LocalPlayer;
        if (!player.Exists) return;
        var am = ActionManager.Instance();
        if (am == null) return;
        // SourceSequence makes the client treat it as that action resolving.
        var target = DeliveryTarget(player);
        player.ReceiveActionEffect(new ActionEffectData(
            actionId, ActionType.Action, 0f, (ushort)actionId, 0, 0, player.Rotation, player.Position,
            AnimationTarget: target,
            Targets: [new ActionEffectTarget(target, [ActionEffectEntry.ActionMarker(actionId, role == ComboRole.Continuation)])],
            SourceSequence: am->LastUsedActionSequence));
    }

    // The player's target, as the server delivers it; the player itself when the target isn't a
    // registered BattleChara the client could deliver to.
    private static GameObjectId DeliveryTarget(BattleCharaProxy player)
    {
        var tgt = (GameObject*)(Plugin.TargetManager.Target?.Address ?? 0);
        if (tgt == null) return player.GameObjectId;
        var cm = CharacterManager.Instance();
        if (cm == null || cm->LookupBattleCharaByEntityId(tgt->EntityId) == null) return player.GameObjectId;
        return tgt->GetGameObjectId();
    }
}
