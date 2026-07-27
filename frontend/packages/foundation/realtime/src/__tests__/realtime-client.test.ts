import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { RealtimeClient, type RealtimeEnvelope } from '../transport/realtime-client';

class MockWebSocket {
  public static readonly CONNECTING = 0;
  public static readonly OPEN = 1;
  public static readonly CLOSING = 2;
  public static readonly CLOSED = 3;

  public url: string;
  public readyState: number = MockWebSocket.CONNECTING;
  public onopen: (() => void) | null = null;
  public onmessage: ((event: { data: string }) => void) | null = null;
  public onclose: (() => void) | null = null;
  public onerror: ((error: unknown) => void) | null = null;

  public sentMessages: string[] = [];

  constructor(url: string) {
    this.url = url;
    MockWebSocket.instances.push(this);
  }

  public send(data: string) {
    this.sentMessages.push(data);
  }

  public close() {
    this.readyState = 3; // CLOSED
    if (this.onclose) this.onclose();
  }

  public simulateOpen() {
    this.readyState = 1; // OPEN
    if (this.onopen) this.onopen();
  }

  public simulateMessage(data: unknown) {
    if (this.onmessage) this.onmessage({ data: JSON.stringify(data) });
  }

  public simulateClose() {
    this.readyState = 3; // CLOSED
    if (this.onclose) this.onclose();
  }

  public static instances: MockWebSocket[] = [];
}

describe('RealtimeClient', () => {
  const originalWebSocket = globalThis.WebSocket;

  beforeEach(() => {
    MockWebSocket.instances = [];
    vi.useFakeTimers();
    // @ts-expect-error Mocking global WebSocket
    globalThis.WebSocket = MockWebSocket;
  });

  afterEach(() => {
    globalThis.WebSocket = originalWebSocket;
    vi.useRealTimers();
  });

  it('starts disconnected and transitions to connecting/connected on connect()', () => {
    const client = new RealtimeClient('ws://localhost:4000');
    expect(client.getState()).toBe('disconnected');

    const stateSpy = vi.fn();
    client.onStateChange(stateSpy);

    client.connect();
    expect(client.getState()).toBe('connecting');

    const mockWs = MockWebSocket.instances[0]!;
    mockWs.simulateOpen();

    expect(client.getState()).toBe('connected');
    expect(stateSpy).toHaveBeenCalledWith('connecting');
    expect(stateSpy).toHaveBeenCalledWith('connected');
  });

  it('handles manual disconnect and prevents automatic reconnection', () => {
    const client = new RealtimeClient('ws://localhost:4000');
    client.connect();
    const mockWs = MockWebSocket.instances[0]!;
    mockWs.simulateOpen();

    const stateSpy = vi.fn();
    client.onStateChange(stateSpy);

    client.disconnect();
    expect(client.getState()).toBe('disconnected');

    // Fast forward time — no reconnect should be scheduled
    vi.advanceTimersByTime(10000);
    expect(MockWebSocket.instances.length).toBe(1);
  });

  it('deduplicates events with identical eventId', () => {
    const client = new RealtimeClient('ws://localhost:4000');
    const listener = vi.fn();
    client.subscribe(listener);

    client.connect();
    const mockWs = MockWebSocket.instances[0]!;
    mockWs.simulateOpen();

    const envelope: RealtimeEnvelope = {
      eventId: 'evt-100',
      eventType: 'board.item.updated',
      workspaceId: 'ws-1',
      correlationId: 'corr-1',
      schemaVersion: 1,
      timestamp: new Date().toISOString(),
      payload: { itemId: 'item-1' },
    };

    // Dispatch message first time
    mockWs.simulateMessage(envelope);
    expect(listener).toHaveBeenCalledTimes(1);

    // Dispatch identical message second time
    mockWs.simulateMessage(envelope);
    expect(listener).toHaveBeenCalledTimes(1); // Should be ignored as duplicate
  });

  it('sends heartbeat ping messages every 30 seconds when connected', () => {
    const client = new RealtimeClient('ws://localhost:4000');
    client.connect();
    const mockWs = MockWebSocket.instances[0]!;
    mockWs.simulateOpen();

    expect(mockWs.sentMessages.length).toBe(0);

    // Advance by 30 seconds
    vi.advanceTimersByTime(30000);
    expect(mockWs.sentMessages.length).toBe(1);
    expect(JSON.parse(mockWs.sentMessages[0]!)).toMatchObject({ type: 'ping' });

    // Advance another 30 seconds
    vi.advanceTimersByTime(30000);
    expect(mockWs.sentMessages.length).toBe(2);
  });
});
