import { describe, expect, it, vi } from 'vitest';
import type { ClockPort, KeyValueStorage } from '@notrelix/platform';
import { createAppRuntime } from '../runtime/app-runtime';
import { createLocalStorageAdapter } from '../storage/local-storage';
import { createCookieAdapter } from '../cookie/cookie';

describe('FND-021 runtime satisfies platform ports', () => {
  it('default runtime clock satisfies the platform ClockPort contract', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-01-01T00:00:00.000Z'));

    try {
      const runtime = createAppRuntime({ apiUrl: 'http://api.test' });

      const clock: ClockPort = runtime.clock;
      const now = clock.now();

      expect(now).toBeInstanceOf(Date);
      expect(clock.isoNow()).toBe(now.toISOString());
    } finally {
      vi.useRealTimers();
    }
  });

  it('local storage adapter satisfies the platform KeyValueStorage contract', () => {
    const store = new Map<string, string>();
    vi.stubGlobal('localStorage', {
      getItem: (key: string) => store.get(key) ?? null,
      setItem: (key: string, value: string) => void store.set(key, value),
      removeItem: (key: string) => void store.delete(key),
      clear: () => store.clear(),
    });

    try {
      const storage: KeyValueStorage = createLocalStorageAdapter();

      expect(storage.getItem('missing')).toBeNull();

      storage.setItem('k', 'v');
      expect(storage.getItem('k')).toBe('v');

      storage.removeItem('k');
      expect(storage.getItem('k')).toBeNull();
    } finally {
      vi.unstubAllGlobals();
    }
  });

  it('cookie adapter exposes a browser cookie API without touching KeyValueStorage', () => {
    const cookies = createCookieAdapter();

    expect(typeof cookies.getCookie).toBe('function');
    expect(typeof cookies.setCookie).toBe('function');
    expect(typeof cookies.deleteCookie).toBe('function');
  });
});
