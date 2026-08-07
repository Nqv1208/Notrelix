export type FrontendLayer =
  | 'app'
  | 'runtime'
  | 'core'
  | 'data'
  | 'react'
  | 'web'
  | 'mobile'
  | 'testing'
  | 'tooling'
  | 'unknown';

function normalizePath(filePath: string): string {
  return filePath.replace(/\\/g, '/');
}

export function classifyLayer(filePath: string, packageName: string): FrontendLayer {
  const normalized = normalizePath(filePath);

  if (normalized.includes('/tooling/')) return 'tooling';
  if (normalized.includes('/apps/')) return 'app';
  if (normalized.includes('/__tests__/') || normalized.includes('/testing/') || packageName.endsWith('-testing')) {
    return 'testing';
  }
  if (packageName.startsWith('@notrelix/runtime-') || normalized.includes('/packages/runtimes/')) {
    return 'runtime';
  }
  if (
    packageName.endsWith('-core') ||
    normalized.includes('/src/core/') ||
    normalized.includes('/packages/foundation/kernel/') ||
    normalized.includes('/packages/foundation/realtime/')
  ) {
    return 'core';
  }
  if (packageName.endsWith('-mobile') || normalized.includes('/src/mobile/') || normalized.includes('/packages/ui/mobile/')) {
    return 'mobile';
  }
  if (packageName.endsWith('-web') || normalized.includes('/src/web/') || normalized.includes('/packages/ui/web/')) {
    return 'web';
  }
  if (normalized.includes('/src/react/') || normalized.includes('/src/query/hooks/') || normalized.includes('/src/hooks/')) {
    return 'react';
  }
  if (
    packageName.endsWith('-state') ||
    normalized.includes('/src/data/') ||
    normalized.includes('/src/api/') ||
    normalized.includes('/src/query/') ||
    normalized.includes('/src/mutations/') ||
    normalized.includes('/src/commands/')
  ) {
    return 'data';
  }

  return 'unknown';
}
