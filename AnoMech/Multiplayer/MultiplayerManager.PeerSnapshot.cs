using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using AnoMech.Core;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Geometry;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Map;
using AnoMech.Core.Native.Implementations;
using AnoMech.Core.SimObjects;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Umad;
using AnoMech.Scenarios.Umad.P3BlackHole;
using static AnoMech.Scenarios.Umad.UmadConstants;
using AnoMech.Core.Native.Implementations.Interop;
using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Multiplayer;

public sealed partial class MultiplayerManager
{
    // ---- Peer: reporting our own pose --------------------------------------

    // Backpressure-gated like SampleAndBroadcastSnapshot.
    private Task? pendingSelfPoseSend;

    private void SendSelfPose()
    {
        if (pendingSelfPoseSend is not (null or { IsCompleted: true })) return;
        var player = Plugin.Game.World.Party.Player;
        if (player == null) return;
        pendingSelfPoseSend = relay!.SendAsync(new SelfPoseMessage(MyPeerId, player.Position.X, player.Position.Y, player.Position.Z, player.Rotation));
    }

    public void ReportAppliedEnemyStatus(IReadOnlyList<SimEnemy> enemies, ushort statusId, float duration)
    {
        if (IsHost || relay is not { IsConnected: true } || enemies.Count == 0) return;
        var netIds = peerEnemies.Where(kv => enemies.Contains(kv.Value)).Select(kv => kv.Key).ToList();
        if (netIds.Count == 0) return;
        DiagnosticLog.Info($"[Multiplayer] Peer: reporting applied status {statusId} on enemy NetIds [{string.Join(",", netIds)}] to host.");
        _ = relay.SendAsync(new PeerAppliedEnemyStatusMessage(MyPeerId, netIds, statusId, duration));
    }

    public void ReportAppliedRoleStatus(IReadOnlyList<PartyRole> roles, ushort statusId, float duration)
    {
        if (IsHost || relay is not { IsConnected: true } || roles.Count == 0) return;
        DiagnosticLog.Info($"[Multiplayer] Peer: reporting applied status {statusId} on roles [{string.Join(",", roles)}] to host.");
        _ = relay.SendAsync(new PeerAppliedRoleStatusMessage(MyPeerId, roles.ToList(), statusId, duration));
    }

    // ---- Host: applying a peer's reported pose to their puppet -------------

    private void OnSelfPoseReceived(SelfPoseMessage msg)
    {
        if (!IsHost) return;
        if (Session.RoleOf(msg.PeerId) is not { } role)
        {
            DiagnosticLog.Debug($"[Multiplayer] SelfPose from {msg.PeerId} but they hold no claimed role -- dropping.");
            return;
        }
        if (!NetGuard.TryPosition(msg.X, msg.Y, msg.Z, out var pose)) return;
        if (Plugin.Game.World.Party.Get(role) is SimNetworkPuppet puppet)
            puppet.ApplyNetworkPose(pose, NetGuard.Rotation(msg.Rotation));
        else
            DiagnosticLog.Debug($"[Multiplayer] SelfPose from {Session.NameOf(msg.PeerId)} ({role}) but that slot isn't a SimNetworkPuppet -- dropping.");
    }

    // ---- Peer: applying a world snapshot ------------------------------------

