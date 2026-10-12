using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Core.Native.Implementations;

// Hooks the native action and movement input paths so a mechanic can stun the local player.
// Status-row writes don't enforce anything (the server overwrites them every packet); the real
// lockout is the flags below, which SimPlayer reconciles each tick.
//
// Signatures and detour shapes lifted from FFXIV-RaidsRewritten's PlayerMovementOverride.cs /
// ActionManagerEx.cs (which credit awgil's vnavmesh + bossmod).
public sealed unsafe class LocalPlayerInputHooks : ILocalPlayerInput, IDisposable
{
    public bool DisableAllActions { get; set; }
    public bool ZeroMovement { get; set; }

    // Raised after the local player successfully executes a real action (the
    // auto-attack-cancel general action is filtered out). The UserActions module
    // subscribes to resolve effects the sim firewall blocks; nothing here depends
    // on a subscriber.
    public event Action<ActionType, uint, ulong>? ActionExecuted;

    // --- Player activity signals (read by SimPlayer to drive Party.Player.IsMoving/IsActing) ---
    // The engine's own per-frame movement sample (the signal bossmod reads), captured before
    // stun-zeroing: input intent, not a position delta.
    public bool MovementInputActive { get; private set; }

    public bool IsAutoAttacking => UIState.Instance()->WeaponState.AutoAttackState.IsAutoAttacking;

    // State poll: a jump is always self-initiated.
    public bool IsJumping => Plugin.Condition[ConditionFlag.Jumping];

    // Latched on a real action press; drained once per frame by SimPlayer.
    private bool actionUsedSincePoll;
    public bool PollActionUsed()
    {
        var used = actionUsedSincePoll;
        actionUsedSincePoll = false;
        return used;
    }

    // Debug aid for pinning down a real action id; DebugMenu shows these.
    private const int RecentActionsCapacity = 50;
    private readonly Queue<(uint ActionId, ActionType Type)> recentActions = new();
    public IReadOnlyCollection<(uint ActionId, ActionType Type)> RecentActions => recentActions;

    private void RecordRecentAction(uint actionId, ActionType type)
    {
        // GeneralAction 1 is the auto-attack engage/re-engage action -- fires constantly, pure noise here.
        if (type == ActionType.GeneralAction && actionId == 1) return;
        ZoneSession.NoteActionPressed(type, actionId);
        recentActions.Enqueue((actionId, type));
        while (recentActions.Count > RecentActionsCapacity) recentActions.Dequeue();
        var jobId = Plugin.ObjectTable.LocalPlayer?.ClassJob.RowId;
        Core.DiagnosticLog.Info($"[LocalPlayerInputHooks] Action pressed: {actionId} ({type}) -- {Core.ActionLookup.Name(actionId)} (job={jobId}).");
    }

    // Edge-triggered gain/loss logging; reads the local player directly so it works with no
    // scenario running.
    // Counted, not just present: the same id can be held twice.
    private readonly Dictionary<ushort, int> lastLoggedStatusCounts = new();

    private void ScanAndLogActiveStatuses()
    {
        var localPlayer = Plugin.ObjectTable.LocalPlayer;
        if (localPlayer == null) return;
        var bc = (BattleChara*)localPlayer.Address;
        if (bc == null) return;
        var jobId = localPlayer.ClassJob.RowId;
        var current = new Dictionary<ushort, (int Count, float Remaining)>();
        foreach (var status in bc->StatusManager.Status)
        {
            if (status.StatusId == 0) continue;
            var seen = current.GetValueOrDefault(status.StatusId);
            current[status.StatusId] = (seen.Count + 1, status.RemainingTime);
        }
        foreach (var (gained, (count, remaining)) in current)
            if (count > lastLoggedStatusCounts.GetValueOrDefault(gained))
                Core.DiagnosticLog.Info($"[LocalPlayerInputHooks] Status gained: {gained} -- {Core.StatusLookup.Name(gained)} (job={jobId}, duration={remaining:F1}, x{count}).");
        foreach (var lost in lastLoggedStatusCounts.Keys.Except(current.Keys))
            Core.DiagnosticLog.Info($"[LocalPlayerInputHooks] Status lost: {lost} -- {Core.StatusLookup.Name(lost)} (job={jobId}).");
        lastLoggedStatusCounts.Clear();
        foreach (var (id, (count, _)) in current) lastLoggedStatusCounts[id] = count;
    }

