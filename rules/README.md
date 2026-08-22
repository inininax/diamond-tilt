# Rules Directory

Topic-specific agent rules, shared by **all** AI coding agents (Claude Code, Codex, OpenCode).

## How it works

- `AGENTS.md` is the entry point every tool reads natively (Codex/OpenCode) or via symlink (`CLAUDE.md` → `AGENTS.md`).
- OpenCode also auto-loads everything matching `rules/**/*.md` via `opencode.json`.
- Other tools follow the index pointers in `AGENTS.md`.

## Adding a new rule file

1. Create `rules/<topic>.md` (kebab-case, one topic per file).
2. Add one line to the Index below AND to the index section in `AGENTS.md`.
3. Keep only high-signal, repo-specific rules — no generic advice.
4. Never duplicate a rule in two places; cross-reference instead.

## Index

| File | Scope |
|---|---|
| [unity-assets.md](unity-assets.md) | Scene/prefab/meta/YAML handling |
| [agent-editor-boundary.md](agent-editor-boundary.md) | What agents cannot do without the Unity Editor GUI |
