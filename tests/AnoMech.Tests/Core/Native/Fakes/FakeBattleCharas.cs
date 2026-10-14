using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;

namespace AnoMech.Tests;

// CharacterManager's slots and the local player, with BattleCharas' spawn recipes reduced to the
// fields the sim reads back.
internal sealed class FakeBattleCharas : IBattleCharas
{
    private const int SlotCount = 100;
    private const uint DoppelEntityIdBase = 0x10FFFE00u;
    private const uint PacketSpawnEntityIdBase = 0x4000FE00u;
    private const uint DoppelMaxHealth = 100_000;
    private const uint EnemyMaxHealth = 1_000_000;
    private const float DoppelHitboxRadius = 0.5f;
    private const uint DemihumanSkeletonIdBase = 10000;
    private const byte IsTargetable = 0x02;

    private readonly FakeCharacter?[] slots = new FakeCharacter?[SlotCount];
    private readonly Dictionary<uint, (uint Caster, uint ActionId)> effects = new();
    private readonly HashSet<int> reserved = [];

    public FakeCharacter Player { get; } = new(0x10000001u, "Player") { MaxHealth = 100_000, Health = 100_000, HitboxRadius = 0.5f, HasDrawObject = true, IsDrawObjectVisible = true };

    public IBattleCharaProxy LocalPlayer { get; }

    public SimTimeline Timeline { get; }

    public FakeBattleCharas(SimTimeline? timeline = null)
    {
        Timeline = timeline ?? new SimTimeline();
        LocalPlayer = new FakeBattleChara(this, -1);
    }

    internal FakeCharacter? Find(uint entityId)
        => Player.EntityId == entityId ? Player : slots.FirstOrDefault(a => a?.EntityId == entityId);

    // An ActionEffect's global sequence, until the first party EffectResult names it (its impact).
    internal void RecordEffect(uint sequence, uint caster, uint actionId) => effects[sequence] = (caster, actionId);

    internal (uint Caster, uint ActionId)? TakeEffect(uint sequence)
        => effects.Remove(sequence, out var effect) ? effect : null;

    internal FakeCharacter? ActorAt(int slot) => slot < 0 ? Player : slots[slot];

    internal void Free(int slot)
    {
        slots[slot] = null;
        reserved.Remove(slot);
    }

    public IBattleCharaProxy? SpawnBattleNpcFromPacket(EnemySpawnConfig config, Placement placement, out uint entityId)
    {
        entityId = 0;
        if (Natives.Data.BNpcBase(config.BNpcBaseId) is not { } bnpc) return null;
        if (Natives.Data.ModelChara(config.ModelCharaId != 0 ? config.ModelCharaId : bnpc.ModelChara) is not { } modelChara) return null;
        var slot = FindFreeSlot();
        if (slot < 0) return null;
        entityId = PacketSpawnEntityIdBase + (uint)slot;
        var name = Natives.Data.BNpcName(config.NameId) ?? $"BNpc {config.BNpcBaseId:X}";
        var actor = new FakeCharacter(entityId, name)
        {
            Position = placement.Position,
            Rotation = placement.Rotation,
            HitboxRadius = NativeHitboxRadius(modelChara, bnpc.Scale),
            MaxHealth = EnemyMaxHealth,
            Health = EnemyMaxHealth,
            HasDrawObject = true,
            IsDrawObjectVisible = config.Visibility == SpawnVisibility.Visible,
        };
        slots[slot] = actor;
        Timeline.Add("spawn", entityId, name, config.BNpcBaseId.ToString(), "BNpcBase", position: placement.Position,
            rotation: placement.Rotation,
            detail: $"bnpcname={config.NameId} visibility={config.Visibility} display_flags=0x{config.DisplayFlags ?? (uint)config.Visibility:X} layout=0x{config.LayoutId:X} model={config.ModelCharaId} sheet_model={bnpc.ModelChara} "
                    + $"level={config.Level} targetable={(config.Targetable ? 1 : 0)} enemy_list={config.EnemyList}"
                    + (config.InitialModeAttributeFlags is { } flags ? $" mode_attr=0x{flags:X}" : ""));
        return new FakeBattleChara(this, slot);
    }

    public IBattleCharaProxy? SpawnDoppel(PartyMemberPreset preset, Placement placement, out uint entityId)
    {
        entityId = 0;
        var slot = FindFreeSlot();
        if (slot < 0) return null;
        entityId = DoppelEntityIdBase + (uint)slot;
        Timeline.Add("spawn_doppel", entityId, preset.Name, preset.ClassJob.ToString(), "ClassJob", position: placement.Position,
            rotation: placement.Rotation);
        slots[slot] = new FakeCharacter(entityId, preset.Name)
        {
            ClassJob = preset.ClassJob,
            Position = placement.Position,
            Rotation = placement.Rotation,
            HitboxRadius = DoppelHitboxRadius,
            MaxHealth = DoppelMaxHealth,
            Health = DoppelMaxHealth,
            TargetableStatus = IsTargetable,
        };
        return new FakeBattleChara(this, slot);
    }

    public bool IsInCharacterManager(uint entityId)
        => Player.EntityId == entityId || slots.Any(a => a?.EntityId == entityId);

    public void ReleaseSlot(int slot) => reserved.Remove(slot);

    internal void Tick(float deltaSeconds)
    {
        Player.Tick(deltaSeconds);
        foreach (var actor in slots) actor?.Tick(deltaSeconds);
    }

    // UNVERIFIED: assumes the engine's packet handler sizes the actor the way
    // ModelContainer.UpdateHitboxRadius does.
    // ModelContainer.UpdateHitboxRadius: Scale × CalculateUnscaledRadius, which prefers the
    // ModelChara row's radius over its skeleton's. Type 2 and the skeleton-less types are UNVERIFIED.
    private static float NativeHitboxRadius(ModelCharaRow modelChara, float scale)
    {
        if (modelChara.Radius > 0f) return modelChara.Radius * scale;
        var skeletonId = modelChara.Type switch
        {
            2 => modelChara.Model + DemihumanSkeletonIdBase,
            3 => modelChara.Model,
            _ => 0u,
        };
        return (Natives.Data.ModelSkeleton(skeletonId)?.Radius ?? 0f) * scale;
    }

    private int FindFreeSlot()
    {
        for (var i = 0; i < slots.Length; i++)
            if (slots[i] == null && !reserved.Contains(i)) return i;
        return -1;
    }
}
