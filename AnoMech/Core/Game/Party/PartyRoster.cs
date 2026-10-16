using System;
using System.Collections.Generic;
using System.Linq;

namespace AnoMech.Core.Game.Party;

// Who sits in one party seat. SimParty respawns a seat only when its slot changes between runs, so
// equal slots must compare equal (records).
public abstract record PartySlot;

public sealed record PlayerSlot : PartySlot;

public sealed record BotSlot(PartyMemberPreset Preset) : PartySlot;

// A seat a real player holds over the network.
public sealed record PuppetSlot(PartyMemberPreset Preset) : PartySlot;

// The party one run plays with, asked when the run starts.
public interface IPartyProvider
{
    // Indexed by PartyRole; null leaves the seat empty.
    IReadOnlyList<PartySlot?> Roster(uint playerJob);
}

// Single player: bots in every other seat, or nobody for Solo.
public sealed record LocalPartyProvider : IPartyProvider
{
    public PartyRole? RoleOverride { get; init; }
    public bool Solo { get; init; }

    public IReadOnlyList<PartySlot?> Roster(uint playerJob)
    {
        var playerRole = RoleOverride ?? PartyPresets.SkipRoleForJob(playerJob);
        return Enum.GetValues<PartyRole>()
            .Select(role => role == playerRole ? new PlayerSlot()
                : Solo ? null
                : (PartySlot?)new BotSlot(PartyPresets.Standard[(int)role]))
            .ToList();
    }
}
