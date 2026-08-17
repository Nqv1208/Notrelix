/**
 * Operation registry — stores all typed mock operations and dispatches requests.
 *
 * Plan: 06-HANDLERS-PROJECTIONS.md §Typed operations, 04-TRANSPORT-PROTOCOL.md
 */

import { createRouteMatcher, type RouteMatcher } from "../transport/route-matcher";
import { createMockResponse } from "../transport/create-response";
import type { MockOperationDefinition } from "./types";
import type { NormalizedMockRequest } from "../transport/normalize-request";
import type { MockStore } from "../state/mock-store";

export class MockOperationRegistry {
  private readonly operations = new Map<string, MockOperationDefinition>();
  private _matcher: RouteMatcher | null = null;

  register(op: MockOperationDefinition): void {
    this.operations.set(op.id, op);
    this._matcher = null; // invalidate compiled matcher
  }

  registerMany(ops: MockOperationDefinition[]): void {
    for (const op of ops) {
      this.operations.set(op.id, op);
    }
    this._matcher = null;
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

  operationIds(): string[] {
    return Array.from(this.operations.keys());
  }
}
