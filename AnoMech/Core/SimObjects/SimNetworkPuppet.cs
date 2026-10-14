using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Motion;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Core.SimObjects;

// A party slot occupied by a real remote player. Same doppel spawn path as SimPartyNpc, but its
// position is the owner's reported pose and orders don't move it (RemoteMotion), so code that
// addresses every slot uniformly skips it the way it skips SimPlayer.
//
// Not a SimPartyNpc subclass: that class is sealed and the two share only a dozen lines.
public sealed class SimNetworkPuppet : SimNpc, ISimPartyMember
{
    private readonly RemoteMotion motion;

    public PartyRole Role { get; set; }
    public byte ClassJob { get; }
    public string DisplayName { get; }

    internal SimNetworkPuppet(IBattleCharaProxy proxy, Coordinates coordinates, PartyRole role, byte classJob, string name)
        : base(proxy, coordinates, self => new RemoteMotion(() => self.Proxy, coordinates))
    {
        motion = (RemoteMotion)Motion;
        Role = role;
        ClassJob = classJob;
        DisplayName = name;
    }

    public void ApplyNetworkPose(Vector3 position, float rotation) => motion.ApplyPose(position, rotation);

    // A forced move here only moves the host's copy through the owner's poses; MultiplayerManager
    // drains these to tell the owning peer to apply them to their real character.
    internal IReadOnlyList<ForcedMove> DrainForcedMoves() => motion.DrainForced();

    protected override void OnPacketSpawned(IBattleCharaProxy chara) => ActorControl.ZoneIn();

    public void OnKilled(bool byEnvironment)
    {
        GoDown(byEnvironment);
    }
}