    private void OnWorldSnapshotReceived(WorldSnapshotMessage snap)
    {
        // Before the zone entry, spawned enemies would be torn down by the load while
        // peerEnemies still tracked them.
        if (!PeerInRun) return;
        var world = Plugin.Game.World;

        var seenEnemyIds = new HashSet<int>();
        foreach (var e in NetGuard.Cap(snap.Enemies, NetGuard.MaxEnemiesPerSnapshot))
        {
            if (!NetGuard.TryPosition(e.X, e.Y, e.Z, out var netPosition))
            {
                DiagnosticLog.Warn($"[Multiplayer] Peer: enemy NetId {e.NetId} sent an out-of-range position -- dropping.");
                continue;
            }
            var placement = new Placement(netPosition, NetGuard.Rotation(e.Rotation));
            if (!SimAssets.Allow(SimAssetKind.BNpcBase, e.BNpcBaseId, $"enemy NetId {e.NetId}")) continue;
            seenEnemyIds.Add(e.NetId);
            var freshlySpawned = !peerEnemies.ContainsKey(e.NetId);
            if (!peerEnemies.TryGetValue(e.NetId, out var enemy))
            {
                // A scenario can override the BNpcBase row's model (UCOB P5's Golden Bahamut), so
                // this has to travel; the allowlist is what stops it naming a foreign one.
                var config = new EnemySpawnConfig(
                    e.BNpcBaseId, e.NameId, e.Level, e.Targetable, Enum.IsDefined(e.EnemyList) ? e.EnemyList : EnemyListMode.Never,
                    Enum.IsDefined(e.Visibility) ? e.Visibility : SpawnVisibility.HiddenUntilShown,
                    placement,
                    ModelCharaId: e.ModelCharaId != 0
                        && SimAssets.Allow(SimAssetKind.ModelChara, e.ModelCharaId, $"enemy NetId {e.NetId} model")
                        ? e.ModelCharaId : 0,
                    e.InitialModeAttributeFlags);
                DiagnosticLog.Info($"[Multiplayer] Peer: first snapshot of enemy NetId {e.NetId} -- BNpcBase {e.BNpcBaseId}, pos ({e.X:F2},{e.Y:F2},{e.Z:F2}), rot {e.Rotation:F2}, visibility {e.Visibility}"
                    + $", cast {e.CastActionId}/seq {e.CastSeq}, effect {e.EffectActionId}/seq {e.EffectSeq} -- spawning local doppel.");
                enemy = world.SpawnEnemy(config);
                if (enemy == null)
                {
                    DiagnosticLog.Warn($"[Multiplayer] Peer: SpawnEnemy returned null for NetId {e.NetId} (BNpcBase {e.BNpcBaseId}) -- skipping this enemy.");
                    continue;
                }
                peerEnemies[e.NetId] = enemy;
            }
            // The engine dropped the packet spawn: the next snapshot recreates it.
            if (enemy.PacketSpawnFailed)
            {
                DiagnosticLog.Warn($"[Multiplayer] Peer: enemy NetId {e.NetId} (BNpcBase {e.BNpcBaseId}) packet spawn failed locally -- respawning.");
                enemy.Despawn();
                ForgetPeerEnemy(e.NetId);
                continue;
            }
            // Interpolated in Tick; a hard SetPosition every snapshot stutters.
            enemy.ApplyNetworkPosition(placement.Position, placement.Rotation);
            enemy.SetTargetable(e.Targetable);
            // Nothing below lands on an actor the engine hasn't created yet; leaving the seqs
            // unrecorded makes the next snapshot retry.
            if (enemy.PacketSpawnPending) continue;
            // Only on change: SetModelState replays the pose transition.
            if (!peerEnemyModelState.TryGetValue(e.NetId, out var lastModelState) || lastModelState != e.ModelState)
            {
                peerEnemyModelState[e.NetId] = e.ModelState;
                DiagnosticLog.Info($"[Multiplayer] Peer: enemy NetId {e.NetId} (BNpcBase {e.BNpcBaseId}) ModelState -> 0x{e.ModelState:X2}.");
                enemy.SetModelState(e.ModelState);
            }
            var enemyStatuses = NetGuard.Cap(e.Statuses, NetGuard.MaxStatusesPerEntity);
            if (!peerEnemyStatusInstances.TryGetValue(e.NetId, out var enemyInstances))
                peerEnemyStatusInstances[e.NetId] = enemyInstances = new Dictionary<ushort, int>();
            if (!peerEnemyReconciledStatuses.TryGetValue(e.NetId, out var enemyReconciled))
                peerEnemyReconciledStatuses[e.NetId] = enemyReconciled = new HashSet<(ushort, GameObjectId)>();
            DropRecreatedStatuses(enemy, enemyStatuses, enemyInstances, _ => true, $"enemy NetId {e.NetId}");
            ReconcileStatuses(enemy, enemyStatuses, enemyReconciled);
            if (!peerEnemyLastLoggedStatuses.TryGetValue(e.NetId, out var lastStatuses))
                peerEnemyLastLoggedStatuses[e.NetId] = lastStatuses = new Dictionary<(ushort Id, int Ordinal), ushort>();
            LogStatusChanges($"Peer: enemy NetId {e.NetId} (BNpcBase {e.BNpcBaseId})",
                enemyStatuses.Select(s => (s.StatusId, s.Stacks, s.RemainingTime)).ToList(), lastStatuses);
            // The host's cast packets, replayed field for field: a peer runs no EnemyAction, so the
            // release arrives as its own effect. Each is keyed on its seq (see EnemyState); seq 0
            // is the never-sent default. An interrupt seen on arrival belongs to a cast long gone.
            var casting = enemy.Casting;
            var cancelKnown = peerEnemyLastCancelSeq.TryGetValue(e.NetId, out var lastCancelSeq);
            peerEnemyLastCancelSeq[e.NetId] = e.CancelSeq;
            if (cancelKnown && lastCancelSeq != e.CancelSeq)
                casting.Cancel(CastCancelReason.Interrupted);
            if (e.CastSeq > 0
                && (!peerEnemyLastCastSeq.TryGetValue(e.NetId, out var lastCastSeq) || lastCastSeq != e.CastSeq))
            {
                peerEnemyLastCastSeq[e.NetId] = e.CastSeq;
                // Snap first: ApplyNetworkPosition above only set an interpolation target.
                enemy.SetPosition(placement);
                var targetLocation = NetGuard.TryPosition(e.CastTargetX, e.CastTargetY, e.CastTargetZ);
                var targetId = ResolvePeerEnd(world, e.CastTargetEnemyNetId, e.CastTargetRole)?.GameObjectId;
                if (SimAssets.Allow(SimAssetKind.Action, e.CastActionId, $"enemy NetId {e.NetId} cast"))
                    casting.NativeCast(e.CastActionId, ActionType.Action,
                        NetGuard.Clamp(e.CastOmenDelay, 0f, 60f), NetGuard.Clamp(e.CastSeconds, 0f, 600f), interruptible: false,
                        rotation: NetGuard.Rotation(e.CastRotation), position: targetLocation, targetId: targetId);
            }
            var effectKnown = peerEnemyLastEffectSeq.TryGetValue(e.NetId, out var lastEffectSeq);
            // NetIds are never reused, so a recorded seq here means state outlived its enemy.
            if (freshlySpawned && effectKnown)
                DiagnosticLog.Warn($"[Multiplayer] Peer: enemy NetId {e.NetId} arrived with a stale effect seq {lastEffectSeq} already recorded -- its action {e.EffectActionId} will be dropped.");
            if (e.EffectSeq > 0 && (!effectKnown || lastEffectSeq != e.EffectSeq))
            {
                DiagnosticLog.Info($"[Multiplayer] Peer: enemy NetId {e.NetId} (BNpcBase {e.BNpcBaseId}) effect -> action {e.EffectActionId} (seq {e.EffectSeq}).");
                peerEnemyLastEffectSeq[e.NetId] = e.EffectSeq;
                enemy.SetPosition(placement); // same snap as above
                if (SimAssets.Allow(SimAssetKind.Action, e.EffectActionId, $"enemy NetId {e.NetId} effect"))
                    casting.NativeActionEffect(e.EffectActionId, NetGuard.Clamp(e.EffectAnimationLock, 0f, 60f, 0.6f),
                        (ushort)e.EffectActionId, e.EffectAnimationVariation, ActionType.Action, 0,
                        rotation: NetGuard.Rotation(e.EffectRotation),
                        position: NetGuard.TryPosition(e.EffectX, e.EffectY, e.EffectZ),
                        animationTargetId: ResolvePeerEnd(world, e.EffectAnimationTargetEnemyNetId, e.EffectAnimationTargetRole)?.GameObjectId,
                        targets: ResolvePeerEnd(world, e.EffectActionTargetEnemyNetId, e.EffectActionTargetRole) is { } actionTarget
                            ? [new ActionEffectTarget(actionTarget.GameObjectId)]
                            : null);
            }
            ApplyEngineState(enemy, e.NetId, e.Engine);
            if (e.AnimationStateArg2 is { } arg2 && e.AnimationStateArg3 is { } arg3
                && (!peerEnemyAnimationState.TryGetValue(e.NetId, out var lastStateSeq) || lastStateSeq != e.AnimationStateSeq))
            {
                peerEnemyAnimationState[e.NetId] = e.AnimationStateSeq;
                DiagnosticLog.Info($"[Multiplayer] Peer: enemy NetId {e.NetId} (BNpcBase {e.BNpcBaseId}) AnimationState -> ({arg2},{arg3}) (seq {e.AnimationStateSeq}).");
                if (arg2 is >= 0 and <= NetGuard.MaxAnimationStateArg && arg3 is >= 0 and <= NetGuard.MaxAnimationStateArg)
                    enemy.SetAnimationState((byte)arg2, (byte)arg3);
            }
        }
        foreach (var staleId in peerEnemies.Keys.Where(id => !seenEnemyIds.Contains(id)).ToList())
        {
            DiagnosticLog.Info($"[Multiplayer] Peer: enemy NetId {staleId} no longer in snapshot -- despawning local doppel.");
            peerEnemies[staleId].Despawn();
            ForgetPeerEnemy(staleId);
        }

        // UMAD P3 black holes: a peer never runs the scenario's obstacle setup, so a debug-bot
        // peer's MoveTo would cut straight through one.
        world.Obstacles.Clear();
        var localPlayer = Plugin.Game.World.Party.Player;
        foreach (var (netId, bh) in peerEnemies.Where(kvp => kvp.Value.BNpcBaseId == BNpcBaseId.BlackHole))
        {
            world.Obstacles.Add(new CircleObstacle(new Vector2(bh.Position.X, bh.Position.Z), UmadP3BlackHoleScenario.BlackHoleAvoidRadius));
            if (localPlayer is null) continue;
            var distSq = localPlayer.Placement().DistanceSq(bh.Position);
            if (distSq < UmadP3BlackHoleScenario.NearBlackHoleLogRadius * UmadP3BlackHoleScenario.NearBlackHoleLogRadius)
                DiagnosticLog.Info(
                    $"[Multiplayer] Peer: local position ({localPlayer.Position.X:F2},{localPlayer.Position.Z:F2}) is {MathF.Sqrt(distSq):F2}y from black hole NetId {netId} at ({bh.Position.X:F2},{bh.Position.Z:F2}).");
        }

        if (TryResolveScenario() is IMultiplayerReplayable replayable)
        {
            replayable.RebuildPeerObstacles(world.Obstacles, peerEnemies, peerEventObjects, localPlayer);
            if (debugShadowStateGeneric != null)
                replayable.RefreshLiveHandles(debugShadowStateGeneric, peerEnemies);
        }

        var seenTetherIds = new HashSet<int>();
        foreach (var t in NetGuard.Cap(snap.Tethers, NetGuard.MaxTethersPerSnapshot))
        {
            if (!SimAssets.Allow(SimAssetKind.Tether, t.TetherId, $"tether NetId {t.NetId}")) continue;
            seenTetherIds.Add(t.NetId);
            var a = ResolvePeerEnd(world, t.AEnemyNetId, t.ARole);
            var b = ResolvePeerEnd(world, t.BEnemyNetId, t.BRole);
            if (a == null && b == null) continue;
            // SimTether's endpoints are fixed at construction, so an endpoint change re-creates it.
            var aDesc = t.AEnemyNetId is { } aId ? $"enemy#{aId}" : t.ARole?.ToString() ?? "null";
            var bDesc = t.BEnemyNetId is { } bId ? $"enemy#{bId}" : t.BRole?.ToString() ?? "null";
            if (peerTethers.TryGetValue(t.NetId, out var existing))
            {
                if (ReferenceEquals(existing.A, a) && ReferenceEquals(existing.B, b)) continue;
                DiagnosticLog.Info($"[Multiplayer] Peer: tether NetId {t.NetId} endpoint changed -- recreating (A={aDesc}, B={bDesc}).");
                existing.Despawn();
            }
            else
            {
                DiagnosticLog.Info($"[Multiplayer] Peer: first snapshot of tether NetId {t.NetId} (TetherId {t.TetherId}) -- A={aDesc}, B={bDesc}.");
            }
            peerTethers[t.NetId] = world.Tether(a, b, t.TetherId);
        }
        foreach (var staleId in peerTethers.Keys.Where(id => !seenTetherIds.Contains(id)).ToList())
        {
            DiagnosticLog.Info($"[Multiplayer] Peer: tether NetId {staleId} no longer in snapshot -- despawning.");
            peerTethers[staleId].Despawn();
            peerTethers.Remove(staleId);
        }

        var seenEventObjectIds = new HashSet<int>();
        foreach (var o in NetGuard.Cap(snap.EventObjects, NetGuard.MaxEventObjectsPerSnapshot))
        {
            if (!NetGuard.TryPosition(o.X, o.Y, o.Z, out var eoPosition))
            {
                DiagnosticLog.Warn($"[Multiplayer] Peer: event object NetId {o.NetId} sent an out-of-range position -- dropping.");
                continue;
            }
            var eoPlacement = new Placement(eoPosition, NetGuard.Rotation(o.Rotation));
            if (!SimAssets.Allow(SimAssetKind.EObj, o.EObjId, $"event object NetId {o.NetId}")) continue;
            if (o.LayoutId != 0 && !SimAssets.Allow(SimAssetKind.Layout, o.LayoutId, $"event object NetId {o.NetId} layout")) continue;
            if (o.EventId != 0 && !SimAssets.Allow(SimAssetKind.EventId, o.EventId, $"event object NetId {o.NetId} EventId")) continue;
            seenEventObjectIds.Add(o.NetId);
            if (!peerEventObjects.TryGetValue(o.NetId, out var eo))
            {
                var config = new EventObjectSpawnConfig
                {
                    EObjId = o.EObjId,
                    Placement = eoPlacement,
                    TimelineState = o.TimelineState,
                    LayoutId = o.LayoutId,
                    EventId = o.EventId,
                    EntityId = o.EntityId,
                    TargetableStatus = o.TargetableStatus,
                    Arg2 = o.Arg2,
                };
                DiagnosticLog.Info($"[Multiplayer] Peer: first snapshot of event object NetId {o.NetId} -- EObj 0x{o.EObjId:X}, pos ({o.X:F2},{o.Y:F2},{o.Z:F2}) -- spawning local copy.");
                eo = world.SpawnEventObject(config);
                if (eo == null)
                {
                    DiagnosticLog.Warn($"[Multiplayer] Peer: SpawnEventObject returned null for NetId {o.NetId} (EObj 0x{o.EObjId:X}) -- skipping.");
                    continue;
                }
                peerEventObjects[o.NetId] = eo;
            }
        }
        foreach (var staleId in peerEventObjects.Keys.Where(id => !seenEventObjectIds.Contains(id)).ToList())
        {
            DiagnosticLog.Info($"[Multiplayer] Peer: event object NetId {staleId} no longer in snapshot -- despawning local copy.");
            peerEventObjects[staleId].Despawn();
            peerEventObjects.Remove(staleId);
        }
    }

