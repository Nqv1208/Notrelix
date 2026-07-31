import { describe, expect, it, vi } from 'vitest';
import type { RealtimeEnvelope } from '@notrelix/realtime';
import { ModuleAdapterRegistry } from '../realtime/module-adapter-registry';

function envelope(eventType: string): RealtimeEnvelope<unknown> {
  return {
    schemaVersion: 1,
    eventId: 'evt-1',
    eventType,
    workspaceId: 'ws-1',
    correlationId: 'corr-1',
    timestamp: new Date('2026-01-01T00:00:00Z').toISOString(),
    payload: {},
  };
}

describe('ModuleAdapterRegistry', () => {
  it('dispatches supported events and reports handled adapter ids', async () => {
    const registry = new ModuleAdapterRegistry();
    const handler = vi.fn();

    registry.register({
      id: 'test-adapter',
      supports: (event) => event.eventType === 'test.updated',
      validateAndHandle: handler,
    });

    const result = await registry.dispatch(envelope('test.updated'), {
      workspaceId: 'ws-1',
      invalidateQueries: vi.fn(),
    });

    expect(result).toEqual({ handled: true, adapterIds: ['test-adapter'] });
    expect(handler).toHaveBeenCalledTimes(1);
  });

  it('returns unhandled for unknown event types', async () => {
    const registry = new ModuleAdapterRegistry();

    await expect(
      registry.dispatch(envelope('unknown.event'), {
        workspaceId: 'ws-1',
        invalidateQueries: vi.fn(),
      }),
    ).resolves.toEqual({ handled: false, adapterIds: [] });
  });
});
