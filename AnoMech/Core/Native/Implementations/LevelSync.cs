using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.Native.Implementations.Pointers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;

namespace AnoMech.Core.Native.Implementations;

// The duty's level and item-level sync on the local player, undone on the way back to the inn.
internal sealed unsafe class LevelSync
{
    private bool levelSynced;
    private int level;
    private int exp;
    private uint restedExp;
    private bool statsSynced;
    private PlayerStatsPacket savedStats;

    // Before the zone load, while the player still has the inn's values.
    public void Capture()
    {
        level = Plugin.ObjectTable.LocalPlayer!.Level;
        exp = Plugin.PlayerState.GetClassJobExperience(Plugin.PlayerState.ClassJob.Value);
        restedExp = Plugin.PlayerState.BaseRestedExperience;
        levelSynced = false;
        statsSynced = false;
    }

    public void Apply(ContentFinderCondition cfc, Character* player, byte classJobId)
    {
        var cfcLevelSync = cfc.ClassJobLevelSync;
        var currentLevel = (byte)Plugin.PlayerState.Level;
        levelSynced = cfcLevelSync != 0 && currentLevel > cfcLevelSync;

        if (levelSynced)
        {
            var updateClassInfoPacket = new UpdateClassInfoPacket
            {
                ClassJobId = classJobId,
                CurrentLevel = currentLevel,
                ClassJobLevel = currentLevel,
                SyncedLevel = cfcLevelSync,
                ClassJobExp = 0,
                BaseRestedExperience = 0
            };

            PacketDispatcherPointers.HandleUpdateClassInfoPacket(0, &updateClassInfoPacket);
            SendCharacterLevel(player, cfcLevelSync);
        }

        var ilvlSync = cfc.ItemLevelSync != 0 ? cfc.ItemLevelSync : cfc.ItemLevelRequired;

        if (ilvlSync != 0)
        {
            Plugin.Log.Debug($"Setting up iLvl sync to {ilvlSync}");
            savedStats = ReadPlayerStats();
            statsSynced = true;

            var stats = savedStats;
            foreach (var (baseParamId, value) in GetSyncedAttributes(cfcLevelSync, ilvlSync))
            {
                if (PacketIndex((uint)baseParamId) is { } index)
                {
                    stats.Attributes[index] = value;
                }
            }

            PacketDispatcherPointers.HandlePlayerStatsPacket(0, &stats);
        }
    }

    public void Reset(Character* player, byte classJobId)
    {
        if (levelSynced)
        {
            var updateClassInfoPacket = new UpdateClassInfoPacket
            {
                ClassJobId = classJobId,
                CurrentLevel = (ushort)level,
                ClassJobLevel = (ushort)level,
                SyncedLevel = 0,
                ClassJobExp = (ushort)exp,
                BaseRestedExperience = restedExp
            };

            PacketDispatcherPointers.HandleUpdateClassInfoPacket(0, &updateClassInfoPacket);
            SendCharacterLevel(player, (byte)level);
        }

        if (statsSynced)
        {
            var stats = savedStats;
            PacketDispatcherPointers.HandlePlayerStatsPacket(0, &stats);
            statsSynced = false;
        }
    }

    // UpdateClassInfo only reaches PlayerState; the character's own level comes with its status
    // list, so the player's current statuses go along unchanged.
    private static void SendCharacterLevel(Character* player, byte characterLevel)
    {
        if (player == null) return;

        var packet = new StatusEffectListDoublePacket
        {
            ClassJob = player->ClassJob,
            Level = characterLevel,
            Level_2 = characterLevel,
            CurrentHP = player->Health,
            MaxHP = player->MaxHealth,
            CurrentMP = (ushort)player->Mana,
            MaxMP = (ushort)player->MaxMana,
            DamageShield = player->ShieldValue,
        };
        var first = (StatusEffectListStatus*)((byte*)&packet + StatusEffectListDoublePacket.StatusesOffset);
        var second = (StatusEffectListStatus*)((byte*)&packet + StatusEffectListDoublePacket.ExtraStatusesOffset);
        var statuses = ((BattleChara*)player)->StatusManager.Status;
        for (var i = 0; i < statuses.Length && i < 2 * StatusEffectListDoublePacket.Capacity; i++)
        {
            var entry = i < StatusEffectListDoublePacket.Capacity ? &first[i] : &second[i - StatusEffectListDoublePacket.Capacity];
            entry->StatusId = statuses[i].StatusId;
            entry->Param = statuses[i].Param;
            entry->RemainingTime = statuses[i].RemainingTime;
            entry->SourceEntityId = statuses[i].SourceObject.ObjectId;
        }

        PacketDispatcherPointers.HandleStatusEffectListDoublePacket(player->EntityId, &packet, 0);
    }

