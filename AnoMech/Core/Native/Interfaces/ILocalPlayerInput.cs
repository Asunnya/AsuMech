namespace AnoMech.Core.Native.Interfaces;

// The real player's input: what they're doing, and the locks a mechanic puts on it.
public interface ILocalPlayerInput
{
    bool MovementInputActive { get; }
    bool IsJumping { get; }
    bool IsAutoAttacking { get; }

    // True once per action used since the last poll.
    bool PollActionUsed();

    // Walks the player along a world XZ direction as if the stick were held, over the keyboard;
    // null lets go. The game moves them at its own speed; `speed` is the run
    // or sprint speed the sim expects, which a headless client moves them at.
    void Steer(System.Numerics.Vector2? direction, float speed);
}
