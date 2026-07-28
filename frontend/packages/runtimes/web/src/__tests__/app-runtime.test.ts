import { describe, it, expect, vi } from 'vitest';
import { createAppRuntime } from '../runtime/app-runtime';

describe('AppRuntime', () => {
  it('instantiates runtime with normalized environment and freezes top-level runtime object', () => {
    const runtime = createAppRuntime({
      apiUrl: 'http://api.test',
      realtimeUrl: 'ws://realtime.test',
    });

    expect(runtime.env.apiUrl).toBe('http://api.test');
    expect(runtime.env.realtimeUrl).toBe('ws://realtime.test');
    expect(Object.isFrozen(runtime)).toBe(true);
  });

  it('supports injecting custom test factories', () => {
    const mockApiClient = { api: {} } as any;
    const createApiClientSpy = vi.fn().mockReturnValue(mockApiClient);

    const mockClock = {
      now: () => new Date('2026-01-01T00:00:00Z'),
      isoNow: () => '2026-01-01T00:00:00Z',
    };

    const runtime = createAppRuntime(
      { apiUrl: 'http://api.test' },
      {
        createApiClient: createApiClientSpy,
        clock: mockClock,
      }
    );

    expect(createApiClientSpy).toHaveBeenCalled();
    expect(runtime.clock.isoNow()).toBe('2026-01-01T00:00:00Z');
  });

  it('executes dispose idempotently and cleans up session events and realtime connection', () => {
    const mockRealtime = {
      disconnect: vi.fn(),
    } as any;

    const runtime = createAppRuntime(
      { apiUrl: 'http://api.test' },
      { createRealtimeClient: () => mockRealtime }
    );

    const sessionSpy = vi.fn();
    runtime.sessionEvents.subscribe(sessionSpy);

    runtime.dispose();
    expect(mockRealtime.disconnect).toHaveBeenCalledTimes(1);

    // Second dispose call should do nothing (idempotent)
    runtime.dispose();
    expect(mockRealtime.disconnect).toHaveBeenCalledTimes(1);
  });
});
