using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Fru.FruConstants;
using static AnoMech.Scenarios.Fru.P4DarklitDragonsong.FruP4DarklitDragonsongState;
using Actions = AnoMech.Scenarios.Fru.FruActions;

namespace AnoMech.Scenarios.Fru.P4DarklitDragonsong;

public sealed class FruP4DarklitDragonsongScenario : IScenario
{
    public string Name => "Darklit Dragonsong";
    public IPhase Phase => FruZone.P4;

    public IReadOnlyList<IScenarioAi> AiStrats => [new FruP4DarklitDragonsongAi(playerBaitsSomberDance: false), new FruP4DarklitDragonsongAi(playerBaitsSomberDance: true)];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly FruP4DarklitDragonsongSettingsWindow settingsWindow = new();

    private static readonly Vector3 FragmentPosition = new(0, 0, -16);
    private const float S = 1f / 2.358f;
    private const float ChainMin = 22f * S;
    private const float ChainMax = 61f * S;
    // The Oracle's jumps land this short of their target.
    private const float JumpLandingShort = 15f * S;

    private SimWorld world = null!;
    private FruP4DarklitDragonsongState state = null!;
    private SimEnemy? usurper, oracle;
    private readonly SimEnemy?[] akhRhaiHelpers = new SimEnemy?[8];
    private readonly SimEnemy?[] helpers = new SimEnemy?[8];
    private int akhRhaiPuddles;
    private readonly List<SimTether> chains = [];
    private bool chainsLocked;
    private bool wiped;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new FruP4DarklitDragonsongState(world.Rng, settingsWindow.Overrides);
        chains.Clear();
        chainsLocked = false;
        wiped = false;
        akhRhaiPuddles = 0;
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<FruP4DarklitDragonsongState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, SpawnActors);
        world.Events.Add(1f, () => usurper?.SetTarget(world.Party.Get(PartyRole.MainTank), follow: false));
        world.Events.Add(1f, () => oracle?.SetTarget(world.Party.Get(PartyRole.OffTank), follow: false));
        world.Events.Add(1.1f, () => world.Map.AddEffect(MapEffect.Show, MapEffect.FragmentOfFate));
        world.Events.Add(6.6f, AkhRhai);
        // TODO: retail sends the dragon form's ModeAttributeFlags (0x31) and ModelState (0x3F) on
        // the same tick as the transformation; their values haven't been read off a replay.
        world.Events.Add(8.8f, () => usurper?.ActorControl.Transform(UsurperDragonBNpcState));
        for (var hit = 1; hit <= 10; hit++)
            world.Events.Add(9.3f + hit * 0.4f, RepeatAkhRhai);
        world.Events.Add(14.2f, () => oracle?.PlayActionTimeline(ActionTimelineId.WarpEnd));
        world.Events.Add(14.2f, () => oracle?.SetTargetable(true));
        world.Events.Add(16.4f, () => usurper?.Cast(Actions.DarklitDragonsong));
        world.Events.Add(16.4f, () => oracle?.Cast(Actions.DarklitDragonsongOracle));
        world.Events.Add(16.4f, () => MarkWaters(LockonId.Stack));
        world.Events.Add(21.6f, () => Waters().ForEach(m => m.AddStatus(StatusId.DarkWaterWaiting, DarkWaterAt - 21.6f)));
        world.Events.Add(ChainsAt, () => world.Party.ActiveMembers().ToList().ForEach(m => m.AddStatus(StatusId.Lightsteeped, 34.3f - ChainsAt, 3, overrideStacks: true)));
        world.Events.Add(ChainsAt, () => Chain(StatusId.ChainsOfEverlastingLight, TetherId.ChainsOfEverlastingLight, CursesAt - ChainsAt));
        world.Events.Add(24.6f, () => usurper?.Cast(Actions.PathOfLightCast));
        world.Events.Add(24.6f, () => world.Map.AddEffect(MapEffect.Show, MapEffect.DarklitFirstTower));
        world.Events.Add(24.6f, () => world.Map.AddEffect(MapEffect.Show, MapEffect.DarklitFirstTower + 1));
        world.Events.Add(CursesAt, () => Chain(StatusId.CurseOfEverlastingLight, TetherId.CurseOfEverlastingLight, CursesEndAt - CursesAt));
        world.Events.Add(CursesAt, () => chainsLocked = true);
        world.Events.Add(TowersAt, BrightHungerAndPathOfLight);
        world.Events.Add(32.6f, () => oracle?.Cast(Actions.SpiritTakerCast));
        world.Events.Add(35.5f, () => MarkWaters(LockonId.WaterCountdown));
        world.Events.Add(35.7f, () => usurper?.Cast(state.EastWing ? Actions.HallowedWingsRight : Actions.HallowedWingsLeft));
        world.Events.Add(36.1f, SpiritTaker);
        world.Events.Add(38.8f, () => oracle?.Cast(Actions.SomberDanceCast));
        world.Events.Add(DarkWaterAt, DarkWater);
        world.Events.Add(CursesEndAt, ClearChains);
        world.Events.Add(44.8f, () => SomberDance(Actions.SomberDanceFar, farthest: true));
        world.Events.Add(47.6f, () => SomberDance(Actions.SomberDanceNear, farthest: false));
        world.Events.Add(51.9f, () => usurper?.PlayActionTimeline(ActionTimelineId.WarpStart));
        world.Events.Add(51.9f, () => oracle?.PlayActionTimeline(ActionTimelineId.WarpStart));
        world.Events.Add(52.2f, () => usurper?.SetPosition(new Placement(new Vector3(-6f * S, 0, 0), MathF.PI)));
        world.Events.Add(52.2f, () => oracle?.SetPosition(new Placement(new Vector3(6f * S, 0, 0), MathF.PI)));
        world.Events.Add(52.2f, () => usurper?.PlayActionTimeline(ActionTimelineId.WarpEnd));
        world.Events.Add(52.2f, () => oracle?.PlayActionTimeline(ActionTimelineId.WarpEnd));
        world.Events.Add(53.2f, () => usurper?.Cast(Actions.AkhMornUsurper, world.Party.Get(PartyRole.MainTank)));
        world.Events.Add(53.2f, () => oracle?.Cast(Actions.AkhMornOracle, world.Party.Get(PartyRole.OffTank)));
        for (var hit = 0; hit < 4; hit++)
            world.Events.Add(58.6f + hit * 0.6f, AkhMorn);
    }

    // TODO: the Visions of Ryne and Gaia float inside the crystal (a 2y draw offset) and the
    // Fragment's HP bar empties when it's hit; neither can be set on a spawned enemy.
    private void SpawnActors()
    {
        usurper = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.UsurperP4,
            NameId: BNpcNameId.Usurper,
            Level: Level,
            Targetable: true,
            EnemyList: EnemyListMode.Always,
            Placement: new Placement(Vector3.Zero, MathF.PI),
            ModelCharaId: ModelCharaId.UsurperLight));
        oracle = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.OracleP4,
            NameId: BNpcNameId.Oracle,
            Level: Level,
            EnemyList: EnemyListMode.OnlyWhenVisible,
            Visibility: SpawnVisibility.HiddenUntilShown,
            Placement: new Placement(new Vector3(0, 0, 10.5f * S), MathF.PI)));
        world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.FragmentOfFate,
            NameId: BNpcNameId.FragmentOfFate,
            Level: Level,
            Targetable: true,
            EnemyList: EnemyListMode.Never,
            Placement: new Placement(FragmentPosition, MathF.PI),
            IsHostile: false));
        SpawnVision(BNpcBaseId.VisionOfRyne, BNpcNameId.Ryne, new Placement(FragmentPosition + new Vector3(-0.65f, 0, 0), MathF.PI / 2));
        SpawnVision(BNpcBaseId.VisionOfGaia, BNpcNameId.Gaia, new Placement(FragmentPosition + new Vector3(0.65f, 0, 0), -MathF.PI / 2));
        for (var i = 0; i < akhRhaiHelpers.Length; i++) akhRhaiHelpers[i] = SpawnHelper();
        for (var i = 0; i < helpers.Length; i++) helpers[i] = SpawnHelper();
    }

    private void SpawnVision(uint bnpcBaseId, uint nameId, Placement placement) => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: bnpcBaseId,
        NameId: nameId,
        Level: Level,
        EnemyList: EnemyListMode.Never,
        Placement: placement,
        IsHostile: false));

    private SimEnemy? SpawnHelper() => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: BNpcBaseId.Helper,
        NameId: BNpcNameId.Usurper,
        Level: Level,
        EnemyList: EnemyListMode.Never,
        Visibility: SpawnVisibility.InvisibleHelper,
        Placement: new Placement(Vector3.Zero, 0f)));

    // Wipes when an attack about to land at `position` reaches the Fragment of Fate.
    private bool BreaksFragment(Vector3 position, float radius, uint actionId)
    {
        if (Vector3.Distance(position, FragmentPosition) > radius) return false;
        Wipe(actionId, "the Fragment of Fate was hit");
        return true;
    }

    private void Wipe(uint actionId, string explanation)
    {
        if (wiped) return;
        wiped = true;
        world.Party.WipeAllPlayers(actionId, explanation);
    }

    private List<SimCharacter> Waters()
        => state.Waters.Select(world.Party.Get).OfType<SimCharacter>().Where(m => m.IsAlive()).ToList();

    private void MarkWaters(uint lockonId) => Waters().ForEach(m => m.AttachLockonVfx(lockonId));

    private void AkhRhai()
    {
        var members = world.Party.ActiveMembers().ToList();
        akhRhaiPuddles = members.Count;
        for (var i = 0; i < members.Count; i++)
        {
            var position = members[i].Position;
            if (BreaksFragment(position, 4f, ActionId.AkhRhai)) return;
            akhRhaiHelpers[i]?.SetPosition(position);
            akhRhaiHelpers[i]?.Cast(Actions.AkhRhaiCast, position);
        }
    }

    private void RepeatAkhRhai()
    {
        foreach (var helper in akhRhaiHelpers.Take(akhRhaiPuddles))
            helper?.Cast(Actions.AkhRhai, helper.Position);
    }

    private void Chain(ushort statusId, ushort tetherId, float duration)
    {
        ClearChains();
        for (var i = 0; i < 4; i++)
        {
            var (from, to) = state.ChainLink(i);
            world.Party.Get(from)?.AddStatus(statusId, duration);
            chains.Add(world.Tether(world.Party.Get(from), world.Party.Get(to), tetherId));
        }
    }

    private void ClearChains()
    {
        chainsLocked = false;
        chains.ForEach(chain => chain.Despawn());
        chains.Clear();
    }

    // Two towers for two each, and Path of Light's four cones on whoever stands nearest the center.
    private void BrightHungerAndPathOfLight()
    {
        world.Map.AddEffect(MapEffect.Hide, MapEffect.DarklitFirstTower);
        world.Map.AddEffect(MapEffect.Hide, MapEffect.DarklitFirstTower + 1);
        var baits = world.Party.ActiveMembers().OrderBy(m => m.Position.LengthSquared()).Take(4).ToList();
        foreach (var z in new[] { -8f, 8f })
        {
            var tower = new Vector3(0, 0, z);
            var soakers = world.Party.Find.InsideCircle(tower, TowerRadius).Count;
            if (soakers != 2)
            {
                Wipe(ActionId.BrightHunger, $"{soakers}/2 in a tower");
                return;
            }
        }
        helpers[0]?.SetPosition(new Vector3(0, 0, -8f));
        helpers[0]?.Cast(Actions.BrightHungerDarklit, new Vector3(0, 0, -8f));
        helpers[1]?.SetPosition(new Vector3(0, 0, 8f));
        helpers[1]?.Cast(Actions.BrightHungerDarklit, new Vector3(0, 0, 8f));
        for (var i = 0; i < baits.Count; i++)
        {
            helpers[2 + i]?.SetPosition(Vector3.Zero);
            helpers[2 + i]?.Cast(Actions.PathOfLight, baits[i]);
        }
    }

    private void SpiritTaker()
    {
        if (oracle is not { } boss || world.Party.Get(state.SpiritTakerTarget) is not { } target || !target.IsAlive()) return;
        if (BreaksFragment(target.Position, 5f, ActionId.SpiritTaker)) return;
        boss.Cast(Actions.SpiritTaker, target);
        JumpTowards(target.Position);
    }

    private void JumpTowards(Vector3 position)
    {
        if (oracle is not { } boss) return;
        var offset = position - boss.Position;
        var landing = offset.Length() <= JumpLandingShort ? boss.Position : position - Vector3.Normalize(offset) * JumpLandingShort;
        world.Events.Add(0.5f, () => boss.SetPosition(landing));
    }

    private void DarkWater()
    {
        var i = 0;
        foreach (var holder in Waters())
        {
            if (BreaksFragment(holder.Position, 6f, ActionId.DarkWater)) return;
            helpers[i]?.SetPosition(holder.Position);
            helpers[i++]?.Cast(Actions.DarkWater, holder);
        }
    }

    private void SomberDance(EnemyAction action, bool farthest)
    {
        if (oracle is not { } boss) return;
        var byDistance = world.Party.ActiveMembers().OrderBy(m => Vector3.DistanceSquared(m.Position, boss.Position)).ToList();
        if (byDistance.Count == 0) return;
        var target = farthest ? byDistance[^1] : byDistance[0];
        if (BreaksFragment(target.Position, 8f, action.ActionId)) return;
        boss.Cast(action, target);
        JumpTowards(target.Position);
    }

    private void AkhMorn()
    {
        CastAkhMorn(helpers[0], Actions.AkhMornUsurperHit, PartyRole.MainTank);
        CastAkhMorn(helpers[1], Actions.AkhMornOracleHit, PartyRole.OffTank);
    }

    private void CastAkhMorn(SimEnemy? helper, EnemyAction action, PartyRole role)
    {
        if (world.Party.Get(role) is not { } tank || !tank.IsAlive()) return;
        if (BreaksFragment(tank.Position, 4f, action.ActionId)) return;
        helper?.SetPosition(tank.Position);
        helper?.Cast(action, tank);
    }

    public void Tick(float delta, float elapsed)
    {
        if (chainsLocked && ChainBroken())
            Wipe(ActionId.RefulgentFate, "an Everlasting Light chain broke");
    }

    private bool ChainBroken()
    {
        for (var i = 0; i < 4; i++)
        {
            var (from, to) = state.ChainLink(i);
            if (world.Party.Get(from) is not { } a || world.Party.Get(to) is not { } b) continue;
            var length = Vector3.Distance(a.Position, b.Position);
            if (!a.IsAlive() || !b.IsAlive() || length < ChainMin || length > ChainMax) return true;
        }
        return false;
    }
}
