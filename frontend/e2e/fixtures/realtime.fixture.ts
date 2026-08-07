import { test as base, expect } from '@playwright/test';

export interface RealtimeFixtureOptions {
  mockEvents?: Array<{ eventId: string; eventType: string; workspaceId: string }>;
}

export const test = base.extend<{ realtimeFixture: RealtimeFixtureOptions }>({
  realtimeFixture: [{ mockEvents: [] }, { option: true }],
});

export { expect };
