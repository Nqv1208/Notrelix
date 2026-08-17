import type { MockRequest } from "./mock-request";

export class MockUnhandledOperationError extends Error {
  constructor(request: Pick<MockRequest, "method" | "url">) {
    super(`[Mock Runtime] Unhandled operation: ${request.method} ${request.url}`);
    this.name = "MockUnhandledOperationError";
    Object.setPrototypeOf(this, MockUnhandledOperationError.prototype);
  }
}
