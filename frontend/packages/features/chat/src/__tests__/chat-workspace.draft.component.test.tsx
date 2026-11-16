import type { ReactNode } from "react";
import { beforeAll, describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@notrelix/testing";
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

describe("ChatWorkspace draft per space (UC-CHT-B07)", () => {
  it("keeps what was typed in each channel while the page is open and does not carry it to another channel", async () => {
    const store = new MockStore({ ...defaultConfig, persona: "owner" });
    const client = createNotrelixClient({
      baseUrl: "http://localhost:8000/api/v1",
      fetchImpl: (input, init) => createMockFetch(store)(input, init),
    });
    const deps = { api: client.api, endpoints: client.endpoints };
    const channels = (
      await createChatHooks(deps).service.listChannels(WORKSPACE)
    ).filter((channel) => channel.isMember && !channel.isArchived);
    const first = channels[0]!;
    const second = channels[1]!;
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
    const tree = (channelId: string) => (
      <ChatWorkspace
        deps={deps}
        workspaceId={WORKSPACE}
        viewerId={store.getCurrentUser().id}
        viewerRole="owner"
        channelId={channelId}
        onOpenChannel={vi.fn()}
        people={new Map()}
      />
    );
    const view = render(tree(first.id), { wrapper });
    const box = () =>
      screen.findByRole("combobox", {
        name: /Message #/,
      }) as Promise<HTMLTextAreaElement>;

    fireEvent.change(await box(), { target: { value: "half written" } });
    view.rerender(tree(second.id));
    expect((await box()).value).toBe("");
    fireEvent.change(await box(), { target: { value: "other draft" } });
    view.rerender(tree(first.id));
    expect((await box()).value).toBe("half written");
    view.rerender(tree(second.id));
    expect((await box()).value).toBe("other draft");
  });
});
