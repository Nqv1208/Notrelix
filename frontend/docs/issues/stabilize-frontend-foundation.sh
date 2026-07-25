#!/usr/bin/env bash
set -euo pipefail

# Stabilize Frontend Foundation — M0 to M3 implementation helper
# Run from repository root OR frontend root.
#
# Usage:
#   chmod +x stabilize-frontend-foundation.sh
#   ./stabilize-frontend-foundation.sh
#
# This script modifies files under frontend/.
# It does not run pnpm install/build. Run validation manually after reviewing changes.

if [ -d "frontend" ] && [ -f "frontend/package.json" ]; then
  FRONTEND_DIR="frontend"
elif [ -f "package.json" ] && [ -f "pnpm-workspace.yaml" ]; then
  FRONTEND_DIR="."
else
  echo "Cannot locate frontend workspace. Run from repo root or frontend/."
  exit 1
fi

cd "$FRONTEND_DIR"

echo "==> Stabilizing frontend foundation in: $(pwd)"

mkdir -p docs/client/archive
mkdir -p docs/client/architecture
mkdir -p docs/client/adr
mkdir -p docs/client/migration
mkdir -p docs/client/audits

echo "==> Step 0.2: Fix pnpm-workspace.yaml"
cat > pnpm-workspace.yaml <<'YAML'
packages:
  - "apps/*"
  - "packages/foundation/*"
  - "packages/runtimes/*"
  - "packages/ui/*"
  - "packages/product/*/*"
  - "packages/features/*"
  - "tooling/*"
YAML

echo "==> Step 0.3: Rewrite README.md"
cat > README.md <<'MD'
# Notrelix Client Workspace

Notrelix client is a multi-client monorepo workspace.

## Runtime projects

```txt
apps/marketing = Next.js App Router public marketing site
apps/web       = Vite + React + TanStack Router authenticated product app
apps/mobile    = Expo / React Native placeholder for future mobile app
```

## Package groups

```txt
packages/foundation = contracts, kernel, platform, query, realtime, observability
packages/runtimes   = web/mobile runtime adapters
packages/ui         = tokens, web UI, mobile UI, icons
packages/product    = work-management, docs, automation
packages/features   = auth, workspace, account, billing, notifications, governance, etc.
tooling             = eslint, tsconfig, codegen, testing, dependency rules
docs/client         = architecture, ADRs, migration, audits, archive
```

## Requirements

```txt
Node >= 22
pnpm >= 10
```

Enable pnpm through Corepack:

```bash
corepack enable
corepack prepare pnpm@10.0.0 --activate
```

## Install

```bash
pnpm install
```

The workspace uses a single root lockfile:

```txt
pnpm-lock.yaml
```

Do not add app-level lockfiles.

## Development

```bash
pnpm dev:marketing
pnpm dev:web
pnpm dev:mobile
```

Default ports:

```txt
Marketing: http://localhost:3000
Product Web: http://localhost:5173
```

## Validation

```bash
pnpm typecheck
pnpm lint
pnpm test
pnpm check:deps
pnpm build
```

Full local quality gate:

```bash
pnpm validate
```

## Architecture rules

```txt
apps/marketing is the only Next.js app.
apps/web must not import next/*.
packages/* must not import next/*.
packages/* must not read env directly.
ui/web must not import ui/mobile.
ui/mobile must not import ui/web.
product/*/core must not import runtime UI.
product/work-management/state must not import UI or Next.js.
```

## Source of truth

Read:

```txt
ARCHITECTURE.md
docs/client/adr/
docs/client/migration/
docs/client/audits/
```

Old single-app FSD documents are archived under:

```txt
docs/client/archive/
```
MD

echo "==> Step 3.1: Archive old ARCHITECTURE.md if needed"
if [ -f ARCHITECTURE.md ]; then
  if [ ! -f docs/client/archive/ARCHITECTURE-v1.md ]; then
    cp ARCHITECTURE.md docs/client/archive/ARCHITECTURE-v1.md
  fi
fi

echo "==> Step 0.4 / 3.1: Create new ARCHITECTURE.md pointer"
cat > ARCHITECTURE.md <<'MD'
# Notrelix Client Architecture

This file is the entry point for the current client architecture.

The canonical architecture is **Multi-Client Architecture v4.2**.

```txt
apps/marketing = Next.js App Router
apps/web       = Vite + React + TanStack Router
apps/mobile    = Expo / React Native placeholder
```

## Canonical documents

```txt
docs/client/architecture/
docs/client/adr/
docs/client/migration/
docs/client/audits/
```

## Package groups

```txt
packages/foundation/
packages/runtimes/
packages/ui/
packages/product/
packages/features/
```

## Hard rules

```txt
1. Apps compose. Packages own logic.
2. Marketing is the only Next.js app.
3. Product Web uses Vite + TanStack Router.
4. packages/* must not import next/*.
5. packages/* must not read env directly.
6. Work Management state is product-specific and lives in packages/product/work-management/state.
7. Work Management state must not import UI.
8. UI is runtime-specific: ui/web and ui/mobile are separate.
9. API calls go through contracts.
10. No delete without Safe Delete Audit.
```

