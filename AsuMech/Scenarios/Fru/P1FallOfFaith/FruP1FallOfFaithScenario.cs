using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.EnemyActions;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Fru.FruConstants;
using static AsuMech.Scenarios.Fru.P1FallOfFaith.FruP1FallOfFaithState;
using Actions = AsuMech.Scenarios.Fru.FruActions;

namespace AsuMech.Scenarios.Fru.P1FallOfFaith;

// Fall of Faith to the kill: four tethered Sinblaze or Sinsmite hits, Burnished Glory, Powder Mark
// Trail, Burnt Strike's towers and the enrage Fatebreaker dies casting. t=0 is 3s before Fall of
// Faith's cast. Fatebreaker's HP is not simulated: he falls when the clear did.
public sealed class FruP1FallOfFaithScenario : IScenario
{
    public string Name => "Fall of Faith";
    public IPhase Phase => FruZone.P1;

    public IReadOnlyList<IScenarioAi> AiStrats => [new FruP1FallOfFaithAi()];

    public void DrawSettings() => settingsWindow.Draw();
    public void DrawPerPlayerSettings() => settingsWindow.DrawPerPlayer();
    public object SettingsOverrides => settingsWindow.Overrides;
    public IReadOnlyList<string> SettingsConflicts => settingsWindow.Overrides.Validate().Problems;
    private readonly FruP1FallOfFaithSettingsWindow settingsWindow = new();

    private static readonly Vector3[] FaithImageSpots = [new(-14.14f, 0, -14.14f), new(0, 0, -20), new(14.14f, 0, -14.14f)];
    // A centred line: retail's own cast from the middle hits the whole north-south strip.
    private static readonly Placement CentreLine = new(new Vector3(0, 0, -40), 0f);
    private const float FettersAfterResolve = 0.6f;
    private const float ChargeAfterResolve = 2.7f;
    private const float BowShockAfterHit = 0.3f;
    public const float Clear = 70.88f;

