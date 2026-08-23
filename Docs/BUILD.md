# Build Guide — iOS / Android

Prereq: Unity project created (Unity Hub, version per `ProjectSettings/ProjectVersion.txt` once present), then run `Diamond Tilt → Bootstrapper` menu (`Assets/Scripts/Editor/DiamondTiltBootstrapper.cs`).

## Player settings (both platforms)

- Scripting Backend: **IL2CPP**; Api Compatibility: **.NET Standard 2.1**
- Managed Stripping: Medium (add `link.xml` preserving `DiamondTilt.Core` if reflection-free code still gets stripped)
- Orientation: Landscape Left/Right only
- Target framerate: 60 (`Application.targetFrameRate = 60` in bootstrap)
- Color space: Linear; URP or Built-in acceptable for sprite work — do not switch after art import

## Android

- Build Format: **AAB**; Min API 24, Target latest installed
- Signing: create keystore outside repo (`~/.keystores/`, never commit); store credentials in CI secrets
- Package name: `com.diamondtilt.game`
- Increment: `versionCode` = build number every submission

## iOS

- Bundle id: `com.diamondtilt.game`; signing team set in Xcode project settings
- Capabilities: none required in v1 (no push yet)
- Crash reporting: choose one SDK before first public build (Unity Cloud Diagnostics OR Firebase Crashlytics); wire symbol/dSYM upload into CI
- TestFlight: upload via Xcode; phased release enabled at App Store Connect

## Versioning scheme

`MAJOR.MINOR.PATCH`
- MAJOR: season-format or save-schema breaking change (migration must exist)
- MINOR: feature release (new mode, monetization surface)
- PATCH: fixes/balance; hotfixes bump PATCH from the release tag

Save `schemaVersion` bumps independently of app version and always ship a migrator test.

## Pre-submission command sequence

```sh
sh Scripts/run-tests.sh        # headless suite gate
# then Editor: run bootstrapper → open BootScene → PlayMode smoke → device build
```

Full checklist lives in `Docs/RUNBOOK.md` §0.
