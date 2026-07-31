import { describe, expect, it } from 'vitest';
import { classifyLayer } from '../layer-classifier';

describe('classifyLayer', () => {
  it('classifies app routes as app layer', () => {
    expect(classifyLayer('/apps/web/src/router.tsx', '@notrelix/app-web')).toBe('app');
  });

  it('classifies package-core package shape as core', () => {
    expect(classifyLayer('/packages/product/docs/core/src/query/hooks/use-page.ts', '@notrelix/docs-core')).toBe('core');
  });

  it('classifies feature folder core as core', () => {
    expect(classifyLayer('/packages/features/auth/src/core/index.ts', '@notrelix/features-auth')).toBe('core');
  });

  it('classifies product state api as data', () => {
    expect(classifyLayer('/packages/product/work-management/state/src/api/board.api.ts', '@notrelix/work-management-state')).toBe('data');
  });

  it('classifies product web package as web', () => {
    expect(classifyLayer('/packages/product/work-management/web/src/components/board.tsx', '@notrelix/work-management-web')).toBe('web');
  });
});
