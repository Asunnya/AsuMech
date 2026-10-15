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
using static AnoMech.Scenarios.Fru.P4CrystallizeTime.FruP4CrystallizeTimeState;
using Actions = AnoMech.Scenarios.Fru.FruActions;

namespace AnoMech.Scenarios.Fru.P4CrystallizeTime;

public sealed class FruP4CrystallizeTimeScenario : IScenario
{
    public string Name => "Crystallize Time";
    public IPhase Phase => FruZone.P4;

    public IReadOnlyList<IScenarioAi> AiStrats => strats;
    private readonly IReadOnlyList<IScenarioAi> strats = [new FruP4CrystallizeTimeAi()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly FruP4CrystallizeTimeSettingsWindow settingsWindow = new();

    private const float PuddleLifetime = 17.8f;
    private const float PuddleGrace = 1f;
    private const float PuddleRadius = 1f;
    private const float DragonsStopAt = ReturnSavedAt;

    private SimWorld world = null!;
    private FruP4CrystallizeTimeState state = null!;
    private IScenarioAi<FruP4CrystallizeTimeState>? ai;
    private SimEnemy? usurper, oracle;
    private readonly SimEnemy?[] hourglasses = new SimEnemy?[6];
    private readonly SimEnemy?[] dragons = new SimEnemy?[2];
    private readonly int[] dragonCatches = new int[2];
    private readonly float[] dragonReadyAt = new float[2];
    private readonly PartyRole?[] lastCatcher = new PartyRole?[2];
    private readonly SimEnemy?[] helpers = new SimEnemy?[8];
    private readonly SimEnemy?[] tidalHelpers = new SimEnemy?[8];
    private readonly List<SimTether> speedTethers = [];
    private readonly Dictionary<PartyRole, (float DroppedAt, SimEventObject? Visual)> puddles = new();
    private readonly Dictionary<PartyRole, Vector3> returns = new();

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new FruP4CrystallizeTimeState(world.Rng, world.Party.PlayerRole, settingsWindow.Overrides);
        Array.Clear(dragons);
        Array.Clear(dragonCatches);
        Array.Clear(lastCatcher);
        Array.Fill(dragonReadyAt, DragonsMoveAt + DragonHitCooldown);
        speedTethers.Clear();
        puddles.Clear();
        returns.Clear();
        ai = selectedAi is { } idx && idx < AiStrats.Count ? (IScenarioAi<FruP4CrystallizeTimeState>)AiStrats[idx] : null;
        ai?.Run(state, world);

        world.Events.Add(0f, SpawnActors);
        world.Events.Add(1f, () => usurper?.SetTarget(world.Party.Get(PartyRole.MainTank), follow: false));
        world.Events.Add(1f, () => oracle?.SetTarget(world.Party.Get(PartyRole.OffTank), follow: false));
        world.Events.Add(1f, () => usurper?.Cast(Actions.CrystallizeTimeUsurper));
        world.Events.Add(1f, () => oracle?.Cast(Actions.CrystallizeTimeOracle));
        world.Events.Add(1.1f, () => world.Map.AddEffect(MapEffect.Show, MapEffect.FragmentOfFate));
        world.Events.Add(DebuffsAt, AssignDebuffs);
        world.Events.Add(DebuffsAt, SpawnHourglasses);
        world.Events.Add(10f, TetherHourglasses);
        world.Events.Add(10f, () => oracle?.PlayActionTimeline(ActionTimelineId.WarpStart));
        world.Events.Add(10f, () => usurper?.SetTargetable(false));
        world.Events.Add(10f, () => oracle?.SetTargetable(false));
        world.Events.Add(DragonsSpawnAt, SpawnDragons);
        world.Events.Add(DragonsSpawnAt + 0.6f, () => dragons.ToList().ForEach(d => d?.PlayActionTimeline(TimelineId.DrachenWandererAppear)));
        world.Events.Add(15.3f, SpeedUpAndSlowDownHourglasses);
        world.Events.Add(16.3f, () => speedTethers.ForEach(t => t.Despawn()));
        world.Events.Add(16.5f, () => Maelstrom(0));
        world.Events.Add(18.7f, () => CastOnHolders(CrystallizeDebuff.BlueWater, Actions.DarkWater, 6f));
        world.Events.Add(20.6f, Fireworks);
        world.Events.Add(21.8f, () => Maelstrom(1));
        world.Events.Add(RedIceCleanseBy, () => CheckCleansed(CrystallizeDebuff.RedIce));
        world.Events.Add(23.6f, () => CastOnHolders(CrystallizeDebuff.BlueDarkness, Actions.UnholyDarkness, 6f));
        world.Events.Add(23.6f, () => PositionUsurperForWaves(second: false));
        world.Events.Add(24.3f, () => TidalLight(second: false, 24.3f));
        world.Events.Add(27f, () => Maelstrom(2));
        world.Events.Add(29.8f, () => PositionUsurperForWaves(second: true));
        world.Events.Add(30.6f, () => TidalLight(second: true, 30.6f));
        world.Events.Add(37.5f, Quietus);
        world.Events.Add(ReturnSavedAt, SaveReturns);
        world.Events.Add(ReturnSavedAt, () => oracle?.SetPosition(new Placement(new Vector3(0, 0, 4.15f), MathF.PI)));
        world.Events.Add(ReturnSavedAt, () => oracle?.PlayActionTimeline(ActionTimelineId.WarpEnd));
        world.Events.Add(39.9f, () => oracle?.Cast(Actions.SpiritTakerCast));
        world.Events.Add(41.4f, () => usurper?.SetPosition(Vector3.Zero));
        // TODO: retail loops the windup (timeline 137) until the cast; PlayActionTimeline plays one.
        world.Events.Add(41.6f, () => usurper?.PlayActionTimeline(TimelineId.HallowedWingsWindup));
        world.Events.Add(42.7f, () => usurper?.Cast(Actions.HallowedWingsCast));
        world.Events.Add(43.4f, SpiritTaker);
        world.Events.Add(CleanseBy, () => Enum.GetValues<CrystallizeDebuff>().ToList().ForEach(CheckCleansed));
        world.Events.Add(StunAt, () => world.Party.ActiveMembers().ToList().ForEach(m => m.AddStatus(StatusId.Stun, StunEndsAt - StunAt)));
        world.Events.Add(48f, Rewind);
        world.Events.Add(48f, () => PositionUsurperForWaves(second: false));
        world.Events.Add(48f, () => usurper?.PlayActionTimeline(TimelineId.HallowedWingsShow));
        world.Events.Add(49.3f, () => usurper?.PlayActionTimeline(TimelineId.HallowedWingsSwing));
        world.Events.Add(49.7f, () => usurper?.Cast(Actions.HallowedWingsKnockback));
        world.Events.Add(53f, () => usurper?.PlayActionTimeline(TimelineId.HallowedWingsHide));
        world.Events.Add(53.1f, () => PositionUsurperForWaves(second: true));
        world.Events.Add(53.1f, () => usurper?.PlayActionTimeline(TimelineId.HallowedWingsShow));
        world.Events.Add(53.9f, () => usurper?.PlayActionTimeline(TimelineId.HallowedWingsSwing));
        world.Events.Add(54.3f, () => usurper?.Cast(Actions.HallowedWingsKnockback));
        world.Events.Add(56.8f, RemoveHourglasses);
        world.Events.Add(57.6f, () => usurper?.SetPosition(new Placement(new Vector3(-2.5f, 0, 0), MathF.PI)));
        world.Events.Add(57.6f, () => oracle?.SetPosition(new Placement(new Vector3(2.5f, 0, 0), MathF.PI)));
        world.Events.Add(57.6f, () => usurper?.SetTargetable(true));
        world.Events.Add(57.6f, () => oracle?.SetTargetable(true));
        world.Events.Add(58.4f, () => usurper?.Cast(Actions.AkhMornUsurper, world.Party.Get(PartyRole.MainTank)));
        world.Events.Add(58.4f, () => oracle?.Cast(Actions.AkhMornOracle, world.Party.Get(PartyRole.OffTank)));
        for (var hit = 0; hit < 4; hit++)
            world.Events.Add(61.9f + 1.1f * hit, AkhMorn);
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
            Placement: new Placement(new Vector3(0, 0, 4.15f), MathF.PI),
            ModelCharaId: ModelCharaId.UsurperDragon));
        oracle = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.OracleP4,
            NameId: BNpcNameId.Oracle,
            Level: Level,
            Targetable: true,
            EnemyList: EnemyListMode.OnlyWhenVisible,
            Placement: new Placement(Vector3.Zero, MathF.PI)));
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
        for (var i = 0; i < helpers.Length; i++) helpers[i] = SpawnHelper();
        for (var i = 0; i < tidalHelpers.Length; i++) tidalHelpers[i] = SpawnHelper();
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
        NameId: BNpcNameId.Oracle,
        Level: Level,
        EnemyList: EnemyListMode.Never,
        Visibility: SpawnVisibility.InvisibleHelper,
        Placement: new Placement(Vector3.Zero, 0f)));

    // TODO: retail also shows an actor-bound clock (VFX 1001, d1049_stlp_b0k1) over each player
    // until their Return is saved; there's no way to attach a VFX sheet row to an actor.
    private void AssignDebuffs()
    {
        foreach (var role in Enum.GetValues<PartyRole>())
        {
            if (world.Party.Get(role) is not { } member || !member.IsAlive()) continue;
            var debuff = state.Debuff(role);
            var cleanseBy = debuff == CrystallizeDebuff.RedIce ? RedIceCleanseBy : CleanseBy;
            // A moment past the check, so the check sees an uncleansed one.
            member.AddStatus(IsRed(debuff) ? StatusId.Wyrmclaw : StatusId.Wyrmfang, cleanseBy - DebuffsAt + 0.1f);
            var (spell, at) = debuff switch
            {
                CrystallizeDebuff.RedAero => (StatusId.DarkAeroWaiting, 20.6f),
                CrystallizeDebuff.BlueEruption => (StatusId.DarkEruptionWaiting, 20.6f),
                CrystallizeDebuff.BlueWater => (StatusId.DarkWaterWaiting, 18.7f),
                CrystallizeDebuff.BlueDarkness => (StatusId.UnholyDarknessWaiting, 23.6f),
                _ => (StatusId.DarkBlizzardWaiting, 20.6f),
            };
            member.AddStatus(spell, at - DebuffsAt);
            member.AddStatus(StatusId.ReturnWaiting, ReturnSavedAt - DebuffsAt);
        }
        foreach (var role in state.Quietus)
            world.Party.Get(role)?.AddStatus(StatusId.QuietusWaiting, 37.5f - DebuffsAt);
    }

    // The hourglass itself is scenery in its MapEffect slot; the BNpc only casts and holds tethers.
    private void SpawnHourglasses()
    {
        for (var i = 0; i < hourglasses.Length; i++)
        {
            hourglasses[i] = world.SpawnEnemy(new EnemySpawnConfig(
                BNpcBaseId: BNpcBaseId.SorrowsHourglassP4,
                NameId: BNpcNameId.SorrowsHourglass,
                Level: Level,
                EnemyList: EnemyListMode.Never,
                Placement: new Placement(Hourglass(i), MathF.PI)));
            world.Map.AddEffect(MapEffect.Show, (byte)(MapEffect.CrystallizeFirstHourglass + i));
        }
    }

    private void TetherHourglasses()
    {
        oracle?.Cast(Actions.Speed);
        foreach (var i in state.HourglassPair(0)) speedTethers.Add(world.Tether(hourglasses[i], oracle, TetherId.Quicken));
        foreach (var i in state.HourglassPair(2)) speedTethers.Add(world.Tether(hourglasses[i], oracle, TetherId.Slow));
    }

    private void SpeedUpAndSlowDownHourglasses()
    {
        var helper = 0;
        foreach (var i in state.HourglassPair(0)) CastAt(helpers[helper++], Actions.Quicken, hourglasses[i]);
        foreach (var i in state.HourglassPair(2)) CastAt(helpers[helper++], Actions.Slow, hourglasses[i]);
    }

    private void Maelstrom(int wave)
    {
        foreach (var i in state.HourglassPair(wave))
        {
            if (BreaksFragment(Hourglass(i), 12f, ActionId.Maelstrom)) return;
            hourglasses[i]?.Cast(Actions.Maelstrom);
        }
    }

    private void RemoveHourglasses()
    {
        for (var i = 0; i < hourglasses.Length; i++)
        {
            hourglasses[i]?.Despawn();
            world.Map.AddEffect(MapEffect.Hide, (byte)(MapEffect.CrystallizeFirstHourglass + i));
        }
    }

    private bool wiped;

    // Wipes when an attack about to land at `position` reaches the Fragment of Fate.
    private bool BreaksFragment(Vector3 position, float radius, uint actionId, float innerRadius = 0f)
    {
        var distance = Vector3.Distance(position, FragmentPosition);
        if (distance > radius || distance < innerRadius) return false;
        if (!wiped) world.Party.WipeAllPlayers(actionId, "the Fragment of Fate was hit");
        wiped = true;
        return true;
    }

    private static void CastAt(SimEnemy? helper, EnemyAction action, SimCharacter? target)
    {
        if (target is null) return;
        helper?.SetPosition(target.Position);
        helper?.Cast(action, target);
    }

    private void CastOnHolders(CrystallizeDebuff debuff, EnemyAction action, float radius, float innerRadius = 0f, int firstHelper = 0)
    {
        var helper = firstHelper;
        foreach (var role in state.Holders(debuff))
        {
            if (world.Party.Get(role) is not { } holder || !holder.IsAlive()) continue;
            if (BreaksFragment(holder.Position, radius, action.ActionId, innerRadius)) return;
            CastAt(helpers[helper++], action, holder);
        }
    }

    private void Fireworks()
    {
        CastOnHolders(CrystallizeDebuff.RedAero, Actions.DarkAero, 15f, firstHelper: 0);
        CastOnHolders(CrystallizeDebuff.BlueEruption, Actions.DarkEruption, 6f, firstHelper: 2);
        CastOnHolders(CrystallizeDebuff.RedIce, Actions.DarkBlizzard, 12f, innerRadius: 3f, firstHelper: 3);
        CastOnHolders(CrystallizeDebuff.BlueIce, Actions.DarkBlizzard, 12f, innerRadius: 3f, firstHelper: 5);
    }

    private void CheckCleansed(CrystallizeDebuff debuff)
    {
        foreach (var role in state.Holders(debuff))
            if (world.Party.Get(role) is { } member && (member.HasStatus(StatusId.Wyrmclaw) || member.HasStatus(StatusId.Wyrmfang)))
                member.Die(ActionId.JoylessDragonsong, "Wyrmclaw / Wyrmfang not cleansed");
    }

    private void PositionUsurperForWaves(bool second)
    {
        var direction = state.WaveDirection(second);
        usurper?.SetPosition(new Placement(state.WaveOrigin(second, 0), MathF.Atan2(direction.X, direction.Z)));
    }

    // Four strips 10y deep, 2s apart, each its own helper's cast.
    private void TidalLight(bool second, float start)
    {
        usurper?.Cast(Actions.TidalLightCast);
        var direction = state.WaveDirection(second);
        for (var step = 0; step < 4; step++)
        {
            var helper = tidalHelpers[(second ? 4 : 0) + step];
            var origin = state.WaveOrigin(second, step);
            helper?.SetPosition(new Placement(origin, MathF.Atan2(direction.X, direction.Z)));
            if (step == 0) helper?.Cast(Actions.TidalLightFirst);
            else world.Events.Add(1f + 2f * step, () => helper?.Cast(Actions.TidalLightRest));
        }
    }

    private void Quietus()
    {
        var helper = 0;
        foreach (var role in state.Quietus)
            CastAt(helpers[helper++], Actions.Quietus, world.Party.Get(role));
    }

    private void SaveReturns()
    {
        foreach (var member in world.Party.ActiveMembers().ToList())
        {
            if (member is not ISimPartyMember slot) continue;
            returns[slot.Role] = member.Position;
            member.AddStatus(StatusId.Return, StunAt - ReturnSavedAt);
            member.PlayAction(ActionId.Return);
            world.PlaceGroundVfx(VfxId.ReturnMarker, member.Position);
        }
    }

    private void SpiritTaker()
    {
        if (oracle is not { } boss || world.Party.Get(state.SpiritTakerTarget) is not { } target || !target.IsAlive()) return;
        if (BreaksFragment(target.Position, 5f, ActionId.SpiritTaker)) return;
        boss.Cast(Actions.SpiritTaker, target);
        var landing = target.Position;
        world.Events.Add(0.5f, () => boss.SetPosition(landing));
    }

    private void Rewind()
    {
        foreach (var (role, position) in returns)
        {
            if (world.Party.Get(role) is not { } member || !member.IsAlive()) continue;
            member.PlayAction(ActionId.ReturnIV);
            var offset = position - member.Position;
            if (offset.LengthSquared() < 0.0001f) continue;
            member.Push(member.Position - offset, offset.Length(), 0.5f);
        }
    }

    private void AkhMorn()
    {
        if (world.Party.Get(PartyRole.MainTank) is { } mainTank && mainTank.IsAlive() && !BreaksFragment(mainTank.Position, 4f, ActionId.AkhMornUsurperHit))
            CastAt(helpers[0], Actions.AkhMornUsurperSolo, mainTank);
        if (world.Party.Get(PartyRole.OffTank) is { } offTank && offTank.IsAlive() && !BreaksFragment(offTank.Position, 4f, ActionId.AkhMornOracleHit))
            CastAt(helpers[1], Actions.AkhMornOracleShared, offTank);
    }

    private void SpawnDragons()
    {
        for (var side = 0; side < 2; side++)
        {
            var start = DragonPosition(side, DragonsMoveAt);
            var ahead = DragonPosition(side, DragonsMoveAt + 0.1f) - start;
            // TODO: retail spawns the heads at scale 2 and shrinks them to 1 after their first
            // catch, with the contact radius; a spawned enemy's scale can't be set or changed.
            dragons[side] = world.SpawnEnemy(new EnemySpawnConfig(
                BNpcBaseId: BNpcBaseId.DrachenWanderer,
                NameId: BNpcNameId.DrachenWanderer,
                Level: Level,
                EnemyList: EnemyListMode.Never,
                Placement: new Placement(start, MathF.Atan2(ahead.X, ahead.Z))));
        }
    }

    public void Tick(float delta, float elapsed)
    {
        if (elapsed >= DragonsMoveAt && elapsed < DragonsStopAt)
            for (var side = 0; side < 2; side++) TickDragon(side, elapsed);
        TickPuddles(elapsed);
        ai?.Tick(state, world, delta);
    }

    // A head flies its circle and catches the first player it touches, at most twice. Whoever it
    // just caught has to step out of it before it can catch again.
    private void TickDragon(int side, float elapsed)
    {
        if (dragons[side] is not { } head || dragonCatches[side] >= 2) return;
        var position = head.Position;
        head.MoveTo(DragonPosition(side, elapsed + 0.5f), DragonSpeed);
        var contact = dragonCatches[side] == 0 ? DragonInitialContactRadius : DragonContactRadius;
        if (lastCatcher[side] is { } previous
            && (world.Party.Get(previous) is not { } catcher || !catcher.IsAlive() || Vector3.Distance(catcher.Position, position) > DragonInitialContactRadius))
            lastCatcher[side] = null;
        if (elapsed < dragonReadyAt[side]) return;
        var target = world.Party.ActiveMembers()
            .Where(m => m is ISimPartyMember slot && slot.Role != lastCatcher[side])
            .Where(m => Vector3.Distance(m.Position, position) <= contact)
            .MinBy(m => Vector3.DistanceSquared(m.Position, position));
        if (target is not ISimPartyMember caught) return;
        lastCatcher[side] = caught.Role;
        dragonReadyAt[side] = elapsed + DragonHitCooldown;
        if (BreaksFragment(position, 12f, ActionId.LongingOfTheLost)) return;
        helpers[6 + side]?.SetPosition(position);
        helpers[6 + side]?.Cast(Actions.LongingOfTheLost, target);
        DropPuddle(caught.Role, position, elapsed);
        if (++dragonCatches[side] < 2) return;
        head.Cast(Actions.DrachenWandererDisappear);
        head.FadeOut();
    }

    private void DropPuddle(PartyRole catcher, Vector3 position, float elapsed)
    {
        var visual = world.SpawnEventObject(new EventObjectSpawnConfig
        {
            EObjId = EObjId.DragonPuddle,
            Placement = new Placement(position, 0f),
            TimelineState = 1,
        });
        puddles[catcher] = (elapsed, visual);
        state.AddDragonPuddle(catcher, position);
    }

    // A puddle cleanses Wyrmfang off the first player to step in it, but never its own catcher's;
    // stepped in without Wyrmfang, it's simply spent.
    private void TickPuddles(float elapsed)
    {
        foreach (var (catcher, puddle) in puddles.ToList())
        {
            if (elapsed - puddle.DroppedAt >= PuddleLifetime)
            {
                RemovePuddle(catcher, puddle.Visual);
                continue;
            }
            if (elapsed - puddle.DroppedAt < PuddleGrace) continue;
            var position = state.DragonPuddles[catcher];
            var stepper = world.Party.ActiveMembers()
                .FirstOrDefault(m => m is ISimPartyMember slot && slot.Role != catcher && Vector3.Distance(m.Position, position) <= PuddleRadius);
            if (stepper is null) continue;
            stepper.RemoveStatus(StatusId.Wyrmfang);
            RemovePuddle(catcher, puddle.Visual);
        }
    }

    private void RemovePuddle(PartyRole catcher, SimEventObject? visual)
    {
        visual?.Despawn();
        puddles.Remove(catcher);
        state.RemoveDragonPuddle(catcher);
    }
}
