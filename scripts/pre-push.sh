#!/usr/bin/env bash
# Local mirror of the CI checks that can fail on a code change, run by the
# git pre-push hook (install with `make hooks-install`). It checks only the
# stacks the pushed commits touch:
#   frontend/ -> prettier on changed files, architecture/UI/i18n/mock/codegen
#                gates, typecheck and lint of affected packages, node tests,
#                UI test markers and source classification against the
#                ui-evidence manifests, and the mock e2e specs of the UI areas
#                touched (see frontend/scripts/run-affected-mock-e2e.mjs)
#   backend/  -> dotnet format, build, Domain, Application and API tests, and
#                the integration tests of the contexts touched (needs Docker)
# Not covered (CI only): container image CVE scans, Linux visual baselines,
# Storybook story evidence, other personas/scenarios of the mock e2e matrix,
# the full integration and web component suites.
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
  # The source half of check:ui-evidence; the story half needs a Storybook build (CI).
  run "ui evidence sources" bash -c '! pnpm -s exec tsx tooling/storybook/web/src/evidence/check-ui-evidence.ts /dev/null/no-index 2>&1 | grep -E "unclassified governed source|is classified [0-9]+ times|is governed by a different owner"'
  run "typecheck (affected packages)" pnpm -s turbo run typecheck --continue --filter="...[$base_ref]" --output-logs=errors-only
  # The marketing host lints with eslint-config-next, whose react-hooks plugin
  # is not resolvable from a local install; its lint runs in CI only.
  run "lint (affected packages)" pnpm -s turbo run lint --continue --filter="...[$base_ref]" --filter='!@notrelix/app-marketing' --output-logs=errors-only
  run "node tests" pnpm -s test:node
  # The mock e2e specs of the UI areas the push touches (own dev server, free port).
  run "mock e2e (affected specs)" bash -c 'printf "%s\n" "$0" | xargs node scripts/run-affected-mock-e2e.mjs' "$changed"
fi

if grep -q '^backend/' <<<"$changed"; then
  cd "$root" || exit 1
  run "dotnet restore" dotnet restore backend/backend.slnx --verbosity quiet
  run "dotnet format" dotnet format backend/backend.slnx --verify-no-changes --no-restore
  run "dotnet build" dotnet build backend/backend.slnx --no-restore --verbosity quiet
  run "Domain tests" dotnet test backend/tests/Notrelix.Domain.Tests --no-build --verbosity quiet
  run "Application tests" dotnet test backend/tests/Notrelix.Application.Tests --no-build --verbosity quiet
  run "API tests" dotnet test backend/tests/Notrelix.API.Tests --no-build --verbosity quiet
  # Integration tests of the contexts the push touches: a context is an Integration.Tests
  # folder whose name appears as a path segment of a changed backend file.
  integration="backend/tests/Notrelix.Integration.Tests"
  filters=()
  for dir in "$integration"/*/; do
    name="$(basename "$dir")"
    [[ "$name" == bin || "$name" == obj ]] && continue
    grep -q "^backend/.*/$name/" <<<"$changed" && filters+=("FullyQualifiedName~Notrelix.Integration.Tests.$name.")
  done
  # A shared host helper change reaches every hand-composed host.
  grep -q "^$integration/Containers/" <<<"$changed" && filters=("FullyQualifiedName~Notrelix.Integration.Tests.")
  if ((${#filters[@]} > 0)); then
    if docker info >/dev/null 2>&1; then
      filter="$(IFS='|'; echo "${filters[*]}")"
      run "integration tests (touched contexts)" dotnet test "$integration" --no-build --verbosity quiet --filter "$filter"
    else
      echo "[pre-push] WARNING: Docker is not running; integration tests of the touched contexts were not run (CI runs them)" >&2
    fi
  fi
fi

if ((${#failures[@]} > 0)); then
  echo
  echo "[pre-push] push blocked; failing checks:" >&2
  printf '  - %s\n' "${failures[@]}" >&2
  exit 1
fi
echo "[pre-push] all checks passed"