    // A status the host dropped and re-added arrives as a new instance of the same id. Refreshed
    // in place it would skip the engine's gain path, which is what applies a param-driven look
    // (Kefka's trance aura), so it is dropped here for the reconcile to add back. Ids the host
    // holds more than once are left to the plain reconcile.
    // A status's identity is (id, source), not id. The host's source ids mean nothing here, so a
    // duplicated id gets a local source per host instance; a unique one keeps the default source,
    // which is also what matches slots the engine wrote.
    private const ulong DuplicateStatusSourceBase = 0xE100_0000;

    private static GameObjectId StatusSource(IReadOnlyList<EnemyStatusState> targets, EnemyStatusState status,
        HashSet<(ushort Id, GameObjectId Source)> tracked)
    {
        var perInstance = (GameObjectId)(DuplicateStatusSourceBase | ((uint)status.Instance & 0xFFFFFF));
        if (targets.Count(s => s.StatusId == status.StatusId) > 1) return perInstance;
        // Sticky: switching the survivor of an expiring pair back would re-add it and flicker.
        return tracked.Contains((status.StatusId, perInstance)) ? perInstance : default;
    }

    // Only what `tracked` holds is removed, so a peer's own statuses are left alone.
    private static void ReconcileStatuses(SimCharacter character, IReadOnlyList<EnemyStatusState> targets,
        HashSet<(ushort Id, GameObjectId Source)> tracked)
    {
        var wanted = new HashSet<(ushort Id, GameObjectId Source)>();
        foreach (var target in targets)
        {
            var source = StatusSource(targets, target, tracked);
            wanted.Add((target.StatusId, source));
            if (character.FindStatus(target.StatusId, source) is { } held && held.Stacks == target.Stacks) continue;
            character.AddStatus(target.StatusId, duration: NetGuard.Clamp(target.RemainingTime, -1f, 3600f),
                stacks: target.Stacks, overrideStacks: true, sourceObject: source);
        }
        foreach (var stale in tracked.Where(s => !wanted.Contains(s)).ToList())
            character.RemoveStatus(stale.Id, stale.Source);
        tracked.Clear();
        tracked.UnionWith(wanted);
    }

