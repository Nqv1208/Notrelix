import type { operations } from '../generated/rest';

/**
 * Typed helpers derived from the generated OpenAPI operation map.
 * Kept outside the regenerated schema so contract re-export does not
 * depend on hand-appended code inside generated files.
 */

export type OperationPathParams<TOperation extends keyof operations> =
  operations[TOperation] extends { parameters: { path: infer TPath } } ? TPath : never;

export type OperationRequestBody<TOperation extends keyof operations> =
  operations[TOperation] extends { requestBody: { content: { "application/json": infer TBody } } } ? TBody : never;

export type OperationResponse<
  TOperation extends keyof operations,
  TStatus extends keyof operations[TOperation]["responses"] = 200 & keyof operations[TOperation]["responses"],
> =
  operations[TOperation]["responses"][TStatus] extends { content: { "application/json": infer TResponse } } ? TResponse : never;
