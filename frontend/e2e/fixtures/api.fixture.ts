import { test as base } from '@playwright/test';

export interface ApiFixtureOptions {
  mockSessionUser?: { id: string; email: string; name: string } | null;
}

export const test = base.extend<{ apiFixture: ApiFixtureOptions }>({
  apiFixture: [{ mockSessionUser: null }, { option: true }],
});

export { expect } from '@playwright/test';
