# AGENTS.md

Shared instructions for all AI coding agents (Claude Code, Codex, OpenCode, Gemini CLI, Cursor).
This is the single source of truth — do not duplicate rules into tool-specific files.
Symlinks for tools needing their own filename: `CLAUDE.md` (Claude Code), `GEMINI.md` (Gemini CLI), `.github/copilot-instructions.md` (GitHub Copilot). Edit `AGENTS.md` only, never write through a symlink. `.cursor/rules/project.mdc` carries a condensed copy (it needs Cursor frontmatter and cannot be a symlink); keep it consistent if you rename or move things.

## Shared rules system

- Topic-scoped rules live in `.agents/rules/*.md` (extension point for future rules). OpenCode auto-loads them via the glob in `opencode.json`; Claude Code resolves the `@import` lines in the index below; other tools follow the readable pointers.
- Adding a rule: create `.agents/rules/<topic>.md`, then add one line to the index here AND in `.agents/rules/README.md`. Never duplicate a rule in two files.

Rule index (`@import` lines — Claude Code resolves these):

@.agents/rules/unity-assets.md
@.agents/rules/agent-editor-boundary.md

## Project

- 2.5D baseball game for mobile (iOS + Android), built with **Unity (C#)**.
- Repo is at bootstrap stage: Unity project not yet initialized. Once created, check `ProjectSettings/ProjectVersion.txt` for the exact Unity version and use it in Unity Hub / CI.
- Target players are on touch devices; design input/UI mobile-first.

## Commands

No CLI build scripts exist yet. Until CI exists:

- Open/run: open the project root in Unity Hub with the version from `ProjectSettings/ProjectVersion.txt`.
- Run tests (CLI, adjust path to your Unity install):

```sh
<unity> -batchmode -nographics -projectPath . \
  -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile - \
; # PlayMode: same with -testPlatform PlayMode
```

- Device builds go through Unity Editor (File > Build Settings) until a CI pipeline exists.

## Unity rules (agents get these wrong)

- Never commit or edit `Library/`, `Temp/`, `Logs/`, `obj/`, `UserSettings/` — all regenerated. `.gitignore` already covers them.
- Every asset has a sibling `.meta` file with a stable GUID. Never hand-edit, delete, or regenerate GUIDs; keep `.meta` with its asset when renaming/moving.
- `.unity`/`.prefab` are YAML and merge-conflict-prone: prefer editing C# scripts; if unavoidable, small additive changes only. Full details: `.agents/rules/unity-assets.md`.
- MonoBehaviour class name must exactly match its file name.
- You cannot open the Unity Editor GUI — see `.agents/rules/agent-editor-boundary.md` for what stays manual and the required "Manual steps" protocol.

## Code conventions

- Put gameplay logic in plain C# classes under `Assets/Scripts/Gameplay/**` with no `UnityEngine.Object` dependencies where possible — logic must be unit-testable in EditMode without entering Play Mode.
- MonoBehaviours are thin adapters: read input, call plain-C# systems, render results.
- Tests live in `Tests/EditMode/` (unit) and `Tests/PlayMode/` (integration). New gameplay features need at least one EditMode test for core logic.
- Check API compatibility level (.NET Standard 2.1 vs 4.x) in `ProjectSettings/ProjectSettings.asset` before using newer BCL APIs.

## Target layout (create as you scaffold)

```
Assets/
  Scripts/
    Gameplay/   # batting, pitching, fielding, physics
    UI/
    Core/       # game state, scoring, save data
  Scenes/
  Prefabs/
  Art/
Tests/
  EditMode/
  PlayMode/
```

## Agent notes

- Verify claims against actual files; this doc describes intent while the repo is empty — update it as the real structure lands.
