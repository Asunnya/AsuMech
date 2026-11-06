# BossMod no AsuMech: plano de ferramentas (branch `bossmod-tooling`, de `learn-module`)

Objetivo: trazer para o AsuMech o que o BossMod (`awgil/ffxiv_bossmod`) e o BossMod Reborn
(`FFXIV-CombatReborn/BossModReborn`) fazem bem no estudo e na validação de uma luta, sem mudar o
que o AsuMech é: a sim roda a strat, a lição descreve a strat. Os dois são consumidos como dados
(módulos em C# lidos como texto, replays, formas), nunca como dependência de runtime. Licença BSD-3
nos dois; o que for portado em código (B5) ganha crédito em `NOTICE`.

Seis entregas, em ordem de retorno:

| | Entrega | O que resolve |
|---|---|---|
| B0 | `bossmod-sync`: módulos deles → JSON por luta; cruzamento no `learn-ready`; nomes para pistas | hoje o cruzamento de IDs, formas e tempos é manual |
| B1 | Gravador de pull real dentro do plugin, no formato da `SimTimeline` | o log do ACT não guarda EObj, EnvControl, MapEffect e posição por frame |
| B2 | `replay-diff`: sim headless × gravação real, cast a cast | o espelho da validação de módulo deles; hoje só "sem mortes" |
| B3 | Zonas de hazard no overlay Assisted e "você tomou X porque estava em Y" | o que mais ensina, e a sim já sabe cada área |
| B4 | Timeline de próximas mecânicas no HUD de prática | as timelines por cenário (F3) já existem, só não aparecem em jogo |
| B5 | Grid de navegação com zonas proibidas para os bots | roteamento em volta de hazard escrito à mão por cenário (tornados do M11S) |
| B6 | `scenario-skeleton`: `*Constants`, `*Actions` e timeline do `*Scenario` a partir do log | o equivalente da geração de enums deles; `asumech-log-mechanic` já extrai os fatos |

## 1. Onde estamos

Já existe, e o plano constrói em cima:
- `tests/Support/SimTimeline.cs`: toda chamada em forma de pacote que a sim faz às fakes vira uma
  linha CSV com colunas fixas (`t, kind, actor, id, name, target, x, z, rot, detail, omen_delay,
  …, source`), pensada para diff contra um replay. O comparador e o `replay_timeline.py` que ela
  cita não estão no repo; B1 e B2 recolocam esse caminho.
- `PacketCaptureDebug` (DEBUG) já hooka o `PacketDispatcher` e despeja cada pacote de zona; os
  ponteiros estão em `Native/Implementations/Pointers/PacketDispatcherPointers.cs`.
- `tools/replay_export_duty_init.py` lê replays `.dat` do ARealmRecorded achando pacotes por
  forma, não por opcode (opcodes mudam a cada build).
- `Learn/Mitigation/CastTimeline` + `MitTimelines` (F3): timeline de casts de todos os 64 cenários
  embutida, `Drift` com folga de 0,25 s, teste por cenário.
- `EnemyAction.AreaSpec`: forma lida da Action sheet (`CastType`, alcance), só a dimensão que a
  sheet não tem vem do cenário. `DeathExplanation` e `ActionResolved` já carregam o porquê.
- `ObstacleField` + `Navigator`: desvio por steering em volta de obstáculos circulares; sem grid,
  sem zonas temporárias de AoE.
- `EventScheduler.Add` guarda `file:line` de cada entrada; a timeline é opaca (Actions), por
  isso B4 lê a `CastTimeline` gravada, não a fila.
- `GuidanceOverlay` desenha anel e seta em espaço de mundo; `FailureBanner` mostra a razão da falha.
- CLI `tools/AnoMech.LessonTools` com 23 comandos; o projeto referencia o de testes
  (`ScenarioRun`, `OverridesCheck`), então rodar cenário headless de dentro da CLI já é possível.

## 2. O que cada fonte deles dá, e o que não dá

- **Módulo** (`<Fight>Enums.cs`, `<Fight>States.cs`, componentes): `AID`/`SID`/`IconID`/`TetherID`
  com comentário por entrada (quem casta, para quem, cast time, forma), `StateMachineBuilder`
  com duração e nome por estado. Posição por role: quase nada. Os dois repos discordam entre si
  com frequência (M11S: Heavy Hitter 22,5° × 15°, tower knockback 23 × 20); entram como hint a
  confrontar com log, nunca como verdade. Caminhos: awgil
  `BossMod/Modules/<Expansion>/<Category>/<Fight>/`, Reborn `BossMod/Modules/<Expansion>/<Category>/<Fight>/`.
- **Replay deles** (`ReplayRecorder`, aberto no `UIDev`): estado completo por frame. Útil quando o
  usuário já grava com o BossMod; B1 é o nosso equivalente para não depender disso.
- **AIHints / NavigationDecision**: zona proibida + meta → caminho num grid. Modelo "não morra",
  não substitui strat. Só o pathfinding interessa (B5).
- **Fora**: autorrotação, dano real, hints por role do Reborn como fonte de strat.

## 3. Fases

### B0. `bossmod-sync` (2 dias)
- `tools/AnoMech.LessonTools`: comando `bossmod-sync --fight <nome> [--repo awgil|reborn|both]`.
  Sparse clone dos dois repos no cache da CLI (`obj/bossmod/<commit>/`, como o datamining dos
  testes), caminho do módulo por tabela em `Learn/Sources/bossmod/fights.json`
  (família → pasta em cada repo).
- Parser de texto, sem Roslyn: enums `AID`, `SID`, `IconID`, `TetherID` (valor + comentário),
  `StateMachineBuilder` (nome, duração, tipo: Cast/CastStart/ComponentCondition/Timeout, phase),
  `AOEShape*` dos componentes com seus parâmetros. Saída `Learn/Sources/bossmod/<fight>.json`
  com o commit de origem, um bloco por repo.
- `learn-ready --scenario X` ganha a coluna BossMod: para cada `EnemyAction` da família, o id
  bate em alguma enum, cast time igual ao comentário deles, forma e alcance iguais à sheet e ao
  componente, duração do estado igual ao intervalo entre casts na `CastTimeline` do cenário.
  Divergência entre awgil e Reborn sai marcada como tal.
- `cues`/`draft` passam a nomear a pista pelo nome da enum deles quando o jogo não dá um nome
  (head markers, tethers, EnvControl), e sugerem entradas de glossário.
- Testes: fixture de um módulo pequeno de cada repo em `tests/.../Learn/Sources/Fixtures`,
  parser determinístico, `learn-ready` com e sem JSON. Primeira família: M11S, já cruzada à mão
  (memória `m11s-scenario-project.md`), para medir quanto o parser acha sozinho.

### B1. Gravador de pull real (3 dias)
- `Core/Native/Implementations/PullRecorder.cs` (Release, ligado por toggle no menu Debug,
  ativo só em território de ultimate/savage e só quando não há run da sim). Hooka o que já
  temos no `PacketDispatcher` e os handlers do cliente que a sim já emula (ActionEffect,
  ActorControl, EffectResult, StatusList, EventPlay, EnvControl/MapEffect, DirectorUpdate, spawn e
  despawn de objetos) e escreve uma linha por evento nas **mesmas colunas da `SimTimeline`**, mais
  linhas `pos` a 10 Hz com posição e rotação de cada BattleChara e EObj visível.
- Pacotes achados por forma (tamanho, remetente, conteúdo), como o `replay_export_duty_init.py`,
  para sobreviver à troca de opcodes; os que não reconhecer saem como `raw` com hex, como o
  `PacketCaptureDebug` já faz.
- Arquivo em `datalogs/replays/<zona>_<data>.csv` (pasta já ignorada pelo git). `SimTimeline`
  sai de `tests/Support` para `Core/Game/Trace` (ou `Learn/Tools`) e vira o escritor único, para
  que gravação real e gravação da sim nunca divirjam em formato.
- Testes: escritor com eventos sintéticos; leitor de uma gravação curta de fixture.

### B2. `replay-diff` (2 dias)
- Comando `replay-diff --scenario X --strat i --replay <csv> [--pull n]`: roda o cenário headless
  com `SimTimeline` ligada, alinha a gravação pelo primeiro cast do boss (ou por `--t0`), e emite
  por cast: diferença de tempo, de posição de origem, de rotação, de alvo; casts só de um lado;
  EObjs e EnvControls que a sim não manda. Saída em tabela e em JSON.
- Teste `[Explicit]`/flag de ambiente (mesmo esquema dos testes que dependem de `datalogs/`, ver
  memória `feedback-write-tests.md`): para cada cenário com gravação em `datalogs/replays/`, drift
  de cast dentro de 0,25 s e nenhum cast faltando. Fica fora da CI pública porque depende de logs
  locais; roda na máquina do usuário.
- Mesmo leitor aceita a `CastTimeline` do F3, para que a gravação real também atualize a timeline
  embutida (`mit-timeline --from-replay`).

### B3. Hazards no overlay e explicação da falha (3 dias)
- Core, mínimo: `SimWorld.Hazards` (somente leitura) enumera os `EnemyActionCast` pendentes com
  origem, rotação, `AreaSpec` resolvida e instante de resolução. Nada de lógica nova em Core: é a
  mesma informação que o handler já usa para acertar, exposta.
- `Windows/Learn/HazardOverlay.cs`: no nível Assisted (e opcional no Guided), desenha em espaço
  de mundo as áreas ativas (círculo, cone, linha, donut, cruz) com a cor por severidade e a
  contagem até resolver; o spot do passo fica por cima. Projeção reaproveita a do anel do
  `GuidanceOverlay`.
- Falha: quando `ActionResolved` ou morte atinge o jogador, o `LessonReport` grava o passo, a área
  (nome da ação como o jogo mostra), onde o jogador estava e onde o passo mandava; o
  `FailureBanner` ganha a frase curta ("Sweeping Victory: você estava a 2 m dentro do cone").
- Config: `Config.ShowHazards` por nível. Testes: `Hazards` headless num cenário com cone e donut;
  texto da falha a partir de um `ActionResolved` sintético.

### B4. Timeline no HUD (1 dia)
- `Windows/Learn/TimelinePanel.cs`: lista dos próximos N casts da `CastTimeline` embutida do
  cenário, relativa a `Events.Elapsed`, com contagem; marca o passo da lição ligado a cada cast
  quando a chave do passo cai na janela. Só leitura, nenhuma mudança em Core.
- Config: ligar por nível, N. Teste: janela de próximos casts para um instante dado.

### B5. Grid de navegação (3 a 4 dias)
- `Core/Game/Geometry/NavGrid.cs`: grid de 0,5 m sobre a arena (`IArenaShape`), células marcadas
  por `ObstacleField` e pelas áreas de `SimWorld.Hazards` cujo instante de resolução cai antes de
  o bot chegar; busca de caminho com custo por proximidade de hazard, porta do
  `NavigationDecision` do BossMod (BSD-3, crédito em `NOTICE`).
- `Navigator`: quando o destino ou o segmento cruza célula bloqueada, segue o caminho do grid;
  caso contrário, o steering atual. Flag por bot (`SimulatedMotion.AvoidHazards`), ligada por
  padrão para bots, nunca para o puppet remoto.
- Primeiro consumidor: tornados do M11S (`M11sUltimateWeaponsAi`), tirando os saltos manuais.
  Critério: `ScenarioCatalogTests` continua sem mortes, tempo de caminho não cresce mais de 10%.
- Testes: caminho em arena com um e dois obstáculos; hazard que expira antes da chegada não
  desvia; `DebugBotReplayTests` continua verde.

### B6. `scenario-skeleton` (2 dias por família nova, 3 de infra)
- Comando `scenario-skeleton --log <acompanhante> --pull n --zone <id> --family X --phase Y`:
  usa os fatos que o `asumech-log-mechanic` já extrai (casts, hits, statuses, markers, tethers,
  spawns, posições) e a Action sheet do datamining para emitir:
  `<Family>Constants.cs` (ids com nome do jogo), `<Family>Actions.cs` (um `EnemyAction` por cast,
  forma da sheet, `Size` marcado UNVERIFIED quando a sheet não tem), e `<Phase>Scenario.cs` com
  `world.Events.Add` em ordem e tempo observados, spawns com coordenadas literais e `TODO` nos
  pontos que dependem de roll. `*Ai` e `*State` ficam para o autor.
- Quando B0 estiver pronto, o esqueleto nasce com as enums deles como segunda opinião: nome e
  forma marcados onde awgil, Reborn e sheet não concordam.
- Testes: esqueleto gerado de um pull de fixture compila e passa no `check` de forma; o
  `replay-diff` (B2) do esqueleto contra o próprio pull tem drift zero por construção.

## 4. Ordem e dependências

```
B0 ──────────────┐
B1 ─► B2 ─────────┼─► B6
B3 ─► B5          │
B4 (independente) ┘
```
Começar por B0, B1 e B3 em paralelo: os três reaproveitam o que a sim já sabe e abrem B2, B5 e
B6. B4 cabe em qualquer buraco.

## 5. Riscos

- **Opcodes e layouts mudam a cada build** (B1). Mitigação: achar por forma, `raw` para o resto,
  teste que falha alto quando um pacote conhecido para de ser reconhecido.
- **Repos deles mudam de pasta e de estilo** (B0). Mitigação: tabela de caminhos por luta com o
  commit pinado, parser tolerante que lista o que não entendeu em vez de abortar.
- **Core engordar por causa de ferramenta** (B3, B5). Regra: Core só expõe o que já calcula
  (`Hazards`) e ganha um algoritmo de caminho; desenho, texto e diff ficam em `Windows/`, `Learn/`
  e na CLI.
- **Hazard pintado errado ensina errado** (B3). A área vem da mesma `AreaSpec` que acerta o
  jogador; se a pintura discorda do hit, o bug é da sim e aparece no `replay-diff`.
- **Pathfinding muda onde os bots param** (B5). Caminho só muda a rota, nunca o destino da strat;
  os testes de catálogo e de paridade dos bots seguram isso.
- **Proveniência no código**: nada de "segundo o BossMod" em comentário (CLAUDE.md); fica no
  JSON de B0, nas notas de sessão e na memória.

## 6. Fora do escopo

Autorrotação e dano real; AI de "evite hazard" como substituto de strat para o jogador ou para
os bots; hints por role do Reborn como fonte de posição; viewer offline de arena (fica para a
branch do redesign, que já tem o canvas; o JSON de dump da CLI é a entrada dele).