## Archived architecture

The previous single-app FSD/Next.js architecture was archived at:

```txt
docs/client/archive/ARCHITECTURE-v1.md
```

It is historical only and must not be used as the current source of truth.
MD

echo "==> Step 3.2: Create ADR documents"

cat > docs/client/adr/ADR-001-framework-split.md <<'MD'
# ADR-001 — Client Framework Split

## Status

Accepted

## Decision

```txt
apps/marketing = Next.js App Router
apps/web       = Vite + React + TanStack Router
apps/mobile    = Expo / React Native placeholder
```

## Rationale

Marketing requires SEO, metadata, OpenGraph, static/server rendering, and public content.

Product Web is an authenticated, client-heavy workspace application requiring realtime collaboration, optimistic updates, command handling, multi-view rendering, and route-level app composition.

## Consequences

```txt
apps/marketing may import next/*.
apps/web must not import next/*.
packages/* must not import next/*.
Product screens must be framework-neutral before moving from legacy Next routes to apps/web.
```
MD

cat > docs/client/adr/ADR-002-package-manager.md <<'MD'
# ADR-002 — Package Manager and Lockfile Policy

## Status

Accepted

## Decision

Use pnpm workspace with a single root lockfile.

```txt
packageManager = pnpm@10.0.0
lockfile       = frontend/pnpm-lock.yaml
```

## Rules

```txt
No package-lock.json.
No yarn.lock.
No bun.lock or bun.lockb in target state.
No app-level lockfiles.
All apps/packages are workspace packages.
```

## Rationale

A single workspace lockfile gives deterministic dependency resolution, simpler CI, consistent shared package linking, and easier dependency review.
MD

cat > docs/client/adr/ADR-003-package-exports.md <<'MD'
# ADR-003 — Package Exports and Import Boundaries

## Status

Accepted

## Decision

Cross-package imports must go through workspace dependencies and package exports.

## Rules

```txt
Use workspace:* dependencies.
Use package exports.
Do not deep import across packages through relative paths.
Do not use tsconfig paths for cross-package imports.
App-local aliases like @/* are allowed.
```

## Example

Allowed:

```ts
import { Button } from "@notrelix/ui-web/ui/button";
```

Forbidden:

```ts
import { Button } from "../../../packages/ui/web/src/components/ui/button";
```

## Rationale

Package exports define public APIs and allow dependency rules to be enforced reliably.
MD

cat > docs/client/adr/ADR-004-no-next-in-packages.md <<'MD'
# ADR-004 — No Next.js in Shared Packages

## Status

Accepted

## Decision

Next.js is allowed only in `apps/marketing`.

## Forbidden

```txt
apps/web importing next/*
packages/* importing next/*
packages/* declaring next as dependency
product/work-management/state depending on next
product/work-management/web depending on next
```

## Rationale

The product web app uses Vite. Shared packages must remain framework-neutral or runtime-specific without coupling to Next.js. This keeps the system mobile-ready and prevents framework leakage into product core/state.
MD

echo "==> Step 1.1/1.2/1.3/1.4/2.1: Patch package.json and tsconfig files"
node <<'NODE'
const fs = require("node:fs");

function readJson(path) {
  return JSON.parse(fs.readFileSync(path, "utf8"));
}

function writeJson(path, value) {
  fs.writeFileSync(path, `${JSON.stringify(value, null, 2)}\n`);
}

function removeDep(pkg, dep) {
  for (const section of ["dependencies", "devDependencies", "peerDependencies", "optionalDependencies"]) {
    if (pkg[section] && Object.prototype.hasOwnProperty.call(pkg[section], dep)) {
      delete pkg[section][dep];
    }
  }
}

function ensureScript(pkg, name, value) {
  pkg.scripts ||= {};
  pkg.scripts[name] = value;
}

// Root package.json
if (fs.existsSync("package.json")) {
  const pkg = readJson("package.json");
  pkg.name = "notrelix-client";
  pkg.private = true;
  pkg.description ||= "Notrelix Client Workspace";
  pkg.packageManager ||= "pnpm@10.0.0";
  pkg.engines ||= {};
  pkg.engines.node ||= ">=22.0.0";
  pkg.engines.pnpm ||= ">=10.0.0";
  pkg.scripts ||= {};
  pkg.scripts["check:deps"] = "pnpm --filter @notrelix/dependency-rules check";
  pkg.scripts["clean"] = "turbo run clean && rm -rf node_modules .turbo";
  pkg.scripts["validate"] = "pnpm typecheck && pnpm lint && pnpm test && pnpm check:deps";
  writeJson("package.json", pkg);
}

// dependency-rules package: add check:deps alias for turbo compatibility
const depRulesPath = "tooling/dependency-rules/package.json";
if (fs.existsSync(depRulesPath)) {
  const pkg = readJson(depRulesPath);
  ensureScript(pkg, "check", "node src/check.mjs");
  ensureScript(pkg, "check:deps", "node src/check.mjs");
  writeJson(depRulesPath, pkg);
}

