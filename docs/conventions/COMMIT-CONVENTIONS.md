# GrassSimulation Commit Conventions

This document is the single authority for commit boundaries and commit messages
in this repository. It applies to new commits; do not rewrite existing history
only to make old commits conform. Every commit follows it, whether a person or an
agent writes it.

## 1. Commit Boundaries

- Each commit must have one coherent, reviewable purpose and one topic.
- Split unrelated features, tooling, content, settings and dependency changes
  into separate commits.
- Keep tests, documentation and generated snapshots with the change they
  directly validate or explain.
- Never mix embedded EncosyTower changes with game changes. Commit the framework
  change first, then the game change that depends on it.
- Split runtime and Source Generator changes when each part is independently
  valid and reviewable. When they must change together to preserve one contract,
  use the topic of the primary behavior. Commit refreshed generator artifacts
  with the generator change that produced them.
- Keep agent-only guidance separate from game code, packages, assets, project
  settings and build configuration.
- Commit every asset with its `.meta` file, and commit `Packages/manifest.json`
  with `Packages/packages-lock.json`.
- Every commit must leave the project compiling in Unity. Do not commit known
  compile errors.
- Preserve unrelated tracked and untracked work. Stage explicit paths, never
  `git add -A` or `git add .`, and inspect the staged diff before committing.

## 2. Subject Format

Use this format:

```text
<Topic>: <brief imperative summary>
```

Subject rules:

- Maximum length is 70 characters, including the topic and colon.
- Use the most specific stable topic that owns the change (Section 3).
- Start the summary with a lowercase imperative verb, such as `add`, `fix`,
  `remove`, `rename`, `refactor`, `update`, `tune`, or `rebuild`.
- Describe the outcome, not the files touched or the work performed.
- Write in English. Do not end the subject with a period.
- Do not combine topics in the subject.
- `WIP` is acceptable only as a temporary local checkpoint. Reword or squash it
  before the commit becomes part of shared history.

Examples:

```text
Gameplay: add swept-area grass cutting
Gameplay: tune mower growth thresholds
Rendering: batch grass chunks with GPU instancing
Scenes: block out first garden level
EncosyTower.Core: fix vault reset on domain reload
EncosyTower.SourceGen.Mvvm: preserve binder contracts
Packages: add Unity Addressables
Design: add level template rules to GDD
Project: add commit conventions
Agents: add Unity CLI skills
```

## 3. Topic Selection

Choose topics by ownership and behavior, not only by directory.

| Topic | Use |
|---|---|
| `<Module>` | Game code and assets owned by one game module `GrassSimulation.<Module>`, named without the `GrassSimulation.` prefix, for example `Gameplay` |
| `Rendering` | Grass shaders, URP renderer features, materials and render-pipeline assets used by the game |
| `Scenes` | Scene layout and level content without a narrower module owner |
| `Content` | Art, audio, VFX, UI assets and other game content without a narrower owner |
| `Tests` | Cross-module game test infrastructure or test-only work without a narrower owner |
| `Packages` | Unity package dependencies in `Packages/manifest.json` and its lock file |
| `Design` | Game design documents in `docs/`, such as the GDD |
| `Project` | Project settings, build profiles, repository configuration, CI, conventions and general documentation |
| `Agents` | `CLAUDE.md`, `.claude/` and other agent-only instruction files |
| `EncosyTower.<Module>` | Embedded framework code, named by the module without the `EncosyTower.` prefix, for example `EncosyTower.Core` or `EncosyTower.Entities.Stats` |
| `EncosyTower.SourceGen` | Shared Source Generator infrastructure or coordinated artifact rebuilds |
| `EncosyTower.SourceGen.<Feature>` | One framework feature's Roslyn generator, analyzer, code refactor or directly supporting tests |

Apply these rules when choosing between related topics:

- A game module topic wins over `Rendering`, `Scenes`, `Content` and `Tests`
  when one module clearly owns the change and its assets.
- Use a module topic for its accompanying tests; use `Tests` only when the test
  change is independently cross-module.
- For a new game module, derive the topic from its stable module name.
- Use `Project` for root documentation unless `Design`, `Agents` or a narrower
  module clearly owns it.
- Imported EncosyTower samples under `Assets/Samples/` use `EncosyTower`
  topics, and change only when the Project Owner asks.

## 4. Commit Body

Omit the body when the subject fully explains the commit. When more context is
needed:

- Leave one blank line after the subject.
- Use concise `- <change>` bullets instead of prose paragraphs.
- Describe behavior, intent, compatibility, or a non-obvious reason.
- Do not list filenames or narrate the implementation process.
- Mention breaking behavior explicitly, including changes that invalidate
  serialized data, saved games or asset references.
- Trailers such as `Co-Authored-By:` go after the body, separated by one blank
  line.

Example:

```text
Gameplay: cut grass by swept mower area

- Query only spatial-grid cells near the swept capsule
- Update cut state per chunk instead of per blade
- Keep mower growth thresholds from the GDD defaults
```

## 5. Enforcement

- The tracked `.githooks/commit-msg` hook rejects subjects that break Section 2.
  Enable it once per clone:

  ```bash
  git config core.hooksPath .githooks
  ```

- The hook checks format only. Topic choice and commit boundaries remain a
  review responsibility.
- Do not bypass the hook with `--no-verify` unless the Project Owner asks.
