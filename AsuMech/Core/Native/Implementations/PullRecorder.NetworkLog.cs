using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using AsuMech.Core.Game.Trace;
using AsuMech.Core.Native.Interfaces;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ActorCastPacket = AsuMech.Core.Native.Implementations.Pointers.ActorCastPacket;
using DalamudObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;

namespace AsuMech.Core.Native.Implementations;

// The recording's network .log, written from the same hooks as the CSV, so every pull has one
// whether or not the player runs a combat parser. Players go out under the CSV's labels.
internal sealed unsafe partial class PullRecorder
{
    private const ushort CategoryDeath = 0x06, CategoryCancel = 0x0F, CategoryTargetIcon = 0x22, CategoryTether = 0x23,
        CategoryTargetable = 0x36, CategoryDirector = 0x6D, CategoryLimitBreak = 505, CategoryHoT = 0x604, CategoryDoT = 0x605;
    private static readonly HashSet<ushort> ExtraCategories = [62, 63, 49, 184, 407, 413, 2100];
    private static readonly HashSet<ushort> SelfExtraCategories = [527, 528];
    private const byte DisplayItemName = 2, DisplayMountName = 13;
    private const long SingleTargetListOffset = 0x120, MaxEffectPacketOffset = 0x1000;

    private StreamWriter? netLogFile;
    private NetworkLogWriter? netLog;
    private readonly Dictionary<uint, CombatantInfo> combatants = [];

    private sealed record CombatantInfo(uint Owner, string Name, int Job, int Level, uint WorldId, string WorldName, uint BNpcNameId, uint BNpcId)
    {
        public CombatantState Last { get; set; }
    }

    private void OpenNetworkLog(uint territory, string zoneName, string path)
    {
        combatants.Clear();
        try
        {
            netLogFile = new StreamWriter(RecordingFiles.LogPath(path), false, new UTF8Encoding(false), 1 << 16) { NewLine = "\r\n" };
        }
        catch (Exception e)
        {
            DiagnosticLog.Warn($"[PullRecorder] can't open the network log: {e.Message}");
            netLogFile = null;
            netLog = null;
            return;
        }
        var anonymizer = new NetworkLogAnonymizer(new Dictionary<uint, string>(), PlayerLabel);
        netLog = new NetworkLogWriter(netLogFile, () => DateTimeOffset.Now, anonymizer.Rewrite);
        netLog.Version($"This is AsuMech {typeof(PullRecorder).Assembly.GetName().Version} network log of the pull");
        netLog.Zone(territory, zoneName);
        if (Plugin.ObjectTable.LocalPlayer is { } player) netLog.PrimaryPlayer(player.EntityId, player.Name.TextValue);
    }

    private void CloseNetworkLog()
    {
        netLog = null;
        netLogFile?.Dispose();
        netLogFile = null;
    }

    // Real names: the anonymizer swaps a player's for the label once it has seen it on an 03 line.
    private string LogName(uint id) => id is 0 or NoTarget ? "" : realNames.TryGetValue(id, out var name) ? name : NameOf(id);

    private static CombatantState? Snapshot(uint id)
        => id is 0 or NoTarget || Plugin.ObjectTable.SearchByEntityId(id) is not { } obj ? null : Snapshot(obj);

    private static CombatantState Snapshot(IGameObject obj)
    {
        var c = obj as ICharacter;
        return new CombatantState(c?.CurrentHp ?? 0, c?.MaxHp ?? 0, c?.CurrentMp ?? 0, c?.MaxMp ?? 0, obj.Position, obj.Rotation);
    }

