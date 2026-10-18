using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Core.UserActions;

// A condition on the action just used — drives status removal (JobActions.StatusClearedOnAction).
// Authored via the factory helpers in JobActions (ActionId(...), AnyWeaponskill()).
internal interface IActionPredicate
{
    bool Matches(uint actionId);
}

internal sealed class ActionIdPredicate(uint id) : IActionPredicate
{
    public bool Matches(uint actionId) => actionId == id;
}

internal sealed class AnyWeaponskillPredicate : IActionPredicate
{
    public bool Matches(uint actionId) => Natives.Data.Action(actionId)?.ActionCategory == 3;
}

// Any GCD — Weaponskill (3) or Spell (2). For procs consumed by the "next spell/GCD".
internal sealed class AnyGcdPredicate : IActionPredicate
{
    public bool Matches(uint actionId) => Natives.Data.Action(actionId)?.ActionCategory is 2 or 3;
}
