using static AsuMech.Scenarios.Fru.FruConstants;

namespace AsuMech.Scenarios.Fru.P5FulgentBlade;

public sealed class FruP5FulgentBlade3Scenario() : FruP5FulgentBladeScenario("Fulgent Blade III",
[
    new(26.32f, 0x2D, GaiaBattleTalk),
    new(31.37f, 0x2E, RyneBattleTalk),
    new(36.28f, 0x2F, BNpcNameId.Pandora),
]);
