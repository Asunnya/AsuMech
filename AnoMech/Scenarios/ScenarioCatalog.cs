using System.Collections.Generic;
using System.Linq;
using AnoMech.Scenarios.Dsr.P1Knights;
using AnoMech.Scenarios.Dsr.P2Thordan;
using AnoMech.Scenarios.Dsr.P3Nidhogg;
using AnoMech.Scenarios.Dsr.P4Eyes;
using AnoMech.Scenarios.Dsr.P5Wrath;
using AnoMech.Scenarios.Dsr.P5Death;
using AnoMech.Scenarios.Dsr.P6DoubleDragons;
using AnoMech.Scenarios.M10s.DeepAerial;
using AnoMech.Scenarios.M10s.DeepVarial;
using AnoMech.Scenarios.M10s.InsaneAir;
using AnoMech.Scenarios.M10s.Opener;
using AnoMech.Scenarios.M10s.SickestTakeoff;
using AnoMech.Scenarios.M10s.Snaking;
using AnoMech.Scenarios.M9s.Aetherletting;
using AnoMech.Scenarios.M9s.Coffinmaker;
using AnoMech.Scenarios.M9s.Deathmatch;
using AnoMech.Scenarios.M9s.Final;
using AnoMech.Scenarios.M9s.Flails;
using AnoMech.Scenarios.M9s.HellInACell;
using AnoMech.Scenarios.M9s.VampStomp;
using AnoMech.Scenarios.M9s.VampStomp2;
using AnoMech.Scenarios.Fru.P2DiamondDust;
using AnoMech.Scenarios.Fru.P2LightRampant;
using AnoMech.Scenarios.Fru.P3Apocalypse;
using AnoMech.Scenarios.Fru.P3UltimateRelativity;
using AnoMech.Scenarios.Fru.P4CrystallizeTime;
using AnoMech.Scenarios.Fru.P4DarklitDragonsong;
using AnoMech.Scenarios.Fru.P5FulgentBlade;
using AnoMech.Scenarios.Fru.P5ParadiseRegained;
using AnoMech.Scenarios.Top.P2PartySynergy;
using AnoMech.Scenarios.Top.P5Delta;
using AnoMech.Scenarios.Top.P5Omega;
using AnoMech.Scenarios.Top.P5Sigma;
using AnoMech.Scenarios.Top.P6WaveCannon2;
using AnoMech.Scenarios.Ucob.P5Exaflares;
using AnoMech.Scenarios.Umad;
using AnoMech.Scenarios.Umad.P1TeleTrouncing;
using AnoMech.Scenarios.Umad.P2Forsaken;
using AnoMech.Scenarios.Umad.P3BlackHole;
using AnoMech.Scenarios.Umad.P3LimitCut;
using AnoMech.Scenarios.Umad.P4KefkaSays;
using AnoMech.Scenarios.Umad.P5Celestriad;
using AnoMech.Scenarios.Umad.P5Exaflares;
using AnoMech.Scenarios.Umad.P5Flood;
using AnoMech.Scenarios.Uwu.P1Garuda;
using AnoMech.Scenarios.Uwu.P2Ifrit;
using AnoMech.Scenarios.Uwu.P3Titan;
using AnoMech.Scenarios.Uwu.PrimalRoulette;
using AnoMech.Scenarios.Uwu.UltimateAnnihilation;
using AnoMech.Scenarios.Uwu.UltimatePredation;
using AnoMech.Scenarios.Uwu.UltimateSuppression;

namespace AnoMech.Scenarios;

// Every runnable scenario, in menu order, plus the zone -> phase -> scenario tree derived from it in
// first-appearance order. Fresh instances per Create: a scenario holds its run state.
public sealed class ScenarioCatalog
{
    public IReadOnlyList<IScenario> Scenarios { get; }
    public IReadOnlyList<IZone> Zones { get; }
    private readonly Dictionary<IZone, IReadOnlyList<IPhase>> phasesByZone;
    private readonly Dictionary<IPhase, IReadOnlyList<IScenario>> scenariosByPhase;

