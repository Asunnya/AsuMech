using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using AnoMech.Core;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Geometry;
using AnoMech.Core.Game.Motion;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Map;
using AnoMech.Core.SimObjects;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Umad;
using AnoMech.Core.Native.Implementations.Interop;

namespace AnoMech.Multiplayer;

public sealed partial class MultiplayerManager
{
    // ---- Host: sampling the live simulation --------------------------------

    // One send in flight at a time, so a slow connection paces the snapshot rate instead of
    // queueing snapshots behind each other.
    private Task? pendingSnapshotSend;

    private MpMessage ForcedMoveMessage(SimWorld world, PartyRole role, ForcedMove move)
    {
        switch (move)
        {
            case ForcedMove.Knockback kb:
                return new KnockbackMessage(role, kb.From.X, kb.From.Y, kb.From.Z, kb.Row.Distance, kb.Row.Speed);
            case ForcedMove.Push push:
                return new PushMessage(role, push.Heading, push.Distance, 0f, push.DurationSeconds);
            case ForcedMove.Carry carry:
                return new CarryMessage(role, carry.To.X, carry.To.Y, carry.To.Z);
            case ForcedMove.Teleport teleport:
                return new TeleportMessage(role, teleport.To.Position.X, teleport.To.Position.Y, teleport.To.Position.Z, teleport.To.Rotation);
            case ForcedMove.Follow follow:
                var (targetEnemy, targetRole) = ResolveEnd(world, follow.Target);
                return new FollowMessage(role, targetRole, targetEnemy, follow.Speed);
            default:
                throw new ArgumentOutOfRangeException(nameof(move), move, null);
        }
    }

