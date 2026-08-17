/**
 * Shared typed route matcher.
 *
 * Compiles route patterns with :param placeholders into regexes.
 * Enforces the closed-world rule:
 *   0 matches → MockUnhandledOperationError
 *   1 match   → returns RouteMatch
 *   >1 match  → MockAmbiguousOperationError
 *
 * Plan: 04-TRANSPORT-PROTOCOL.md §Closed world, §Route matching
 */

// ─── Errors ──────────────────────────────────────────────────────────────────

export class MockUnhandledOperationError extends Error {
  constructor(method: string, pathname: string) {
    super(`[MockFetch] Unhandled closed-world operation: ${method} ${pathname}`);
    this.name = "MockUnhandledOperationError";
  }
}

export class MockAmbiguousOperationError extends Error {
  constructor(method: string, pathname: string, matchedIds: string[]) {
    super(
      `[MockFetch] Ambiguous operation: ${method} ${pathname} matched [${matchedIds.join(", ")}]`,
    );
    this.name = "MockAmbiguousOperationError";
  }
}

// ─── Types ───────────────────────────────────────────────────────────────────

export interface RouteDefinition {
  readonly operationId: string;
  /** Use "*" to match any HTTP method. */
  readonly method: string;
  /** Pathname pattern, e.g. "/workspaces/:id/boards". */
  readonly pattern: string;
}

export interface RouteMatch {
  readonly operationId: string;
  readonly params: Record<string, string>;
}

// ─── Compilation ─────────────────────────────────────────────────────────────

const PARAM_SEGMENT = /:([^/]+)/g;

interface CompiledRoute extends RouteDefinition {
  readonly regex: RegExp;
  readonly paramNames: readonly string[];
}

function compileRoute(route: RouteDefinition): CompiledRoute {
  const paramNames: string[] = [];
  const regexStr = route.pattern.replace(PARAM_SEGMENT, (_, name: string) => {
    paramNames.push(name);
    return "([^/]+)";
  });
  return {
    ...route,
    regex: new RegExp(`^${regexStr}$`),
    paramNames,
  };
}

// ─── Matcher factory ─────────────────────────────────────────────────────────

export type RouteMatcher = (method: string, normalizedPathname: string) => RouteMatch;

export function createRouteMatcher(routes: RouteDefinition[]): RouteMatcher {
  const compiled = routes.map(compileRoute);

  return function matchRoute(method: string, normalizedPathname: string): RouteMatch {
    const matches: RouteMatch[] = [];

    for (const route of compiled) {
      if (route.method !== "*" && route.method !== method) continue;

      const m = normalizedPathname.match(route.regex);
      if (!m) continue;

      const params: Record<string, string> = {};
      route.paramNames.forEach((name, i) => {
        params[name] = m[i + 1]!;
      });
      matches.push({ operationId: route.operationId, params });
    }

    if (matches.length === 0) {
      throw new MockUnhandledOperationError(method, normalizedPathname);
    }
    if (matches.length > 1) {
      throw new MockAmbiguousOperationError(
        method,
        normalizedPathname,
        matches.map((match) => match.operationId),
      );
    }
    return matches[0]!;
  };
}
