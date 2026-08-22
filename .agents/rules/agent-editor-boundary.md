# Agent ↔ Editor Boundary

AI coding agents have no access to the Unity Editor GUI. Everything that requires the Editor is a manual step for the human.

## Agents CAN do (headless)

- Write/edit C# scripts, asmdefs, test files.
- Edit plain-text configs: `Packages/manifest.json`, `.gitignore`, markdown docs.
- Run EditMode tests via CLI batchmode (command in `AGENTS.md`) — if Unity CLI is installed.
- Create/edit `.unity`/`.prefab` YAML carefully per `rules/unity-assets.md` (last resort).

## Agents CANNOT do (manual steps for the human)

- Create the initial Unity project / install Editor versions (Unity Hub).
- Press Play, visually verify rendering/layout, tune camera or UI by eye.
- Import/download Asset Store packages, generate lightmaps, configure signing/provisioning.
- Device builds and simulator runs.

## Protocol when finishing work

1. List exact "Manual steps" for Editor-side actions (menu paths included).
2. State how to verify (what to open, what to expect on screen, which test to run).
3. Never claim visual/rendering results you could not observe.
