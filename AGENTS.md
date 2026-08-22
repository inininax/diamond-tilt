# AGENTS.md

2.5D baseball game for mobile (iOS + Android), built with **Unity (C#)**. Touch-first input/UI; the game spec and phased build plan live in `PROMPT.md`.

## Commands

No CLI build scripts exist yet (the repo may predate the Unity project itself):

```sh
# Open/run: open the repo root in Unity Hub,
# matching the version in ProjectSettings/ProjectVersion.txt (once it exists)

# Run EditMode tests headless (set <unity> to your Editor binary):
<unity> -batchmode -nographics -projectPath . \
  -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile -
# PlayMode: identical command with -testPlatform PlayMode
```

- Device builds go through the Unity Editor (File > Build Settings) until a CI pipeline exists.
- Agents cannot open the Unity Editor GUI — finish work with an exact "Manual steps" list for Editor-side actions (see `.agents/rules/agent-editor-boundary.md`).

## Where the docs live

- `PROMPT.md` — original game spec + phased plan (Phase 0–6), technical/security production bar, and the agent-pipeline protocol. Pick up there if asked to "continue" building the game.
- Agent role definitions live in `.agents/agents/*.md` (analyzer, designer, developer, reviewer) — load the relevant one before acting in that role.
- Topic-scoped rules live in `.agents/rules/*.md` (extension point for future rules). OpenCode auto-loads them via the glob in `opencode.json`; Claude Code resolves the `@import` list at the bottom of this file; other tools follow the readable pointers. Read `.agents/rules/README.md` before adding a new rule file.
- This file (`AGENTS.md`) is the single source of truth for agent instructions. Codex/OpenCode/Cursor (newer) read it natively. Symlinks to it exist for tools needing their own filename: `CLAUDE.md` (Claude Code), `GEMINI.md` (Gemini CLI), `.github/copilot-instructions.md` (GitHub Copilot) — edit `AGENTS.md` only, never write through a symlink. `.cursor/rules/project.mdc` carries a condensed copy (it needs Cursor frontmatter and cannot be a symlink); keep it consistent if you rename or move things.

## Architecture invariant: gameplay core stays UnityEngine.Object-free

Gameplay/simulation code under `Assets/Scripts/Gameplay/**` and `Assets/Scripts/Core/**` must be plain C#: no `MonoBehaviour`, no `UnityEngine.Object` dependencies. The goal is running rules logic in EditMode tests without entering Play Mode. MonoBehaviours are thin adapters only — read input, call plain-C# systems, render results.

Verify after touching core scripts (once the project exists):

```sh
grep -rnE "UnityEngine\.|MonoBehaviour" Assets/Scripts/Gameplay/ Assets/Scripts/Core/
```

## Unity gotchas (agents get these wrong)

- Never commit or edit `Library/`, `Temp/`, `Logs/`, `obj/`, `UserSettings/` — all regenerated; `.gitignore` already covers them.
- Every asset has a sibling `.meta` file with a stable GUID. Never hand-edit, delete, or regenerate GUIDs; keep `.meta` beside its asset when renaming/moving. Full details: `.agents/rules/unity-assets.md`.
- `.unity` scenes / `.prefab` files are YAML and merge-conflict-prone: prefer editing C# scripts; if unavoidable, small additive changes only — never reorder serialized fields.
- MonoBehaviour class name must exactly match its file name.

## Testing quirks

- Unit tests = EditMode (headless via the CLI command above); integration tests = PlayMode. New gameplay features need at least one EditMode test for core logic before being called done.
- Check API compatibility level (.NET Standard 2.1 vs 4.x) in `ProjectSettings/ProjectSettings.asset` before using newer BCL APIs.

## Target layout (scaffold as you go)

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

## In-progress work (as of last session)

- Phase 1 rules engine is implemented and verified **headlessly** (no Unity needed): plain-C# core in `Assets/Scripts/Core/` (MatchEngine, BaseRunnerEngine, MatchState, seeded RNG) with NUnit tests in `Tests/EditMode/`. Run them anywhere via `dotnet test Tests/DotNet/EditMode.Tests/EditMode.Tests.csproj` (needs .NET SDK; Unity not required). 36/36 green as of this session.
- Agent pipeline exists: role definitions in `.agents/agents/*.md` (analyzer → designer → developer → reviewer); cycle repeats until the reviewer reports zero blocking findings. Pipeline usage is specified in `PROMPT.md` ("Agent pipeline").
- **Still blocked for Editor work:** the Unity project does not exist yet (Unity not installed on this machine; verified 2026-08-23). Next action: create/open the project in Unity Hub, then execute `PROMPT.md` Phase 0 wiring (asmdefs so `Tests/EditMode` also runs inside Unity) and continue to Phase 2 (ball flight + CPU AI).
- Until the Unity project exists, do not fabricate scenes/prefabs or claim Editor builds/tests were run.

## Conventions

- Design input/UI mobile-first; players are on touch devices.
- Code is written without comments; keep names self-explanatory instead.

---

Rule index (@import lines — Claude Code resolves these):

@.agents/rules/unity-assets.md
@.agents/rules/agent-editor-boundary.md
