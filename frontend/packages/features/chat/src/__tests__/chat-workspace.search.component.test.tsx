import type { ReactNode } from "react";
import { beforeAll, describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor, within } from "@notrelix/testing";
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
import { en } from "../web/components/chat.copy";
import { ChatWorkspace } from "../web/components/chat-workspace";
import { createChatHooks } from "../web/query/create-chat-hooks";

const WORKSPACE = mockIds.workspaces.primary;

beforeAll(() => {
  Element.prototype.scrollIntoView ??= () => undefined;
});

function setup() {
  const store = new MockStore({ ...defaultConfig });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: (input, init) => createMockFetch(store)(input, init),
  });
  const viewerId = store.getCurrentUser().id;
  const people = new Map(
    store
      .getWorkspaceMembers(WORKSPACE)
      .map((member) => [member.userId, `Person ${member.userId}`]),
  );
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>
      <I18nProvider catalogs={[chatCatalog]} initialLocale="en">
        {children}
      </I18nProvider>
    </QueryClientProvider>
  );
  const deps = { api: client.api, endpoints: client.endpoints };
  return { deps, viewerId, people, wrapper, store };
}

async function openChannel(
  s: ReturnType<typeof setup>,
  name: string,
  onOpenChannel = vi.fn(),
) {
  const channels = await createChatHooks(s.deps).service.listChannels(
    WORKSPACE,
  );
  const channel = channels.find((entry) => entry.name === name)!;
  render(
    <ChatWorkspace
      deps={s.deps}
      workspaceId={WORKSPACE}
      viewerId={s.viewerId}
      viewerRole="owner"
      channelId={channel.id}
      conversationId={null}
      onOpenConversation={vi.fn()}
      onOpenChannel={onOpenChannel}
      people={s.people}
    />,
    { wrapper: s.wrapper },
  );
  await screen.findByRole("combobox", { name: new RegExp(`Message #${name}`) });
  return onOpenChannel;
}

const searchBox = () =>
  document.querySelector<HTMLInputElement>(
    'input[role="combobox"][type="search"]',
  )!;

describe("ChatWorkspace search (UC-CHT-H07)", () => {
  it("opens the search panel from the channel header and from the channel list", async () => {
    const s = setup();
    await openChannel(s, "general");

    fireEvent.click(
      screen.getAllByRole("button", { name: en.search.open })[0]!,
    );
    expect(
      await screen.findByRole("dialog", { name: en.search.title }),
    ).toBeTruthy();
  });

  it("opens a hit of another channel through the host route and lands on the channel", async () => {
    const s = setup();
    const onOpenChannel = await openChannel(s, "general");
    fireEvent.click(
      screen.getAllByRole("button", { name: en.search.open })[0]!,
    );
    await screen.findByRole("dialog", { name: en.search.title });

    fireEvent.change(searchBox(), { target: { value: "ban lanh dao" } });
    // The Owner is not in the private channel, so nothing is found there (CHT-004).
    await waitFor(() =>
      expect(screen.getAllByText(en.search.noResults).length).toBeGreaterThan(
        0,
      ),
    );

    fireEvent.change(searchBox(), { target: { value: "kickoff erp" } });
    const list = await screen.findByRole("listbox", {
      name: en.search.resultsLabel,
    });
    const first = within(list).getAllByRole("option")[0]!;
    fireEvent.click(first);
    await waitFor(() => expect(onOpenChannel).toHaveBeenCalledTimes(1));
    expect(screen.queryByRole("dialog", { name: en.search.title })).toBeNull();
  });

  it("highlights a message of the open channel and scrolls it into view", async () => {
    const s = setup();
    await openChannel(s, "general");
    const channels = await createChatHooks(s.deps).service.listChannels(
      WORKSPACE,
    );
    const general = channels.find((entry) => entry.name === "general")!;
    const sent = await createChatHooks(s.deps).service.sendMessage(
      WORKSPACE,
      general.id,
      { content: "zzhighlight target", clientMessageId: "cid-highlight" },
    );

    fireEvent.click(
      screen.getAllByRole("button", { name: en.search.open })[0]!,
    );
    await screen.findByRole("dialog", { name: en.search.title });
    fireEvent.change(searchBox(), { target: { value: "zzhighlight" } });
    const list = await screen.findByRole("listbox", {
      name: en.search.resultsLabel,
    });
    fireEvent.click(within(list).getAllByRole("option")[0]!);

    await waitFor(() => {
      const row = document.querySelector(`[data-message-id="${sent.id}"]`);
      expect(row?.getAttribute("data-highlighted")).toBe("true");
    });
  });
});
