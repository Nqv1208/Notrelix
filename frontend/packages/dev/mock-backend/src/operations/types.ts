/**
 * Typed mock operation definition.
 *
 * Every handler returns MockHttpResult<ResponseBody> keyed to official
 * contract types. The defineMockOperation helper preserves generic
 * type safety while allowing heterogeneous collections.
 *
 * Plan: 06-HANDLERS-PROJECTIONS.md §Typed operations
 */

import type { NormalizedMockRequest } from "../transport/normalize-request";
import type { MockHttpResult } from "../transport/create-response";
import type { MockStore } from "../state/mock-store";

export interface MockOperationContext<
  Params extends Record<string, string> = Record<string, string>,
  Body = unknown,
> {
  readonly params: Params;
  readonly query: Record<string, string>;
  readonly body: Body;
  readonly store: MockStore;
  readonly request: NormalizedMockRequest;
}

export interface MockOperationDefinition<
  Params extends Record<string, string> = Record<string, string>,
  Body = unknown,
  // ResponseBody is documented intent; handle returns MockHttpResult<unknown>
  // so notFound/unauthorized/etc. can co-exist with typed success paths.
  ResponseBody = unknown, // eslint-disable-line @typescript-eslint/no-unused-vars
> {
  readonly id: string;
  readonly method: string;
  readonly route: string;
  handle(ctx: MockOperationContext<Params, Body>): Promise<MockHttpResult<unknown>>;
}

/**
 * Type-safe helper that narrows generic params but casts to the base type
 * for heterogeneous collections (MockOperationDefinition[]).
 */
export function defineMockOperation<
  Params extends Record<string, string> = Record<string, string>,
  Body = unknown,
  ResponseBody = unknown,
>(
  def: MockOperationDefinition<Params, Body, ResponseBody>,
): MockOperationDefinition {
  return def as MockOperationDefinition;
}
