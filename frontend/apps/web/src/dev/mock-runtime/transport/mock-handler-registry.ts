import type { MockHandler, MockHandlerContext } from "./mock-handler";
import type { MockRequest } from "./mock-request";
import { MockUnhandledOperationError } from "./mock-unhandled-operation-error";

export function createMockHandlerRegistry(handlers: readonly MockHandler[]) {
  return {
    async dispatch(request: MockRequest, context: MockHandlerContext) {
      const handler = handlers.find((candidate) => candidate.matches(request));
      if (!handler) throw new MockUnhandledOperationError(request);
      context.journal.record({
        ...request,
        matchedHandlerId: handler.id,
        occurredAt: context.now().toISOString(),
      });
      return handler.handle(request, context);
    },
  };
}