    private SimWorld world = null!;
    private FruP1FallOfFaithState state = null!;
    private SimEnemy? fatebreaker;
    private readonly SimEnemy?[] faithCasters = new SimEnemy?[4];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new FruP1FallOfFaithState(world.Rng, world.Party.PlayerRole, settingsWindow.Overrides);
        Array.Clear(faithCasters);
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<FruP1FallOfFaithState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, SpawnFatebreaker);
        world.Events.Add(1f, () => fatebreaker?.SetTarget(world.Party.Get(PartyRole.MainTank), follow: false));
        world.Events.Add(5.9f, () => SpawnFaithImage(1));
        world.Events.Add(8.5f, () => SpawnFaithImage(2));
        world.Events.Add(10.95f, () => SpawnFaithImage(3));
        for (var k = 0; k < 4; k++)
        {
            var faith = k;
            world.Events.Add(FaithCastAt[faith] - 0.12f, () => TetherFaith(faith));
            world.Events.Add(FaithCastAt[faith], () => CastFaith(faith));
            world.Events.Add(FaithResolveAt(faith) + FettersAfterResolve, () => Fetter(faith));
            world.Events.Add(FaithResolveAt(faith) + ChargeAfterResolve, () => Charge(faith));
            world.Events.Add(FaithHitAt(faith), () => FaithHit(faith));
            world.Events.Add(FaithHitAt(faith) + BowShockAfterHit, () => BowShock(faith));
        }
        world.Events.Add(25f, () => faithCasters.Skip(1).ToList().ForEach(image => image?.FadeOut()));
        world.Events.Add(26.82f, () => fatebreaker?.Cast(Actions.BurnishedGlory));
        world.Events.Add(35.47f, MarkPowderMark);
        world.Events.Add(35.47f, () => fatebreaker?.Cast(Actions.PowderMarkTrail, world.Party.Get(PartyRole.OffTank)));
        world.Events.Add(45.34f, BurntStrike);
        world.Events.Add(45.34f, SpawnTowers);
        world.Events.Add(60.93f, SayBattleTalk);
        world.Events.Add(61.09f, () => fatebreaker?.Cast(Actions.BurnishedGloryEnrage));
        world.Events.Add(Clear, () => fatebreaker?.Defeat(corpseFadeDelay: 3f));
    }

    private void SpawnFatebreaker()
    {
        fatebreaker = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.Fatebreaker,
            NameId: BNpcNameId.Fatebreaker,
            Level: Level,
            Targetable: true,
            Placement: new Placement(Vector3.Zero, MathF.PI),
            LayoutId: LayoutId.Fatebreaker));
        faithCasters[0] = fatebreaker;
    }

    private void SpawnFaithImage(int faith) => faithCasters[faith] = world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: BNpcBaseId.FatebreakerImage,
        NameId: BNpcNameId.FatebreakerImage,
        Level: Level,
        EnemyList: EnemyListMode.Never,
        Placement: new Placement(FaithImageSpots[faith - 1], 0f).Face(Vector3.Zero),
        LayoutId: LayoutId.FatebreakerImage));

    private SimCharacter? Holder(int faith) => world.Party.Get(state.Tethered[faith]) is { } member && member.IsAlive() ? member : null;

    private void TetherFaith(int faith)
    {
        if (Holder(faith) is not { } holder || faithCasters[faith] is not { } caster) return;
        var tether = state.FaithFire[faith] ? TetherId.FaithFire : TetherId.FaithLightning;
        world.Tether(caster, holder, tether, FaithResolveAt(faith) + FettersAfterResolve - FaithCastAt[faith] + 0.12f);
        holder.AddStatus(StatusId.Prey);
    }

    private void CastFaith(int faith)
        => faithCasters[faith]?.Cast(state.FaithFire[faith] ? ActionId.FallOfFaithFire : ActionId.FallOfFaithLightning, animationLock: 1.1f);

    private void Fetter(int faith)
    {
        if (Holder(faith) is not { } holder) return;
        holder.AddStatus(StatusId.FloatingFetters, 3.94f);
        faithCasters[faith]?.Cast(ActionId.FloatingFetters, holder, animationLock: 0.6f);
    }

    private void Charge(int faith)
    {
        if (Holder(faith) is not { } holder) return;
        faithCasters[faith]?.Cast(faith == 0 ? ActionId.FallOfFaithChargeBoss : ActionId.FallOfFaithChargeImage, holder, animationLock: 1.1f);
    }

    private IEnumerable<SimCharacter> NearestTo(SimCharacter holder)
        => world.Party.ActiveMembers().Where(m => m.IsAlive() && !ReferenceEquals(m, holder))
            .OrderBy(m => Vector3.DistanceSquared(m.Position, holder.Position));

    // Sinblaze's cone points at the player nearest its holder; Sinsmite hits the holder alone.
    private void FaithHit(int faith)
    {
        if (Holder(faith) is not { } holder || fatebreaker is not { } caster) return;
        holder.RemoveStatus(StatusId.Prey);
        if (!state.FaithFire[faith])
        {
            caster.CastFromHelper(Actions.FallOfFaithSinsmite, holder, at: new Placement(holder.Position, 0f));
            return;
        }
        var aim = NearestTo(holder).FirstOrDefault()?.Position ?? Vector3.Zero;
        caster.CastFromHelper(Actions.Sinblaze, holder, at: new Placement(holder.Position, 0f).Face(aim));
    }

    // A 120° cone from Sinsmite's holder at each of the three nearest.
    private void BowShock(int faith)
    {
        if (state.FaithFire[faith] || Holder(faith) is not { } holder || fatebreaker is not { } caster) return;
        foreach (var target in NearestTo(holder).Take(3).ToList())
            caster.CastFromHelper(Actions.BowShock, at: new Placement(holder.Position, 0f).Face(target.Position));
    }

    private void MarkPowderMark()
    {
        if (world.Party.Get(PartyRole.OffTank) is { } tank && tank.IsAlive()) tank.AttachLockonVfx(LockonId.PowderMarkTrail);
    }

    private void BurntStrike()
    {
        if (fatebreaker is not { } boss) return;
        boss.Cast(state.TowersFire ? ActionId.BurntStrikeFire : ActionId.BurntStrikeLightning, animationLock: 2.1f);
        boss.CastFromHelper(state.TowersFire ? Actions.BurntStrikeFire : Actions.BurntStrikeLightning, at: CentreLine);
        boss.CastFromHelper(state.TowersFire ? Actions.BlastburnTowers : Actions.BurnoutTowers, at: CentreLine);
    }

    private EnemyAction[] TowerActions => state.TowersFire ? Actions.FireTowers : Actions.LightningTowers;

    private void SpawnTowers()
    {
        for (var tower = 0; tower < 3; tower++)
            fatebreaker?.CastFromHelper(TowerActions[state.TowerSoakers[tower] - 1], at: new Placement(state.Tower(tower), 0f));
    }

    private void SayBattleTalk()
    {
        if (fatebreaker is { } boss) world.Map.DirectorUpdate(0x80000027U, 0x2U, 0x2U, FatebreakerBattleTalk, boss.EntityId);
    }

    public void OnActionResolved(ActionResolved resolved)
    {
        var punishment = state.TowersFire ? Actions.UnmitigatedExplosionFire : Actions.UnmitigatedExplosionLightning;
        for (var soakers = 1; soakers <= 4; soakers++)
            AsuMech.Core.Game.Towers.ResolveTower(world, resolved, TowerActions[soakers - 1].ActionId, punishment, soakers);
    }

    public void OnStatusLost(StatusLost lost) => FruPowderMarkTrail.Detonate(world, fatebreaker, lost);
}
