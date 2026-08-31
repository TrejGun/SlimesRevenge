# Slime's Revenge

Mobile Unity game (iOS + Android). Bundle id: `com.trejgun.slimesrevenge`.  
Unity **6000.3.23f1**. Landscape.

Flow: `Splash` → `Main` (menu) ↔ `Game` (campaign or duel).

## Play in Editor

```bash
./scripts/play.sh              # Splash → Main; exits 0 when ready
./scripts/play.sh --duel       # default duel; exits 0 when arena is up
./scripts/play.sh --campaign   # default campaign; exits 0 when arena is up
./scripts/stop.sh              # quit editor; exits 0 when gone
```

`play.sh` starts Unity in the background, waits for a `[PlayReady]` line in `Logs/play-*.log`, then exits (0 = ready, non-zero = fail/timeout). Trust the exit code — do not scrape the log yourself. Override wait with `PLAY_READY_MAX_SEC` (default 300).

`stop.sh` finds Unity by `-projectPath` (no PID file, no logs) and exits 0 when it is gone.

## Local build

```bash
./scripts/build.sh Android   # -> Build/Android/SlimesRevenge.apk
./scripts/build.sh iOS       # -> Build/iOS (Xcode project)
```

## C# formatting

```bash
dotnet tool restore
./scripts/format.sh          # format Assets/Scripts + Assets/Tests
./scripts/format.sh check    # CI-style check
git config core.hooksPath .githooks   # once per clone
```

## CI

Default branch: **`dev`**. Releases: **`main`**.

| Workflow | When | What |
| --- | --- | --- |
| [`test.yml`](.github/workflows/test.yml) | Push/PR to `dev`/`main` | EditMode tests |
| [`build.yml`](.github/workflows/build.yml) | Push/`workflow_dispatch` on `main` | Tests + Android + iOS |

Secrets: `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD` — see [GameCI activation](https://game.ci/docs/github/activation).

## Layout

- `Assets/Scenes` — Splash, Main, Game
- `Assets/Scripts/Runtime` — gameplay (one asmdef)
- `Assets/Scripts/Editor` — build / play helpers
- `Assets/Tests/Editor` — EditMode tests
- `Assets/ElvGames` — Fantasy Dreamland tilesets
- `docs/` — design notes
- `scripts/` — `play.sh`, `build.sh`, `format.sh`
