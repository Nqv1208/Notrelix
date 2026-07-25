# Notrelix Client Workspace — Technical Project Structure

**Version:** 1.0  
**Purpose:** File-system technical blueprint cho Notrelix client workspace, mô tả rõ từng project dùng công nghệ gì, cấu hình gì, lockfile như nào, package reference ra sao, và boundary giữa Marketing Web, Product Web, Mobile, Packages.  
**Target architecture:** Multi-Client Enterprise Architecture v4.2

---

## 0. Technical Decisions

```txt
apps/marketing = Next.js App Router
apps/web       = Vite + React + TanStack Router
apps/mobile    = Expo / React Native placeholder
package manager = pnpm
root lockfile   = pnpm-lock.yaml
```

Rules:

```txt
Only one lockfile exists at client workspace root.
No package-lock.json.
No yarn.lock.
No bun.lock / bun.lockb in target state.
No app-level lockfile inside apps/marketing, apps/web, apps/mobile.
```

If the current repo already uses Bun, keep it only during transition if needed. The target state standardizes on `pnpm` because it is stable for large TypeScript monorepos, works well with Turborepo, strict workspace dependency boundaries, package exports, and CI frozen lockfile installation.

---

## 1. Final Root Structure

```txt
frontend/
  apps/
    marketing/
    web/
    mobile/

  packages/
    foundation/
      contracts/
      kernel/
      platform/
      query/
      realtime/
      observability/

    runtimes/
      web/
      mobile/

    ui/
      tokens/
      web/
      mobile/
      icons/

    product/
      work-management/
        core/
        state/
        plugins/
        web/
        mobile/
        testing/

      docs/
        core/
        collaboration/
        web/
        mobile/

      automation/
        core/
        web/
        mobile/

    features/
      auth/
      workspace/
      account/
      billing/
      integrations/
      notifications/
      activity/
      governance/
      search/
      collaboration/

  tooling/
    eslint/
    typescript/
    dependency-rules/
    codegen/
    testing/
    storybook/
    generators/

  docs/
    client/
      architecture/
      adr/
      migration/
      audits/
      archive/

  package.json
  pnpm-workspace.yaml
  pnpm-lock.yaml
  turbo.json
  tsconfig.base.json
  .npmrc
  .node-version
  .editorconfig
  .gitignore
  README.md
```

---

## 2. Root Configuration Files

### 2.1 `frontend/package.json`

Purpose:

```txt
Root workspace scripts.
Package manager declaration.
Workspace-level dev dependencies.
No business runtime code.
```

Recommended shape:

```json
{
  "name": "notrelix-client",
  "private": true,
  "packageManager": "pnpm@10.0.0",
  "engines": {
    "node": ">=22.0.0",
    "pnpm": ">=10.0.0"
  },
  "scripts": {
    "dev": "turbo dev",
    "dev:marketing": "pnpm --filter @notrelix/app-marketing dev",
    "dev:web": "pnpm --filter @notrelix/app-web dev",
    "dev:mobile": "pnpm --filter @notrelix/app-mobile dev",
    "build": "turbo build",
    "typecheck": "turbo typecheck",
    "lint": "turbo lint",
    "test": "turbo test",
    "format": "turbo format",
    "codegen": "turbo codegen",
    "check:deps": "turbo check:deps",
    "validate": "pnpm typecheck && pnpm lint && pnpm test && pnpm check:deps"
  },
  "devDependencies": {
    "turbo": "latest",
    "typescript": "latest",
    "eslint": "latest",
    "prettier": "latest"
  }
}
```

Rules:

```txt
Root package.json owns workspace commands only.
Apps/packages own their own runtime dependencies.
Do not install product dependencies at root unless they are tooling-only.
```

---

### 2.2 `frontend/pnpm-workspace.yaml`

Purpose:

```txt
Define workspace packages.
```

Content:

```yaml
packages:
  - "apps/*"
  - "packages/foundation/*"
  - "packages/runtimes/*"
  - "packages/ui/*"
  - "packages/product/*/*"
  - "packages/features/*"
  - "tooling/*"
```

`packages/product/*/*` is required because product domains are nested packages, for example:

```txt
packages/product/work-management/core
packages/product/work-management/state
packages/product/docs/web
```

---

### 2.3 `frontend/pnpm-lock.yaml`

Rules:

```txt
Commit this file.
Never manually edit it.
Generated only by pnpm install.
CI uses pnpm install --frozen-lockfile.
Exactly one lockfile exists in target state.
```

Forbidden:

```txt
apps/marketing/pnpm-lock.yaml
apps/web/pnpm-lock.yaml
apps/mobile/pnpm-lock.yaml
package-lock.json
yarn.lock
bun.lock
bun.lockb
```

---

### 2.4 `frontend/.npmrc`

