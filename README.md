# Slime's Revenge

Mobile Unity game (iOS + Android). After the splash and title screen the app loads a 10×10 grass world with a slime in the center.

Unity **6000.5.9f1**. Bundle id: `com.trejgun.slimesrevenge`. Portrait.

## Local build

Install Unity 6000.5.9f1 with the **Android** and/or **iOS** modules, activate a license in Unity Hub, then:

```bash
./scripts/build.sh Android   # -> Build/Android/SlimesRevenge.apk
./scripts/build.sh iOS       # -> Build/iOS (Xcode project)
```

Override the editor path with `UNITY_EDITOR` if needed.

This machine currently has the editor without mobile modules, and batchmode needs an active Unity license. CI is the path that actually produces Android/iOS artifacts until those are installed locally.

## CI (GitHub Actions)

Default branch is **`dev`** (day-to-day work). **`main`** is for releases.

Two workflows ([GameCI](https://game.ci/docs/github/getting-started)):

| Workflow | When | What |
| --- | --- | --- |
| [`test.yml`](.github/workflows/test.yml) | Push to `dev`, PR → `dev`/`main`, manual | EditMode tests only |
| [`build.yml`](.github/workflows/build.yml) | Push to `main`, manual on `main` | Tests, then Android + iOS |

EditMode steps live once in [`unity-editmode.yml`](.github/workflows/unity-editmode.yml) (reusable `workflow_call`); both workflows call it with `secrets: inherit`.

A new commit on the same branch **cancels** the previous in-progress run for that workflow.

Feature branches: open a PR into `dev` (or `main`) — only `Test` runs.

Add repository secrets (Personal license):

1. `UNITY_LICENSE` — contents of local `Unity_lic.ulf` (already set from this machine).
2. `UNITY_EMAIL` — Unity account email (already set).
3. `UNITY_PASSWORD` — Unity account password (already set).

One-shot renewal helper: [`.github/workflows/activation.yml`](.github/workflows/activation.yml) (workflow_dispatch → `.alf` artifact → https://license.unity3d.com → new `.ulf`).

See [GameCI activation](https://game.ci/docs/github/activation).

Artifacts (only from `Build` on `main`):

- `slimesrevenge-Android` — debug-signed APK (installable on a device)
- `slimesrevenge-iOS` — exported Xcode project (sign and archive on a Mac for TestFlight/App Store)

## Project layout

- `Assets/Scenes/Main.unity` — title after splash, then loads `Game`
- `Assets/Scenes/Game.unity` — 10×10 grass world, slime in the center, rat/cat/dog beside it
- `Assets/Art` — characters, UI, terrain sheets and blob `Tile` assets
- `Assets/Localization/Resources` — English and Russian string tables (`Resources.Load`, no Addressables)
- `Assets/Scripts/Runtime` — gameplay, one asmdef, grouped by feature:
  - `App` — title hold and scene load
  - `World` — grid, terrain, blob tiles, `WorldView`, Chebyshev pathfinding via built-in NavMesh (`com.unity.modules.ai`)
  - `Creatures` — `Creature` with vision, speed, and a stored behavior pattern; combat switches acting to aggressive
  - `Volume` — units and substances
  - `Status` — timed effects on a creature (permanent = duration never ends)
  - `Turns` — `GameSession`, `TurnManager`, input, combat
  - `Behavior` — turn-based mob patterns (`Cowardly`, `Passive`, `Aggressive`); Unity Behavior graph nodes in `Actions/` and `Conditions/`
  - `Localization` — `I18n` + `TextKey`
- `Assets/Scripts/Editor` — `Build/` (Android / iOS / OSX) and `Art/` (`Create Terrain Tiles`)
- `Assets/Tests/Editor` — EditMode tests, mirrored by feature