    // An object the spawn packets didn't announce (the table already held it when the recording began).
    private void LogSeen(IGameObject obj)
    {
        if (netLog == null || combatants.ContainsKey(obj.EntityId) || obj.ObjectKind == DalamudObjectKind.EventObj) return;
        var character = obj as ICharacter;
        var player = obj as IPlayerCharacter;
        var npc = obj as IBattleNpc;
        LogAdd(obj.EntityId, new CombatantInfo(obj.OwnerId is var o and not NoTarget ? o : 0, LogName(obj.EntityId), (int)(character?.ClassJob.RowId ?? 0),
            character?.Level ?? 0, player?.HomeWorld.RowId ?? 0, player?.HomeWorld.ValueNullable?.Name.ExtractText() ?? "",
            npc?.NameId ?? 0, npc != null ? obj.BaseId : 0), Snapshot(obj));
    }

    private void LogAdd(uint id, CombatantInfo info, CombatantState now)
    {
        if (netLog == null) return;
        info.Last = now;
        combatants[id] = info;
        netLog.AddCombatant(id, info.Owner, info.Name, info.Job, info.Level, info.WorldId, info.WorldName, info.BNpcNameId, info.BNpcId, now);
        netLog.Combatant("Add", id, new (string, string)[] { ("Name", info.Name), ("Job", info.Job.ToString()), ("BNpcID", info.BNpcId.ToString("X")),
            ("BNpcNameID", info.BNpcNameId.ToString("X")), ("CurrentHP", now.Hp.ToString()), ("MaxHP", now.MaxHp.ToString()) }
            .Concat(NetworkLogWriter.PoseFields(now.Position, now.Heading)));
    }

    private void LogGone(uint id)
    {
        if (netLog == null || !combatants.Remove(id, out var info)) return;
        netLog.RemoveCombatant(id, info.Owner, info.Name, info.Job, info.Level, info.WorldId, info.WorldName, info.BNpcNameId, info.BNpcId, info.Last);
        netLog.Combatant("Remove", id, []);
    }

    private void LogPose(IGameObject obj)
    {
        if (netLog == null || !combatants.TryGetValue(obj.EntityId, out var info)) return;
        info.Last = Snapshot(obj);
        netLog.Combatant("Change", obj.EntityId, NetworkLogWriter.PoseFields(obj.Position, obj.Rotation));
    }

    private void LogHp(IBattleChara chara)
    {
        if (netLog == null) return;
        var now = Snapshot(chara);
        if (combatants.TryGetValue(chara.EntityId, out var info)) info.Last = now;
        netLog.UpdateHp(chara.EntityId, LogName(chara.EntityId), now);
    }

    private void LogStatus(bool gained, uint target, ushort statusId, float remaining, uint source, ushort param)
    {
        if (netLog == null) return;
        var name = Natives.Data.Status(statusId)?.Name ?? "";
        var sourceId = source == 0 ? NoTarget : source;
        var targetMax = Snapshot(target)?.MaxHp;
        var sourceMax = Snapshot(source)?.MaxHp;
        if (gained) netLog.StatusAdd(statusId, name, remaining, sourceId, LogName(source), target, LogName(target), param, targetMax, sourceMax);
        else netLog.StatusRemove(statusId, name, sourceId, LogName(source), target, LogName(target), param, targetMax, sourceMax);
    }

