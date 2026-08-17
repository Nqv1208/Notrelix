export interface ForbiddenPatternRule {
  readonly id: string;
  readonly description: string;
  readonly isAllowed: (filePath: string, packageName: string) => boolean;
}

export function isForbiddenClientCall(filePath: string): boolean {
  const normalized = filePath.replace(/\\/g, "/");
  return !(
    normalized.includes("/runtimes/") ||
    normalized.includes("/contracts/") ||
    normalized.includes("/__tests__/") ||
    normalized.includes(".test.") ||
    normalized.includes(".spec.")
  );
}

export function isForbiddenWebSocketInstantiation(filePath: string): boolean {
  const normalized = filePath.replace(/\\/g, "/");
  // Browser WebSocket construction is allowed only in web runtime adapters
  // and test fixtures. React Native global WebSocket is allowed only in the
  // mobile runtime's native-websocket-factory.ts. Foundation realtime must
  // receive an injected socket factory; features/product may never construct
  // sockets.
  if (
    normalized.includes("/__tests__/") ||
    normalized.includes(".test.") ||
    normalized.includes(".spec.")
  ) {
    return false;
  }
  if (normalized.includes("/runtimes/web/")) {
    return false;
  }
  if (normalized.includes("/runtimes/mobile/")) {
    return !/realtime\/native-websocket-factory\.ts$/.test(normalized);
  }
  return true;
}

export function isForbiddenStorageAccess(filePath: string): boolean {
  const normalized = filePath.replace(/\\/g, "/");
  // Direct localStorage/sessionStorage access is rejected inside shared
  // packages (features, product, ui, foundation). UI/feature/product code
  // must receive storage through the runtime KeyValueStorage port. Runtime
  // adapters and explicit test fixtures are the only allowed readers.
  // Apps are app-owned and out of this rejection scope.
  if (!normalized.includes("/packages/")) {
    return false;
  }
  return !(
    normalized.includes("/runtimes/") ||
    normalized.includes("/__tests__/") ||
    normalized.includes(".test.") ||
    normalized.includes(".spec.")
  );
}

export function isForbiddenRouteCreation(filePath: string): boolean {
  const normalized = filePath.replace(/\\/g, "/");
  // TanStack route construction is app-owned. Packages (features, product,
  // foundation, ui, runtimes) must never create routes.
  return /^\/packages\/(features|product|foundation|ui|runtimes)\//.test(
    normalized,
  );
}

export function isForbiddenQueryClientInstantiation(filePath: string): boolean {
  const normalized = filePath.replace(/\\/g, "/");
  return !(
    normalized.includes("/query/") ||
    normalized.includes("/runtimes/") ||
    normalized.includes("/__tests__/") ||
    normalized.includes(".test.") ||
    normalized.includes(".spec.")
  );
}

export function isForbiddenBackendFetch(filePath: string): boolean {
  const normalized = filePath.replace(/\\/g, "/");
  if (normalized.includes("/__tests__/") || normalized.includes(".test.") || normalized.includes(".spec.")) return false;
  return /^\/(apps\/web|packages\/(features|product))\//.test(normalized);
}

export function isDeepSrcImport(importPath: string): boolean {
  return /^@notrelix\/[^/]+\/src\//.test(importPath);
}
