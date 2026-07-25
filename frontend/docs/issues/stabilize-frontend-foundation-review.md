# Stabilize Frontend Foundation — Plan Review & Implementation Notes

## Verdict

The plan is correct and should be executed in strict order.

The only corrections are:

```txt
1. M3 must archive old ARCHITECTURE.md and then create a new pointer file.
2. check:deps should not rely only on turbo unless a child package exposes check:deps.
3. apps/web must remove next-themes because it pulls a Next-oriented theme dependency into the Vite app.
4. apps/web tsconfig must not contain cross-package paths.
```

## Current evidence from branch

- Root package.json is already a workspace controller with `notrelix-client`, `turbo` scripts, `pnpm@10.0.0`, and `check:deps` currently set to `turbo check:deps`.
- `pnpm-workspace.yaml` is currently rendered as a single line and should be rewritten as a YAML block.
- `apps/web/package.json` still includes `next-themes`.
- `apps/web/src/providers/app-providers.tsx` imports `ThemeProvider` from `next-themes`.
- `apps/web/tsconfig.json` still contains cross-package path aliases.
- `work-management/state` and `work-management/web` still declare `next` in package.json.
- Old README/ARCHITECTURE content still describes the old single Next.js/FSD model.

## Recommended implementation detail

Use:

```txt
root check:deps = pnpm --filter @notrelix/dependency-rules check
```

instead of relying on:

```txt
turbo check:deps
```

unless the dependency-rules package also exposes a `check:deps` script.

## Implementation artifact

Use `stabilize-frontend-foundation.sh` from the same download group.

Run it from repo root or frontend root:

```bash
chmod +x stabilize-frontend-foundation.sh
./stabilize-frontend-foundation.sh
```

Then run:

```bash
pnpm install
pnpm typecheck
pnpm lint
pnpm test
pnpm check:deps
pnpm build
```

## Important limitation

This script updates files and package manifests. It does not update `pnpm-lock.yaml` by itself. Running `pnpm install` is required after package.json changes.
