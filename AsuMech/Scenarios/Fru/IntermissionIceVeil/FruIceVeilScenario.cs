using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Fru.FruConstants;
using static AsuMech.Scenarios.Fru.IntermissionIceVeil.FruIceVeilState;
using Actions = AsuMech.Scenarios.Fru.FruActions;

namespace AsuMech.Scenarios.Fru.IntermissionIceVeil;

// t=0 is two seconds before Absolute Zero, the Usurper back in the centre after Light Rampant.
public sealed class FruIceVeilScenario : IScenario
{
    public string Name => "Ice Veil";
    public IPhase Phase => FruZone.P2;

    public IReadOnlyList<IScenarioAi> AiStrats => [new FruIceVeilAi()];
    public float BgmSecondsAtStart => 177.3f;

    private const float RayTetherSeconds = 5.16f;
    private const float PuddleShowsAfterRay = 0.53f;
    // UNVERIFIED: when it starts to hurt, and that it bleeds like P2's light puddles.
    private const float PuddleArmsAfterShowing = 1.5f;
    private const float PuddleFadesAfterShowing = 8.85f;
    private const float PuddleLastsAfterShowing = 12.07f;
    private const float PuddleRadius = 4f;
    // Straight on from Light Rampant, the Usurper auto-attacks this much longer before Absolute Zero.
    private const float LongerAfterLightRampant = 4.75f;

