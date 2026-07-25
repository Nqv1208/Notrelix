import { describe, it, expect } from 'vitest';
import { ALLOWED_IMPORTS } from '../allowed-imports';

describe('Dependency Boundary Rules Engine', () => {
  it('should define allowed import boundaries for core packages', () => {
    expect(ALLOWED_IMPORTS['@notrelix/kernel']).toBeDefined();
    expect(ALLOWED_IMPORTS['@notrelix/contracts']).toBeDefined();
  });

  it('kernel must not depend on outer API or HTTP layers', () => {
    const kernelAllowed = ALLOWED_IMPORTS['@notrelix/kernel'] ?? [];
    expect(kernelAllowed.some((dep: string) => dep.includes('api') || dep.includes('http'))).toBe(false);
  });
});
