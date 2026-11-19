using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Fru.FruConstants;
using static AsuMech.Scenarios.Fru.P1UtopianSky.FruP1UtopianSkyState;
using Actions = AsuMech.Scenarios.Fru.FruActions;

namespace AsuMech.Scenarios.Fru.P1UtopianSky;

// The pull to Utopian Sky's lines: Cyclonic Break, Powder Mark Trail and its Burn Marks, and the
// images' Blasting Zones with Sinbound Fire III or Thunder III. t=0 is 3s before the opening
// Cyclonic Break.
//
// Simplification: the fog doesn't hide the images, and only the three that cut a line cast.
public sealed class FruP1UtopianSkyScenario : IScenario
{
    public string Name => "Utopian Sky";
    public IPhase Phase => FruZone.P1;

    public IReadOnlyList<IScenarioAi> AiStrats => [new FruP1UtopianSkyAi()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly FruP1UtopianSkySettingsWindow settingsWindow = new();

    private SimWorld world = null!;
    private FruP1UtopianSkyState state = null!;
    private SimEnemy? fatebreaker;
    private readonly List<SimEnemy> images = [];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new FruP1UtopianSkyState(world.Rng, settingsWindow.Overrides);
        images.Clear();
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<FruP1UtopianSkyState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, SpawnFatebreaker);
        world.Events.Add(1f, () => fatebreaker?.SetTarget(world.Party.Get(PartyRole.MainTank), follow: false));
        world.Events.Add(3f, () => fatebreaker?.Cast(state.CyclonicFire ? ActionId.CyclonicBreakFire : ActionId.CyclonicBreakLightning, animationLock: 1.1f));
        world.Events.Add(10.05f, () => { if (fatebreaker is { } boss) FruCyclonicBreak.Bait(world, boss, state.CyclonicFire); });
        world.Events.Add(15.01f, MarkPowderMark);
        world.Events.Add(15.01f, () => fatebreaker?.Cast(Actions.PowderMarkTrail, world.Party.Get(PartyRole.MainTank)));
        world.Events.Add(26.5f, SayBattleTalk);
        world.Events.Add(26.5f, () => fatebreaker?.Cast(state.UtopianFire ? ActionId.UtopianSkyFire : ActionId.UtopianSkyLightning, animationLock: 3.1f));
        world.Events.Add(30.6f, () => fatebreaker?.SetTargetable(false));
        world.Events.Add(30.6f, () => world.Map.SetWeather(WeatherId.Fog, 3f));
        world.Events.Add(31.2f, () => fatebreaker?.FadeOut());
        world.Events.Add(33.59f, SpawnImages);
        world.Events.Add(35.79f, CastBlastingZones);
        world.Events.Add(41f, FadeIdleImages);
        world.Events.Add(44.9f, BlastingZones);
        world.Events.Add(45.58f, SinboundElement);
        world.Events.Add(46.5f, () => images.ForEach(image => image.FadeOut()));
        world.Events.Add(47.2f, () => world.Map.SetWeather(WeatherId.Fatebreaker, 3f));
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
    }

    private SimEnemy? Caster => fatebreaker is { IsActive: true } ? fatebreaker : images.FirstOrDefault(i => i.IsActive);

    private void SayBattleTalk()
    {
        if (fatebreaker is { } boss) world.Map.DirectorUpdate(0x80000027U, 0x1U, 0x2U, FatebreakerBattleTalk, boss.EntityId);
    }

    private void MarkPowderMark()
    {
        if (world.Party.Get(PartyRole.MainTank) is { } tank && tank.IsAlive()) tank.AttachLockonVfx(LockonId.PowderMarkTrail);
    }

    private void SpawnImages()
    {
        for (var octant = 0; octant < 8; octant++)
        {
            var position = ImagePosition(octant);
            if (world.SpawnEnemy(new EnemySpawnConfig(
                    BNpcBaseId: BNpcBaseId.FatebreakerImage,
                    NameId: BNpcNameId.FatebreakerImage,
                    Level: Level,
                    EnemyList: EnemyListMode.Never,
                    Placement: new Placement(position, 0f).Face(Vector3.Zero),
                    LayoutId: LayoutId.FatebreakerImage)) is not { } image) continue;
            image.AddStatus(StatusId.Concealed);
            images.Add(image);
        }
    }

    private SimEnemy? ImageAt(int octant)
        => images.FirstOrDefault(i => Vector3.DistanceSquared(i.Position, ImagePosition(octant)) < 1f);

    private void CastBlastingZones()
    {
        foreach (var octant in state.ActiveImages) ImageAt(octant)?.Cast(ActionId.BlastingZoneCast, animationLock: 1.1f);
    }

    private void FadeIdleImages()
    {
        foreach (var image in images.Where(i => !state.ActiveImages.Any(o => ReferenceEquals(ImageAt(o), i))).ToList())
        {
            image.FadeOut();
            images.Remove(image);
        }
    }

    private void BlastingZones()
    {
        foreach (var octant in state.ActiveImages)
            if (ImageAt(octant) is { } image)
                image.CastFromHelper(Actions.BlastingZone, at: new Placement(image.Position, 0f).Face(Vector3.Zero));
    }

    private void SinboundElement()
    {
        if (Caster is not { } caster) return;
        if (state.UtopianFire)
        {
            foreach (var role in state.FireTargets)
                if (world.Party.Get(role) is { } target && target.IsAlive())
                    caster.CastFromHelper(Actions.SinboundFireIII, target, at: new Placement(target.Position, 0f));
            return;
        }
        foreach (var member in world.Party.ActiveMembers().Where(m => m.IsAlive()).ToList())
            caster.CastFromHelper(Actions.SinboundThunderIII, member, at: new Placement(member.Position, 0f));
    }

    public void OnStatusLost(StatusLost lost) => FruPowderMarkTrail.Detonate(world, Caster, lost);
}
