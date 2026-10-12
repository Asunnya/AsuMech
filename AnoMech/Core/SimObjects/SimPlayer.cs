using System;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.UserActions;

namespace AnoMech.Core.SimObjects;

public sealed class SimPlayer(Coordinates coordinates) : SimCharacter(coordinates), ISimPartyMember
{
    public PartyRole Role { get; set; }

    // For stillness/movement mechanics: IsMoving = movement input, a jump, any action, or an
    // in-flight debug-bot MoveTo; IsActing also counts auto-attacks. Forced false while KO'd.
    public bool IsMoving { get; private set; }
    public bool IsActing { get; private set; }

    internal override IBattleCharaProxy Proxy => Natives.BattleCharas.LocalPlayer;

    private protected override PlayerMovement Movement => field ??= new PlayerMovement(this);

    // The client is authoritative for its own player, as in the real game.
    protected override bool ModelDriven => true;

    // Only a turn reaches here (PlayerMovement walks through input, not steps); the client drops
    // ActorMove for its own player, so the turn goes as a SetPos on the spot.
    protected override void SendMove(Vector3 worldPosition, float rotation, ActorMoveFlags flags, float speed)
        => Proxy.SetPos(Proxy.Position, rotation);

    public void UseSprint(float duration)
    {
        if (DebugBotControl.Enabled && !Dead && !ActionsLocked) SprintHandler.Apply(this, duration);
    }

    // Pushes go to the client as the retail push packet rather than through Movement.
    private float pushRemaining;

    // Holds a debug bot's Movement while the client slides, so the two don't fight over the position.
    public override bool AnimationLock => pushRemaining > 0f;

    public void Knockback(Vector3 source, float distance, float speed)
    {
        var dx = Position.X - source.X;
        var dz = Position.Z - source.Z;
        if (dx * dx + dz * dz < 1e-6f || speed <= 0f) return;
        Push(MathF.Atan2(dx, dz), distance, distance / speed);
    }

    public void PushInDirection(float heading, float distance, float speed)
    {
        if (speed > 0f) Push(heading, distance, distance / speed);
    }

    public void PushInDirectionEased(float heading, float distance, float durationSeconds) => Push(heading, distance, durationSeconds);

    private void Push(float heading, float distance, float durationSeconds)
    {
        if (Dead) return;
        ActorControl.Push(heading, distance, durationSeconds);
        pushRemaining = durationSeconds;
    }

    // A knockback the client applies itself from the ActionEffect: nothing to send, only the hold.
    internal void HoldForClientKnockback(float durationSeconds)
    {
        if (!Dead) pushRemaining = durationSeconds;
    }

    // The input lock is re-derived every tick from Dead/Movement/statuses.
    public override void Tick(float deltaSeconds)
    {
        pushRemaining = MathF.Max(0f, pushRemaining - deltaSeconds);
        base.Tick(deltaSeconds);
        SampleActivity();
        SyncInputLock();
    }

    private void SampleActivity()
    {
        var hooks = Natives.PlayerInput;
        // Drained every frame, even while dead, so a stale press can't carry over.
        var actedThisFrame = hooks.PollActionUsed();
        if (Dead)
        {
            IsMoving = false;
            IsActing = false;
            return;
        }
        IsMoving = hooks.MovementInputActive || actedThisFrame || hooks.IsJumping || Movement.IsMoving || pushRemaining > 0f;
        // The press is one frame, but the whole cast bar counts: an Acceleration Bomb landing
        // mid-cast catches the player acting, exactly as it would in the fight.
        IsActing = IsMoving || hooks.IsAutoAttacking || Proxy.IsCasting;
    }

    public void OnKilled(bool byEnvironment)
    {
        if (!GoDown(byEnvironment)) return;
        SyncInputLock(); // engage the lock now, not one frame later
    }

    public override void Despawn()
    {
        base.Despawn();
        StopMoving();
        pushRemaining = 0f;
        Revive();
        // Nothing ticks between a reset and the next scenario, so the lock must clear here.
        SyncInputLock();
    }

    private void SyncInputLock()
    {
        var hooks = Natives.PlayerInput;
        hooks.ZeroMovement = Dead || Movement.IsMoving || MovementLocked;
        hooks.DisableAllActions = Dead || ActionsLocked;
    }
}
