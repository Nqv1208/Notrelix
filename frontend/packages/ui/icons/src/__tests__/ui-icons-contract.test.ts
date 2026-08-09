import { describe, expect, it } from 'vitest';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import {
  ICON_SIZE_LG,
  ICON_SIZE_MD,
  ICON_SIZE_SM,
  ICON_STROKE_WIDTH,
  ICON_SIZES,
  Search,
  Plus,
  type LucideIcon,
} from '../index';

const packageJsonPath = join(__dirname, '../../package.json');

describe('@notrelix/ui-icons contract', () => {
  it('has a non-empty curated icon set with typed exports', () => {
    expect(typeof Search).toBe('object');
    expect(typeof Plus).toBe('object');
  });

  it('exposes the LucideIcon type', () => {
    const icon: LucideIcon = Search;
    expect(icon).toBeDefined();
  });

  it('defines stable sizing/stroke conventions', () => {
    expect(ICON_SIZE_SM).toBe(16);
    expect(ICON_SIZE_MD).toBe(20);
    expect(ICON_SIZE_LG).toBe(24);
    expect(ICON_STROKE_WIDTH).toBe(2);
    expect(ICON_SIZES.sm).toBe(ICON_SIZE_SM);
    expect(ICON_SIZES.lg).toBe(ICON_SIZE_LG);
  });

  it('has no platform/query/product dependencies', () => {
    const pkg = JSON.parse(readFileSync(packageJsonPath, 'utf8'));
    const deps = new Set([
      ...Object.keys(pkg.dependencies ?? {}),
      ...Object.keys(pkg.devDependencies ?? {}),
    ]);
    for (const forbidden of [
      '@notrelix/platform',
      '@notrelix/query',
      '@notrelix/contracts',
      '@notrelix/kernel',
    ]) {
      expect(deps.has(forbidden)).toBe(false);
    }
  });
});
