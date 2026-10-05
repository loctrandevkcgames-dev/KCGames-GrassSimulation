#!/usr/bin/env bash
# Run `unity command` against THIS project's Editor only, with agent-friendly output.
#   ucmd.sh <editor-command> [args...]      e.g. ucmd.sh editor_status
#   ucmd.sh                                 list command tags
# Extra `unity command` flags (--timeout, --result-only, --detach, ...) can be appended.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# scripts/ -> grass-unity-editor/ -> skills/ -> .claude/ -> project root
ROOT_POSIX="$(cd "$SCRIPT_DIR/../../../.." && pwd)"
# Git Bash: convert to a Windows path, which is what the Editor reports in `unity status`.
ROOT="$(cd "$ROOT_POSIX" && (pwd -W 2>/dev/null || pwd))"

export UNITY_NO_PAGER=1 UNITY_NO_BANNER=1 UNITY_NON_INTERACTIVE=1 NO_COLOR=1

# CLI flags go BEFORE the command name: unknown flags after it are forwarded to the Editor command.
exec unity command --project-path "$ROOT" --format json "$@"