Recommended during migration:

```ini
strict-peer-dependencies=false
auto-install-peers=false
shared-workspace-lockfile=true
link-workspace-packages=true
prefer-workspace-packages=true
```

Later, after dependency cleanup, tighten if the team wants stricter CI:

```ini
strict-peer-dependencies=true
```

---

### 2.5 `frontend/.node-version`

Recommended:

```txt
22
```

Use either `.node-version` or `.nvmrc`, not both, unless the team explicitly standardizes both.

---

### 2.6 `frontend/turbo.json`

Purpose:

```txt
Task orchestration.
Build graph.
Cache outputs.
Run app/package scripts consistently.
```

Recommended:

```json
{
  "$schema": "https://turbo.build/schema.json",
  "tasks": {
    "dev": {
      "cache": false,
      "persistent": true
    },
    "build": {
      "dependsOn": ["^build"],
      "outputs": ["dist/**", ".next/**", "build/**", "expo/**"]
    },
    "typecheck": {
      "dependsOn": ["^build"],
      "outputs": []
    },
    "lint": {
      "outputs": []
    },
    "test": {
      "dependsOn": ["^build"],
      "outputs": ["coverage/**"]
    },
    "format": {
      "outputs": []
    },
    "codegen": {
      "outputs": ["packages/foundation/contracts/src/generated/**"]
    },
    "check:deps": {
      "outputs": []
    }
  }
}
```

---

### 2.7 `frontend/tsconfig.base.json`

Purpose:

```txt
Shared compiler options.
No app-specific JSX/runtime config.
No broad paths aliases that bypass package exports.
```

Recommended:

```json
{
  "compilerOptions": {
    "target": "ES2022",
    "lib": ["ES2022", "DOM", "DOM.Iterable"],
    "module": "ESNext",
    "moduleResolution": "Bundler",
    "strict": true,
    "noUncheckedIndexedAccess": true,
    "exactOptionalPropertyTypes": true,
    "verbatimModuleSyntax": true,
    "skipLibCheck": true,
    "resolveJsonModule": true,
    "allowSyntheticDefaultImports": true,
    "forceConsistentCasingInFileNames": true,
    "baseUrl": "."
  }
}
```

Temporary path aliases are allowed only during migration and must be removed later.

---

## 3. Project: `apps/marketing`

### 3.1 Technology

```txt
Framework: Next.js App Router
Language: TypeScript
Styling: Tailwind via @notrelix/ui-web
Purpose: public SEO website
Env prefix: NEXT_PUBLIC_*
```

### 3.2 File tree

```txt
apps/
  marketing/
    src/
      app/
        layout.tsx
        page.tsx
        pricing/
          page.tsx
        about/
          page.tsx
        contact/
          page.tsx
        legal/
          privacy/
            page.tsx
          terms/
            page.tsx
        sitemap.ts
        robots.ts

      sections/
        home/
          hero-section.tsx
          product-demo-section.tsx
          social-proof-section.tsx
          pricing-preview-section.tsx
          cta-section.tsx
        pricing/
          pricing-hero.tsx
          pricing-table.tsx
          faq-section.tsx

      components/
        marketing-header.tsx
        marketing-footer.tsx
        marketing-shell.tsx
        seo-json-ld.tsx

      content/
        navigation.ts
        pricing-copy.ts
        faq.ts
        testimonials.ts

      lib/
        metadata.ts
        seo.ts
        marketing-routes.ts

      config/
        env.ts

      styles/
        globals.css

    public/
      images/
      icons/
      og/

    next.config.ts
    tailwind.config.ts
    postcss.config.mjs
    tsconfig.json
    package.json
    .env.example
```

### 3.3 Technical responsibilities

```txt
src/app/layout.tsx
  HTML shell, metadata defaults, global stylesheet import.
  No workspace auth, realtime provider, or product app providers.

src/app/page.tsx
  Landing page composition only.

src/components/marketing-header.tsx
  Public navigation and CTA to web app.
  CTA uses NEXT_PUBLIC_WEB_APP_URL.

src/config/env.ts
  Only file allowed to read process.env.NEXT_PUBLIC_*.

next.config.ts
  Next.js config for marketing only.
  No product workspace routing.
```

### 3.4 `apps/marketing/package.json`

```json
{
  "name": "@notrelix/app-marketing",
  "private": true,
  "scripts": {
    "dev": "next dev --port 3000",
    "build": "next build",
    "start": "next start --port 3000",
    "typecheck": "tsc --noEmit",
    "lint": "eslint ."
  },
  "dependencies": {
    "next": "latest",
    "react": "latest",
    "react-dom": "latest",
    "@notrelix/ui-web": "workspace:*",
    "@notrelix/ui-tokens": "workspace:*",
    "@notrelix/icons": "workspace:*"
  }
}
```

