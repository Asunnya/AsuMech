using System;
using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.Game.Party;

namespace AnoMech.Multiplayer;

// The lobby as it stands at each start. Every seat another player claimed is a puppet wearing
// their name and job. The free seats are the host's bots, which a peer only mirrors, so on a peer
// they are puppets too.
internal sealed class LobbyPartyProvider(MultiplayerManager lobby, bool isPeer) : IPartyProvider
{
    public IReadOnlyList<PartySlot?> Roster(uint playerJob)
    {
        var seats = lobby.ClaimedRoleSeats();
        return Enum.GetValues<PartyRole>().Select(role => Slot(role, seats)).ToList();
    }

    private PartySlot Slot(PartyRole role, IReadOnlyDictionary<PartyRole, NetworkSeat> seats)
    {
        if (role == lobby.MyClaimedRole) return new PlayerSlot();
        var preset = PartyPresets.Standard[(int)role];
        if (seats.GetValueOrDefault(role) is { } seat) return new PuppetSlot(seat.Dress(preset));
        return isPeer ? new PuppetSlot(preset) : new BotSlot(preset);
    }
}
