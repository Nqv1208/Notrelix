/**
 * @notrelix/dev-mock-backend — public API surface.
 *
 * Composition root imports only from this file.
 * Dev-support only — never imported by production application code.
 *
 * Plan: 02-PACKAGE-DEPENDENCY-GOVERNANCE.md §Import policy
 */

// Config + presets
export * from "./config/mock-config";

// State
export * from "./state/records";
export * from "./state/mock-ids";
export * from "./state/mock-store";
export { createMockClock, defaultClock } from "./state/clock";
export type { MockClock } from "./state/clock";
export { createFactories, defaultFactories } from "./state/factories";
export type { MockFactories } from "./state/factories";

// Transport
export * from "./transport/create-mock-fetch";
export { MockUnhandledOperationError, MockAmbiguousOperationError } from "./transport/route-matcher";
export type { NormalizedMockRequest } from "./transport/normalize-request";
export type { MockHttpResult } from "./transport/create-response";

// Operations
export { MockOperationRegistry } from "./operations/operation-registry";
export { buildOperationRegistry } from "./operations/build-registry";
export type { MockOperationDefinition, MockOperationContext } from "./operations/types";
export { defineMockOperation } from "./operations/types";
