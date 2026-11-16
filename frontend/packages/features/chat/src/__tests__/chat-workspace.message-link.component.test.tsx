import type { ReactNode } from "react";
import { beforeAll, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@notrelix/testing";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { createNotrelixClient } from "@notrelix/contracts";
import { I18nProvider } from "@notrelix/i18n";
import {
  MockStore,
  createMockFetch,
  defaultConfig,
  mockIds,
} from "@notrelix/dev-mock-backend";
import { chatCatalog } from "../web/components/chat.catalog";
import { ChatWorkspace } from "../web/components/chat-workspace";
import { createChatHooks } from "../web/query/create-chat-hooks";

const WORKSPACE = mockIds.workspaces.primary;

beforeAll(() => {
  Element.prototype.scrollIntoView ??= () => undefined;
});

describe("ChatWorkspace message link (UC-CHT-B10)", () => {
  it("opens around the linked sequence, highlights that message and nothing for a missing sequence", async () => {
    const store = new MockStore({ ...defaultConfig, persona: "owner" });
    const client = createNotrelixClient({
      baseUrl: "http://localhost:8000/api/v1",
      fetchImpl: (input, init) => createMockFetch(store)(input, init),
    });
    const deps = { api: client.api, endpoints: client.endpoints };
    const hooks = createChatHooks(deps);
    const general = (await hooks.service.listChannels(WORKSPACE)).find(
      (channel) => channel.name === "general",
    )!;
    const page = await hooks.service.listMessages(
      WORKSPACE,
      general.id,
      {},
      50,
    );
    const target = page.items.find((m) => m.kind === "user" && !m.deleted)!;
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    const wrapper = ({ children }: { children: ReactNode }) => (
      <QueryClientProvider client={queryClient}>
        <I18nProvider catalogs={[chatCatalog]} initialLocale="en">
          {children}
        </I18nProvider>
      </QueryClientProvider>
    );
    const props = {
      deps,
      workspaceId: WORKSPACE,
      viewerId: store.getCurrentUser().id,
      viewerRole: "owner" as const,
      channelId: general.id,
      onOpenChannel: vi.fn(),
      people: new Map<string, string>(),
    };
    const view = render(
      <ChatWorkspace {...props} linkedSequence={target.sequence} />,
      { wrapper },
    );
    await waitFor(() => {
      const row = document.querySelector(
        `[data-message-id="${target.id}"]`,
      ) as HTMLElement | null;
      expect(row?.getAttribute("data-highlighted")).toBe("true");
    });
    view.unmount();

    render(<ChatWorkspace {...props} linkedSequence={999999} />, { wrapper });
    await screen.findByRole("combobox", { name: /Message #general/ });
    await waitFor(() =>
      expect(document.querySelector("[data-message-id]")).not.toBeNull(),
    );
    expect(document.querySelector('[data-highlighted="true"]')).toBeNull();
  });
});
