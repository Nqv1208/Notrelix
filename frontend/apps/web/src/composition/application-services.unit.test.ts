import { describe, expect, it, vi } from 'vitest';
import type { AppRuntime } from '@notrelix/runtime-web';
import type { NotrelixClient } from '@notrelix/contracts';
import { createWebApplicationServices } from './application-services';

function createRuntime(client: NotrelixClient): AppRuntime {
  return {
    api: client,
    environment: {
      apiBaseUrl: 'http://api.test',
      realtimeUrl: 'ws://realtime.test',
    },
    sessionEvents: {
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      dispatchEvent: vi.fn(),
    } as unknown as AppRuntime['sessionEvents'],
    dispose: vi.fn(),
  } as unknown as AppRuntime;
}

function createClient(label: string): NotrelixClient {
  return {
    api: {
      get: vi.fn(async () => label),
      post: vi.fn(async () => label),
      put: vi.fn(async () => label),
      patch: vi.fn(async () => label),
      delete: vi.fn(async () => label),
    },
    endpoints: {},
  } as unknown as NotrelixClient;
}

describe('createWebApplicationServices', () => {
  it('keeps Work Management services scoped to their runtime client', async () => {
    const firstClient = createClient('first');
    const secondClient = createClient('second');

    const first = createWebApplicationServices(createRuntime(firstClient));
    const second = createWebApplicationServices(createRuntime(secondClient));

    await first.workManagement.cards.moveCard({
      cardId: 'card-1',
      listId: 'group-1',
      position: 1,
    });
    await second.workManagement.cards.moveCard({
      cardId: 'card-2',
      listId: 'group-2',
      position: 2,
    });

    expect(firstClient.api.post).toHaveBeenCalledWith('/cards/card-1/move', {
      listId: 'group-1',
      position: 1,
    });
    expect(secondClient.api.post).toHaveBeenCalledWith('/cards/card-2/move', {
      listId: 'group-2',
      position: 2,
    });
    expect(firstClient.api.post).not.toHaveBeenCalledWith('/cards/card-2/move', expect.anything());
    expect(secondClient.api.post).not.toHaveBeenCalledWith('/cards/card-1/move', expect.anything());
  });
});