### 3.5 Marketing import rules

Allowed:

```txt
next/*
@notrelix/ui-web
@notrelix/ui-tokens
@notrelix/icons
```

Forbidden:

```txt
@notrelix/work-management-core
@notrelix/work-management-state
@notrelix/work-management-web
@notrelix/realtime
@notrelix/runtime-mobile
@notrelix/ui-mobile
```

---

## 4. Project: `apps/web`

### 4.1 Technology

```txt
Framework: React
Bundler: Vite
Router: TanStack Router
Data fetching: TanStack Query
Purpose: authenticated product web app
Env prefix: VITE_*
```

### 4.2 File tree

```txt
apps/
  web/
    index.html

    src/
      main.tsx
      router.tsx
      route-tree.gen.ts

      providers/
        app-providers.tsx
        config-provider.tsx
        query-provider.tsx
        platform-provider.tsx
        realtime-provider.tsx
        work-management-provider.tsx

      routes/
        __root.tsx
        index.tsx
        sign-in.tsx
        sign-up.tsx
        forgot-password.tsx
        invite/
          $token.tsx

        workspaces/
          $workspaceId/
            route.tsx
            index.tsx
            boards/
              $boardId.tsx
            docs/
              $docId.tsx
            dashboard.tsx
            chat.tsx
            members.tsx
            settings.tsx
            billing.tsx
            automations.tsx
            integrations.tsx

      shell/
        app-shell.tsx
        workspace-shell.tsx
        sidebar/
          workspace-sidebar.tsx
          sidebar-nav.tsx
          sidebar-section.tsx
        topbar/
          workspace-topbar.tsx
          command-button.tsx
          user-menu.tsx
        guards/
          auth-guard.tsx
          workspace-guard.tsx
          permission-guard.tsx
        boundaries/
          root-error-boundary.tsx
          workspace-error-boundary.tsx
          route-loading.tsx

      config/
        env.ts
        app-config.ts

      styles/
        globals.css

    public/
      favicon.svg

    vite.config.ts
    tailwind.config.ts
    postcss.config.mjs
    tsconfig.json
    package.json
    .env.example
```

### 4.3 Technical responsibilities

```txt
index.html
  Vite HTML entry, minimal root div only.

src/main.tsx
  React root creation, AppProviders, RouterProvider.
  No feature or board business logic.

src/router.tsx
  TanStack Router instance and route tree registration.
  Must not import next/*.

src/route-tree.gen.ts
  Generated by TanStack Router plugin.
  Do not manually edit.

src/providers/app-providers.tsx
  Composition root for Config, Observability, RuntimeWeb, Platform, Query, Realtime, WorkManagement.

src/config/env.ts
  Only file allowed to read import.meta.env.VITE_*.

src/routes/*
  Routes compose screens only.
```

Route example:

```tsx
export function RouteComponent() {
  const { workspaceId, boardId } = Route.useParams();
  const { view } = Route.useSearch();

  return (
    <BoardScreen
      workspaceId={workspaceId}
      boardId={boardId}
      viewId={view}
    />
  );
}
```

Forbidden in routes:

```txt
generated API call for board graph
BoardSnapshotDto mapping
Entity patch applying
field validation
permission rules
direct mutation logic
```

### 4.4 `apps/web/package.json`

```json
{
  "name": "@notrelix/app-web",
  "private": true,
  "scripts": {
    "dev": "vite --port 5173",
    "build": "vite build",
    "preview": "vite preview --port 5173",
    "typecheck": "tsc --noEmit",
    "lint": "eslint .",
    "test": "vitest"
  },
  "dependencies": {
    "@vitejs/plugin-react": "latest",
    "vite": "latest",
    "react": "latest",
    "react-dom": "latest",
    "@tanstack/react-router": "latest",
    "@tanstack/router-plugin": "latest",
    "@tanstack/react-query": "latest",
    "@notrelix/contracts": "workspace:*",
    "@notrelix/platform": "workspace:*",
    "@notrelix/query": "workspace:*",
    "@notrelix/realtime": "workspace:*",
    "@notrelix/observability": "workspace:*",
    "@notrelix/runtime-web": "workspace:*",
    "@notrelix/ui-web": "workspace:*",
    "@notrelix/ui-tokens": "workspace:*",
    "@notrelix/work-management-web": "workspace:*",
    "@notrelix/work-management-state": "workspace:*"
  }
}
```

### 4.5 `apps/web/vite.config.ts`

```ts
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { TanStackRouterVite } from "@tanstack/router-plugin/vite";

export default defineConfig({
  plugins: [
    TanStackRouterVite(),
    react()
  ],
  server: {
    port: 5173
  }
});
```

### 4.6 Web import rules

Allowed:

