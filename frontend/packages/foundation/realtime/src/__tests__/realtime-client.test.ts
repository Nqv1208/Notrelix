import { describe, it, expect, vi, beforeEach } from 'vitest';
import { RealtimeClient, transitionState, type WebSocketLike } from '../transport/realtime-client';

describe('RealtimeStateTransition Machine', () => {
  it('transitions idle -> connecting on CONNECT_REQUESTED', () => {
    expect(transitionState('idle', 'CONNECT_REQUESTED')).toBe('connecting');
  });

  it('transitions connecting -> connected on SOCKET_OPENED', () => {
    expect(transitionState('connecting', 'SOCKET_OPENED')).toBe('connected');
  });

  it('transitions connected -> reconnecting on SOCKET_CLOSED', () => {
    expect(transitionState('connected', 'SOCKET_CLOSED')).toBe('reconnecting');
  });

  it('transitions any state to closed on MANUAL_DISCONNECT or DISPOSED', () => {
    expect(transitionState('connected', 'MANUAL_DISCONNECT')).toBe('closed');
    expect(transitionState('reconnecting', 'DISPOSED')).toBe('closed');
  });
});

describe('RealtimeClient Unit Tests', () => {
  let mockSocket: WebSocketLike;
  let mockSocketFactory: any;

  beforeEach(() => {
    mockSocket = {
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

    mockSocketFactory = vi.fn().mockImplementation(() => mockSocket);
  });

  it('connects and reaches connected state on socket open', async () => {
    const client = new RealtimeClient({
      socketFactory: mockSocketFactory,
    });

    const stateSpy = vi.fn();
    client.subscribeState(stateSpy);

    const connectPromise = client.connect({ sessionGeneration: 'gen-1' });

    expect(client.getState()).toBe('connecting');

    // Wait microtask for getDescriptor promise resolution
    await Promise.resolve();

    // Simulate open
    mockSocket.onopen?.({});

    await connectPromise;

    expect(client.getState()).toBe('connected');
    expect(stateSpy).toHaveBeenCalledWith('connected');
  });

  it('filters events strictly by workspaceId and eventTypes', async () => {
    const client = new RealtimeClient({ socketFactory: mockSocketFactory });

    const ws1Spy = vi.fn();
    const ws2Spy = vi.fn();

    client.subscribe({ workspaceId: 'ws-1', eventTypes: ['board.updated'] }, ws1Spy);
    client.subscribe({ workspaceId: 'ws-2' }, ws2Spy);

    const connectPromise = client.connect({ sessionGeneration: 'gen-1' });
    await Promise.resolve();
    mockSocket.onopen?.({});
    await connectPromise;

    const envWs1 = JSON.stringify({
      schemaVersion: 1,
      eventId: 'evt-1',
      eventType: 'board.updated',
      workspaceId: 'ws-1',
      correlationId: 'corr-1',
      timestamp: new Date().toISOString(),
      payload: {},
    });

    const envWs2 = JSON.stringify({
      schemaVersion: 1,
      eventId: 'evt-2',
      eventType: 'card.created',
      workspaceId: 'ws-2',
      correlationId: 'corr-2',
      timestamp: new Date().toISOString(),
      payload: {},
    });

    mockSocket.onmessage?.({ data: envWs1 });
    mockSocket.onmessage?.({ data: envWs2 });

    expect(ws1Spy).toHaveBeenCalledTimes(1);
    expect(ws2Spy).toHaveBeenCalledTimes(1);
  });

  it('detects sequence gaps and emits recovery event', async () => {
    const client = new RealtimeClient({ socketFactory: mockSocketFactory });

    const recoverySpy = vi.fn();
    client.subscribeRecovery(recoverySpy);

    const connectPromise = client.connect({ sessionGeneration: 'gen-1' });
    await Promise.resolve();
    mockSocket.onopen?.({});
    await connectPromise;

    const msgSeq1 = JSON.stringify({
      schemaVersion: 1,
      eventId: 'evt-1',
      eventType: 'test',
      workspaceId: 'ws-1',
      correlationId: 'corr-1',
      timestamp: new Date().toISOString(),
      sequence: 1,
      payload: {},
    });

    const msgSeq5 = JSON.stringify({
      schemaVersion: 1,
      eventId: 'evt-5',
      eventType: 'test',
      workspaceId: 'ws-1',
      correlationId: 'corr-5',
      timestamp: new Date().toISOString(),
      sequence: 5,
      payload: {},
    });

    mockSocket.onmessage?.({ data: msgSeq1 });
    mockSocket.onmessage?.({ data: msgSeq5 });

    expect(recoverySpy).toHaveBeenCalledWith({
      workspaceId: 'ws-1',
      subscriptionId: undefined,
      expected: 2,
      received: 5,
    });
  });

  it('deduplicates events with identical eventId', async () => {
    const client = new RealtimeClient({ socketFactory: mockSocketFactory });

    const eventSpy = vi.fn();
    client.subscribe({ workspaceId: 'ws-1' }, eventSpy);

    const connectPromise = client.connect({ sessionGeneration: 'gen-1' });
    await Promise.resolve();
    mockSocket.onopen?.({});
    await connectPromise;

    const msg = JSON.stringify({
      schemaVersion: 1,
      eventId: 'evt-dup-1',
      eventType: 'test',
      workspaceId: 'ws-1',
      correlationId: 'corr-1',
      timestamp: new Date().toISOString(),
      payload: {},
    });

    mockSocket.onmessage?.({ data: msg });
    mockSocket.onmessage?.({ data: msg });

    expect(eventSpy).toHaveBeenCalledTimes(1);
  });
});
