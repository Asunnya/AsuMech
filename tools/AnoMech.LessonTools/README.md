# AnoMech.LessonTools

Lesson authoring CLI. Runs strats headless through the test harness, so it needs the same
Dalamud SDK the tests do (`DALAMUD_HOME`).

```
dotnet build tools/AnoMech.LessonTools -p:GenerateSupportedOSPlatformAttribute=false
TOOL=$(find tools/AnoMech.LessonTools/bin -name AnoMech.LessonTools.dll | head -1)

# every order the strat gives the player's slot, over seeds × 8 roles
dotnet $TOOL record   --scenario UwuP1GarudaScenario --strat 0 --seeds 1,2,3 --out garuda.dump.json
# one step per strat call, titles from method names, rules left empty
dotnet $TOOL skeleton --dump garuda.dump.json --out lesson.json
# after a strat edit moved time literals: follow the source lines, report orphans
dotnet $TOOL rekey    --lesson AnoMech/Learn/Lessons/Uwu/P1Garuda.zeal-the-balance.json --dump garuda.dump.json
# validation + coverage both ways (what LessonLoaderTests also checks in CI)
dotnet $TOOL check    --lesson AnoMech/Learn/Lessons/Uwu/P1Garuda.zeal-the-balance.json --dump garuda.dump.json
```

Flow for a new scenario: `record` → `skeleton` → write `rule`/`cue` (or `skip`) per step → `check` →
embed under `AnoMech/Learn/Lessons/<Family>/`.