    // Latched whenever the game calls Hotbar.CancelCast — i.e. the player requested a
    // cast cancel (ESC / the cancel-cast keybind, however it's bound; also cancels
    // auto-attack). This is the outgoing intent the server would act on; the sim
    // firewall eats that round-trip, so UserActions drains this each frame and
    // synthesizes the interrupt itself.
    private bool cancelCastRequested;
    public bool PollCancelCast()
    {
        var requested = cancelCastRequested;
        cancelCastRequested = false;
        return requested;
    }

    private delegate void RMIWalkDelegate(void* self, float* sumLeft, float* sumForward, float* sumTurnLeft, byte* haveBackwardOrStrafe, byte* a6, byte bAdditiveUnk);
    [Signature("E8 ?? ?? ?? ?? 80 7B 3E 00 48 8D 3D")]
    private Hook<RMIWalkDelegate> rmiWalkHook = null!;

    private enum KeybindType
    {
        StrafeLeft = 325,
        StrafeRight = 326,
    }

    [return: MarshalAs(UnmanagedType.U1)]
    private delegate bool CheckStrafeKeybindDelegate(IntPtr ptr, KeybindType keybind);
    [Signature("E8 ?? ?? ?? ?? 84 C0 74 04 41 C6 06 01 BA 44 01 00 00")]
    private Hook<CheckStrafeKeybindDelegate> checkStrafeKeybindHook = null!;

    private readonly Hook<InputData.Delegates.IsInputIdPressed> isInputIdPressedHook;
    private readonly Hook<ActionManager.Delegates.Update> updateHook;
    private readonly Hook<ActionManager.Delegates.UseAction> useActionHook;
    private readonly Hook<ActionManager.Delegates.UseActionLocation> useActionLocationHook;
    private readonly Hook<Hotbar.Delegates.CancelCast> cancelCastHook;

    public LocalPlayerInputHooks(IGameInteropProvider hook)
    {
        hook.InitializeFromAttributes(this);

        isInputIdPressedHook = hook.HookFromAddress<InputData.Delegates.IsInputIdPressed>(
            InputData.Addresses.IsInputIdPressed.Value, IsInputIdPressedDetour);
        updateHook = hook.HookFromAddress<ActionManager.Delegates.Update>(
            ActionManager.Addresses.Update.Value, UpdateDetour);
        useActionHook = hook.HookFromAddress<ActionManager.Delegates.UseAction>(
            ActionManager.Addresses.UseAction.Value, UseActionDetour);
        useActionLocationHook = hook.HookFromAddress<ActionManager.Delegates.UseActionLocation>(
            ActionManager.Addresses.UseActionLocation.Value, UseActionLocationDetour);
        cancelCastHook = hook.HookFromAddress<Hotbar.Delegates.CancelCast>(
            Hotbar.Addresses.CancelCast.Value, CancelCastDetour);

        rmiWalkHook.Enable();
        checkStrafeKeybindHook.Enable();
        isInputIdPressedHook.Enable();
        updateHook.Enable();
        useActionHook.Enable();
        useActionLocationHook.Enable();
        cancelCastHook.Enable();
    }

    public void Dispose()
    {
        rmiWalkHook?.Dispose();
        checkStrafeKeybindHook?.Dispose();
        isInputIdPressedHook?.Dispose();
        updateHook?.Dispose();
        useActionHook?.Dispose();
        useActionLocationHook?.Dispose();
        cancelCastHook?.Dispose();
    }

    // The player asked to cancel their cast; latch it and let the original run (its
    // outgoing packet is firewalled in the sim, so it has no visible effect here).
    private void CancelCastDetour(Hotbar* thisPtr)
    {
        cancelCastRequested = true;
        cancelCastHook.Original(thisPtr);
    }

