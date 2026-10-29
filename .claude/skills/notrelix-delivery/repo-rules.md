# Repo rules for roles that write

Every `nrx-*` role that changes files or runs heavy jobs follows these. They sit on top of AGENTS.md, RULE.md and the scoped `backend/AGENTS.md`, `backend/tests/AGENTS.md` and `frontend/AGENTS.md`.

## Where you work

- Work only in the worktree path named in your dispatch, and `cd` there before every git command. The coordinator creates worktrees from the main session.
- Branch from `origin/develop`, then open a PR into `develop`. Never push to `develop` or `main`, and never merge (the coordinator merges).
- If a hook or permission blocks a command, stop and report the exact message. Running the command through another path (`/usr/bin/git`, `rtk proxy`) is a bypass.

## Git

- Commit messages are plain English describing the change. Never add Co-Authored-By or Claude-Session trailers, plan keys, phase or task labels. PR bodies carry no attribution lines.
- Stage explicit paths only.
- These paths are never staged:
  - `docs/superpowers/`
  - `e2e/**-snapshots/`
  - `.impeccable`
  - `frontend/apps/marketing/AGENTS.md`
  - `backend/src/Notrelix.API/Contracts/openapi/`
  - `backend/src/Notrelix.API/storage/`
- History is append-only: new commits and merge commits. `reset`, `clean`, `checkout --`, `restore`, `stash`, `rebase` and force-push are off limits.
- Pre-push (`scripts/pre-push.sh`) must pass in full. `--no-verify` and `NOTRELIX_SKIP_PREPUSH` are never used.

## Shared machine

- **Heavy-run lock.** Take it before pre-push, Playwright, `dotnet test` suites or Testcontainers, and release it the moment the run ends:
  - take: `mkdir /tmp/notrelix-heavy-run.lock`, then write your role and task into `owner` inside it;
  - wait: if the lock exists, wait in a background until-loop and try again;
  - release: delete `owner`, then `rmdir` the lock.
- **Ports.** 5173 and 3080 belong to the dev stack. Use 5300 or higher, and stop every server you start.
- **Containers.** The `notrelix-*` dev containers are the owner's: leave their state as it is. Testcontainers starts its own.
- **Env files.** `.env.prod` is never opened. `.env.dev` is read-only. Secrets and seed passwords are never printed.

## Code

- Follow the surrounding code: house components, tokens and patterns first. A new dependency needs the architect's tech design.
- Generated files (OpenAPI client, package-boundary inventories) are regenerated with their producer, never edited by hand.
- A migration must apply to an existing database that already has RLS policies. Change nullability with `SET NOT NULL` SQL instead of `AlterColumn`.
- Prettier: `./node_modules/.bin/prettier --write <files>`. A fresh worktree first needs `pnpm install --offline --frozen-lockfile` in `frontend/`.
- Visual baselines come only from the CI renderer. They are never generated locally.
- UI rules (DESIGN.md):
  - no top header bar on desktop or tablet;
  - no keyboard-hint chips on buttons: shortcuts go through `aria-keyshortcuts`, tooltips and the shortcut sheet;
  - progressive forms grow in place.
- Every FUI marker is unique, and every new interaction case is in the package's ui-evidence manifest.
