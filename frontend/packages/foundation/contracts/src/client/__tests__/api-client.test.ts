import { describe, it, expect, vi } from 'vitest';
import { createNotrelixClient, type SessionExpiredEvent } from '../api-client';

describe('createNotrelixClient — Instance-scoped API Client', () => {
  it('uses injected fetchImpl and adds X-Correlation-ID header', async () => {
    const mockFetch = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ data: 'ok' }), { status: 200 })
    );

    const client = createNotrelixClient({
      baseUrl: 'http://api.test',
      fetchImpl: mockFetch as unknown as typeof fetch,
      createCorrelationId: () => 'test-corr-id-123',
    });

    const result = await client.api.get<{ data: string }>('/test');
    expect(result).toEqual({ data: 'ok' });
    expect(mockFetch).toHaveBeenCalledWith(
      'http://api.test/test',
      expect.objectContaining({
        headers: expect.objectContaining({
          'X-Correlation-ID': 'test-corr-id-123',
        }),
      })
    );
  });

  it('adds Idempotency-Key header when request option is provided', async () => {
    const mockFetch = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ data: 'ok' }), { status: 200 })
    );

    const client = createNotrelixClient({
      baseUrl: 'http://api.test',
      fetchImpl: mockFetch as unknown as typeof fetch,
      createCorrelationId: () => 'corr-id',
    });

    await client.api.post('/commands', { title: 'Run' }, { idempotencyKey: 'idem-1' });

    expect(mockFetch).toHaveBeenCalledWith(
      'http://api.test/commands',
      expect.objectContaining({
        headers: expect.objectContaining({
          'X-Correlation-ID': 'corr-id',
          'Idempotency-Key': 'idem-1',
        }),
      }),
    );
  });

  it('triggers refreshOnce only ONCE for multiple concurrent 401 requests', async () => {
    let refreshCallCount = 0;

    const mockFetch = vi.fn().mockImplementation((url: string) => {
      if (url.includes('/auth/refresh')) {
        refreshCallCount++;
        return Promise.resolve(new Response(JSON.stringify({ ok: true }), { status: 200 }));
      }
      if (url.includes('/resource-1') || url.includes('/resource-2')) {
        if (refreshCallCount === 0) {
          return Promise.resolve(new Response('Unauthorized', { status: 401 }));
        }
        return Promise.resolve(new Response(JSON.stringify({ success: true }), { status: 200 }));
      }
      return Promise.resolve(new Response(null, { status: 404 }));
    });

    const client = createNotrelixClient({
      baseUrl: 'http://api.test',
      fetchImpl: mockFetch as unknown as typeof fetch,
    });

    const [res1, res2] = await Promise.all([
      client.api.get('/resource-1'),
      client.api.get('/resource-2'),
    ]);

    expect(res1).toEqual({ success: true });
    expect(res2).toEqual({ success: true });
    expect(refreshCallCount).toBe(1);
  });

  it('publishes onSessionExpired event exactly ONCE when refresh fails and does NOT dispatch window event', async () => {
    const windowEventSpy = vi.fn();
    if (typeof window !== 'undefined') {
      window.addEventListener('auth:failure', windowEventSpy);
    }

    const sessionExpiredSpy = vi.fn();

    const mockFetch = vi.fn().mockImplementation((url: string) => {
      if (url.includes('/auth/refresh')) {
        return Promise.resolve(new Response(JSON.stringify({ error: 'invalid_token' }), { status: 401 }));
      }
      return Promise.resolve(new Response('Unauthorized', { status: 401 }));
    });

    const client = createNotrelixClient({
      baseUrl: 'http://api.test',
      fetchImpl: mockFetch as unknown as typeof fetch,
      onSessionExpired: sessionExpiredSpy,
    });

    await expect(client.api.get('/resource')).rejects.toThrow();

    expect(sessionExpiredSpy).toHaveBeenCalledTimes(1);
    const call = sessionExpiredSpy.mock.calls[0];
    expect(call).toBeDefined();
    const event: SessionExpiredEvent = call![0];
    expect(event.reason).toBe('refresh-rejected');
    expect(event.error.kind).toBe('auth');

    expect(windowEventSpy).not.toHaveBeenCalled();
  });

  it('ensures separate client instances do NOT share refresh promises', async () => {
    let client1Refresh = 0;
    let client2Refresh = 0;

    const fetch1 = vi.fn().mockImplementation((url: string) => {
      if (url.includes('/auth/refresh')) {
        client1Refresh++;
        return Promise.resolve(new Response('OK', { status: 200 }));
      }
      return Promise.resolve(new Response('Unauthorized', { status: 401 }));
    });

    const fetch2 = vi.fn().mockImplementation((url: string) => {
      if (url.includes('/auth/refresh')) {
        client2Refresh++;
        return Promise.resolve(new Response('OK', { status: 200 }));
      }
      return Promise.resolve(new Response('Unauthorized', { status: 401 }));
    });

    const client1 = createNotrelixClient({ baseUrl: 'http://api1.test', fetchImpl: fetch1 as unknown as typeof fetch });
    const client2 = createNotrelixClient({ baseUrl: 'http://api2.test', fetchImpl: fetch2 as unknown as typeof fetch });

    await Promise.allSettled([client1.api.get('/item'), client2.api.get('/item')]);

    expect(client1Refresh).toBe(1);
    expect(client2Refresh).toBe(1);
  });
});