    private static void DropRecreatedStatuses(SimCharacter character, IReadOnlyList<EnemyStatusState> targets,
        Dictionary<ushort, int> instances, Func<ushort, bool> mayDrop, string who)
    {
        foreach (var target in targets)
        {
            if (targets.Count(s => s.StatusId == target.StatusId) != 1)
            {
                instances.Remove(target.StatusId);
                continue;
            }
            if (instances.TryGetValue(target.StatusId, out var seen) && seen != target.Instance
                && mayDrop(target.StatusId) && character.HasStatus(target.StatusId))
            {
                DiagnosticLog.Info($"[Multiplayer] Peer: {who} status {target.StatusId} was re-added by the host -- re-adding it here.");
                character.RemoveStatus(target.StatusId);
            }
            instances[target.StatusId] = target.Instance;
        }
        foreach (var id in instances.Keys.Where(id => targets.All(s => s.StatusId != id)).ToList())
            instances.Remove(id);
    }

    private void ForgetPeerEnemy(int netId)
    {
        peerEnemies.Remove(netId);
        peerEnemyStatusInstances.Remove(netId);
        peerEnemyReconciledStatuses.Remove(netId);
        peerEnemyModelState.Remove(netId);
        peerEnemyLastLoggedStatuses.Remove(netId);
        peerEnemyAnimationState.Remove(netId);
        peerEnemyLastEffectSeq.Remove(netId);
        peerEnemyLastCastSeq.Remove(netId);
        peerEnemyLastCancelSeq.Remove(netId);
        peerEnemyEngineSeqs.Remove(netId);
    }

