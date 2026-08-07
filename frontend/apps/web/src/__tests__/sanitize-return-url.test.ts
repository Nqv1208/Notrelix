import { describe, it, expect } from 'vitest';
import { sanitizeInternalReturnUrl } from '../routing/sanitize-return-url';

describe('sanitizeInternalReturnUrl', () => {
  it('preserves valid internal paths with search params and hash', () => {
    expect(sanitizeInternalReturnUrl('/workspaces/123/dashboard?tab=active#section-1')).toBe(
      '/workspaces/123/dashboard?tab=active#section-1'
    );
  });

  it('rejects external URLs starting with http/https', () => {
    expect(sanitizeInternalReturnUrl('https://malicious.com')).toBe('/');
  });

  it('rejects protocol relative URLs starting with //', () => {
    expect(sanitizeInternalReturnUrl('//malicious.com')).toBe('/');
  });

  it('rejects invalid inputs', () => {
    expect(sanitizeInternalReturnUrl('')).toBe('/');
    expect(sanitizeInternalReturnUrl('javascript:alert(1)')).toBe('/');
  });
});