    private Task SampleAndBroadcastSnapshot()
    {
        var world = Plugin.Game.World;

        if (TryResolveScenario() is IMultiplayerReplayable replayable
            && replayable.BuildMidRunUpdateMessage() is { } midRunUpdateMsg)
            _ = relay!.SendAsync(midRunUpdateMsg);

        // In the order the mechanic made them: Umad P1's arrow releases the chase, then carries.
        foreach (var role in Enum.GetValues<PartyRole>())
        {
            if (world.Party.Get(role) is not SimNetworkPuppet puppet) continue;
            foreach (var move in puppet.DrainForcedMoves())
                _ = relay!.SendAsync(ForcedMoveMessage(world, role, move));
        }

        var liveEnemies = world.Children.OfType<SimEnemy>().Where(e => e.IsActive).ToList();
        foreach (var stale in hostEnemyNetIds.Keys.Where(e => !liveEnemies.Contains(e)).ToList())
        {
            DiagnosticLog.Debug($"[Multiplayer] Host: enemy NetId {hostEnemyNetIds[stale]} ({stale.BNpcBaseId}) no longer active -- dropping from broadcast.");
            hostEnemyNetIds.Remove(stale);
            hostEnemyLastLoggedModelState.Remove(stale);
            hostEnemyLastLoggedStatuses.Remove(stale);
            hostEnemyLastLoggedAnimationState.Remove(stale);
            hostEnemyLastLoggedEffectSeq.Remove(stale);
        }

        var enemies = new List<EnemyState>(liveEnemies.Count);
        foreach (var enemy in liveEnemies)
        {
            if (!hostEnemyNetIds.TryGetValue(enemy, out var netId))
            {
                netId = nextEnemyNetId++;
                hostEnemyNetIds[enemy] = netId;
                DiagnosticLog.Info($"[Multiplayer] Host: broadcasting new enemy NetId {netId} -- BNpcBase {enemy.BNpcBaseId}, pos {enemy.Position}, visibility {enemy.SpawnConfig.Visibility}.");
            }
            var cfg = enemy.SpawnConfig;
            var modelState = enemy.ModelState;
            if (!hostEnemyLastLoggedModelState.TryGetValue(enemy, out var lastLogged) || lastLogged != modelState)
            {
                hostEnemyLastLoggedModelState[enemy] = modelState;
                DiagnosticLog.Info($"[Multiplayer] Host: enemy NetId {netId} (BNpcBase {enemy.BNpcBaseId}) ModelState -> 0x{modelState:X2}.");
            }
            var statusSnapshot = enemy.ActiveStatusSnapshot;
            if (!hostEnemyLastLoggedStatuses.TryGetValue(enemy, out var lastStatuses))
                hostEnemyLastLoggedStatuses[enemy] = lastStatuses = new Dictionary<(ushort Id, int Ordinal), ushort>();
            LogStatusChanges($"Host: enemy NetId {netId} (BNpcBase {enemy.BNpcBaseId})", statusSnapshot, lastStatuses);
            if (enemy.AnimationState is { } animState
                && (!hostEnemyLastLoggedAnimationState.TryGetValue(enemy, out var lastStateSeq) || lastStateSeq != enemy.AnimationStateSeq))
            {
                hostEnemyLastLoggedAnimationState[enemy] = enemy.AnimationStateSeq;
                DiagnosticLog.Info($"[Multiplayer] Host: enemy NetId {netId} (BNpcBase {enemy.BNpcBaseId}) AnimationState -> ({animState.Slot},{animState.Value}) (seq {enemy.AnimationStateSeq}).");
            }
            var casting = enemy.Casting;
            // Pairs with the peer's line, so a missing effect narrows to the send or the receive.
            if (casting.EffectSeq > 0
                && (!hostEnemyLastLoggedEffectSeq.TryGetValue(enemy, out var lastEffectLogged) || lastEffectLogged != casting.EffectSeq))
            {
                hostEnemyLastLoggedEffectSeq[enemy] = casting.EffectSeq;
                DiagnosticLog.Info($"[Multiplayer] Host: enemy NetId {netId} (BNpcBase {enemy.BNpcBaseId}) effect -> action {casting.EffectActionId} "
                    + $"(seq {casting.EffectSeq}, lock={casting.EffectAnimationLock:F2}).");
            }
            var (castTargetEnemyNetId, castTargetRole) = ResolveTargetId(world, casting.TargetId);
            var (effectAnimationTargetEnemyNetId, effectAnimationTargetRole) = ResolveTargetId(world, casting.EffectAnimationTargetId);
            var (effectActionTargetEnemyNetId, effectActionTargetRole) = ResolveTargetId(world, casting.EffectActionTargetId);
            SimAssets.WarnIfUnknown(SimAssetKind.BNpcBase, enemy.BNpcBaseId, "enemy BNpcBase");
            SimAssets.WarnIfUnknown(SimAssetKind.Action, casting.ActionId, "enemy cast");
            SimAssets.WarnIfUnknown(SimAssetKind.Action, casting.EffectActionId, "enemy effect");
            enemies.Add(new EnemyState(
                netId, enemy.BNpcBaseId, cfg.NameId, cfg.Level, enemy.Targetable, enemy.EnemyListMode,
                cfg.ModelCharaId, cfg.InitialModeAttributeFlags, cfg.Visibility, modelState,
                enemy.ActiveStatuses.Where(s => !SimOnlyStatus.Is(s.StatusId)).Select(s => new EnemyStatusState(s.StatusId, s.Stacks, s.RemainingTime, s.Instance)).ToList(),
                enemy.AnimationState?.Slot, enemy.AnimationState?.Value, enemy.AnimationStateSeq,
                enemy.Position.X, enemy.Position.Y, enemy.Position.Z, enemy.Rotation,
                casting.IsCasting, casting.CastSeq, casting.ActionId, casting.Total, casting.OmenDelay, casting.Rotation,
                casting.TargetLocation?.X, casting.TargetLocation?.Y, casting.TargetLocation?.Z,
                castTargetEnemyNetId, castTargetRole,
                casting.CancelSeq,
                casting.EffectSeq, casting.EffectActionId, casting.EffectAnimationLock, casting.EffectAnimationVariation, casting.EffectRotation,
                casting.EffectPosition?.X, casting.EffectPosition?.Y, casting.EffectPosition?.Z,
                effectAnimationTargetEnemyNetId, effectAnimationTargetRole,
                effectActionTargetEnemyNetId, effectActionTargetRole,
                enemy.HasEngineState
                    ? new ActorEngineState(enemy.LastMode?.Mode ?? 0, enemy.LastMode?.Param ?? 0, enemy.ModeSeq)
                    : null));
        }

        var liveTethers = world.Children.OfType<SimTether>().Where(t => t.IsActive).ToList();
        foreach (var stale in hostTetherNetIds.Keys.Where(t => !liveTethers.Contains(t)).ToList())
        {
            DiagnosticLog.Debug($"[Multiplayer] Host: tether NetId {hostTetherNetIds[stale]} no longer active -- dropping from broadcast.");
            hostTetherNetIds.Remove(stale);
        }

        var tethers = new List<TetherState>(liveTethers.Count);
        foreach (var tether in liveTethers)
        {
            var (aEnemy, aRole) = ResolveEnd(world, tether.A);
            var (bEnemy, bRole) = ResolveEnd(world, tether.B);
            if (!hostTetherNetIds.TryGetValue(tether, out var netId))
            {
                netId = nextTetherNetId++;
                hostTetherNetIds[tether] = netId;
                DiagnosticLog.Info($"[Multiplayer] Host: broadcasting new tether NetId {netId} (TetherId {tether.TetherId}) -- A={(aEnemy is { } ae ? $"enemy#{ae}" : aRole?.ToString() ?? "null")}, B={(bEnemy is { } be ? $"enemy#{be}" : bRole?.ToString() ?? "null")}.");
            }
            SimAssets.WarnIfUnknown(SimAssetKind.Tether, tether.TetherId, "tether");
            tethers.Add(new TetherState(netId, tether.TetherId, aEnemy, aRole, bEnemy, bRole));
        }

        var liveEventObjects = world.Children.OfType<SimEventObject>().Where(o => o.IsActive).ToList();
        foreach (var stale in hostEventObjectNetIds.Keys.Where(o => !liveEventObjects.Contains(o)).ToList())
        {
            DiagnosticLog.Debug($"[Multiplayer] Host: event object NetId {hostEventObjectNetIds[stale]} (EObj 0x{stale.EObjRowId:X}) no longer active -- dropping from broadcast.");
            hostEventObjectNetIds.Remove(stale);
        }

        var eventObjects = new List<EventObjectState>(liveEventObjects.Count);
        foreach (var eo in liveEventObjects)
        {
            if (!hostEventObjectNetIds.TryGetValue(eo, out var netId))
            {
                netId = nextEventObjectNetId++;
                hostEventObjectNetIds[eo] = netId;
                DiagnosticLog.Info($"[Multiplayer] Host: broadcasting new event object NetId {netId} -- EObj 0x{eo.EObjRowId:X}, pos {eo.Position}.");
            }
            SimAssets.WarnIfUnknown(SimAssetKind.EObj, eo.EObjRowId, "event object");
            var eoConfig = eo.SpawnConfig;
            var eventId = (uint)eoConfig.EventId;
            SimAssets.WarnIfUnknown(SimAssetKind.EventId, eventId, "event object EventId");
            SimAssets.WarnIfUnknown(SimAssetKind.Layout, eoConfig.LayoutId, "event object LayoutId");
            eventObjects.Add(new EventObjectState(
                netId, eo.EObjRowId, eoConfig.TimelineState,
                eo.Position.X, eo.Position.Y, eo.Position.Z, eo.Rotation, eoConfig.LayoutId,
                eventId, eoConfig.EntityId, eoConfig.TargetableStatus, eoConfig.Arg2));
        }

        return relay!.SendAsync(new WorldSnapshotMessage(enemies, tethers, eventObjects));
    }

    private Task? pendingRolesSend;

    private unsafe Task SampleAndBroadcastRoles()
    {
        var world = Plugin.Game.World;
        var roles = new List<RoleState>(8);
        foreach (var role in Enum.GetValues<PartyRole>())
        {
            var member = world.Party.Get(role);
            var dead = member is ISimPartyMember { Dead: true };
            IReadOnlyList<EnemyStatusState> statuses = [];
            uint currentHp = 0, maxHp = 0;
            if (member != null)
            {
                var statusSnapshot = member.ActiveStatusSnapshot;
                if (!hostRoleLastLoggedStatuses.TryGetValue(role, out var lastStatuses))
                    hostRoleLastLoggedStatuses[role] = lastStatuses = new Dictionary<(ushort Id, int Ordinal), ushort>();
                LogStatusChanges($"Host: role {role} ({DescribeRoleOwner(role, member)})", statusSnapshot, lastStatuses);
                statuses = member.ActiveStatuses.Where(s => !SimOnlyStatus.Is(s.StatusId)).Select(s => new EnemyStatusState(s.StatusId, s.Stacks, s.RemainingTime, s.Instance)).ToList();
                var bc = member.BattleCharaPtr();
                if (bc != null)
                {
                    currentHp = bc->Health;
                    maxHp = bc->MaxHealth;
                }
            }
            else
            {
                hostRoleLastLoggedStatuses.Remove(role);
            }
            roles.Add(new RoleState(role, member != null, dead,
                member?.Position.X ?? 0f, member?.Position.Y ?? 0f, member?.Position.Z ?? 0f, member?.Rotation ?? 0f,
                statuses, currentHp, maxHp));
        }
        return relay!.SendAsync(new RolesSnapshotMessage(roles));
    }

    private (int? enemyNetId, PartyRole? role) ResolveEnd(SimWorld world, SimCharacter? c)
    {
        if (c is null) return (null, null);
        if (c is SimEnemy e) return hostEnemyNetIds.TryGetValue(e, out var id) ? (id, null) : (null, null);
        foreach (var role in Enum.GetValues<PartyRole>())
            if (ReferenceEquals(world.Party.Get(role), c)) return (null, role);
        return (null, null);
    }

    // ResolveEnd for a cast packet's target, which SimCast stores as a raw GameObjectId.
    private (int? enemyNetId, PartyRole? role) ResolveTargetId(SimWorld world, GameObjectId? targetId)
    {
        if (targetId is not { } id) return (null, null);
        foreach (var role in Enum.GetValues<PartyRole>())
            if (world.Party.Get(role)?.GameObjectId == id) return (null, role);
        foreach (var (enemy, netId) in hostEnemyNetIds)
            if (enemy.GameObjectId == id) return (netId, null);
        return (null, null);
    }

    private void OnPartyMemberKilledHost(PartyRole role, string cause, uint? actionId)
        => _ = relay?.SendAsync(new RoleKilledMessage(role, cause));

    // The host reads its own bookkeeping; a peer reads what the host relayed (PeerStatusMessage).
    public bool IsPeerStale(Guid peerId) => IsHost
        ? peerLastSeenMs.TryGetValue(peerId, out var lastSeen) && Environment.TickCount64 - lastSeen > PeerStaleTimeoutMs
        : peerStatuses.TryGetValue(peerId, out var entry) && entry.SecondsSinceLastSeen * 1000f > PeerStaleTimeoutMs;

    // TickCount64 only moves every ~15.6 ms, too coarse for the clock lead built on this ping.
    private static long PingClockMs() => Stopwatch.GetElapsedTime(0).Ticks / TimeSpan.TicksPerMillisecond;

    private void SendPingAndRefreshStatuses()
    {
        var nowMs = Environment.TickCount64;
        _ = relay!.SendAsync(new PingMessage(PingClockMs()));

        peerStatuses.Clear();
        foreach (var peerId in Session.ClaimedBy.Values.Distinct())
        {
            if (peerId == MyPeerId || !peerLastSeenMs.TryGetValue(peerId, out var lastSeen)) continue;
            var latency = peerLatencyMs.TryGetValue(peerId, out var ms) ? ms : (float?)null;
            peerStatuses[peerId] = new PeerStatusEntry(latency, (nowMs - lastSeen) / 1000f);
        }
        _ = relay.SendAsync(new PeerStatusMessage(new Dictionary<Guid, PeerStatusEntry>(peerStatuses)));

        CheckPeerLiveness();
    }

    private void CheckPeerLiveness()
    {
        foreach (var (role, peerId) in Session.ClaimedBy)
        {
            if (peerId == MyPeerId) continue;
            var stale = IsPeerStale(peerId);
            if (stale && warnedStalePeers.Add(peerId))
            {
                DiagnosticLog.Warn($"[Multiplayer] {Session.NameOf(peerId)} ({role}) hasn't reported in over {PeerStaleTimeoutMs / 1000}s -- likely disconnected.");
                // Ends the run but keeps the role claim: staleness may be a blip, and their
                // client is already reconnecting.
                if (running && Plugin.Game.World.Map.IsInInstance)
                {
                    DiagnosticLog.Info($"[Multiplayer] Ending the run because {Session.NameOf(peerId)} went stale mid-fight.");
                    Plugin.Game.Leave();
                }
            }
            else if (!stale && warnedStalePeers.Remove(peerId))
                DiagnosticLog.Info($"[Multiplayer] {Session.NameOf(peerId)} ({role}) is reporting in again.");
        }
    }

}