// Remove next from work-management packages
for (const p of [
  "packages/product/work-management/state/package.json",
  "packages/product/work-management/web/package.json"
]) {
  if (fs.existsSync(p)) {
    const pkg = readJson(p);
    removeDep(pkg, "next");
    writeJson(p, pkg);
  }
}

// Remove next-themes from apps/web
const webPkgPath = "apps/web/package.json";
if (fs.existsSync(webPkgPath)) {
  const pkg = readJson(webPkgPath);
  removeDep(pkg, "next-themes");
  writeJson(webPkgPath, pkg);
}

// Normalize apps/web tsconfig: keep app-local alias only
const webTsconfigPath = "apps/web/tsconfig.json";
if (fs.existsSync(webTsconfigPath)) {
  const tsconfig = readJson(webTsconfigPath);
  tsconfig.compilerOptions ||= {};
  tsconfig.compilerOptions.paths = {
    "@/*": ["./src/*"]
  };
  tsconfig.compilerOptions.types = Array.from(new Set([...(tsconfig.compilerOptions.types || []), "vite/client"]));
  writeJson(webTsconfigPath, tsconfig);
}
NODE

echo "==> Step 1.3: Replace next-themes in apps/web AppProviders"
cat > apps/web/src/providers/app-providers.tsx <<'TSX'
import {
  createContext,
  type ReactNode,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from 'react';
import { QueryClientProvider } from '@tanstack/react-query';
import { Toaster } from 'sonner';
import { createQueryClient } from '@notrelix/query';

type Theme = 'light' | 'dark' | 'system';

type ThemeContextValue = {
  theme: Theme;
  resolvedTheme: 'light' | 'dark';
  setTheme: (theme: Theme) => void;
};

const THEME_STORAGE_KEY = 'notrelix:web-theme';

const ThemeContext = createContext<ThemeContextValue | null>(null);

const queryClient = createQueryClient();

function getSystemTheme(): 'light' | 'dark' {
  if (typeof window === 'undefined') {
    return 'light';
  }

  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

function resolveTheme(theme: Theme): 'light' | 'dark' {
  return theme === 'system' ? getSystemTheme() : theme;
}

function applyTheme(theme: Theme): 'light' | 'dark' {
  const resolvedTheme = resolveTheme(theme);

  if (typeof document !== 'undefined') {
    document.documentElement.classList.toggle('dark', resolvedTheme === 'dark');
    document.documentElement.dataset.theme = resolvedTheme;
  }

  return resolvedTheme;
}

function readInitialTheme(defaultTheme: Theme): Theme {
  if (typeof window === 'undefined') {
    return defaultTheme;
  }

  const storedTheme = window.localStorage.getItem(THEME_STORAGE_KEY);

  if (storedTheme === 'light' || storedTheme === 'dark' || storedTheme === 'system') {
    return storedTheme;
  }

  return defaultTheme;
}

export function ThemeProvider({
  children,
  defaultTheme = 'system',
}: {
  children: ReactNode;
  defaultTheme?: Theme;
}) {
  const [theme, setThemeState] = useState<Theme>(() => readInitialTheme(defaultTheme));
  const [resolvedTheme, setResolvedTheme] = useState<'light' | 'dark'>(() =>
    applyTheme(readInitialTheme(defaultTheme)),
  );

  const setTheme = useCallback((nextTheme: Theme) => {
    setThemeState(nextTheme);
    window.localStorage.setItem(THEME_STORAGE_KEY, nextTheme);
    setResolvedTheme(applyTheme(nextTheme));
  }, []);

  useEffect(() => {
    setResolvedTheme(applyTheme(theme));

    if (theme !== 'system') {
      return;
    }

    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');

    const handleChange = () => {
      setResolvedTheme(applyTheme('system'));
    };

    mediaQuery.addEventListener('change', handleChange);

    return () => {
      mediaQuery.removeEventListener('change', handleChange);
    };
  }, [theme]);

  const value = useMemo<ThemeContextValue>(
    () => ({
      theme,
      resolvedTheme,
      setTheme,
    }),
    [theme, resolvedTheme, setTheme],
  );

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

export function useTheme() {
  const context = useContext(ThemeContext);

  if (!context) {
    throw new Error('useTheme must be used within ThemeProvider');
  }

  return context;
}

export function AppProviders({ children }: { children: ReactNode }) {
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider>
        {children}
        <Toaster richColors closeButton />
      </ThemeProvider>
    </QueryClientProvider>
  );
}
TSX

echo "==> Completed file changes."
echo ""
echo "Next manual steps:"
echo "  1. pnpm install"
echo "  2. pnpm typecheck"
echo "  3. pnpm lint"
echo "  4. pnpm test"
echo "  5. pnpm check:deps"
echo "  6. pnpm build"
echo ""
echo "Also verify there is no next dependency in packages/* package.json."