    private SimWorld world = null!;
    private FruIceVeilState state = null!;
    private SimEnemy? usurper, gaia, veil;
    private readonly SimEnemy?[] lights = new SimEnemy?[4];
    private readonly SimEnemy?[] darks = new SimEnemy?[4];
    private readonly Dictionary<SimEnemy, SimTether> veilTethers = [];
    private readonly SimCharacter?[] rayTargets = new SimCharacter?[4];
    private readonly FruPuddles puddles = new(EObjId.HiemalRayPuddle, PuddleRadius);

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new FruIceVeilState(world.Rng);
        Array.Clear(lights);
        Array.Clear(darks);
        veilTethers.Clear();
        Array.Clear(rayTargets);
        puddles.Clear();
        usurper = null;
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<FruIceVeilState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, SpawnUsurperAndGaia);
        if (!world.Continued)
        {
            world.Events.Add(0.6f, () => usurper?.SetModelState(4));
            world.Events.Add(0.6f, () => usurper?.AddStatusParam(TransfigurationStatus, UsurperLightTransformation));
        }
        world.Events.Add(2f, () => usurper?.Cast(ActionId.AbsoluteZero, animationLock: 2.1f));
        world.Events.Add(11.97f, () => usurper?.SetTargetable(false));
        world.Events.Add(12.65f, () => usurper?.CastFromHelper(Actions.AbsoluteZero, at: new Placement(Vector3.Zero, 0f)));
        // UNVERIFIED what model state 7 shows: retail sends it as she is encased.
        world.Events.Add(14.65f, () => usurper?.SetModelState(7));
        world.Events.Add(14.65f, () => gaia?.SetModelState(4));
        world.Events.Add(14.91f, () => usurper?.CastFromHelper(Actions.SwellingFrost, at: new Placement(Vector3.Zero, 0f)));
        world.Events.Add(28.1f, () => world.Map.AddEffect(MapEffect.IceVeilArenaShow, MapEffect.IceVeilArena));
        world.Events.Add(CrystalsSpawnAt, SpawnCrystals);
        world.Events.Add(35.63f, () => world.Tether(gaia, veil, TetherId.GaiaToVeil));
        world.Events.Add(36.54f, () => gaia?.SetModelState(5));
        world.Events.Add(CrystalsTetheredAt, TetherCrystalsToTheVeil);
        world.Events.Add(EndlessIceAgeAt, () => veil?.Cast(Actions.EndlessIceAge));
        foreach (var at in BlizzardAt) world.Events.Add(at, SinboundBlizzard);
        foreach (var at in StormAt) world.Events.Add(at, HiemalStorm);
        world.Events.Add(LightTethersAt, TetherLightsToNearest);
        for (var wave = 0; wave < HiemalRayAt.Length; wave++)
        {
            var w = wave;
            var showsAt = HiemalRayAt[w] + PuddleShowsAfterRay;
            world.Events.Add(HiemalRayAt[w], () => HiemalRay(w));
            world.Events.Add(showsAt, () => puddles.Show(world, w));
            world.Events.Add(showsAt + PuddleFadesAfterShowing, () => puddles.Fade(w));
            world.Events.Add(showsAt + PuddleLastsAfterShowing, () => puddles.Remove(w));
        }
        for (var light = 0; light < 4; light++)
        {
            var l = light;
            world.Events.Add(state.LightFallsAt(l), () => LightFalls(l));
        }
        world.Events.Add(LastLightFallsAt, DarknessChannelsIntoTheVeil);
        world.Events.Add(IcecrusherAt - 0.12f, () => gaia?.SetModelState(0));
        world.Events.Add(IcecrusherAt, Icecrusher);
        world.Events.Add(VeilFallsAt - 0.8f, DespawnCrystals);
        world.Events.Add(VeilFallsAt, VeilFalls);
        if (world.Continued)
        {
            world.Events.Delay(LongerAfterLightRampant);
            world.Events.Add(0f, SpawnUsurperAndGaia);
        }
    }

    private void SpawnUsurperAndGaia()
    {
        if (usurper is not null) return;
        usurper = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.UsurperP2,
            NameId: BNpcNameId.Usurper,
            Level: Level,
            Targetable: true,
            Placement: new Placement(Vector3.Zero, 0f),
            ModelCharaId: ModelCharaId.UsurperLight,
            InitialModeAttributeFlags: 0xFC,
            LayoutId: LayoutId.UsurperP2,
            // Carrying on, she is already in the light form Light Rampant left her in.
            Statuses: world.Continued ? [new SpawnStatus(TransfigurationStatus, UsurperLightTransformation)] : null,
            ModelState: world.Continued ? (byte)4 : (byte)0));
        // UNVERIFIED that the Vision's gear is the intermission Gaia's.
        gaia = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.GaiaIntermission,
            NameId: BNpcNameId.GaiaIntermission,
            Level: Level,
            EnemyList: EnemyListMode.Never,
            Placement: new Placement(new Vector3(0, 0, 10), MathF.PI),
            IsHostile: false,
            Appearance: Appearance.VisionOfGaia));
    }

    private void SpawnCrystals()
    {
        for (var i = 0; i < 4; i++)
        {
            lights[i] = Crystal(BNpcBaseId.CrystalOfLight, BNpcNameId.CrystalOfLight, Light(i));
            darks[i] = Crystal(BNpcBaseId.CrystalOfDarkness, BNpcNameId.CrystalOfDarkness, Dark(i));
        }
        veil = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.IceVeil,
            NameId: BNpcNameId.IceVeil,
            Level: Level,
            EnemyList: EnemyListMode.Manual,
            Placement: new Placement(Vector3.Zero, 0f)));
    }

    private SimEnemy Crystal(uint baseId, uint nameId, Vector3 at)
        => world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: baseId,
            NameId: nameId,
            Level: Level,
            Targetable: true,
            Placement: new Placement(at, 0f).Face(2 * at)));

    private IEnumerable<SimEnemy> Living(IEnumerable<SimEnemy?> crystals)
        => crystals.OfType<SimEnemy>().Where(c => c.IsAlive());

    private void TetherCrystalsToTheVeil()
    {
        if (veil is null) return;
        foreach (var crystal in Living(lights.Concat(darks)))
            veilTethers[crystal] = world.Tether(crystal, veil, TetherId.CrystalToVeil);
        veil.SetTargetable(true);
        veil.SetVisibleInEnemyList(true);
        veil.AddStatus(StatusId.Invincibility);
        world.PlayBgm(BgmId.Intermission);
    }

    private SimCharacter? NearestTo(Vector3 at)
        => world.Party.ActiveMembers().MinBy(m => Vector3.DistanceSquared(m.Position, at));

    private void SinboundBlizzard()
    {
        foreach (var dark in Living(darks))
            if (NearestTo(dark.Position) is { } target)
                dark.CastFromHelper(Actions.SinboundBlizzard, at: new Placement(dark.Position, 0f).Face(target.Position));
    }

    private void HiemalStorm()
    {
        foreach (var light in Living(lights))
            if (NearestTo(light.Position) is { } target)
                light.CastFromHelper(Actions.HiemalStorm, target.Position, at: new Placement(target.Position, 0f));
    }

    private void TetherLightsToNearest()
    {
        for (var i = 0; i < 4; i++)
        {
            rayTargets[i] = null;
            if (lights[i] is not { } light || !light.IsAlive() || NearestTo(light.Position) is not { } target) continue;
            rayTargets[i] = target;
            world.Tether(light, target, TetherId.CrystalOfLightToPlayer, RayTetherSeconds);
        }
    }

    private void HiemalRay(int wave)
    {
        for (var i = 0; i < 4; i++)
            if (lights[i] is { } light && light.IsAlive() && rayTargets[i] is { } target && target.IsAlive())
            {
                light.CastFromHelper(Actions.HiemalRay, target, at: new Placement(light.Position, 0f));
                puddles.Drop(wave, target, HiemalRayAt[wave] + PuddleShowsAfterRay + PuddleArmsAfterShowing);
            }
    }

    private void LightFalls(int i)
    {
        if (lights[i] is not { } light || !light.IsAlive() || light.HasStatus(StatusId.VulnerabilityDown)) return;
        light.SetHpFraction(0f);
        light.Defeat(corpseFadeDelay: 3f);
        if (veilTethers.Remove(light, out var tether)) tether.Despawn();
    }

    // With a Crystal of Light still up, the Veil stays invincible and Endless Ice Age goes off.
    private void DarknessChannelsIntoTheVeil()
    {
        if (veil is null || Living(lights).Any()) return;
        foreach (var tether in veilTethers.Values) tether.Despawn();
        veilTethers.Clear();
        var living = Living(darks).ToList();
        for (var i = 0; i < living.Count; i++)
            world.Tether(living[i], i % 2 == 0 ? living[(i + 1) % living.Count] : gaia, TetherId.DarknessChannel);
        veil.RemoveStatus(StatusId.Invincibility);
        veil.AddStatus(StatusId.VulnerabilityDown);
    }

    private bool VeilBreakable => veil is { } v && v.IsAlive() && !v.HasStatus(StatusId.Invincibility);

    private void Icecrusher()
    {
        if (!VeilBreakable) return;
        gaia?.Cast(ActionId.Icecrusher, veil, animationLock: 2.1f);
        veil?.SetHpFraction(0.25f);
    }

    private void DespawnCrystals()
    {
        if (!VeilBreakable) return;
        foreach (var crystal in Living(lights.Concat(darks))) crystal.Despawn();
    }

    private void VeilFalls()
    {
        if (!VeilBreakable || veil is null) return;
        veil.SetHpFraction(0f);
        veil.Defeat(corpseFadeDelay: 8f);
        world.Announce("The Eternal Ice Fragment is saved.");
    }

    private void WarnOfAnUnbreakableLight(StatusAdded added)
    {
        if (added.StatusId == StatusId.VulnerabilityDown && added.PreviousStacks == 0
            && added.Holder is SimEnemy { BNpcBaseId: BNpcBaseId.CrystalOfLight })
            world.Announce("Sinbound Blizzard hit a Crystal of Light: it can no longer be destroyed.");
    }

    public void OnStatusAdded(StatusAdded added) => WarnOfAnUnbreakableLight(added);

    public void Tick(float delta, float elapsed)
    {
        puddles.BleedWhoeverStandsIn(world, elapsed, "stood in a Hiemal Ray puddle (move off it after each ray)");
        for (var i = 0; i < 4; i++)
            if (lights[i] is { } light && light.IsAlive() && !light.HasStatus(StatusId.VulnerabilityDown))
                light.SetHpFraction(Remaining(elapsed, CrystalsTetheredAt, state.LightFallsAt(i)));
        if (VeilBreakable)
            veil!.SetHpFraction(elapsed < IcecrusherAt
                ? 0.5f + 0.5f * Remaining(elapsed, LastLightFallsAt, IcecrusherAt)
                : 0.25f * Remaining(elapsed, IcecrusherAt, VeilFallsAt));
    }

    private static float Remaining(float now, float from, float to) => Math.Clamp((to - now) / (to - from), 0f, 1f);
}