```txt
@notrelix/runtime-web
@notrelix/ui-web
@notrelix/platform
@notrelix/query
@notrelix/realtime
@notrelix/work-management-web
@notrelix/features-*/web exports
```

Forbidden:

```txt
next/*
next/link
next/image
next/navigation
next/server
@notrelix/ui-mobile
@notrelix/runtime-mobile
```

---

## 5. Project: `apps/mobile`

### 5.1 Technology

```txt
Framework: Expo / React Native
Router: Expo Router or React Navigation
Purpose: future mobile app placeholder
Env prefix: EXPO_PUBLIC_*
```

### 5.2 File tree

```txt
apps/
  mobile/
    app/
      _layout.tsx
      index.tsx
      sign-in.tsx
      sign-up.tsx

      workspaces/
        [workspaceId]/
          index.tsx
          boards/
            [boardId].tsx
          items/
            [itemId].tsx
          docs/
            [docId].tsx
          notifications.tsx
          settings.tsx

    src/
      providers/
        mobile-app-providers.tsx
        mobile-config-provider.tsx
        mobile-query-provider.tsx
        mobile-platform-provider.tsx
        mobile-realtime-provider.tsx

      navigation/
        linking.ts
        navigation-theme.ts

      shell/
        mobile-app-shell.tsx
        workspace-tab-shell.tsx
        guards/
          mobile-auth-guard.tsx
          mobile-workspace-guard.tsx

      config/
        env.ts
        app-config.ts

    assets/
      icon.png
      splash.png

    app.json
    babel.config.js
    metro.config.js
    tsconfig.json
    package.json
    .env.example
```

### 5.3 `apps/mobile/package.json`

```json
{
  "name": "@notrelix/app-mobile",
  "private": true,
  "scripts": {
    "dev": "expo start",
    "android": "expo run:android",
    "ios": "expo run:ios",
    "typecheck": "tsc --noEmit",
    "lint": "eslint ."
  },
  "dependencies": {
    "expo": "latest",
    "react": "latest",
    "react-native": "latest",
    "@notrelix/runtime-mobile": "workspace:*",
    "@notrelix/ui-mobile": "workspace:*",
    "@notrelix/ui-tokens": "workspace:*",
    "@notrelix/platform": "workspace:*",
    "@notrelix/query": "workspace:*",
    "@notrelix/work-management-mobile": "workspace:*"
  }
}
```

### 5.4 Mobile rules

Allowed:

```txt
@notrelix/runtime-mobile
@notrelix/ui-mobile
@notrelix/platform
@notrelix/query
@notrelix/features-*/mobile
@notrelix/work-management-mobile
```

Forbidden:

```txt
@notrelix/ui-web
shadcn
radix
next/*
DOM APIs
```

---

## 6. Foundation Package Details

### 6.1 `@notrelix/contracts`

Location:

```txt
packages/foundation/contracts/
```

Files:

```txt
src/
  generated/
    rest/
      api.ts
      schemas.ts
      models.ts
      client.ts
    events/
      board-events.ts
      notification-events.ts
      activity-events.ts
      presence-events.ts
      automation-events.ts

  client/
    create-notrelix-client.ts
    api-client-config.ts
    request-context.ts
    error-mapper.ts

  types/
    api-error.ts
    pagination.ts
    problem-details.ts

  index.ts

openapi.config.ts
asyncapi.config.ts
package.json
tsconfig.json
```

Package exports:

```json
{
  "name": "@notrelix/contracts",
  "private": true,
  "type": "module",
  "exports": {
    ".": "./src/index.ts",
    "./client": "./src/client/create-notrelix-client.ts",
    "./generated/rest": "./src/generated/rest/api.ts",
    "./generated/events": "./src/generated/events/index.ts"
  }
}
```

Rules:

```txt
No React.
No app framework.
No env read.
No internal package import.
Generated files are read-only.
```

---

### 6.2 `@notrelix/kernel`

Location:

```txt
packages/foundation/kernel/
```

Files:

```txt
src/
  result/
    result.ts
    errors.ts
  ids/
    client-operation-id.ts
    correlation-id.ts
  date/
    clock.ts
    format.ts
  events/
    typed-event-emitter.ts
  assertions/
    invariant.ts
  env/
    env-schema.ts
  index.ts

package.json
tsconfig.json
```

Rule:

```txt
Must not import any internal package.
```

---

### 6.3 `@notrelix/platform`

Location:

```txt
packages/foundation/platform/
```

Files:

```txt
src/
  auth/
    auth-session.ts
    auth-provider.tsx
    use-auth-session.ts
    logout.ts

  workspace/
    workspace-context.ts
    workspace-provider.tsx
    use-current-workspace.ts
    workspace-scope.ts

  permissions/
    ability.ts
    ability-provider.tsx
    capability-manifest.ts
    can.tsx
    use-ability.ts
    command-guard.ts

  feature-flags/
    flag-client.ts
    feature-flag-provider.tsx
    use-feature-flag.ts

  runtime/
    runtime-adapter.ts
    storage-adapter.ts
    navigation-adapter.ts
    notification-adapter.ts
    clipboard-adapter.ts
    network-adapter.ts
    file-picker-adapter.ts

  config/
    runtime-config.ts
    config-provider.tsx

  index.ts

package.json
tsconfig.json
```

Rules:

```txt
Defines runtime contracts.
Does not implement web/native APIs.
Does not import ui-web/ui-mobile.
```

---

### 6.4 `@notrelix/query`

Location:

```txt
packages/foundation/query/
```

Files:

```txt
src/
  query-client.ts
  query-provider.tsx
  query-error-policy.ts
  query-invalidation.ts
  index.ts

package.json
tsconfig.json
```

Rules:

```txt
Feature query keys do not live here.
Work Management Type B data must not use Query as source of truth.
```

---

### 6.5 `@notrelix/realtime`

Location:

```txt
packages/foundation/realtime/
```

Files:

```txt
src/
  transport/
    realtime-client.ts
    signalr-client.ts
    connection-state.ts
    reconnect-policy.ts

  subscriptions/
    subscription-manager.ts
    workspace-subscription.ts
    board-subscription.ts
    presence-subscription.ts

  event-router/
    event-router.ts
    event-handler-registry.ts
    unknown-event-handler.ts

  presence/
    presence-client.ts
    presence-heartbeat.ts
    presence-events.ts

  index.ts

package.json
tsconfig.json
```

Rules:

```txt
No BroadcastChannel direct usage.
No AppState direct usage.
No board patch semantics.
```

---

### 6.6 `@notrelix/observability`

Location:

```txt
packages/foundation/observability/
```

Files:

```txt
src/
  telemetry-client.ts
  telemetry-provider.tsx
  metrics.ts
  trace.ts
  error-reporter.ts
  performance-markers.ts
  index.ts

package.json
tsconfig.json
```

---

## 7. Runtime Package Details

### 7.1 `@notrelix/runtime-web`

```txt
packages/runtimes/web/
  src/
    storage/
      web-storage-adapter.ts
    navigation/
      web-navigation-adapter.ts
    notifications/
      web-notification-adapter.ts
    clipboard/
      web-clipboard-adapter.ts
    network/
      web-network-adapter.ts
    broadcast-channel/
      web-broadcast-channel.ts
    index.ts

  package.json
  tsconfig.json
```

Allowed APIs:

```txt
window
document
localStorage
navigator
BroadcastChannel
```

---

### 7.2 `@notrelix/runtime-mobile`

```txt
packages/runtimes/mobile/
  src/
    storage/
      secure-storage-adapter.ts
      async-storage-adapter.ts
    navigation/
      mobile-navigation-adapter.ts
      deep-linking-adapter.ts
    notifications/
      push-notification-adapter.ts
    clipboard/
      mobile-clipboard-adapter.ts
    network/
      mobile-network-adapter.ts
    files/
      mobile-file-picker-adapter.ts
    app-state/
      app-state-adapter.ts
    index.ts

  package.json
  tsconfig.json
```

Forbidden:

```txt
DOM APIs
ui-web
shadcn
radix
```

---

## 8. UI Package Details

### 8.1 `@notrelix/ui-tokens`

```txt
packages/ui/tokens/
  src/
    colors.ts
    typography.ts
    spacing.ts
    radius.ts
    shadows.ts
    motion.ts
    density.ts
    semantic.ts
    themes/
      light.ts
      dark.ts
      high-contrast.ts
    index.ts

  package.json
  tsconfig.json
```

Rules:

```txt
No React.
No DOM.
No React Native.
No business imports.
```

---

### 8.2 `@notrelix/ui-web`

```txt
packages/ui/web/
  src/
    components/
      ui/
        button.tsx
        input.tsx
        dialog.tsx
        dropdown-menu.tsx
        popover.tsx
        tabs.tsx
        tooltip.tsx
        badge.tsx
        card.tsx
        table.tsx
        skeleton.tsx
        toast.tsx
        avatar.tsx
        command.tsx
        sidebar.tsx
        sheet.tsx
        calendar.tsx
        checkbox.tsx
        select.tsx
        textarea.tsx

      feedback/
        empty-state.tsx
        error-state.tsx
        loading-state.tsx

      enterprise/
        data-grid/
        inline-editor/
        command-menu/
        conflict-banner/
        permission-gate/
        presence-avatar/
        resizable-layout/

    styles/
      globals.css
      tokens.css

    lib/
      cn.ts
      compose-refs.ts

    hooks/
      use-media-query.ts
      use-controllable-state.ts

    index.ts
    ui.ts
    feedback.ts
    enterprise.ts

  components.json
  tailwind.preset.ts
  package.json
  tsconfig.json
```

