# AI rules directory (extension point)

This directory holds additional, topic-scoped AI rules beyond the root
`AGENTS.md`. OpenCode picks up every `*.md` here automatically via the glob in
`opencode.json`. Other tools read `AGENTS.md`, which links to these files.

## Tool wiring map

| Tool | Reads | Mechanism |
|---|---|---|
| Codex CLI / OpenCode / newer Cursor | `AGENTS.md` | native auto-load |
| OpenCode (this dir) | `.agents/rules/**/*.md` | `instructions` glob in `opencode.json` |
| Claude Code | `CLAUDE.md` -> `AGENTS.md` | symlink; `@import` lines inside AGENTS.md |
| Gemini CLI | `GEMINI.md` -> `AGENTS.md` | symlink |
| GitHub Copilot | `.github/copilot-instructions.md` -> `AGENTS.md` | symlink |
| Cursor | `.cursor/rules/project.mdc` | condensed copy (needs mdc frontmatter) |

## How to add a new rule

1. Create `<topic>.md` in this directory (e.g. `netcode.md`, `rendering.md`).
   Keep it short, factual, and verified against the code.
2. Add one line to the rule list at the bottom of root `AGENTS.md`:
   `@.agents/rules/<topic>.md`
   - Claude Code resolves the `@import`; other agents see a readable pointer.
3. If the rule matters for quick context, mirror one sentence into
   `.cursor/rules/project.mdc`.

Never edit files through their symlinks; always edit the source
(`AGENTS.md` or files in this directory).

## Existing rules

- [unity-assets.md](unity-assets.md) — scene/prefab/meta/YAML handling details
- [agent-editor-boundary.md](agent-editor-boundary.md) — what agents cannot do without the Unity Editor GUI
