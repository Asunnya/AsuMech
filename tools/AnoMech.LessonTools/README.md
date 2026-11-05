# AnoMech.LessonTools

Lesson authoring CLI. Runs strats headless through the test harness, so it needs the same
Dalamud SDK the tests do (`DALAMUD_HOME`).

```
dotnet build tools/AnoMech.LessonTools -p:GenerateSupportedOSPlatformAttribute=false
TOOL=$(find tools/AnoMech.LessonTools/bin -name AnoMech.LessonTools.dll | head -1)

# where a scenario stands before any authoring: what the chain can read from it and how it degrades without each piece
dotnet $TOOL learn-ready --scenario UwuP2IfritScenario [--strat 0] [--no-run]
# pin each roll to the value a free run drew and see whether the seed's other rolls move (also part of learn-ready)
dotnet $TOOL overrides-check --scenario UwuP1GarudaScenario [--strat 0] [--seed 1]
# the scenario x strat matrix: lesson, draft, steps, registered plan, toolbox block, embedded plan, example seed/example, glossary, mit timeline/plan
dotnet $TOOL status [--family Uwu]

# one command per strat: record -> skeleton -> rule miner -> cue extractor -> draft(glossary) [-> raidplan import -> oracle -> example pinned]
# writes <out>/<Scenario>.<strat>.{dump.json,lesson.json,context.md}; the plan link and layout come from AnoMech/Learn/Toolbox/plans.json
# unless --plan/--layout say otherwise; --lesson lends an existing lesson's toolbox example, which is then recorded pinned and scored
dotnet $TOOL author --scenario UwuP1GarudaScenario --strat 0 --seeds 1..12 --out work/ [--plan <link|id|file> --layout Naur] [--lesson existing.json]
# the same for every catalog strat without an embedded lesson, in the Lessons folder layout
dotnet $TOOL author-all --out work/all --seeds 1..12 [--family Uwu | --scenario Uwu] [--layout Naur] [--plans "UwuP3TitanScenario=463534803605261"]
# draft the text again from an existing dump (after a miner or glossary change)
dotnet $TOOL draft --dump x.dump.json --lesson x.lesson.json --out x.lesson.json [--zone UwuZone --layout Naur]

# the pieces, when one is needed on its own:
dotnet $TOOL record   --scenario UwuP1GarudaScenario --strat 0 --seeds 1..8 --out garuda.dump.json [--example "SuparnaSongBearing=270;..." | --lesson lesson.json]
dotnet $TOOL mine     --dump garuda.dump.json --zone UwuZone --layout Naur
dotnet $TOOL cues     --dump garuda.dump.json [--window 8]
dotnet $TOOL oracle   --plan AnoMech/Learn/Toolbox/Plans/122994122467471.json --lesson <lesson.json> --dump garuda.dump.json [--write] [--pinned-dump pinned.dump.json]
dotnet $TOOL skeleton --dump garuda.dump.json --out lesson.json
dotnet $TOOL rekey    --lesson AnoMech/Learn/Lessons/Uwu/P1Garuda.zeal-the-balance.json --dump garuda.dump.json
dotnet $TOOL check    --lesson AnoMech/Learn/Lessons/Uwu/P1Garuda.zeal-the-balance.json --dump garuda.dump.json
dotnet $TOOL context  --dump x.dump.json --lesson x.lesson.json --out x.context.md [--plan plans/<id>.json]
# the plan's roll as the lesson's example: adds -> spot lists / bearings, buffs -> role lists; --write stores it (author does this when the lesson has none)
dotnet $TOOL example  --plan plans/<id>.json --lesson x.lesson.json --dump x.dump.json [--write]

# raidplans: any of these links becomes the same ToolboxPlan (viewer, alignment, oracle, example); calibration by waymarks A-D
#   ff14.toolboxgaming.space/?id=<id>   raidplan.io/plan/<code>   ffxivstrats.io/fight/<fight>?strategy=<id>   pasted "[stgy:...]" codes (one board per step)
#   xivplan.netlify.app/#/plan/<scene> or a .xivplan file
dotnet $TOOL toolbox-import --plan <link> --zone UwuZone --layout Naur [--lesson <lesson.json> --dump garuda.dump.json --write] --out-plan AnoMech/Learn/Toolbox/Plans/<id>.json

# text sources
#   WTFDIG (github.com/mczub/wtfdig, MIT): data.ts + mits.ts converted to JSON without running code, embedded under Learn/Sources/wtfdig/<fight>.json;
#   author lists the fight's mechanic texts in the context and credits it in the lesson's sources
dotnet $TOOL wtfdig-fetch --fight umad [--path src/routes/ultimates/umad] [--branch main] [--out AnoMech/Learn/Sources/wtfdig/umad.json]
#   a public Google Slides deck: text, notes and images per slide; XIVPlan links in the notes become plans when --zone is given
dotnet $TOOL slides-fetch --deck <google id|url|file.pptx> --out work/deck [--zone FruZone --layout "Inner Circle"]

# mitigation: cast timeline per scenario (embedded under Learn/Mitigation/Timelines), then a plan from a sheet, a CSV or WTFDIG
dotnet $TOOL mit-timeline --all [--check]        # every scenario; CI (MitTimelinesTests) fails when a run drifts from its embedded timeline
dotnet $TOOL mit-timeline --scenario UwuP1GarudaScenario --out AnoMech/Learn/Mitigation/Timelines/UwuP1GarudaScenario.json
dotnet $TOOL mit-import --plan uwu-ethel-master --sheet-id <google sheet id> --tabs "Garuda=UwuP1GarudaScenario,Ifrit=UwuP2IfritScenario" --credits "A;B" --out AnoMech/Learn/Mitigation/Plans/uwu-ethel-master.json
dotnet $TOOL mit-import --plan <name> --csv exported.csv --scenario UwuP1GarudaScenario --out Plans/<name>.json
dotnet $TOOL mit-import --plan umad-ikuya --wtfdig umad --mit-plan ikuya --tabs "p1=UmadP1TeleTrouncingScenario,p3=UmadP3LimitCutScenario|UmadP3BlackHoleScenario" --accept-unresolved --out Plans/umad-ikuya.json
#   --accept-unresolved writes the rows still unresolved into the plan's knownUnresolved (read them first); CI fails on any other
```

Per-step authoring context (role spots, cue candidates, strat method): `~/.claude/skills/asumech-lesson`.
The whole plan, phases and checklist: `tools/learn-authoring-plan.md`.

Reviewing in game: set the dev folder in Learn → Authoring… to the repo's `AnoMech/Learn/Lessons` (or a
copy); edits to the JSON reload within a second, also mid-run. The window shows the current order's key,
what the sim showed before it, and the run's rolls.

Flow for a new scenario: `learn-ready` → register the plan link in `plans.json` → `author` (or
`author-all --family`) → read the context, rewrite the drafted `rule`/`cue` in player language where
the draft is a table → `check` → embed under `AnoMech/Learn/Lessons/<Family>/` with `draft: true`
until reviewed in game. A reviewed lesson (`draft: false`) may not keep a "Depends on …" rule; the
family's `glossary.json` words State members and values so fewer rules come out as tables.
