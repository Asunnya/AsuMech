using System;
using System.Linq;
using AnoMech.Core.Native.Implementations.Pointers;
using AnoMech.Core.Native.Interfaces;
using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.Game;
using Condition = Lumina.Excel.Sheets.Condition;

namespace AnoMech.Core.Native.Implementations;

internal sealed unsafe class PlayerConditions : IPlayerConditions
{
    // Retail has five networked "suffering status affliction" conditions (28, 29, 63, 72, 73).
    // Confused raises 63; 73 locks both movement and actions.
    private const ConditionFlag StatusAfflictionFlag = ConditionFlag.SufferingStatusAffliction73;
    private const ConditionFlag ControlAfflictionFlag = ConditionFlag.SufferingStatusAffliction63;

    private readonly ConditionFlag[] networked = Plugin.DataManager.GetExcelSheet<Condition>()
        .Where(row => row.IsNetworked)
        .Select(row => (ConditionFlag)row.RowId)
        .OrderBy(flag => flag)
        .ToArray();

    public bool StatusAffliction => Plugin.Condition[StatusAfflictionFlag];
    public bool ControlAffliction => Plugin.Condition[ControlAfflictionFlag];

    public void SetAfflictions(bool status, bool control) => Send(flag => flag switch
    {
        StatusAfflictionFlag => status,
        ControlAfflictionFlag => control,
        _ => Plugin.Condition[flag],
    });

    public void EndClientLoad() => Send(flag => flag switch
    {
        ConditionFlag.BetweenAreas or ConditionFlag.OccupiedInQuestEvent => false,
        _ => Plugin.Condition[flag],
    });

    // The packet carries every networked condition, so the ones not being changed are copied.
    private void Send(Func<ConditionFlag, bool> isOn)
    {
        var bits = stackalloc byte[PacketDispatcherPointers.PlayerStateFlagsLength];
        for (var i = 0; i < PacketDispatcherPointers.PlayerStateFlagsLength; i++) bits[i] = 0;
        for (var bit = 0; bit < networked.Length && bit < PacketDispatcherPointers.PlayerStateFlagsLength * 8; bit++)
            if (isOn(networked[bit])) bits[bit >> 3] |= (byte)(1 << (bit & 7));
        PacketDispatcherPointers.HandlePlayerStateFlagsPacket(Conditions.Instance(), bits);
    }
}