    private void LogActorControl(uint id, uint category, uint arg1, uint arg2, uint arg3, uint arg4, uint arg5, uint arg6)
    {
        if (netLog == null) return;
        var name = LogName(id);
        switch ((ushort)category)
        {
            case CategoryDoT or CategoryHoT:
                netLog.DoT(id, name, category == CategoryHoT, arg1, arg2, Snapshot(id), arg3, LogName(arg3), arg4, Snapshot(arg3));
                break;
            case CategoryCancel:
                netLog.Cancel(id, name, arg3, ActionName(arg3), arg4 == 0, arg4 == 1);
                break;
            case CategoryDeath:
                netLog.Death(id, name, arg1, LogName(arg1));
                break;
            case CategoryTargetIcon:
                netLog.TargetIcon(id, name, 0, 0, arg1, arg2, arg3, arg4);
                break;
            case CategoryTargetable:
                netLog.NameToggle(id, name, (byte)(arg1 & 0xFF));
                break;
            case CategoryTether:
                netLog.Tether(id, name, arg3, LogName(arg3), 0, arg1, arg2, arg3, arg4, 0);
                break;
            case CategoryDirector when (arg2 & 0xFFFF) != 0 && (arg2 & 0xFF000000) != 0:
                netLog.Director(arg1, arg2, arg3, arg4, arg5, arg6);
                break;
            case CategoryLimitBreak:
                netLog.LimitBreak((byte)(arg1 & 0xFF), arg2 & 0xFFFF);
                break;
            case var c when ExtraCategories.Contains(c):
                netLog.ActorControlExtra(false, id, c, arg1, arg2, arg3, arg4);
                break;
            case var c when SelfExtraCategories.Contains(c):
                netLog.ActorControlExtra(true, id, c, arg1, arg2, arg3, arg4);
                break;
        }
    }

    // Line 21 is the single-target packet and 22 the others. Which one arrived shows in where its
    // target list starts: right after one effect block, or after eight or more. UNVERIFIED that
    // the handler is passed the list inside the packet; when it isn't, a target count above one
    // stands in.
    private void LogAbility(uint caster, Character* casterChara, Vector3* targetPos, ActionEffectHandler.Header* header,
        ActionEffectHandler.TargetEffects* targetEffects, GameObjectId* targetIds)
    {
        if (netLog == null) return;
        var displayType = header->ActionType;
        var skill = displayType switch
        {
            DisplayMountName => 0x0D000000 + header->ActionId,
            DisplayItemName => 0x02000000 + header->ActionId,
            _ => (uint)header->SpellId,
        };
        var skillName = ActionName(skill);
        var casterName = LogName(caster);
        var casterNow = Snapshot(caster);
        var owner = casterChara != null && casterChara->OwnerId is var o and not NoTarget and not 0 ? o : 0;
        var count = header->NumTargets;
        var total = 0;
        for (var i = 0; i < count; i++)
            if (Id(targetIds[i]) != 0) total++;
        var offset = (long)((byte*)targetIds - (byte*)dispatchingPayload);
        var aoe = dispatchingPayload != 0 && offset is > 0 and < MaxEffectPacketOffset ? offset > SingleTargetListOffset : count > 1;
        var rotation = MathUtil.DequantizeRotation(header->RotationInt);
        if (total == 0)
            netLog.Ability(false, caster, casterName, skill, skillName, NoTarget, "", casterNow, null, [], header->GlobalSequence, 0, 0,
                owner, LogName(owner), displayType, header->ActionId, header->SpellId, header->AnimationLock, header->RotationInt);
        for (var i = 0; i < count; i++)
        {
            var target = Id(targetIds[i]);
            if (target == 0) continue;
            var effects = new ReadOnlySpan<ulong>((ulong*)(targetEffects + i), 8);
            netLog.Ability(aoe, caster, casterName, skill, skillName, target, LogName(target), casterNow, Snapshot(target), effects,
                header->GlobalSequence, i, total, owner, LogName(owner), displayType, header->ActionId, header->SpellId, header->AnimationLock, header->RotationInt);
        }
        Vector3? point = aoe && targetPos != null ? *targetPos : null;
        netLog.AbilityExtra(caster, header->ActionId, header->GlobalSequence, point, rotation, Id(header->AnimationTargetId) is var t and not 0 ? t : NoTarget);
    }

    private void LogCast(uint id, ActorCastPacket* packet, Vector3 position, float rotation)
    {
        if (netLog == null) return;
        var target = packet->TargetEntityId;
        netLog.StartsCasting(id, LogName(id), target, LogName(target), packet->ActionId_2, ActionName(packet->ActionId_2), packet->CastTime, Snapshot(id));
        netLog.CastExtra(id, packet->ActionId_2, position, rotation);
    }
}