    // The engine-level state behind a VFX-only cue: the carrier's AnimLock. Applies on its seq change.
    private void ApplyEngineState(SimEnemy enemy, int netId, ActorEngineState? engine)
    {
        if (engine is null) return;
        if (engine.ModeSeq > 0 && engine.ModeSeq != peerEnemyEngineSeqs.GetValueOrDefault(netId))
        {
            peerEnemyEngineSeqs[netId] = engine.ModeSeq;
            if (Enum.IsDefined((CharacterModes)engine.Mode))
            {
                DiagnosticLog.Info($"[Multiplayer] Peer: enemy NetId {netId} Mode -> {(CharacterModes)engine.Mode}/{engine.ModeParam} (seq {engine.ModeSeq}).");
                enemy.SetMode((CharacterModes)engine.Mode, engine.ModeParam);
            }
            else
                DiagnosticLog.Warn($"[Multiplayer] Peer: enemy NetId {netId} sent mode {engine.Mode}, which is no CharacterModes value -- dropping.");
        }
    }

    private unsafe void OnRolesSnapshotReceived(RolesSnapshotMessage snap)
    {
        if (!PeerInRun) return;
        var world = Plugin.Game.World;

        var myRole = MyClaimedRole;
        foreach (var r in NetGuard.Cap(snap.Roles, Enum.GetValues<PartyRole>().Length))
        {
            // Position is self-authoritative for our own role; statuses and HP are not.
            if (r.Role != myRole && world.Party.Get(r.Role) is SimNetworkPuppet puppet
                && NetGuard.TryPosition(r.X, r.Y, r.Z, out var rolePosition))
                puppet.ApplyNetworkPose(rolePosition, NetGuard.Rotation(r.Rotation));

            if (world.Party.Get(r.Role) is not { } member) continue;

            var roleStatuses = NetGuard.Cap(r.Statuses, NetGuard.MaxStatusesPerEntity);
            if (!peerRoleReconciledStatuses.TryGetValue(r.Role, out var reconciled))
                peerRoleReconciledStatuses[r.Role] = reconciled = new HashSet<(ushort, GameObjectId)>();
            if (!peerRoleStatusInstances.TryGetValue(r.Role, out var roleInstances))
                peerRoleStatusInstances[r.Role] = roleInstances = new Dictionary<ushort, int>();
            DropRecreatedStatuses(member, roleStatuses, roleInstances, id => reconciled.Any(s => s.Id == id), $"role {r.Role}");
            ReconcileStatuses(member, roleStatuses, reconciled);
            if (!peerRoleLastLoggedStatuses.TryGetValue(r.Role, out var lastStatuses))
                peerRoleLastLoggedStatuses[r.Role] = lastStatuses = new Dictionary<(ushort Id, int Ordinal), ushort>();
            LogStatusChanges($"Peer: role {r.Role} ({DescribeRoleOwner(r.Role, member)})",
                roleStatuses.Select(s => (s.StatusId, s.Stacks, s.RemainingTime)).ToList(), lastStatuses);
        }
    }