    private static PlayerStatsPacket ReadPlayerStats()
    {
        var packet = new PlayerStatsPacket();
        var playerState = &UIState.Instance()->PlayerState;

        foreach (var row in Plugin.DataManager.GetExcelSheet<BaseParam>())
        {
            if (row.RowId < playerState->Attributes.Length && PacketIndex(row.RowId) is { } index)
            {
                packet.Attributes[index] = playerState->Attributes[(int)row.RowId];
            }
        }

        var baseStats = &playerState->BaseStrength;
        for (var i = 0; i < PlayerStatsPacket.BaseStatsCapacity; i++)
        {
            packet.BaseStats[i] = baseStats[i];
        }

        return packet;
    }

    private static int? PacketIndex(uint baseParamId)
    {
        if (!Plugin.DataManager.GetExcelSheet<BaseParam>().TryGetRow(baseParamId, out var row)) return null;
        int index = row.PacketIndex;
        return index is >= 0 and < PlayerStatsPacket.AttributesCapacity ? index : null;
    }

    private static Dictionary<int, int> GetSyncedAttributes(byte level, ushort ilvl)
    {
        var stats = new Dictionary<int, int>();

        var itemSheet = Plugin.DataManager.GetExcelSheet<Item>();
        var baseParamSheet = Plugin.DataManager.GetExcelSheet<BaseParam>();
        var paramGrowSheet = Plugin.DataManager.GetExcelSheet<ParamGrow>();

        var supportedAttributes = new PlayerAttribute[]
        {
                PlayerAttribute.SkillSpeed,
                PlayerAttribute.SpellSpeed
        };

        foreach (var attribute in supportedAttributes)
        {
            switch (attribute)
            {
                case PlayerAttribute.SkillSpeed:
                case PlayerAttribute.SpellSpeed:
                    stats[(int)attribute] = paramGrowSheet[level].BaseSpeed;
                    break;
                default:
                    continue;
            }
        }

        // Get Item Stats
        var inventoryManager = InventoryManager.Instance();
        var equippedItems = inventoryManager->GetInventoryContainer(InventoryType.EquippedItems);

        for (int i = 0; i < equippedItems->GetSize(); i++)
        {
            var inventoryItem = equippedItems->Items[i];
            var itemId = inventoryItem.ItemId;

            if (itemId == 0)
            {
                continue;
            }

            var item = itemSheet[itemId];
            var itemLevel = item.LevelItem.RowId;

            if (item.LevelItem.RowId <= ilvl) // Add it as is if the ilvl is the same or less than sync
            {
                Plugin.Log.Debug($"Item \"{item.Name}\" (ilvl {item.LevelItem.RowId}) fits the ilvl sync requirements.");

                for (int x = 0; x < item.BaseParam.Count; x++)
                {
                    var baseParam = item.BaseParam[x];
                    var baseParamId = (int)baseParam.RowId;

                    if (!supportedAttributes.Contains((PlayerAttribute)baseParamId))
                    {
                        continue;
                    }

                    stats[baseParamId] += item.BaseParamValue[x];
                }

                // Add extra stats
                ApplyExtraStats(stats, GetMateriaStats(inventoryItem));
                ApplyExtraStats(stats, GetMandervilleWeaponStats(inventoryItem));
            }
            else // Calculate the sync value
            {
                var resistanceStats = new Dictionary<int, int>();

                var resistanceSheet = Plugin.DataManager.GetExcelSheet<ResistanceWeaponAdjust>();
                if (resistanceSheet.TryGetRow(itemId, out var _))
                {
                    resistanceStats = GetMateriaStats(inventoryItem);
                }

                var mandervilleStats = GetMandervilleWeaponStats(inventoryItem);

                // Doing this instead of calling ExdModule.GetItemRowById because it's not guaranteed to always return on this frame
                var dummyItem = new byte[0xA0];
                fixed (byte* dummyItemPtr = dummyItem)
                {
                    *(dummyItemPtr + 0x54) = item.BaseParamModifier;
                    *(ushort*)(dummyItemPtr + 0x8A) = ilvl;
                    *(dummyItemPtr + 0x9A) = (byte)item.EquipSlotCategory.RowId;

                    for (int x = 0; x < item.BaseParam.Count; x++)
                    {
                        var baseParam = item.BaseParam[x];
                        var baseParamId = (int)baseParam.RowId;

                        if (!supportedAttributes.Contains((PlayerAttribute)baseParamId))
                        {
                            continue;
                        }

                        var statValue = item.BaseParamValue[x];

                        if (resistanceStats.TryGetValue(baseParamId, out var resistanceStat))
                        {
                            statValue += (short)resistanceStat;
                        }

                        if (mandervilleStats.TryGetValue(baseParamId, out var mandervilleStat))
                        {
                            statValue += (short)mandervilleStat;
                        }

                        var statMaxValue = (int)InventoryItem.GetParameterMaxValue((uint)baseParamId, dummyItemPtr);
                        var syncedValue = int.Min(statValue, statMaxValue);
                        stats[baseParamId] += syncedValue;

                        Plugin.Log.Debug($"Item \"{item.Name}\" (ilvl {item.LevelItem.RowId})'s {(PlayerAttribute)baseParamId} was synced to {syncedValue}");
                    }
                }
            }
        }

        // Get Food Buffs
        var localPlayer = (BattleChara*)Plugin.ObjectTable.LocalPlayer!.Address;

        if (localPlayer != null)
        {
            var statusManager = localPlayer->StatusManager;

            foreach (var status in statusManager.Status)
            {
                var statusId = status.StatusId;

                if (statusId == 0 || statusId != 48)
                {
                    continue;
                }

                var hq = status.Param > 10000;
                var foodId = hq ? status.Param - 10000 : status.Param;

                if (!Plugin.DataManager.GetExcelSheet<ItemFood>().TryGetRow((uint)foodId, out var food))
                {
                    continue;
                }

                foreach (var bonus in food.Params)
                {
                    var stat = (int)bonus.BaseParam.RowId;

                    if (stat == 0 || !stats.ContainsKey(stat))
                    {
                        continue;
                    }

                    var value = hq ? bonus.ValueHQ : bonus.Value;
                    var max = hq ? bonus.MaxHQ : bonus.Max;
                    int statBuff;

                    if (bonus.IsRelative)
                    {
                        var currentStat = stats[stat];
                        var percentBuff = (int)(currentStat * (value / 100f));
                        statBuff = int.Min(percentBuff, max);
                    }
                    else
                    {
                        statBuff = value;
                    }

                    stats[stat] += statBuff;
                }
            }
        }

        return stats;
    }

