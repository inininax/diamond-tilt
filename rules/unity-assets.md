# Unity Assets: Scenes, Prefabs, Meta Files

Details for handling Unity YAML assets. Critical top-level rules live in `AGENTS.md`; this file is the deep reference.

## `.meta` files

- Every asset (including folders) has a sibling `.meta` with a stable GUID. The GUID is the asset's identity — references in scenes/prefabs point to it.
- Never hand-edit, delete, or regenerate a GUID. Doing so silently breaks every reference.
- Renaming or moving an asset = rename/move the `.meta` together with it, preserving contents.
- Creating a new file outside the Editor means creating its `.meta` too — but prefer letting the Editor generate it on next focus; a missing `.meta` is auto-created, a wrong one is not.

## Scenes (`.unity`) and prefabs (`.prefab`)

- Both are serialized YAML. Conflicts are common because one scene holds thousands of lines.
- Order of preference:
  1. Don't touch them — change C# scripts instead.
  2. Small additive edits only: append new GameObjects/components with fresh `fileID`s.
  3. Never reorder existing serialized fields, never renumber `fileID`s, never reformat.
- One scene per feature area. Shared things become prefabs; avoid giant scenes.
- When a conflict does happen: do not merge by eye — pick one side and re-apply the other's intent as a small edit.

## Serialization settings

- `ProjectSettings/EditorSettings.asset` must keep `Serialization: 2` (Force Text). Binary assets are unreviewable and unmergeable.

## Script → asset naming

- MonoBehaviour class name must exactly match the file name, or Unity fails to attach it.

## Adding packages

- Edit `Packages/manifest.json` (text-safe), never via Asset Store GUI assumptions. After changing it, note that the human must let Unity resolve/recompile once.
