using System.Collections.Generic;

namespace AnoMech.Core.Native.Interfaces;

// Gear slots in NpcSpawn order: head, body, hands, legs, feet, ears, neck, wrists, right ring, left ring.
public sealed record NpcEquipRow(uint Id, ulong ModelMainHand, uint DyeMainHand, ulong ModelOffHand, uint DyeOffHand,
    IReadOnlyList<uint> Models, IReadOnlyList<uint> Dyes);
