using AnoMech.Core.SimObjects;
using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace AnoMech.Core.Native.Implementations.Interop;

// For native code that works on a sim character's BattleChara directly (HUD mirrors, debug
// windows).
internal static unsafe class SimCharacterPointers
{
    public static BattleChara* BattleCharaPtr(this SimCharacter character)
        => character.Proxy is { } proxy ? BattleCharaProxy.PtrAt(proxy.Slot) : null;
}
