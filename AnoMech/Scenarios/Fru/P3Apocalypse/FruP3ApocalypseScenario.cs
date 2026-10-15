using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Fru.FruConstants;
using static AnoMech.Scenarios.Fru.P3Apocalypse.FruP3ApocalypseState;
using Actions = AnoMech.Scenarios.Fru.FruActions;

namespace AnoMech.Scenarios.Fru.P3Apocalypse;

public sealed class FruP3ApocalypseScenario : IScenario
{
    public string Name => "Apocalypse";
    public IPhase Phase => FruZone.P3;

    public IReadOnlyList<IScenarioAi> AiStrats => [new FruP3ApocalypseAi()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly FruP3ApocalypseSettingsWindow settingsWindow = new();

    // Darkest Dance lands this short of its target.
    private const float DanceLandingShort = 6.3f;

    private SimWorld world = null!;
    private FruP3ApocalypseState state = null!;
    private SimEnemy? oracle;
    private readonly SimEnemy?[] waterHelpers = new SimEnemy?[2];
    private readonly SimEnemy?[] eruptionHelpers = new SimEnemy?[8];
    private readonly SimEnemy?[] apocalypseHelpers = new SimEnemy?[6];
    private Vector3 danceLanding;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new FruP3ApocalypseState(world.Rng, settingsWindow.Overrides);
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<FruP3ApocalypseState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, SpawnActors);
        world.Events.Add(1f, () => oracle?.SetTarget(world.Party.Get(PartyRole.MainTank), follow: false));
        world.Events.Add(2.1f, () => oracle?.Cast(Actions.SpellInWaitingRefrain));
        world.Events.Add(7.2f, () => oracle?.Cast(Actions.DarkWaterCast));
        world.Events.Add(7.2f, () => MarkWaterHolders(LockonId.Stack, 1, 2, 3));
        world.Events.Add(12.8f, ApplyDarkWater);
        world.Events.Add(15.3f, () => oracle?.Cast(Actions.ApocalypseCast));
        world.Events.Add(WaterAt(1) - 5.1f, () => MarkWaterHolders(LockonId.WaterCountdown, 1));
        world.Events.Add(21.5f, () => oracle?.Cast(Actions.SpiritTakerCast));
        world.Events.Add(WaterAt(1), () => DarkWater(1));
        world.Events.Add(25.5f, () => oracle?.Cast(Actions.SpiritTaker, world.Party.Get(state.SpiritTakerTarget)));
        world.Events.Add(30.9f, () => oracle?.Cast(Actions.DarkEruptionCast));
        world.Events.Add(30.9f, () => world.Party.ActiveMembers().ToList().ForEach(m => m.AttachLockonVfx(LockonId.Spread)));
        // TODO: the lights that telegraph each wave are scenery AVFX (b2164kido1_o) played through
        // trigger stages (straight, either arc, ring pulse); there's no way to queue an AVFX trigger.
        for (var wave = 0; wave < WaveCount; wave++)
        {
            var w = wave;
            world.Events.Add(WaveAt(w), () => Explode(w));
        }
        world.Events.Add(35.7f, DarkEruption);
        world.Events.Add(WaterAt(2) - 5.1f, () => MarkWaterHolders(LockonId.WaterCountdown, 2));
        world.Events.Add(38.5f, () => oracle?.Cast(Actions.DarkestDanceCast));
        world.Events.Add(WaterAt(2), () => DarkWater(2));
        world.Events.Add(43.2f, DarkestDance);
        world.Events.Add(44.2f, () => oracle?.SetPosition(danceLanding));
        world.Events.Add(WaterAt(3) - 5.1f, () => MarkWaterHolders(LockonId.WaterCountdown, 3));
        world.Events.Add(47.6f, () => oracle?.Cast(Actions.DarkestDanceKnockback));
        world.Events.Add(WaterAt(3), () => DarkWater(3));
        world.Events.Add(51.2f, () => oracle?.Cast(Actions.ShockwavePulsar));
    }

    private void SpawnActors()
    {
        oracle = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.OracleP3,
            NameId: BNpcNameId.Oracle,
            Level: Level,
            Targetable: true,
            EnemyList: EnemyListMode.Always,
            Placement: new Placement(Vector3.Zero, MathF.PI)));
        for (var i = 0; i < waterHelpers.Length; i++) waterHelpers[i] = SpawnHelper();
        for (var i = 0; i < eruptionHelpers.Length; i++) eruptionHelpers[i] = SpawnHelper();
        for (var i = 0; i < apocalypseHelpers.Length; i++) apocalypseHelpers[i] = SpawnHelper();
    }

    private SimEnemy? SpawnHelper() => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: BNpcBaseId.Helper,
        NameId: BNpcNameId.Oracle,
        Level: Level,
        EnemyList: EnemyListMode.Never,
        Visibility: SpawnVisibility.InvisibleHelper,
        Placement: new Placement(Vector3.Zero, 0f)));

    private void MarkWaterHolders(uint lockonId, params int[] durations)
    {
        foreach (var duration in durations)
            foreach (var role in state.WaterHolders(duration))
                if (world.Party.Get(role) is { } member && member.IsAlive()) member.AttachLockonVfx(lockonId);
    }

    private void ApplyDarkWater()
    {
        foreach (var role in Enum.GetValues<PartyRole>())
            if (state.WaterDuration(role) is > 0 and var duration && world.Party.Get(role) is { } member && member.IsAlive())
                member.AddStatus(StatusId.DarkWaterWaiting, WaterAt(duration) - 12.8f);
    }

    private void DarkWater(int duration)
    {
        var holders = state.WaterHolders(duration).Select(world.Party.Get).ToList();
        for (var i = 0; i < holders.Count; i++)
            if (holders[i] is { } holder && holder.IsAlive())
            {
                waterHelpers[i]?.SetPosition(holder.Position);
                waterHelpers[i]?.Cast(Actions.DarkWater, holder);
            }
    }

    private void Explode(int wave)
    {
        var i = 0;
        foreach (var position in state.Explosions(wave))
        {
            apocalypseHelpers[i]?.SetPosition(position);
            apocalypseHelpers[i]?.Cast(Actions.Apocalypse);
            i++;
        }
    }

    private void DarkEruption()
    {
        var members = world.Party.ActiveMembers().ToList();
        for (var i = 0; i < members.Count; i++)
        {
            eruptionHelpers[i]?.SetPosition(members[i].Position);
            eruptionHelpers[i]?.Cast(Actions.DarkEruption, members[i]);
        }
    }

    private void DarkestDance()
    {
        if (oracle is not { } boss) return;
        var target = world.Party.ActiveMembers().MaxBy(m => Vector3.DistanceSquared(m.Position, boss.Position));
        if (target is null) return;
        boss.Cast(Actions.DarkestDance, target);
        var offset = target.Position - boss.Position;
        danceLanding = offset.Length() > DanceLandingShort
            ? boss.Position + Vector3.Normalize(offset) * (offset.Length() - DanceLandingShort)
            : boss.Position;
    }
}
