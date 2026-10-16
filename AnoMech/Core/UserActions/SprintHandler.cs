using AnoMech.Core.SimObjects;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace AnoMech.Core.UserActions;

// Sprint's status, which the server would grant on use. Runs even when the module is disabled.
internal sealed class SprintHandler : IUserActionHandler
{
    private const uint SprintActionId = 3;
    public const ushort SprintStatusId = 50;
    private const float SprintDuration = 10f;
    private const ushort SprintStatusParam = 30; // speed magnitude, carried in Status.Param

    public bool RunsWhenDisabled => true;

    public void OnAction(ActionType actionType, uint actionId)
    {
        // Effect lands on the scenario's SimPlayer (null off-scenario, so this no-ops).
        if (actionType == ActionType.Action && actionId == SprintActionId)
            if (Plugin.Game?.Player is { } player) Apply(player, SprintDuration);
    }

    public static void Apply(SimCharacter character, float duration)
    {
        character.AddStatus(SprintStatusId, duration, SprintStatusParam);
        if (character is ISimPartyMember member) HostReport.RoleStatus([member.Role], SprintStatusId, duration, SprintStatusParam);
    }
}
