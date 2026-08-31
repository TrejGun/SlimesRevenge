# AGENTS.md

Guidance for AI agents working in **Slime's Revenge** (Unity mobile game).

## Repository layout

```
Assets/
  Scripts/
    Runtime/              # Gameplay (one asmdef: SlimesRevenge.Runtime)
      App/                # Menu, action log, popups, cards, run config
      Behavior/           # Unity Behavior graphs + Decide / Hunt / Moves
      Creatures/          # Slime, Bat, pack enemies, catalogs (retired: Rat/Cat/Dog/Scorpion)
      Localization/       # I18n + TextKey
      Status/             # StatusEffect hierarchy (innate / body / timed)
      Turns/              # GameSession, TurnManager, Combat, cell menu
      Volume/             # Substances, Volume, dominance, floor puddles
      World/              # Grid, terrain, GridPath (A* Pathfinding Project), Floor
    Editor/               # Build / play helpers (MobileBuilder, RunConfigPlayModeBridge)
  Tests/Editor/           # EditMode tests, mirrored by feature
  AstarPathfindingProject/# Aron Granberg A* Pathfinding Project (Free)
  Scenes/                 # Splash (bootstrap) → Main (menu) → Game
  Localization/Resources/ # en / ru string tables
  Resources/              # Runtime-loaded sprites (animals, status icons)
docs/                     # Design / architecture notes (human + agent)
scripts/                  # play.sh, stop.sh, build.sh, format.sh
.github/workflows/        # GameCI EditMode tests + mobile builds
.cursor/rules/            # Cursor agent rules (see below)
```

Domain docs (read before changing the matching feature):

| Topic | Doc |
| --- | --- |
| Dependency inversion (statuses + world↛player) | [docs/architecture/dependency-inversion.md](docs/architecture/dependency-inversion.md) |
| Mob AI (personality vs fear/hate) | [docs/behavior.md](docs/behavior.md) |
| Status effects | [docs/status-effects.md](docs/status-effects.md) |
| Strike vs puddle | [docs/strike-vs-puddle.md](docs/strike-vs-puddle.md) |
| Slime volume dominance | [docs/slime-dominance.md](docs/slime-dominance.md) |
| Action log | [docs/architecture/action-log.md](docs/architecture/action-log.md) |
| Popup cards | [docs/architecture/popup-cards.md](docs/architecture/popup-cards.md) |

## Cursor rules

Project rules live in [`.cursor/rules/`](.cursor/rules/). Open or edit matching files so they attach by glob:

| Rule | Intent |
| --- | --- |
| [`.cursor/rules/behavior-astar-architecture.mdc`](.cursor/rules/behavior-astar-architecture.mdc) | Mob turn AI: **Unity Behavior** chooses *what*, **A* Pathfinding Project** chooses *where*. No DIY chase heuristics. |
| [`.cursor/rules/dependency-inversion.mdc`](.cursor/rules/dependency-inversion.mdc) | Statuses apply via hooks; board/occupied does not require a controlled player. |
| [`.cursor/rules/fdr-enemy-sprite-generation.mdc`](.cursor/rules/fdr-enemy-sprite-generation.mdc) | FDR enemy sheets: GenerateImage + verbatim rat prompt; larger raster OK. |
| [`.cursor/rules/play-mode-unity.mdc`](.cursor/rules/play-mode-unity.mdc) | Play Mode: `open -na`, never a child of the agent shell. |

## Board vs controlled

- **Board** = `World` + unified `GameSession.occupied` (every living body).
- **Controlled** = optional input (`TurnManager.Controlled` / `ControlledCell`). Hunt sims use board-only + `TakeTurn(..., player: null)`.
- Details: [docs/architecture/dependency-inversion.md](docs/architecture/dependency-inversion.md) (World ↛ player).

## Engineering discipline

- Prefer the smallest change that matches existing patterns in `Assets/Scripts/Runtime`.
- Do not invent parallel AI brains, custom A* grids, or combat `if (status is Poisonous)` switches when hooks already exist.
- Keep personality → slime only; fear/hate → mob↔mob only. There is **no** `FearsSlimes` status.
- Do not pad maps or hide a fake slime to “disable” Aggressive during mob↔mob hunts — omit the player instead.
- Address the user as **Oleg** when chatting in this project (user preference).

## Unity version

**Unity 6000.5.9f1**. Bundle id: `com.trejgun.slimesrevenge`.

Default local editor path on this machine:

```text
/Applications/Unity/Hub/Editor/6000.5.9f1/Unity.app/Contents/MacOS/Unity
```

Override with `UNITY_EDITOR` for builds (`scripts/build.sh`).

## Play in Editor (agents)

**Always** use these scripts. Do **not** invoke Unity `-executeMethod` by hand or scrape `Logs/play-*.log` to decide if Play Mode is up — `play.sh` already waits for readiness and returns an exit code.

The Cursor **agent shell is not a place to own the editor**. Starting `Unity &` there dies with the turn (SIGHUP) while stdout still said `READY`. `play.sh` therefore launches via **`open -na` Unity.app** (LaunchServices), outside that shell. Rule: [`.cursor/rules/play-mode-unity.mdc`](.cursor/rules/play-mode-unity.mdc). If the user says Unity is not open, believe them and run `play.sh` again.

```bash
./scripts/play.sh              # Splash → Main menu
./scripts/play.sh --duel       # Game duel (Bat, default water loadout)
./scripts/play.sh --campaign   # Game campaign (10×10, full cast)
./scripts/stop.sh              # quit this project's Unity Editor (finds by -projectPath; no PID file)
```

