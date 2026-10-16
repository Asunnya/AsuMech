using System;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace AnoMech.Core.Native.Interfaces;

// The real player's input: what they're doing, and the locks a mechanic puts on it.
public interface ILocalPlayerInput : IDisposable
{
    // Raised after the player successfully executes an action: type, action id, target id.
    event Action<ActionType, uint, ulong>? ActionExecuted;

    bool MovementInputActive { get; }
    bool IsJumping { get; }
    bool IsAutoAttacking { get; }

    // True once per action used since the last poll.
    bool PollActionUsed();

    // True once per cast-cancel request (cancel keybind) since the last poll.
    bool PollCancelCast();

    // Actions the player pressed, oldest first; a bounded debug history.
    IReadOnlyCollection<(uint ActionId, ActionType Type)> RecentActions { get; }

    // Walks the player along a world XZ direction as if the stick were held, over the keyboard;
    // null lets go. The game moves them at its own speed; `speed` is the run
    // or sprint speed the sim expects, which a headless client moves them at.
    void Steer(System.Numerics.Vector2? direction, float speed);
}
