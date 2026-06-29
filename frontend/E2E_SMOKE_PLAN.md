# Notrelix E2E Smoke Testing Plan

This document outlines the End-to-End (E2E) smoke testing plan for the Notrelix Enterprise frontend. Since there is currently no E2E testing framework installed, this plan acts as a blueprint for implementing Playwright tests.

---

## 1. Setup & Tooling Recommendation

We recommend using **Playwright** as the E2E testing framework due to its speed, reliability, and built-in support for multiple browsers, parallel execution, and trace viewing.

### Installation
Run the following command in the `frontend` root:
```bash
bun add -d @playwright/test
bun x playwright install
```

### Configuration (`playwright.config.ts`)
Configure Playwright to run against the local development server:
```typescript
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: 'html',
  use: {
    baseURL: 'http://localhost:3000',
    trace: 'on-first-retry',
    video: 'on-first-retry',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  webServer: {
    command: 'bun run dev',
    url: 'http://localhost:3000',
    reuseExistingServer: !process.env.CI,
  },
});
```

---

## 2. Core Smoke Test Scenarios

Create the test files under `frontend/e2e/`.

### Scenario 1: Authentication Flow (`e2e/auth.spec.ts`)
Verify that users can sign in successfully.

```typescript
import { test, expect } from '@playwright/test';

test('User can sign in successfully', async ({ page }) => {
  await page.goto('/sign-in');
  
  await page.fill('input[name="email"]', 'owner@notrelix.com');
  await page.fill('input[name="password"]', 'password123');
  await page.click('button[type="submit"]');
  
  // Should redirect to workspace home
  await expect(page).toHaveURL(/\/home/);
  await expect(page.locator('text=Workspace')).toBeVisible();
});
```

### Scenario 2: Auth Refresh Failure Redirect (`e2e/auth-refresh.spec.ts`)
Verify that a refresh token failure (HTTP 401 on `/auth/refresh`) dispatches the `auth:failure` event and redirects the user to the sign-in page.

```typescript
import { test, expect } from '@playwright/test';

test('Auth refresh failure redirects to sign-in', async ({ page }) => {
  await page.goto('/home');
  
  // Mock the refresh endpoint to return 401
  await page.route('**/api/v1/auth/refresh', async (route) => {
    await route.fulfill({
      status: 401,
      contentType: 'application/json',
      body: JSON.stringify({ message: 'Session expired' }),
    });
  });

  // Trigger a request that will fail auth, prompting a refresh attempt
  await page.evaluate(() => {
    window.dispatchEvent(new CustomEvent('auth:failure'));
  });
  
  // Should clear cache and redirect to sign-in
  await expect(page).toHaveURL(/\/sign-in/);
});
```

### Scenario 3: Workspace Switching (`e2e/workspace.spec.ts`)
Verify that users can switch between workspaces.

```typescript
import { test, expect } from '@playwright/test';

test('User can switch workspaces', async ({ page }) => {
  await page.goto('/home');
  
  // Click workspace switcher
  await page.click('[data-testid="workspace-switcher"]');
  
  // Select a different workspace
  await page.click('[data-testid="workspace-item-other"]');
  
  // URL should update to the new workspace ID
  await expect(page).toHaveURL(/\/([a-f0-9-]+)\/dashboard/);
});
```

### Scenario 4: Open Board View (`e2e/board.spec.ts`)
Verify that a board view renders successfully and switches tabs.

```typescript
import { test, expect } from '@playwright/test';

test('User can open a board and switch views', async ({ page }) => {
  await page.goto('/workspace-id/boards/board-id');
  
  // Board title should be visible
  await expect(page.locator('h1')).toBeVisible();
  
  // Verify Table view is active by default
  await expect(page.locator('[data-testid="table-view"]')).toBeVisible();
  
  // Switch to Kanban view
  await page.click('text=Kanban');
  await expect(page.locator('[data-testid="kanban-view"]')).toBeVisible();
});
```

### Scenario 5: Open Docs Page (`e2e/docs.spec.ts`)
Verify that a document page loads and the editor is interactive.

```typescript
import { test, expect } from '@playwright/test';

test('User can open a document and edit it', async ({ page }) => {
  await page.goto('/workspace-id/docs/page-id');
  
  // Editor should load
  await expect(page.locator('.tiptap')).toBeVisible();
  
  // Type in the editor
  await page.focus('.tiptap');
  await page.keyboard.type('Hello Notrelix Docs!');
  
  // Verify text is present
  await expect(page.locator('.tiptap')).toContainText('Hello Notrelix Docs!');
});
```

### Scenario 6: Mock Disabled State (`e2e/mock-mode.spec.ts`)
Verify that features show a "Mock Disabled" state when mock mode is off and the backend is not available.

```typescript
import { test, expect } from '@playwright/test';

test('Shows Mock Disabled state when mock mode is disabled', async ({ page }) => {
  // Inject environment config mockMode = false
  await page.addInitScript(() => {
    window.__mock_mode_config = { billing: false };
  });
  
  await page.goto('/workspace-id/billing');
  
  // Should show MockDisabledState component
  await expect(page.locator('text=Tính năng đang được phát triển')).toBeVisible();
  await expect(page.locator('text=Mock Mode đang tắt')).toBeVisible();
});
```

### Scenario 7: Access Denied State (`e2e/permissions.spec.ts`)
Verify that unauthorized users see an "Access Denied" state.

```typescript
import { test, expect } from '@playwright/test';

test('Shows Access Denied state for unauthorized resources', async ({ page }) => {
  // Navigate to a page the user has no permissions for (e.g. settings as member)
  await page.goto('/workspace-id/settings/governance');
  
  // Should render AccessDeniedState component
  await expect(page.locator('text=Không có quyền truy cập')).toBeVisible();
  await expect(page.locator('text=Tài khoản của bạn không được cấp quyền')).toBeVisible();
});
```
