import { describe, expect, it, vi } from 'vitest';
import { QueryClient } from '@tanstack/react-query';
import { RealtimeClient, type WebSocketLike } from '@notrelix/realtime';

function createMockSocket() {
  const socket: WebSocketLike = {
    readyState: 1,
    send: vi.fn(),
    close: vi.fn(function (this: WebSocketLike) {
      if (this.onclose) this.onclose({ code: 1000, reason: 'Closed' });
    }),
    onopen: null,
    onmessage: null,
    onclose: null,
    onerror: null,
  };
  return socket;
}

function validEnvelope(eventId: string, eventType = 'board.item.updated'): string {
  return JSON.stringify({
    schemaVersion: 1,
    eventId,
    eventType,
    workspaceId: 'ws-1',
    correlationId: 'corr-1',
    timestamp: new Date().toISOString(),
    payload: {},
  });
}

describe('integration: realtime invalidation reaches the query cache', () => {
  it('an invalidation callback from a realtime event invalidates the matching query key', async () => {
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });

    const fullBoardKey = ['boards', 'full', 'ws-1', 'board-1'];
    queryClient.setQueryData(fullBoardKey, { title: 'Old' });

    const socket = createMockSocket();
    const client = new RealtimeClient('wss://ws.test/realtime', {
      socketFactory: vi.fn(() => socket),
    });

    const connectPromise = client.connect({ sessionGeneration: 'gen-1' });
    await Promise.resolve();
    socket.onopen?.({});
    await connectPromise;

    client.subscribe({ workspaceId: 'ws-1' }, () => {
      void queryClient.invalidateQueries({ queryKey: fullBoardKey });
    });

    socket.onmessage?.({ data: validEnvelope('evt-2') });

    const state = queryClient.getQueryState(fullBoardKey);
    expect(state?.isInvalidated).toBe(true);
  });
});
