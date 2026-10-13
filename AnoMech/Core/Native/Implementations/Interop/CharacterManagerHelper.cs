using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace AnoMech.Core.Native.Implementations.Interop;

public static unsafe class CharacterManagerHelper
{
    // Slots a packet spawn is in flight for (SimNpc.BeginPacketSpawn): the engine fills the
    // packet's SpawnIndex a few frames after HandleSpawnNpcPacket returns and replaces whatever
    // sits there by then (build 29497D: a fallback actor created in the slot meanwhile was gone
    // by +5 frames), so nothing else may be created in such a slot until it resolves.
    private static readonly HashSet<int> reserved = new();
    public static void Reserve(int index) => reserved.Add(index);
    public static void Release(int index) => reserved.Remove(index);

    // First free, unreserved BattleChara slot, or -1.
    public static int FindFreeIndex()
    {
        var characterManager = CharacterManager.Instance();
        if (characterManager == null) return -1;
        for (int i = 0; i < characterManager->BattleCharas.Length; i++)
            if (characterManager->BattleCharas[i] == null && !reserved.Contains(i)) return i;
        return -1;
    }
}
