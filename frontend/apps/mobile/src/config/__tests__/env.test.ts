import { describe, expect, it } from 'vitest';
import { env } from '../env';

describe('mobile app env config', () => {
  it('defines all required service URLs as valid absolute URLs', () => {
    expect(new URL(env.apiUrl).protocol).toMatch(/^https?:$/);
    expect(new URL(env.realtimeUrl).protocol).toMatch(/^https?:$/);
    expect(new URL(env.webUrl).protocol).toMatch(/^https?:$/);
  });

  it('is a static config object with the expected values', () => {
    expect(env).toEqual({
      apiUrl: 'https://api.notrelix.com',
      realtimeUrl: 'https://api.notrelix.com/realtime',
      webUrl: 'https://app.notrelix.com',
    });
  });
});
