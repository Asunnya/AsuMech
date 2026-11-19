using System;
using System.Collections.Generic;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Fru.FruConstants;
using Actions = AsuMech.Scenarios.Fru.FruActions;

namespace AsuMech.Scenarios.Fru.P5PandorasBox;

// A tank LB3 check: the raidwide kills everyone the LB3's status doesn't cover.
public sealed class FruP5PandorasBoxScenario : IScenario
{
    public string Name => "Pandora's Box";
    public IPhase Phase => FruZone.P5;

    public IReadOnlyList<IScenarioAi> AiStrats => [new FruP5PandorasBoxAi()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly FruP5PandorasBoxSettingsWindow settingsWindow = new();

    private const float LimitBreakBars = 3f;

    private SimWorld world = null!;
    private SimEnemy? pandora;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        var state = new FruP5PandorasBoxState(world.Rng, settingsWindow.Overrides);
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<FruP5PandorasBoxState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, SpawnActors);
        world.Events.Add(0f, () => world.Party.LimitBreak.Set(LimitBreakBars));
        world.Events.Add(1f, () => pandora?.SetTarget(world.Party.Get(PartyRole.MainTank), follow: false));
        world.Events.Add(1.5f, () => pandora?.KeepFacing(world.Party.Get(PartyRole.MainTank)));
        world.Events.Add(1.5f, () => pandora?.Cast(Actions.PandorasBox));
        world.Events.Add(2.3f, () => new FruBattleTalk(0, 0x22, BNpcNameId.Pandora).Say(world, pandora));
        world.Events.Add(9.33f, () => new FruBattleTalk(0, 0x26, BNpcNameId.Pandora).Say(world, pandora));
    }

    private void SpawnActors()
    {
        pandora = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.Pandora,
            NameId: BNpcNameId.Pandora,
            Level: Level,
            Targetable: true,
            EnemyList: EnemyListMode.Always,
            Placement: new Placement(Vector3.Zero, MathF.PI),
            LayoutId: LayoutId.PandoraP5));
    }
}
