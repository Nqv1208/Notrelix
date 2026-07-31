import { describe, expect, test, vi } from 'vitest';
import { initObservability, trackEvent, reportError, getObservabilityConfig } from '../index';

describe('Observability', () => {
  test('initializes with custom configuration', () => {
    initObservability({ enabled: true, environment: 'staging' });
    const config = getObservabilityConfig();
    expect(config.environment).toBe('staging');
    expect(config.enabled).toBe(true);
  });

  test('does not report when disabled', () => {
    initObservability({ enabled: false });
    const consoleSpy = vi.spyOn(console, 'log').mockImplementation(() => {});

    trackEvent('user_login', { method: 'oauth' });
    expect(consoleSpy).not.toHaveBeenCalled();

    consoleSpy.mockRestore();
  });
});
