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
# every catalog strat without an embedded lesson: dump + skeleton per strat, in the Lessons layout
dotnet $TOOL skeleton-all --out skel/ [--scenario Uwu] [--seeds 1,2] [--force]
# toolbox plan (id downloads strats/<id>.json; or a saved file): tolerant parse, A-D calibration against the zone's
# waymark layout, slot->role by job then refined by the opener, step alignment; --write stores the toolbox block
dotnet $TOOL toolbox-import --plan 122994122467471 --zone UwuZone --layout Naur --lesson <lesson.json> --dump garuda.dump.json --out-plan AnoMech/Learn/Toolbox/Plans/122994122467471.json --write
# validation + coverage both ways (what LessonLoaderTests also checks in CI)
dotnet $TOOL check    --lesson AnoMech/Learn/Lessons/Uwu/P1Garuda.zeal-the-balance.json --dump garuda.dump.json
```

Per-step authoring context (role spots, cue candidates, strat method): `~/.claude/skills/asumech-lesson`.

Flow for a new scenario: `record` → `skeleton` → write `rule`/`cue` (or `skip`) per step → `check` →
embed under `AnoMech/Learn/Lessons/<Family>/`.
