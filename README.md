# Slime

Mobile Unity game (iOS + Android). After the splash screen the app shows a static title image.

Unity **2021.3.24f1**. Bundle id: `com.trejgun.slime`. Portrait.

## Local build

Install Unity 2021.3.24f1 with the **Android** and/or **iOS** modules, activate a license in Unity Hub, then:

```bash
./scripts/build.sh Android   # -> Build/Android/slime.apk
./scripts/build.sh iOS       # -> Build/iOS (Xcode project)
```

Override the editor path with `UNITY_EDITOR` if needed.

This machine currently has the editor without mobile modules, and batchmode needs an active Unity license. CI is the path that actually produces Android/iOS artifacts until those are installed locally.

## CI (GitHub Actions)

[`.github/workflows/build.yml`](.github/workflows/build.yml) runs EditMode tests, then builds Android (APK) and iOS (Xcode project) with [GameCI](https://game.ci/docs/github/getting-started).

Add repository secrets (Personal license):

1. Open Unity Hub, activate the license once, then copy the license file contents into `UNITY_LICENSE`.
2. Set `UNITY_EMAIL` and `UNITY_PASSWORD` for the Unity account.

See [GameCI activation](https://game.ci/docs/github/activation).

Artifacts:

- `slime-Android` — debug-signed APK (installable on a device)
- `slime-iOS` — exported Xcode project (sign and archive on a Mac for TestFlight/App Store)

## Project layout

- `Assets/Scenes/Main.unity` — full-screen `Title.png` after splash
- `Assets/Editor/MobileBuilder.cs` — local Android/iOS build methods
- `Assets/Tests/Editor` — smoke tests for scene, art, and bundle ids