Responsibilities:

```txt
Web-only design system.
shadcn/Radix/Tailwind primitives.
Generic web enterprise components.
```

Rules:

```txt
No business imports.
No mobile imports.
```

---

### 8.3 `@notrelix/ui-mobile`

```txt
packages/ui/mobile/
  src/
    components/
      primitives/
        button.tsx
        input.tsx
        text.tsx
        card.tsx
        sheet.tsx
        modal.tsx
        list.tsx
        avatar.tsx
        badge.tsx
        spinner.tsx
        toast.tsx

      feedback/
        empty-state.tsx
        error-state.tsx
        loading-state.tsx

      enterprise/
        mobile-list/
        bottom-sheet-form/
        conflict-banner/
        permission-gate/
        presence-avatar/

    theme/
      mobile-theme-provider.tsx
      create-native-styles.ts

    index.ts

  package.json
  tsconfig.json
```

---

### 8.4 `@notrelix/icons`

```txt
packages/ui/icons/
  src/
    brand/
      notrelix-logo.tsx
      notrelix-symbol.tsx
    generated/
      index.ts
    index.ts

  package.json
  tsconfig.json
```

---

## 9. Product Package Details

### 9.1 `@notrelix/work-management-core`

```txt
packages/product/work-management/core/
  src/
    entities/
      board/
        board.entity.ts
        board.types.ts
        board.normalizer.ts
      view/
        board-view.entity.ts
        board-view.types.ts
      group/
        group.entity.ts
        group.types.ts
      item/
        item.entity.ts
        item.types.ts
      field/
        field.entity.ts
        field.types.ts
      cell/
        cell-value.entity.ts
        cell-value.types.ts
      comment/
        comment.entity.ts
        comment.types.ts
      presence/
        presence.entity.ts
        presence.types.ts

    commands/
      command.ts
      command-bus.ts
      command-result.ts
      create-item.command.ts
      update-cell.command.ts
      move-item.command.ts
      create-field.command.ts
      update-field-config.command.ts
      delete-field.command.ts
      update-board-view.command.ts

    view-engine/
      view-config.ts
      filter-engine.ts
      sort-engine.ts
      group-engine.ts
      aggregation-engine.ts
      invalid-view-config.ts

    permissions/
      work-management-abilities.ts
      command-permission-map.ts

    index.ts

  package.json
  tsconfig.json
```

Rules:

```txt
No UI.
No DOM.
No React Native.
No direct runtime API.
```

---

### 9.2 `@notrelix/work-management-state`

```txt
packages/product/work-management/state/
  src/
    store/
      work-management-store.ts
      store-provider.tsx
      entity-scope.ts

    patches/
      board-patch.ts
      patch-reducer.ts
      patch-ordering.ts
      revision.ts
      tombstone.ts

    optimistic/
      optimistic-transaction.ts
      transaction-log.ts
      rollback.ts
      temp-id-map.ts

    selectors/
      select-board.ts
      select-board-views.ts
      select-items.ts
      select-cell-value.ts
      select-table-rows.ts
      select-kanban-columns.ts
      selector-cache.ts
      structural-sharing.ts

    sync/
      board-snapshot-loader.ts
      board-snapshot-normalizer.ts
      board-patch-handler.ts
      board-realtime-bindings.ts
      board-resync.ts

    diagnostics/
      store-devtools.tsx
      store-metrics.ts

    index.ts

  package.json
  tsconfig.json
```

Rules:

```txt
Can import work-management-core.
Can import contracts/realtime/platform.
Cannot import UI.
```

---

### 9.3 `@notrelix/work-management-plugins`

```txt
packages/product/work-management/plugins/
  src/
    field-types/
      field-type-definition.ts
      field-type-registry.ts
      built-in/
        text/
          text.definition.ts
        status/
          status.definition.ts
        date/
          date.definition.ts
        people/
          people.definition.ts
        checkbox/
          checkbox.definition.ts
        link/
          link.definition.ts

    view-types/
      view-type-definition.ts
      view-type-registry.ts

    dashboard-widgets/
      widget-definition.ts
      widget-registry.ts

    index.ts

  package.json
  tsconfig.json
```

Rules:

```txt
Logic-only plugin definitions.
No React renderer.
No web/mobile UI.
```

---

### 9.4 `@notrelix/work-management-web`

