#!/usr/bin/env bash
# Local mirror of the CI checks that can fail on a code change, run by the
# git pre-push hook (install with `make hooks-install`). It checks only the
# stacks the pushed commits touch:
#   frontend/ -> prettier on changed files, architecture/UI/i18n/mock/codegen
#                gates, typecheck and lint of affected packages, node tests,
#                and UI test markers against the ui-evidence manifests
#   backend/  -> dotnet format, build, Domain and Application tests
# Not covered (CI only): container image CVE scans, Linux visual baselines,
# Testcontainers/integration suites, the full web component suite.
# Emergency skip for one push: NOTRELIX_SKIP_PREPUSH=1 git push ...
set -uo pipefail

if [[ "${NOTRELIX_SKIP_PREPUSH:-}" == "1" ]]; then
  echo "[pre-push] skipped by NOTRELIX_SKIP_PREPUSH=1"
  exit 0
fi

root="$(git rev-parse --show-toplevel)"
zero="0000000000000000000000000000000000000000"

# Files changed by what is being pushed: stdin gives one line per ref.
changed=""
while read -r _local_ref local_sha _remote_ref remote_sha; do
  [[ "$local_sha" == "$zero" ]] && continue
  if [[ "$remote_sha" != "$zero" ]] && git cat-file -e "$remote_sha" 2>/dev/null; then
    base="$remote_sha"
  else
    base="$(git merge-base "$local_sha" origin/develop 2>/dev/null || true)"
  fi
  [[ -z "$base" ]] && continue
  changed+="$(git diff --name-only --diff-filter=ACMR "$base" "$local_sha")"$'\n'
done
changed="$(printf '%s' "$changed" | sed '/^$/d' | sort -u)"

if [[ -z "$changed" ]]; then
  echo "[pre-push] no changed files to check"
  exit 0
fi

failures=()
run() {
  local label="$1"
  shift
  echo "[pre-push] $label"
  if ! "$@"; then
    failures+=("$label")
  fi
}

if grep -q '^frontend/' <<<"$changed"; then
  cd "$root/frontend" || exit 1
  if [[ ! -d node_modules ]]; then
    echo "[pre-push] frontend/node_modules is missing: run pnpm install first" >&2
    exit 1
  fi
  base_ref="$(git merge-base HEAD origin/develop 2>/dev/null || echo origin/develop)"
  fe_files=()
  while read -r file; do
    [[ -f "$file" ]] && fe_files+=("$file")
  done < <(grep '^frontend/' <<<"$changed" | sed 's#^frontend/##')
  if ((${#fe_files[@]} > 0)); then
    run "prettier (changed files)" pnpm exec prettier --check --ignore-unknown "${fe_files[@]}"
  fi
  run "architecture" pnpm -s check:architecture
  run "ui purity" pnpm -s check:ui-purity
  run "i18n catalogs" pnpm -s check:i18n
  run "ui actions" pnpm -s check:ui-actions
  run "ui fixtures" pnpm -s check:ui-fixtures
  run "mock freeze" pnpm -s mock:freeze:check
  run "generated contracts" pnpm -s codegen:check
  run "ui test markers" node scripts/check-ui-markers-static.mjs
  run "typecheck + lint (affected packages)" pnpm -s turbo run typecheck lint --filter="...[$base_ref]" --output-logs=errors-only
  run "node tests" pnpm -s test:node
fi

if grep -q '^backend/' <<<"$changed"; then
  cd "$root" || exit 1
  run "dotnet restore" dotnet restore backend.slnx --verbosity quiet
  run "dotnet format" dotnet format backend.slnx --verify-no-changes --no-restore
  run "dotnet build" dotnet build backend.slnx --no-restore --verbosity quiet
  run "Domain tests" dotnet test backend/tests/Notrelix.Domain.Tests --no-build --verbosity quiet
  run "Application tests" dotnet test backend/tests/Notrelix.Application.Tests --no-build --verbosity quiet
fi

if ((${#failures[@]} > 0)); then
  echo
  echo "[pre-push] push blocked; failing checks:" >&2
  printf '  - %s\n' "${failures[@]}" >&2
  exit 1
fi
echo "[pre-push] all checks passed"
