using static AsuMech.Scenarios.Fru.FruConstants;

namespace AsuMech.Scenarios.Fru.P5FulgentBlade;

public sealed class FruP5FulgentBlade2Scenario() : FruP5FulgentBladeScenario("Fulgent Blade II",
[
    new(16.78f, 0x2B, RyneBattleTalk),
    new(21.78f, 0x2C, GaiaBattleTalk),
    new(28.92f, 0x25, BNpcNameId.Pandora),
], carriedOnDelay: 3.1f);
