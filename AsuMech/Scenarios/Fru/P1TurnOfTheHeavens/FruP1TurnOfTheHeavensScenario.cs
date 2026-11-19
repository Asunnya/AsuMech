using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Fru.FruConstants;
using Actions = AsuMech.Scenarios.Fru.FruActions;

namespace AsuMech.Scenarios.Fru.P1TurnOfTheHeavens;

// From the image's Cyclonic Break to the second Burnished Glory: Turn of the Heavens' halos, the
// two Burnt Strikes and Blastburn's knockback, and Bound of Faith's Sinsmoke stacks. t=0 is 3s
// before the image's Cyclonic Break.
//
// Simplification: Fated Burn Mark's blast when its holder dies is not modelled.
public sealed class FruP1TurnOfTheHeavensScenario : IScenario
{
    public string Name => "Turn of the Heavens";
    public IPhase Phase => FruZone.P1;

    public IReadOnlyList<IScenarioAi> AiStrats => [new FruP1TurnOfTheHeavensAi()];

    public void DrawSettings() => settingsWindow.Draw();
    public void DrawPerPlayerSettings() => settingsWindow.DrawPerPlayer();
    public object SettingsOverrides => settingsWindow.Overrides;
    public IReadOnlyList<string> SettingsConflicts => settingsWindow.Overrides.Validate().Problems;
    private readonly FruP1TurnOfTheHeavensSettingsWindow settingsWindow = new();

    private static readonly Placement CyclonicSpot = new(Vector3.Zero, MathF.PI);
    private static readonly Placement HeavensSpot = new(new Vector3(0, 0, -7.5f), 0f);
    private static readonly Placement WestStrikeSpot = new(new Vector3(-20, 0, 0), MathF.PI / 2);
    private static readonly Placement NorthStrikeSpot = new(new Vector3(0, 0, -20), 0f);
    private static readonly Vector3[] FaithSpots = [new(-14.14f, 0, 14.14f), new(14.14f, 0, 14.14f)];
    private static readonly int[] WestHalfOctants = [0, 7, 6, 5];
    private const float HaloRadius = 15f;
    private const float TethersAt = 18.89f;
    private const float FettersAt = 29.6f;

