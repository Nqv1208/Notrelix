#!/usr/bin/env bash
# Self-test of .githooks/pre-push. Builds a throw-away repo with a tiny backend solution that carries the
# repository's own backend/.editorconfig and Directory.Build.props (so IDE0005 is judged by the real rules),
# then feeds the hook the lines git would pass on stdin. Needs the .NET SDK; about 30 seconds.
#   bash .githooks/test/pre-push.test.sh
set -uo pipefail

repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
hook="$repo/.githooks/pre-push"
fx="$(mktemp -d)"
trap 'rm -rf "$fx"' EXIT

passed=0
failed=0
expect() { # expect <label> <expected exit: 0|nonzero> <actual exit> [<pattern that must appear in the output>] [<output file>]
  local label="$1" want="$2" got="$3" pattern="${4:-}" out="${5:-}"
  local ok=1
  if [[ "$want" == "0" && "$got" != "0" ]]; then ok=0; fi
  if [[ "$want" == "nonzero" && "$got" == "0" ]]; then ok=0; fi
  if [[ -n "$pattern" ]] && ! grep -q -- "$pattern" "$out"; then ok=0; fi
  if ((ok)); then
    passed=$((passed + 1))
    echo "ok   $label"
  else
    failed=$((failed + 1))
    echo "FAIL $label (exit $got, wanted $want${pattern:+, output must contain: $pattern})"
    [[ -n "$out" ]] && sed 's/^/     | /' "$out"
  fi
}

# --- the fixture repo
cd "$fx" || exit 1
git init -q .
git config user.email "selftest@example.invalid"
git config user.name "selftest"
mkdir -p backend/src/App backend/tests/Notrelix.Architecture.Tests frontend docs
cp "$repo/backend/.editorconfig" "$repo/backend/Directory.Build.props" backend/
cat >backend/backend.slnx <<'XML'
<Solution>
  <Project Path="src/App/App.csproj" />
  <Project Path="tests/Notrelix.Architecture.Tests/Notrelix.Architecture.Tests.csproj" />
</Solution>
XML
for p in src/App/App.csproj tests/Notrelix.Architecture.Tests/Notrelix.Architecture.Tests.csproj; do
  printf '<Project Sdk="Microsoft.NET.Sdk"></Project>\n' >"backend/$p"
done
printf 'namespace App;\n\npublic class Clean { }\n' >backend/src/App/Clean.cs
printf 'namespace Notrelix.Architecture.Tests;\n\npublic class Marker { }\n' >backend/tests/Notrelix.Architecture.Tests/Marker.cs
printf 'readme\n' >docs/readme.md
printf '{}\n' >frontend/package.json
printf 'obj/\nbin/\n' >.gitignore
(cd backend && dotnet restore backend.slnx >/dev/null 2>&1) || {
  echo "FAIL the fixture solution could not be restored"
  exit 1
}
git add -A
git commit -q -m "baseline"
baseline="$(git rev-parse HEAD)"

push() { # push <sha> : what git writes on the hook's stdin for an update of refs/heads/x from the baseline
  printf 'refs/heads/x %s refs/heads/x %s\n' "$1" "$baseline" | "$hook" origin file:///fixture
}
commit_all() {
  git add -A
  git commit -q -m "$1"
  git rev-parse HEAD
}

# --- 1. a push that changes neither side checks nothing
printf 'more\n' >>docs/readme.md
sha="$(commit_all docs)"
out="$fx/out1"
push "$sha" >"$out" 2>&1
expect "a docs-only push exits 0 and checks nothing" 0 $? "nothing to check" "$out"

# --- 2. a clean backend change passes
printf 'namespace App;\n\npublic class Another { }\n' >backend/src/App/Another.cs
sha="$(commit_all backend-clean)"
out="$fx/out2"
push "$sha" >"$out" 2>&1
expect "a clean backend change exits 0" 0 $? "all checks passed" "$out"

# --- 3. an unused using blocks the push and says what to run
printf 'using System;\n\nnamespace App;\n\npublic class Unused { }\n' >backend/src/App/Unused.cs
sha="$(commit_all backend-unused-using)"
out="$fx/out3"
push "$sha" >"$out" 2>&1
expect "an unused using exits non-zero and names the failed check" nonzero $? "FAILED  backend format" "$out"
grep -q "IDE0005" "$out" && grep -q "fix: cd backend && dotnet restore backend.slnx && dotnet format backend.slnx --no-restore" "$out"
expect "  ... the output carries IDE0005 and the command that fixes it" 0 $?

# --- 4. a frontend change without installed dependencies is refused, not skipped
git checkout -q -b frontend-case "$baseline"
printf '{ "name": "x" }\n' >frontend/package.json
sha="$(commit_all frontend-no-deps)"
out="$fx/out4"
push "$sha" >"$out" 2>&1
expect "a frontend change without node_modules exits non-zero and says to install" nonzero $? "pnpm install" "$out"

# --- 5. deleting a branch pushes no code
out="$fx/out5"
printf 'refs/heads/x 0000000000000000000000000000000000000000 refs/heads/x %s\n' "$baseline" | "$hook" origin file:///fixture >"$out" 2>&1
expect "a branch deletion exits 0" 0 $? "nothing to check" "$out"

echo
echo "pre-push self-test: $passed passed, $failed failed"
((failed == 0))