| Exit code | Meaning |
| --- | --- |
| `0` | Ready — stdout contains `READY: … ([PlayReady] …)`. Unity stays running in the background. |
| non-zero | Fail / timeout / wrong mode — see the `FAIL:` line; details in `Logs/play-*.log`. |

`stop.sh` exit codes:

| Exit code | Meaning |
| --- | --- |
| `0` | Stopped (or already not running) — stdout `STOPPED: …` |
| non-zero | Still running after TERM/KILL — stdout/stderr `FAIL: …` |

- Ready markers (written by the game/editor, consumed only by `play.sh`): `[PlayReady] Splash`, `[PlayReady] Duel 15x5`, `[PlayReady] Campaign 10x10`.
- `play.sh` stops any prior editor for this project, clears `Temp/__Backupscenes` + `Assets/_Recovery`, then launches.
- Optional: `PLAY_READY_MAX_SEC` (default `300`), `UNITY_EDITOR`.
- `RunConfig` for `--duel` / `--campaign` survives play-mode domain reload via Editor `SessionState` (`RunConfigPlayModeBridge`) — **no disk files**.

Agent rules:
- After `./scripts/play.sh …`, trust exit `0` + the `READY:` line. Do **not** poll Unity logs yourself.
- After `./scripts/stop.sh`, trust exit `0` + `STOPPED:`. Do **not** `pgrep` / scrape logs to double-check.
- If the **user** says Play Mode / the editor is not open, that overrides `READY` from an earlier turn. Run `play.sh` again. Do not argue from `pgrep`.

## Setup

1. Open the project in Unity Hub with editor **6000.5.9f1**.
2. Wait for script compile (packages: Unity Behavior, A* Pathfinding Project under `Assets/`).
3. Activate a Unity license for batchmode / CI.
4. (Optional) C# formatting — needs a local **dotnet** SDK **8+** (CSharpier 1.x):

```bash
export PATH="$HOME/.dotnet:$PATH"   # if dotnet is only under ~/.dotnet
dotnet tool restore
./scripts/format.sh          # format Assets/Scripts + Assets/Tests
./scripts/format.sh check    # CI-style check, no writes
```

Enable the pre-commit hook once per clone:

```bash
git config core.hooksPath .githooks
```

Config: [`.csharpierrc.json`](.csharpierrc.json), ignore: [`.csharpierignore`](.csharpierignore). Vendor tree `Assets/AstarPathfindingProject/` is excluded.

## Testing (EditMode)

Tests are NUnit EditMode under `Assets/Tests/Editor`. CI runs them via GameCI (`.github/workflows/test.yml`).

### Full EditMode suite (local batchmode)

```bash
UNITY="/Applications/Unity/Hub/Editor/6000.5.9f1/Unity.app/Contents/MacOS/Unity"
"$UNITY" -batchmode -nographics \
  -projectPath "$(pwd)" \
  -runTests -testPlatform EditMode \
  -testResults "$(pwd)/Logs/editmode-results.xml" \
  -logFile "$(pwd)/Logs/editmode.log"
```

### Focused filter

```bash
UNITY="/Applications/Unity/Hub/Editor/6000.5.9f1/Unity.app/Contents/MacOS/Unity"
"$UNITY" -batchmode -nographics \
  -projectPath "$(pwd)" \
  -runTests -testPlatform EditMode \
  -testFilter "SlimesRevenge.Tests.BehaviorRulesTests" \
  -testResults "$(pwd)/Logs/focused-results.xml" \
  -logFile "$(pwd)/Logs/focused.log"
```

### Test output (agents)

- Prefer writing Unity logs under `Logs/` with `-logFile` / `-testResults`.
- Do **not** use `head` / `tail` to truncate the only evidence of a run; use `tee` if you need a copy, then summarize from the XML/log file.
- Never claim green without the results XML (or full log) for that run.

Useful focused suites while touching AI / pathfinding:

| Filter prefix | Covers |
| --- | --- |
| `SlimesRevenge.Tests.BehaviorRulesTests` | Duel approach / personality contract |
| `SlimesRevenge.Tests.BehaviorGraphBindOwnerProofTests` | Behavior bind / Actor |
| `SlimesRevenge.Tests.EnemyHuntSimulationTests` | Corridor / maze / DoT doors |
| `SlimesRevenge.Tests.GridPathTests` | A* grid paths |
| `SlimesRevenge.Tests.PathfindingPursuitTests` | Pursuit memory, puddle penalties |

## Builds

```bash
./scripts/build.sh Android   # -> Build/Android/SlimesRevenge.apk
./scripts/build.sh iOS       # -> Build/iOS (Xcode project)
```

See [README.md](README.md) for CI secrets and GameCI workflows.

## Review checklist (AI / combat / status)

Before finishing a change in Behavior, World pathfinding, Combat, or Status:

1. Does Decide still go through Unity Behavior graphs (`CreaturePolicyGraphs` / `CreatureBrain`)?
2. Does movement still go through `GridPath` → A* Pathfinding Project (no ring-scan “best toward” chase)?
3. Did Combat / CreatureMoves gain a new `if (status is X)`? If yes, move the behavior onto `StatusEffect` hooks instead.
4. Are fear/hate still status-driven (`IsPrey` / `IsPredator`) rather than hardcoded in Combat?
5. Did you run the focused EditMode filter for the area you touched?