    private SimWorld world = null!;
    private FruP1TurnOfTheHeavensState state = null!;
    private SimEnemy? cyclonicImage, heavensImage, westImage, northImage, fatebreaker;
    private readonly List<SimEnemy> faithImages = [];
    private readonly List<(SimEnemy Halo, bool Flame)> halos = [];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new FruP1TurnOfTheHeavensState(world.Rng, world.Party.PlayerRole, settingsWindow.Overrides);
        faithImages.Clear();
        halos.Clear();
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<FruP1TurnOfTheHeavensState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.Map.SetWeather(WeatherId.Fog, 0f));
        world.Events.Add(0f, () => cyclonicImage = SpawnImage(CyclonicSpot));
        world.Events.Add(3f, () => cyclonicImage?.Cast(state.CyclonicFire ? ActionId.CyclonicBreakImageFire : ActionId.CyclonicBreakImageLightning, animationLock: 1.1f));
        world.Events.Add(5.8f, () => world.Map.SetWeather(WeatherId.Fatebreaker, 3f));
        world.Events.Add(10f, () => heavensImage = SpawnImage(HeavensSpot));
        world.Events.Add(10f, () => westImage = SpawnImage(WestStrikeSpot));
        world.Events.Add(10.6f, () => { if (cyclonicImage is { } image) FruCyclonicBreak.Bait(world, image, state.CyclonicFire); });
        world.Events.Add(10.9f, () => heavensImage?.Cast(state.HeavensFire ? ActionId.TurnOfTheHeavensFire : ActionId.TurnOfTheHeavensLightning, animationLock: 1.1f));
        world.Events.Add(10.9f, () => westImage?.Cast(Actions.BurntStrikeWest));
        world.Events.Add(10.9f, () => westImage?.CastFromHelper(Actions.BurnoutWest, at: WestStrikeSpot));
        world.Events.Add(16.4f, () => northImage = SpawnImage(NorthStrikeSpot));
        world.Events.Add(16.96f, () => northImage?.Cast(Actions.BurntStrikeNorth));
        world.Events.Add(16.96f, () => northImage?.CastFromHelper(Actions.BlastburnNorth, at: NorthStrikeSpot));
        world.Events.Add(18.28f, SpawnHalos);
        world.Events.Add(18.5f, SpawnFaithImages);
        world.Events.Add(TethersAt, TetherFaithTargets);
        world.Events.Add(19.07f, () => faithImages.ForEach(image => image.Cast(ActionId.BoundOfFaith, animationLock: 1.1f)));
        world.Events.Add(21.13f, CastBrightfire);
        world.Events.Add(FettersAt, FetterFaithTargets);
        world.Events.Add(31.6f, ChargeFaithTargets);
        world.Events.Add(32.3f, () => halos.ForEach(h => h.Halo.Despawn()));
        world.Events.Add(32.94f, Sinsmoke);
        world.Events.Add(33.1f, () => ForEachTethered(member => member.RemoveStatus(StatusId.Prey)));
        world.Events.Add(33.5f, FadeImages);
        world.Events.Add(34.5f, SpawnFatebreaker);
        world.Events.Add(35.57f, () => fatebreaker?.Cast(Actions.BurnishedGlory));
    }

    private SimEnemy? SpawnImage(Placement placement) => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: BNpcBaseId.FatebreakerImage,
        NameId: BNpcNameId.FatebreakerImage,
        Level: Level,
        EnemyList: EnemyListMode.Never,
        Placement: placement,
        LayoutId: LayoutId.FatebreakerImage));

    private void SpawnFatebreaker()
    {
        fatebreaker = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.Fatebreaker,
            NameId: BNpcNameId.Fatebreaker,
            Level: Level,
            Targetable: true,
            Placement: new Placement(Vector3.Zero, MathF.PI),
            LayoutId: LayoutId.Fatebreaker));
    }

    private void SpawnHalos()
    {
        for (var octant = 0; octant < 8; octant++)
        {
            var flame = WestHalfOctants.Contains(octant) == state.FlameWest;
            var position = FruP1Geometry.Point(octant, HaloRadius);
            if (world.SpawnEnemy(new EnemySpawnConfig(
                    BNpcBaseId: flame ? BNpcBaseId.HaloOfFlame : BNpcBaseId.HaloOfLevin,
                    NameId: flame ? BNpcNameId.HaloOfFlame : BNpcNameId.HaloOfLevin,
                    Level: Level,
                    EnemyList: EnemyListMode.Never,
                    Placement: new Placement(position, 0f).Face(Vector3.Zero),
                    LayoutId: flame ? LayoutId.HaloOfFlame : LayoutId.HaloOfLevin)) is { } halo)
                halos.Add((halo, flame));
        }
    }

    // The halos matching the element blast their whole circle; the others only their own spot.
    private void CastBrightfire()
    {
        foreach (var (halo, flame) in halos)
            halo.Cast(flame == state.HeavensFire ? Actions.BrightfireLarge : Actions.BrightfireSmall);
    }

    private void SpawnFaithImages()
    {
        foreach (var spot in FaithSpots)
            if (SpawnImage(new Placement(spot, 0f).Face(Vector3.Zero)) is { } image) faithImages.Add(image);
    }

    private void ForEachTethered(Action<SimCharacter> act)
    {
        foreach (var role in state.Tethered)
            if (world.Party.Get(role) is { } member && member.IsAlive()) act(member);
    }

    private void TetherFaithTargets()
    {
        for (var i = 0; i < state.Tethered.Count && i < faithImages.Count; i++)
        {
            if (world.Party.Get(state.Tethered[i]) is not { } member || !member.IsAlive()) continue;
            world.Tether(faithImages[i], member, TetherId.FaithFire, FettersAt - TethersAt);
            member.AddStatus(StatusId.Prey);
            member.AddStatus(StatusId.FatedBurnMark, 14.98f);
        }
    }

    private void FetterFaithTargets()
    {
        ForEachTethered(member => member.AddStatus(StatusId.FloatingFetters, 3.94f));
        foreach (var image in faithImages) image.Cast(ActionId.FloatingFetters, animationLock: 0.6f);
    }

    private void ChargeFaithTargets()
    {
        for (var i = 0; i < state.Tethered.Count && i < faithImages.Count; i++)
            if (world.Party.Get(state.Tethered[i]) is { } member && member.IsAlive())
                faithImages[i].Cast(ActionId.BoundOfFaithCharge, member, animationLock: 1.1f);
    }

    // UNVERIFIED: retail moves a dead holder's stack onto a random player; here it is skipped.
    private void Sinsmoke()
    {
        if (faithImages.FirstOrDefault() is not { } caster) return;
        ForEachTethered(member => caster.CastFromHelper(Actions.BoundOfFaithSinsmoke, member, at: new Placement(member.Position, 0f)));
    }

    private void FadeImages()
    {
        foreach (var image in new[] { cyclonicImage, heavensImage, westImage, northImage }.Concat(faithImages))
            image?.FadeOut();
    }
}
