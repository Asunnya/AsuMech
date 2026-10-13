using System;
using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.SimObjects;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using GroupPartyMember = FFXIVClientStructs.FFXIV.Client.Game.Group.PartyMember;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.Native.Implementations.Interop;
using AnoMech.Core.Native.Implementations.Pointers;

namespace AnoMech.Core.Native.Implementations;

// Mirrors the sim party into MainGroup the way the server does: an UpdateParty packet whenever
// the roster changes. The _PartyList addon only renders members when MemberCount > 0, and
// resolves each slot's BattleChara by EntityId (registered in CharacterManager; scenarios are
// inn-gated upstream) for status icons and timer text.
//
// The firewall keeps real UpdateParty packets out during a sim, so the pre-sim party captured
// at the first send is what Clear restores.
internal sealed unsafe class PartyHud : IPartyHud
{
    // Doppels have no ContentId; this top byte never appears on a real one.
    private const ulong DoppelContentIdTag = 0xFF00000000000000UL;
    private const long SimPartyId = 0xFFFFFFFF;

    private readonly List<(uint EntityId, byte ClassJob, byte Level)> sentRoster = new();
    private readonly List<(uint EntityId, byte ClassJob, byte Level)> roster = new();
    private bool ownsRoster;
    private UpdatePartyPacket realParty;

    public void Refresh(SimParty party)
    {
        if (party.AllMembers().Count() < 2) return;

        var gm = GroupManager.Instance();
        if (gm == null) return;

        var members = OrderedMembers(party);
        roster.Clear();
        foreach (var p in members)
        {
            var bc = (BattleChara*)p;
            roster.Add((((GameObject*)bc)->EntityId, bc->ClassJob, bc->Level));
        }

        if (!ownsRoster || !roster.SequenceEqual(sentRoster))
        {
            if (!ownsRoster)
            {
                realParty = SnapshotPacket(ref gm->MainGroup);
                ownsRoster = true;
            }
            SendRoster(members, realParty);
            sentRoster.Clear();
            sentRoster.AddRange(roster);
        }
    }

    public void Clear()
    {
        if (!ownsRoster) return;
        ownsRoster = false;
        sentRoster.Clear();
        var packet = realParty;
        PacketDispatcherPointers.HandleUpdatePartyPacket(0, &packet, 0);
        realParty = default;
    }

    // The player first, then the bots in role order: the slot order the player sees.
    private static List<nint> OrderedMembers(SimParty party)
    {
        var list = new List<nint>();
        foreach (var member in party.AllMembers().OrderBy(m => m is SimNpc ? 1 : 0))
        {
            var bc = member.BattleCharaPtr();
            if (bc != null) list.Add((nint)bc);
        }
        return list;
    }

    private void SendRoster(List<nint> members, in UpdatePartyPacket real)
    {
        var packet = new UpdatePartyPacket();
        var entries = (UpdatePartyPacketMember*)&packet;
        var count = Math.Min(members.Count, UpdatePartyPacket.Capacity);
        for (var i = 0; i < count; i++)
            WriteEntry(&entries[i], (BattleChara*)members[i]);
        packet.MemberCount = (byte)count;
        packet.LeaderIndex = 0;
        packet.PartyId = real.PartyId != 0 ? real.PartyId : SimPartyId;
        packet.PartyId_2 = real.PartyId_2 != 0 ? real.PartyId_2 : SimPartyId;
        PacketDispatcherPointers.HandleUpdatePartyPacket(0, &packet, 0);
    }

    private static void WriteEntry(UpdatePartyPacketMember* entry, BattleChara* bc)
    {
        var obj = (GameObject*)bc;
        // The local player keeps its real ids: AgentReadyCheck and other party lookups match on them.
        entry->ContentId = bc->ContentId != 0 ? bc->ContentId : DoppelContentIdTag | obj->EntityId;
        entry->AccountId = bc->AccountId != 0 ? bc->AccountId : DoppelContentIdTag | obj->EntityId;
        entry->EntityId = obj->EntityId;
        entry->PetEntityId = 0xE0000000;
        entry->CompanionEntityId = 0xE0000000;
        entry->CurrentHP = bc->Health;
        entry->MaxHP = bc->MaxHealth;
        entry->CurrentMP = (ushort)bc->Mana;
        entry->MaxMP = (ushort)bc->MaxMana;
        entry->HomeWorld = bc->HomeWorld;
        entry->TerritoryType = (ushort)Plugin.ClientState.TerritoryType;
        entry->Valid = 1;
        entry->ClassJob = bc->ClassJob;
        entry->Sex = bc->DrawData.CustomizeData.Sex;
        entry->Level = bc->Level;
        for (var i = 0; i < UpdatePartyPacketMember.NameLength - 1; i++)
        {
            var b = obj->Name[i];
            entry->Name[i] = b;
            if (b == 0) break;
        }
    }

    // The packet the server would send for the party as it stands; all zero when solo, which is
    // what the server sends on leaving a party.
    private static UpdatePartyPacket SnapshotPacket(ref GroupManager.Group grp)
    {
        var packet = new UpdatePartyPacket();
        if (grp.MemberCount == 0) return packet;
        var entries = (UpdatePartyPacketMember*)&packet;
        var count = Math.Min((int)grp.MemberCount, UpdatePartyPacket.Capacity);
        for (var i = 0; i < count; i++)
            SnapshotEntry(&entries[i], ref grp.PartyMembers[i]);
        packet.MemberCount = (byte)count;
        packet.LeaderIndex = (byte)grp.PartyLeaderIndex;
        packet.PartyId = grp.PartyId;
        packet.PartyId_2 = grp.PartyId_2;
        return packet;
    }

    private static void SnapshotEntry(UpdatePartyPacketMember* entry, ref GroupPartyMember slot)
    {
        entry->AccountId = slot.AccountId;
        entry->ContentId = slot.ContentId;
        entry->EntityId = slot.EntityId;
        entry->PetEntityId = slot.PetEntityId;
        entry->CompanionEntityId = slot.CompanionEntityId;
        entry->CurrentHP = slot.CurrentHP;
        entry->MaxHP = slot.MaxHP;
        entry->CurrentMP = slot.CurrentMP;
        entry->MaxMP = slot.MaxMP;
        entry->HomeWorld = slot.HomeWorld;
        entry->TerritoryType = slot.TerritoryType;
        entry->Valid = 1;
        entry->ClassJob = slot.ClassJob;
        entry->Sex = slot.Sex;
        entry->Level = slot.Level;
        entry->SyncedLevel = (byte)((slot.Flags & 0x8) != 0 ? slot.Level : 0);
        for (var i = 0; i < UpdatePartyPacketMember.NameLength - 1; i++)
        {
            var b = slot.Name[i];
            entry->Name[i] = b;
            if (b == 0) break;
        }
        var statuses = (UpdatePartyPacketStatus*)((byte*)entry + UpdatePartyPacketMember.StatusesOffset);
        for (var i = 0; i < UpdatePartyPacketMember.StatusCount; i++)
        {
            ref var s = ref slot.StatusManager.Status[i];
            statuses[i].StatusId = s.StatusId;
            statuses[i].Param = s.Param;
            statuses[i].RemainingTime = s.RemainingTime;
            statuses[i].SourceEntityId = s.SourceObject.ObjectId;
        }
    }
}