    private Vector2? steerDirection;

    public void Steer(Vector2? direction, float speed)
        => steerDirection = direction is { } d && d.LengthSquared() > 0f ? Vector2.Normalize(d) : null;

    private void RMIWalkDetour(void* self, float* sumLeft, float* sumForward, float* sumTurnLeft, byte* haveBackwardOrStrafe, byte* a6, byte bAdditiveUnk)
    {
        rmiWalkHook.Original(self, sumLeft, sumForward, sumTurnLeft, haveBackwardOrStrafe, a6, bAdditiveUnk);
        // self is a MoveControllerSubMemberForMine*; the sums are its move vector.
        MovementInputActive = *sumLeft != 0 || *sumForward != 0;
        // bAdditiveUnk != 0 is the game's second, additive sample; bossmod only steers the first.
        if (steerDirection is { } dir && bAdditiveUnk == 0 && SteerReference() is { } reference)
        {
            var relative = MathF.Atan2(dir.X, dir.Y) - reference;
            *sumLeft = MathF.Sin(relative);
            *sumForward = MathF.Cos(relative);
            *haveBackwardOrStrafe = 0;
            return;
        }
        if (!ZeroMovement) return;
        *sumLeft = 0;
        *sumForward = 0;
        *haveBackwardOrStrafe = 0;
    }

    // The angle the move vector is relative to (bossmod's MovementOverride): the character's
    // facing in standard mode, the camera's back in legacy mode. Null with no player or camera.
    private static float? SteerReference()
    {
        var player = Plugin.ObjectTable.LocalPlayer;
        if (player == null) return null;
        var legacy = Plugin.GameConfig.UiControl.TryGetUInt("MoveMode", out var mode) && mode == 1;
        if (!legacy) return player.Rotation;
        var camera = FFXIVClientStructs.FFXIV.Client.Game.Control.CameraManager.Instance()->GetActiveCamera();
        var render = camera != null ? camera->SceneCamera.RenderCamera : null;
        if (render == null) return null;
        var view = render->ViewMatrix;
        return MathF.Atan2(view.M13, view.M33) + MathF.PI;
    }

    private bool CheckStrafeKeybindDetour(IntPtr ptr, KeybindType keybind)
    {
        if (ZeroMovement && (keybind == KeybindType.StrafeLeft || keybind == KeybindType.StrafeRight))
            return false;
        return checkStrafeKeybindHook.Original(ptr, keybind);
    }

    private bool IsInputIdPressedDetour(InputData* inputData, InputId inputId)
    {
        if (ZeroMovement && (inputId == InputId.JUMP || inputId == InputId.PAD_JUMPANDCANCELCAST))
            return false;
        return isInputIdPressedHook.Original(inputData, inputId);
    }

    private void UpdateDetour(ActionManager* self)
    {
        updateHook.Original(self);
        ScanAndLogActiveStatuses();
    }

    private bool UseActionDetour(ActionManager* self, ActionType actionType, uint actionId, ulong targetId, uint extraParam, ActionManager.UseActionMode mode, uint comboRouteId, bool* outOptAreaTargeted)
    {
        RecordRecentAction(actionId, actionType);
        if (DisableAllActions) return false;
        var result = useActionHook.Original(self, actionType, actionId, targetId, extraParam, mode, comboRouteId, outOptAreaTargeted);
        if (result)
        {
            actionUsedSincePoll = true;
            ActionExecuted?.Invoke(actionType, actionId, targetId);
        }
        return result;
    }

    private bool UseActionLocationDetour(ActionManager* self, ActionType actionType, uint actionId, ulong targetId, Vector3* location, uint extraParam, byte a7)
    {
        if (DisableAllActions) return false;
        var result = useActionLocationHook.Original(self, actionType, actionId, targetId, location, extraParam, a7);
        if (result)
        {
            actionUsedSincePoll = true;
            ActionExecuted?.Invoke(actionType, actionId, targetId);
        }
        return result;
    }
}