    // Forced moves land on our own character only: every other slot is a puppet the host's
    // pose snapshots already place.
    private SimCharacter? OwnMember(PartyRole role, string what)
    {
        if (role != MyClaimedRole) return null;
        if (Plugin.Game.World.Party.Get(role) is { } member) return member;
        DiagnosticLog.Debug($"[Multiplayer] {what} for {role} but that slot is empty locally -- dropping.");
        return null;
    }

    private void OnTeleportReceived(TeleportMessage msg)
    {
        if (!PeerInRun) return;
        if (!NetGuard.TryPosition(msg.X, msg.Y, msg.Z, out var position)) return;
        OwnMember(msg.Role, "Teleport")?.TeleportTo(new Placement(position, NetGuard.Rotation(msg.Rotation)));
    }

    private void OnCarryReceived(CarryMessage msg)
    {
        if (!PeerInRun) return;
        if (!NetGuard.TryPosition(msg.X, msg.Y, msg.Z, out var destination)) return;
        OwnMember(msg.Role, "Carry")?.CarryTo(destination);
    }

    private void OnPushReceived(PushMessage msg)
    {
        if (!PeerInRun) return;
        if (OwnMember(msg.Role, "Push") is not { } member) return;
        var heading = NetGuard.Rotation(msg.Heading);
        var distance = NetGuard.Clamp(msg.Distance, 0f, 200f);
        if (msg.DurationSeconds > 0f)
            member.Push(heading, distance, NetGuard.Clamp(msg.DurationSeconds, 0.01f, 60f));
        else if (msg.Speed > 0f)
            member.Push(heading, distance, distance / NetGuard.Clamp(msg.Speed, 0.01f, 500f));
    }