```txt
packages/product/work-management/web/
  src/
    screens/
      board-screen/
        board-screen.tsx
        board-screen-loader.tsx
        board-screen-error.tsx
        board-screen-toolbar.tsx
      workspace-home-screen/
        workspace-home-screen.tsx

    views/
      table/
        table-view.tsx
        table-header.tsx
        table-row.tsx
        table-cell.tsx
        table-virtualizer.tsx
        table-keyboard-navigation.ts
      kanban/
        kanban-view.tsx
        kanban-column.tsx
        kanban-card.tsx
        kanban-dnd.ts
      calendar/
        calendar-view.tsx
        calendar-event.tsx
      timeline/
        timeline-view.tsx
        timeline-row.tsx
        timeline-bar.tsx
      dashboard/
        dashboard-view.tsx
        widgets/

    field-renderers/
      text.web.tsx
      status.web.tsx
      date.web.tsx
      people.web.tsx
      checkbox.web.tsx
      link.web.tsx

    field-editors/
      text-editor.web.tsx
      status-editor.web.tsx
      date-editor.web.tsx
      people-editor.web.tsx

    components/
      board-toolbar/
      cell-renderer/
      item-card/
      presence-layer/
      conflict/

    hooks/
      use-board-screen.ts
      use-cell-editing.ts

    index.ts

  package.json
  tsconfig.json
```

Rules:

```txt
Can import ui-web.
Cannot import ui-mobile.
Cannot import runtime-mobile.
```

---

### 9.5 `@notrelix/work-management-mobile`

```txt
packages/product/work-management/mobile/
  src/
    screens/
      board-screen/
        mobile-board-screen.tsx
        mobile-board-header.tsx
      item-detail/
        mobile-item-detail-screen.tsx
        mobile-item-fields.tsx
      workspace-home/
        mobile-workspace-home.tsx

    views/
      list/
        mobile-item-list.tsx
        mobile-item-row.tsx
      kanban/
        mobile-kanban-view.tsx
        mobile-kanban-column.tsx
        mobile-kanban-card.tsx
      calendar/
        mobile-calendar-agenda.tsx

    field-renderers/
      text.native.tsx
      status.native.tsx
      date.native.tsx
      people.native.tsx
      checkbox.native.tsx
      link.native.tsx

    field-editors/
      text-editor.native.tsx
      status-editor.native.tsx
      date-editor.native.tsx
      people-editor.native.tsx

    components/
      bottom-sheet-cell-editor/
      mobile-board-toolbar/
      mobile-conflict-banner/
      mobile-presence-indicator/

    index.ts

  package.json
  tsconfig.json
```

---

## 10. Feature Package Pattern

Each feature uses:

```txt
packages/features/<feature>/
  core/
    api/
      <feature>.api.ts
    query/
      <feature>.keys.ts
      use-<feature>.ts
    mutations/
      use-<feature>-mutation.ts
    model/
      <feature>.types.ts
      <feature>.mapper.ts
    schemas/
      <feature>.schema.ts
    permissions/
      <feature>.permissions.ts

  web/
    screens/
      <feature>-screen.web.tsx
    components/

  mobile/
    screens/
      <feature>-screen.native.tsx
    components/

  testing/
    <feature>.fixtures.ts

  index.ts
  package.json
  tsconfig.json
```

Feature package names:

```txt
@notrelix/features-auth
@notrelix/features-workspace
@notrelix/features-account
@notrelix/features-billing
@notrelix/features-integrations
@notrelix/features-notifications
@notrelix/features-activity
@notrelix/features-governance
@notrelix/features-search
@notrelix/features-collaboration
```

Rules:

```txt
core has no UI.
web imports ui-web.
mobile imports ui-mobile.
Type A/C uses Query.
Type B Work Management data does not live in features.
```

---

## 11. Tooling Details

```txt
tooling/
  eslint/
    package.json
    index.js
    react.js
    boundaries.js
    web.js
    mobile.js

  typescript/
    base.json
    react-library.json
    web-app.json
    mobile-app.json
    node-tooling.json

  dependency-rules/
    dependency-cruiser.config.cjs
    package-boundaries.ts
    forbidden-imports.ts

  codegen/
    openapi/
      orval.config.ts
      generate-openapi.ts
    asyncapi/
      asyncapi.config.ts
      generate-events.ts

  testing/
    vitest.config.ts
    playwright.config.ts
    mobile-test-utils.ts
    web-test-utils.ts
    mock-server.ts

  storybook/
    web/
    mobile/

  generators/
    create-feature/
    create-product-module/
    create-ui-component/
```

---

## 12. Reference Strategy

### 12.1 Workspace references

Use workspace protocol:

```json
{
  "dependencies": {
    "@notrelix/ui-web": "workspace:*",
    "@notrelix/platform": "workspace:*"
  }
}
```

Forbidden:

```txt
../../../../packages/ui/web/src/components/ui/button
```

Allowed:

```txt
@notrelix/ui-web/ui/button
```

---

### 12.2 Package exports

