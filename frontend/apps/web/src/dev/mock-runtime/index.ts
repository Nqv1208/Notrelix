import type { NotrelixClient } from "@notrelix/contracts";
import type { RealtimeTransport } from "@notrelix/realtime";
import type { ClockPort } from "@notrelix/runtime-web";
import type { MockRuntimeConfig } from "./config/mock-runtime-config";
import { authHandlers } from "./handlers/auth.handlers";
import { workspaceHandlers } from "./handlers/workspace.handlers";
import { searchHandlers } from "./handlers/search.handlers";
import { workManagementHandlers } from "./handlers/work-management.handlers";
import { documentHandlers } from "./handlers/documents.handlers";
import { accountHandlers } from "./handlers/account.handlers";
import { notificationHandlers } from "./handlers/notifications.handlers";
import { createMockRealtimeTransport } from "./realtime/create-mock-realtime-transport";
import { mockClock } from "./state/mock-clock";
import { MockStore } from "./state/mock-store";
import { createMockNotrelixClient } from "./transport/create-mock-notrelix-client";
import { createMockRequestJournal, type MockRequestJournal } from "./transport/mock-request-journal";

export interface WebMockRuntime {
  readonly api: NotrelixClient;
  readonly realtime: RealtimeTransport;
  readonly clock: ClockPort;
  readonly store: MockStore;
  readonly journal: MockRequestJournal;
}

export function createWebMockRuntime(config: MockRuntimeConfig): WebMockRuntime {
  const store = new MockStore(config.persona, config.scenario);
  const journal = createMockRequestJournal();
  return {
    store,
    journal,
    clock: mockClock,
    api: createMockNotrelixClient({
      config,
      store,
      journal,
      now: mockClock.now,
      handlers: [
        ...authHandlers,
        ...workspaceHandlers,
        ...searchHandlers,
        ...accountHandlers,
        ...notificationHandlers,
        ...workManagementHandlers,
        ...documentHandlers,
      ],
    }),
    realtime: createMockRealtimeTransport(),
  };
}

export { readMockRuntimeConfig } from "./config/read-mock-runtime-config";
export { MockUnhandledOperationError } from "./transport/mock-unhandled-operation-error";