    private void OnFollowReceived(FollowMessage msg)
    {
        if (!PeerInRun) return;
        if (OwnMember(msg.Role, "Follow") is not { } me) return;
        var target = ResolvePeerEnd(Plugin.Game.World, msg.TargetEnemyNetId, msg.TargetRole);
        DiagnosticLog.Info($"[Multiplayer] Peer: {msg.Role} {(target == null ? "released from follow" : $"following {msg.TargetRole?.ToString() ?? $"enemy#{msg.TargetEnemyNetId}"} at {msg.Speed:F1}y/s")}.");
        // Only forced follows are ever sent (see RemoteMotion.ForceFollow).
        me.ForceFollow(target, NetGuard.Clamp(msg.Speed, 0f, 20f));
    }

    private SimCharacter? ResolvePeerEnd(SimWorld world, int? enemyNetId, PartyRole? role)
    {
        if (enemyNetId is { } id) return peerEnemies.GetValueOrDefault(id);
        if (role is { } r) return world.Party.Get(r);
        return null;
    }

    private void OnRoleKilledReceived(RoleKilledMessage msg)
    {
        if (!PeerInRun) return;
        var cause = NetGuard.Clean(msg.Cause);
        DiagnosticLog.Info($"[Multiplayer] {msg.Role} killed: {cause}");
        if (Plugin.Game.World.Party.Get(msg.Role) is ISimPartyMember member)
            Plugin.Game.Kill(member, cause);
        else
            DiagnosticLog.Debug($"[Multiplayer] RoleKilled for {msg.Role} but that slot isn't an ISimPartyMember locally -- dropping.");
    }

