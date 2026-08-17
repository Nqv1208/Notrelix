import type { ApiRequestOptions } from "@notrelix/contracts";

export type MockHttpMethod = "GET" | "POST" | "PUT" | "PATCH" | "DELETE";

export interface MockRequest {
  readonly method: MockHttpMethod;
  readonly url: string;
  readonly body?: unknown;
  readonly options?: ApiRequestOptions;
}
