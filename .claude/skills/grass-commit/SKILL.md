---
name: grass-commit
description: Required workflow for every git commit in the GrassSimulation repository — splitting changes into one-topic commits, choosing the topic, staging explicit paths with their .meta files, writing the subject/body, and passing the commit-msg hook. Use whenever the user asks to commit, make commits, save work to git, or split changes into commits, and before any commit the agent decides to create.
---

# Committing in GrassSimulation

The authority is [docs/conventions/COMMIT-CONVENTIONS.md](../../../docs/conventions/COMMIT-CONVENTIONS.md).
Read it before the first commit in a session. This skill is the procedure; if the two disagree, the document wins.

Commit only when the user asks, or when the user has already authorized commits for the current task.

## Procedure

1. **Check that the hook is on.** If `git config --get core.hooksPath` does not print `.githooks`, run
   `git config core.hooksPath .githooks`.

2. **Survey the changes.** Run `git status --porcelain=v1 -uall` and `git diff --stat`. Ignore paths you did not
   change for this task. Never stage or revert the user's unrelated work.

3. **Make sure Unity state is saved and compiles** if the change touches code, scenes or assets. Use the
   `grass-unity-editor` skill:
   - Save the scenes and assets you edited (`save_scene` / `save_all`).
   - `recompile`, then `recompile_status`, must report no errors.

   If no Editor is reachable, say so in the report and do not claim that the code compiles.

4. **Plan the commit series** before staging anything. Use one topic per commit, by ownership:

   | Change | Topic |
   |---|---|
   | Game module `GrassSimulation.<Module>` code, assets and tests | `<Module>` (e.g. `Gameplay`) |
   | Grass shaders, URP renderer features, materials | `Rendering` |
   | Scene/level content without a module owner | `Scenes` |
   | Art/audio/VFX/UI content without a module owner | `Content` |
   | `Packages/manifest.json` + lock file | `Packages` |
   | `docs/` design docs (GDD) | `Design` |
   | ProjectSettings, build profiles, repo config, conventions | `Project` |
   | `CLAUDE.md`, `.claude/` | `Agents` |
   | Embedded framework module | `EncosyTower.<Module>` |
   | Framework generator / analyzer + refreshed artifacts | `EncosyTower.SourceGen[.<Feature>]` |

   Order: framework commits first, then game commits that depend on them. Tests and docs go in the commit they
   validate. Show the user the planned series when it has more than one commit.

5. **Stage each commit by explicit path.** Never use `git add -A`, `git add .` or `git commit -a`.
   - Stage every asset together with its `.meta` file. For each staged `X`, `X.meta` must also be staged when it
     changed. A new folder needs its folder `.meta`.
   - Stage `Packages/manifest.json` together with `Packages/packages-lock.json`.
   - Never stage generated `*.csproj`, `*.slnx`, `Library/`, `Temp/`, `Logs/` or `UserSettings/`.
   - Mark a new shell script or hook executable: `git add --chmod=+x <path>`.

6. **Review the staged diff:** `git diff --cached --stat`, then `git diff --cached`. Confirm that it has one
   purpose and no stray files.

7. **Write the message:**
   - Subject: `<Topic>: <lowercase imperative summary>`, at most 70 characters, in English, with no trailing
     period. Describe the outcome, not the files.
   - Body (optional): a blank line, then `- ` bullets about behavior, intent or compatibility. Say explicitly if
     the change breaks serialized data, saves or asset references.
   - Trailer: a blank line, then the attribution lines required by the current session's instructions.

   Pass the message with a heredoc so that line breaks survive:

   ```bash
   git commit -F - <<'EOF'
   Gameplay: cut grass by swept mower area

   - Query only spatial-grid cells near the swept capsule
   - Update cut state per chunk instead of per blade

   Co-Authored-By: ...
   EOF
   ```

8. **If the hook rejects the message**, fix the message and commit again. Never use `--no-verify` unless the user
   explicitly asks.

9. **Report** each commit's hash and subject, plus anything left uncommitted and why.

## Do not

- Amend, rebase, squash or force-push existing commits unless the user asks.
- Bump EncosyTower versions or run release steps as part of a commit.
- Combine two topics in one subject, or use a vague topic such as `Misc` or `Update`.
