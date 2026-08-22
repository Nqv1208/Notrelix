/**
 * Operation registry — stores all typed mock operations and dispatches requests.
 *
 * Enforces registry uniqueness (no duplicate IDs, no duplicate method+route).
 *
 * Plan: 01-FREEZE-SPEC.md §FZ-S12, 02-IMPLEMENTATION-PLAN.md §MFB-FZ-06
 */

import {
  createRouteMatcher,
  type RouteMatcher,
} from "../transport/route-matcher";
import { createMockResponse } from "../transport/create-response";
import type { MockOperationDefinition } from "./types";
import type { NormalizedMockRequest } from "../transport/normalize-request";
import type { MockStore } from "../state/mock-store";

export class MockDuplicateOperationIdError extends Error {
  constructor(operationId: string, route: string) {
    super(
      `[MockDuplicateOperationIdError] Operation ID "${operationId}" is already registered (route: "${route}"). Duplicate IDs are forbidden.`,
    );
    this.name = "MockDuplicateOperationIdError";
  }
}

export class MockDuplicateRouteError extends Error {
  constructor(
    newOpId: string,
    existingOpId: string,
    method: string,
    route: string,
  ) {
    super(
      `[MockDuplicateRouteError] Route "${method} ${route}" for operation "${newOpId}" conflicts with already-registered operation "${existingOpId}".`,
    );
    this.name = "MockDuplicateRouteError";
  }
}

function normalizeRoutePattern(route: string): string {
  return route.replace(/:[a-zA-Z0-9_]+/g, ":param");
}

export class MockOperationRegistry {
  private readonly operations = new Map<string, MockOperationDefinition>();
  private _matcher: RouteMatcher | null = null;

  register(op: MockOperationDefinition): void {
    if (this.operations.has(op.id)) {
      throw new MockDuplicateOperationIdError(op.id, op.route);
    }

    // Check for exact method + route duplicate or parameter-equivalent ambiguity
    const normalizedOpRoute = normalizeRoutePattern(op.route);
    for (const existing of this.operations.values()) {
      if (
        (existing.method === op.method ||
          existing.method === "*" ||
          op.method === "*") &&
        normalizeRoutePattern(existing.route) === normalizedOpRoute
      ) {
        throw new MockDuplicateRouteError(
          op.id,
          existing.id,
          op.method,
          op.route,
        );
      }
    }

    this.operations.set(op.id, op);
    this._matcher = null; // invalidate compiled matcher
  }

  registerMany(ops: MockOperationDefinition[]): void {
    for (const op of ops) {
      this.register(op);
    }
  }

  private getMatcher(): RouteMatcher {
    if (!this._matcher) {
      const routes = Array.from(this.operations.values()).map((op) => ({
        operationId: op.id,
        method: op.method,
        pattern: op.route,
      }));
      this._matcher = createRouteMatcher(routes);
    }
    return this._matcher;
  }

  async dispatch(
    request: NormalizedMockRequest,
    store: MockStore,
  ): Promise<Response> {
    const match = this.getMatcher()(request.method, request.normalizedPathname);
    const op = this.operations.get(match.operationId)!;

    const query: Record<string, string> = {};
    request.searchParams.forEach((value, key) => {
      query[key] = value;
    });

    const result = await op.handle({
      params: match.params,
      query,
      body: request.jsonBody,
      store,
      request,
    });

    return createMockResponse(result);
  }

  operationMetadata(): {
    id: string;
    contract:
      import("./types").CanonicalContract | import("./types").GapContract;
    method: string;
    route: string;
  }[] {
    return Array.from(this.operations.values()).map((op) => ({
      id: op.id,
      contract: op.contract,
      method: op.method,
      route: op.route,
    }));
  }

  operationIds(): string[] {
    return Array.from(this.operations.keys());
  }
}
