using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Tests;

// Nobody at the keyboard, unless a test holds a movement key.
internal sealed class FakeLocalPlayerInput : ILocalPlayerInput
{
    public bool MovementInputActive { get; set; }
    public bool IsJumping => false;
    public bool IsAutoAttacking => false;
    public bool PollActionUsed() => false;

    public bool ZeroMovement { get; set; }
    public bool DisableAllActions { get; set; }

    public System.Numerics.Vector2? SteerDirection { get; private set; }
    public float SteerSpeed { get; private set; }

    public void Steer(System.Numerics.Vector2? direction, float speed)
    {
        SteerDirection = direction is { } d && d.LengthSquared() > 0f ? System.Numerics.Vector2.Normalize(d) : null;
        SteerSpeed = speed;
    }
}
