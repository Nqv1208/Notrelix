export interface ForbiddenPatternRule {
  readonly id: string;
  readonly description: string;
  readonly isAllowed: (filePath: string, packageName: string) => boolean;
}

export function isForbiddenClientCall(filePath: string): boolean {
  const normalized = filePath.replace(/\\/g, '/');
  return !(
    normalized.includes('/runtimes/') ||
    normalized.includes('/contracts/') ||
    normalized.includes('/__tests__/') ||
    normalized.includes('.test.') ||
    normalized.includes('.spec.')
  );
}

export function isForbiddenWebSocketInstantiation(filePath: string): boolean {
  const normalized = filePath.replace(/\\/g, '/');
  // Browser WebSocket construction is allowed only in runtime adapters and
  // test fixtures. Foundation realtime must receive an injected socket
  // factory; features/product may never construct sockets.
  return !(
    normalized.includes('/runtimes/') ||
    normalized.includes('/__tests__/') ||
    normalized.includes('.test.') ||
    normalized.includes('.spec.')
  );
}

export function isForbiddenQueryClientInstantiation(filePath: string): boolean {
  const normalized = filePath.replace(/\\/g, '/');
  return !(
    normalized.includes('/query/') ||
    normalized.includes('/runtimes/') ||
    normalized.includes('/__tests__/') ||
    normalized.includes('.test.') ||
    normalized.includes('.spec.')
  );
}

export function isDeepSrcImport(importPath: string): boolean {
  return /^@notrelix\/[^/]+\/src\//.test(importPath);
}
