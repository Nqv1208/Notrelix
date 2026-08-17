import type { MockStore } from "../state/mock-store";
import type { MockRequestJournal } from "./mock-request-journal";
import type { MockRequest } from "./mock-request";

export interface MockHandlerContext {
  readonly store: MockStore;
  readonly journal: MockRequestJournal;
  readonly now: () => Date;
}

export interface MockHandler {
  readonly id: string;
  matches(request: MockRequest): boolean;
  handle(request: MockRequest, context: MockHandlerContext): Promise<unknown>;
}
