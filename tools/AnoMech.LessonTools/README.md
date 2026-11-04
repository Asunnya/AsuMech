# AnoMech.LessonTools

Lesson authoring CLI. Runs strats headless through the test harness, so it needs the same
Dalamud SDK the tests do (`DALAMUD_HOME`).

```
dotnet build tools/AnoMech.LessonTools -p:GenerateSupportedOSPlatformAttribute=false
TOOL=$(find tools/AnoMech.LessonTools/bin -name AnoMech.LessonTools.dll | head -1)

# one command per strat: record -> skeleton -> rule miner -> cue extractor -> draft [-> toolbox import -> oracle]
# writes <out>/<Scenario>.<strat>.{dump.json,lesson.json,context.md}; the lesson already has a rule, cue, roles,
# tolerance and skip per step from the mined data, the context says where each came from
dotnet $TOOL author --scenario UwuP1GarudaScenario --strat 0 --seeds 1..12 --out work/ [--plan 122994122467471 --layout Naur]
# the same for every catalog strat without an embedded lesson, in the Lessons folder layout
dotnet $TOOL author-all --out work/all --seeds 1..12 [--scenario Uwu] [--layout Naur] [--plans "UwuP3TitanScenario=463534803605261"]
# draft the text again from an existing dump (after a miner change)
dotnet $TOOL draft --dump x.dump.json --lesson x.lesson.json --out x.lesson.json [--zone UwuZone --layout Naur]

# the pieces, when one is needed on its own:
# every order the strat gives the player's slot, over seeds × 8 roles, plus each seed's *State and visible events
dotnet $TOOL record   --scenario UwuP1GarudaScenario --strat 0 --seeds 1..8 --out garuda.dump.json
# which rolls pick each step's spot per role (fixed / by <field> / unexplained), spots named by compass and waymark
dotnet $TOOL mine     --dump garuda.dump.json --zone UwuZone --layout Naur
# what the player could see in the 8 s before each order: casts, statuses, tethers, markers, spawns, ranked
dotnet $TOOL cues     --dump garuda.dump.json [--window 8]
# the toolbox against the strat: example seed (the roll the plan draws) and per-step residuals; --write stores exampleSeed
dotnet $TOOL oracle   --plan AnoMech/Learn/Toolbox/Plans/122994122467471.json --lesson <lesson.json> --dump garuda.dump.json [--write]
# one step per strat call, titles from method names, rules left empty
dotnet $TOOL skeleton --dump garuda.dump.json --out lesson.json
# after a strat edit moved time literals: follow the source lines, report orphans
dotnet $TOOL rekey    --lesson AnoMech/Learn/Lessons/Uwu/P1Garuda.zeal-the-balance.json --dump garuda.dump.json
# every catalog strat without an embedded lesson: dump + skeleton per strat, in the Lessons layout
dotnet $TOOL skeleton-all --out skel/ [--scenario Uwu] [--seeds 1,2] [--force]
# toolbox plan (id downloads strats/<id>.json; or a saved file): tolerant parse, A-D calibration against the zone's
# waymark layout, slot->role by job then refined by the opener, step alignment; --write stores the toolbox block
dotnet $TOOL toolbox-import --plan 122994122467471 --zone UwuZone --layout Naur --lesson <lesson.json> --dump garuda.dump.json --out-plan AnoMech/Learn/Toolbox/Plans/122994122467471.json --write
# mitigation: cast timeline per scenario (embedded under Learn/Mitigation/Timelines), then a plan from a Google Sheet
dotnet $TOOL mit-timeline --scenario UwuP1GarudaScenario --out AnoMech/Learn/Mitigation/Timelines/UwuP1GarudaScenario.json
dotnet $TOOL mit-import --plan uwu-ethel-master --sheet-id <google sheet id> --tabs "Garuda=UwuP1GarudaScenario,Ifrit=UwuP2IfritScenario" --credits "A;B" --out AnoMech/Learn/Mitigation/Plans/uwu-ethel-master.json
# validation + coverage both ways (what LessonLoaderTests also checks in CI)
dotnet $TOOL check    --lesson AnoMech/Learn/Lessons/Uwu/P1Garuda.zeal-the-balance.json --dump garuda.dump.json
```

Per-step authoring context (role spots, cue candidates, strat method): `~/.claude/skills/asumech-lesson`.

Flow for a new scenario: `author` (or `author-all`) → read the context, rewrite the drafted `rule`/`cue`
in player language where the draft is a table → `check` → embed under `AnoMech/Learn/Lessons/<Family>/`.
The draft's rules come in three shapes: a fixed place ("Go to near D (W 8m)."), a relation to a rolled
field ("Go opposite OpenerSafeSpot, 17m out." / "with NailBearing as north: NW wall") or a value table
("Depends on NailBearing: 270 → N 10m; …"); tables are the ones worth rephrasing. The miner only sees what the
`*State` exposes as public members; a spot that stays "unexplained" with 12 seeds is usually a
geometric rule over a list field (hazard spots), which the context names as the closest field.