    private ScenarioCatalog(IReadOnlyList<IScenario> scenarios)
    {
        Scenarios = scenarios;
        var zones = new List<IZone>();
        var phases = new Dictionary<IZone, List<IPhase>>();
        var byPhase = new Dictionary<IPhase, List<IScenario>>();
        foreach (var scenario in scenarios)
        {
            var phase = scenario.Phase;
            var zone = phase.Zone;
            if (!phases.TryGetValue(zone, out var zonePhases))
            {
                zonePhases = [];
                phases[zone] = zonePhases;
                zones.Add(zone);
            }
            if (!zonePhases.Contains(phase)) zonePhases.Add(phase);
            if (!byPhase.TryGetValue(phase, out var phaseScenarios))
            {
                phaseScenarios = [];
                byPhase[phase] = phaseScenarios;
            }
            phaseScenarios.Add(scenario);
        }
        Zones = zones;
        phasesByZone = phases.ToDictionary(kv => kv.Key, IReadOnlyList<IPhase> (kv) => kv.Value);
        scenariosByPhase = byPhase.ToDictionary(kv => kv.Key, IReadOnlyList<IScenario> (kv) => kv.Value);
    }

    public IReadOnlyList<IPhase> PhasesOf(IZone zone) => phasesByZone[zone];
    public IReadOnlyList<IScenario> ScenariosOf(IPhase phase) => scenariosByPhase[phase];

    public static ScenarioCatalog Create() => new(
    [
        new UmadP1TeleTrouncingScenario(),
        new UmadP2ForsakenScenario(),
        new UmadP3LimitCutScenario(),
        new UmadP3BlackHoleScenario(),
        new UmadP4KefkaSaysScenario(),
        new UmadP5FloodScenario(),
        new UmadP5CelestriadScenario(),
        new UmadP5ExaflaresScenario(),
        new UmadP5ForsakenNull(),
        new FruP2DiamondDustScenario(),
        new FruP2LightRampantScenario(),
        new FruP3UltimateRelativityScenario(),
        new FruP3ApocalypseScenario(),
        new FruP4DarklitDragonsongScenario(),
        new FruP4CrystallizeTimeScenario(),
        new FruP5FulgentBladeScenario(),
        new FruP5ParadiseRegainedScenario(),
        new TopP2PartySynergyScenario(),
        new TopP5DeltaScenario(),
        new TopP5SigmaScenario(),
        new TopP5OmegaScenario(),
        new TopP6WaveCannon2Scenario(),
        new UwuP1GarudaScenario(),
        new UwuP2IfritScenario(),
        new UwuP3TitanScenario(),
        new UltimatePredationScenario(),
        new UltimateAnnihilationScenario(),
        new UltimateSuppressionScenario(),
        new PrimalRouletteScenario(),
        new UcobP5ExaflaresScenario(),
        new DsrP1KnightsScenario(),
        new DsrP2ThordanScenario(),
        new DsrP2MeteorsScenario(),
        new DsrP3NidhoggScenario(),
        new DsrP4EyesScenario(),
        new DsrP5WrathScenario(),
        new DsrP5DeathScenario(),
        new DsrP6DoubleDragonsScenario(),
        new DsrP6CauterizeScenario(),
        new M9sVampStompScenario(),
        new M9sCoffinmakerScenario(),
        new M9sAetherlettingScenario(),
        new M9sVampStomp2Scenario(),
        new M9sFlailsScenario(),
        new M9sHellInACellScenario(),
        new M9sDeathmatchScenario(),
        new M9sFinalScenario(),
        new M10sOpenerScenario(),
        new M10sSickestTakeoffScenario(),
        new M10sInsaneAirScenario(),
        new M10sSnakingScenario(),
        new M10sDeepVarialScenario(),
        new M10sDeepAerialScenario(),
    ]);
}