    // This also works for Resistance Weapons
    private static Dictionary<int, int> GetMateriaStats(InventoryItem inventoryItem)
    {
        var stats = new Dictionary<int, int>();
        var materiaSheet = Plugin.DataManager.GetExcelSheet<Materia>();

        for (int i = 0; i < inventoryItem.Materia.Length; i++)
        {
            var materiaId = inventoryItem.Materia[i];

            if (materiaId == 0 || !materiaSheet.TryGetRow(materiaId, out var materiaRow))
            {
                continue;
            }

            var index = inventoryItem.MateriaGrades[i];
            var statValue = materiaRow.Value[index];
            var stat = materiaRow.BaseParam.RowId;
            stats[(int)stat] = statValue;
        }

        return stats;
    }

    // https://github.com/VioletStardustXIV/XivGearExport/blob/main/XivGearExport/XivGearSet.cs#L447
    private static Dictionary<int, int> GetMandervilleWeaponStats(InventoryItem inventoryItem)
    {
        var stats = new Dictionary<int, int>();

        var statsSheet = Plugin.DataManager.GetExcelSheet<MandervilleWeaponEnhance>();
        var materiaSheet = Plugin.DataManager.GetExcelSheet<Materia>();

        if (!statsSheet.TryGetRow(inventoryItem.ItemId, out var _))
        {
            return stats;
        }

        for (int i = 0; i < inventoryItem.Materia.Length; i++)
        {
            var materiaId = inventoryItem.Materia[i];

            if (materiaId == 0 || !materiaSheet.TryGetRow(materiaId, out var materiaRow))
            {
                continue;
            }

            var index = inventoryItem.MateriaGrades[i];
            var statValue = materiaRow.Value[index];

            switch (materiaId)
            {
                case 1403:
                    stats[(int)PlayerAttribute.CriticalHit] = statValue;
                    break;
                case 1404:
                    stats[(int)PlayerAttribute.DirectHitRate] = statValue;
                    break;
                case 1405:
                    stats[(int)PlayerAttribute.Determination] = statValue;
                    break;
                case 1406:
                    stats[(int)PlayerAttribute.SkillSpeed] = statValue;
                    break;
                case 1407:
                    stats[(int)PlayerAttribute.SpellSpeed] = statValue;
                    break;
                case 1408:
                    stats[(int)PlayerAttribute.Tenacity] = statValue;
                    break;
                case 1409:
                    stats[(int)PlayerAttribute.Piety] = statValue;
                    break;
            }
        }

        return stats;
    }

    private static void ApplyExtraStats(Dictionary<int, int> stats, Dictionary<int, int> extraStats)
    {
        foreach (var (attribute, value) in extraStats)
        {
            if (!stats.ContainsKey(attribute))
            {
                continue;
            }

            stats[attribute] += value;
        }
    }
}