    private void OnKnockbackReceived(KnockbackMessage msg)
    {
        if (!PeerInRun) return;
        if (!NetGuard.TryPosition(msg.SourceX, msg.SourceY, msg.SourceZ, out var source)) return;
        // A push, not a Knockback: the peer's replay of the hit's ActionEffect carries no knockback
        // entry for its client to apply.
        if (Plugin.Game.World.Party.Get(msg.Role) is not { } member)
        {
            DiagnosticLog.Debug($"[Multiplayer] Knockback for {msg.Role} but that slot is empty locally -- dropping.");
            return;
        }
        var distance = NetGuard.Clamp(msg.Distance, 0f, 200f);
        if (msg.Speed > 0f) member.Push(source, distance, distance / NetGuard.Clamp(msg.Speed, 0.01f, 500f));
    }

    private void OnEndReceived(EndMessage msg)
    {
        if (IsHost) return;
        DiagnosticLog.Info($"[Multiplayer] Peer received EndMessage (ReturnedToInn={msg.ReturnedToInn}, Reason={msg.Reason ?? "none"}).");
        if (NetGuard.Clean(msg.Reason) is { Length: > 0 } reason) AnnounceRunEnded(reason);
        running = false;
        StopDebugBotReplay();
        // Leave() assumes a zone was entered: an end that beats our queued entry is acted on by
        // OnPeerStartResolved once the entry completes.
        if (peerEntryQueued)
        {
            endAfterPeerEntry = msg.ReturnedToInn;
            DiagnosticLog.Info("[Multiplayer] EndMessage received while our zone entry is queued -- acting on it once it completes.");
            return;
        }
        if (!Plugin.Game.World.Map.IsInInstance) return;
        if (msg.ReturnedToInn)
            Plugin.Game.Leave();
        else
            Plugin.Game.Reset();
    }

}