Each package must expose public API through `exports`.

Example:

```json
{
  "name": "@notrelix/ui-web",
  "exports": {
    ".": "./src/index.ts",
    "./ui/button": "./src/components/ui/button.tsx",
    "./ui/dialog": "./src/components/ui/dialog.tsx",
    "./feedback": "./src/feedback.ts",
    "./enterprise": "./src/enterprise.ts",
    "./tailwind-preset": "./tailwind.preset.ts"
  }
}
```

Forbidden:

```txt
deep import into src internals unless explicitly exported
```

---

### 12.3 TypeScript references

Recommended:

```txt
Apps and packages import through package names.
Turborepo manages task order.
TypeScript project references are optional, not a replacement for package boundaries.
```

Optional app reference:

```json
{
  "references": [
    { "path": "../../packages/ui/web" },
    { "path": "../../packages/foundation/platform" }
  ]
}
```

---

### 12.4 Barrel exports

Allowed:

```txt
Package-level public API.
Small grouped public exports.
```

Avoid:

```txt
export * from every internal file.
giant barrel that exposes internals.
```

Preferred:

```ts
export { Button } from "./components/ui/button";
export { Dialog } from "./components/ui/dialog";
```

---

## 13. Env Strategy

### 13.1 Marketing env

```txt
apps/marketing/.env.example
```

```env
NEXT_PUBLIC_SITE_URL=https://www.notrelix.com
NEXT_PUBLIC_WEB_APP_URL=https://app.notrelix.com
NEXT_PUBLIC_API_URL=https://api.notrelix.com
```

Only read in:

```txt
apps/marketing/src/config/env.ts
```

---

### 13.2 Web env

```txt
apps/web/.env.example
```

```env
VITE_APP_URL=https://app.notrelix.com
VITE_MARKETING_URL=https://www.notrelix.com
VITE_API_URL=https://api.notrelix.com
VITE_REALTIME_URL=https://api.notrelix.com/realtime
```

Only read in:

```txt
apps/web/src/config/env.ts
```

---

### 13.3 Mobile env

```txt
apps/mobile/.env.example
```

```env
EXPO_PUBLIC_API_URL=https://api.notrelix.com
EXPO_PUBLIC_REALTIME_URL=https://api.notrelix.com/realtime
EXPO_PUBLIC_WEB_URL=https://app.notrelix.com
```

Only read in:

```txt
apps/mobile/src/config/env.ts
```

---

### 13.4 Package env rule

Packages do not read env.

Correct:

```ts
createNotrelixClient({
  baseUrl: appConfig.apiUrl
});
```

Forbidden:

```txt
process.env inside packages
import.meta.env inside packages
```

---

## 14. CI / Validation Commands

Required root commands:

```bash
pnpm install --frozen-lockfile
pnpm typecheck
pnpm lint
pnpm test
pnpm build
pnpm check:deps
```

Required CI stages:

```txt
Install
Generate contracts
Typecheck
Lint
Unit tests
Dependency boundary check
Build packages
Build marketing
Build web
Mobile typecheck placeholder
```

---

## 15. Technical Acceptance Checklist

```txt
[ ] Exactly one pnpm-lock.yaml exists at root.
[ ] No package-lock.json/yarn.lock/bun.lock in target state.
[ ] apps/marketing uses Next.js only.
[ ] apps/web uses Vite + TanStack Router only.
[ ] apps/mobile uses Expo/React Native only.
[ ] packages do not import next/*.
[ ] packages do not read env directly.
[ ] apps/web does not import next/*.
[ ] ui/web contains shadcn components.
[ ] ui/mobile does not import ui/web.
[ ] work-management/core has no UI imports.
[ ] work-management/state has no UI imports.
[ ] product UI is split into web/mobile.
[ ] feature core is UI-free.
[ ] feature web imports ui-web.
[ ] feature mobile imports ui-mobile.
[ ] dependency boundary check exists.
[ ] package exports prevent deep imports.
[ ] current UI is preserved during migration.
[ ] no delete without Safe Delete Audit.
```

---

## 16. Final Summary

```txt
apps/marketing = Next.js public web
apps/web       = Vite product web
apps/mobile    = Expo mobile placeholder

packages/foundation = contracts/platform/query/realtime/kernel/observability
packages/runtimes   = runtime-web/runtime-mobile adapters
packages/ui         = tokens/web/mobile/icons
packages/product    = work-management/docs/automation
packages/features   = normal business modules
tooling             = lint/typescript/codegen/testing/boundary
docs/client         = architecture/adr/migration/audits
```

The most important implementation rule:

```txt
Apps compose.
Packages own logic.
UI is runtime-specific.
Core is runtime-neutral.
Lockfile is root-only.
References go through package exports.
No delete without audit.
```
