import { test as base, expect } from '@playwright/test';

export interface AuthFixtureOptions {
  authenticatedUser?: { id: string; email: string; workspaceId: string } | null;
}

export const test = base.extend<{ authFixture: AuthFixtureOptions }>({
  authFixture: [{ authenticatedUser: null }, { option: true }],
});

export { expect };
