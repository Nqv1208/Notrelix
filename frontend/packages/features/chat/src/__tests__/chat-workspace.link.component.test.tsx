import type { ReactNode } from "react";
import { beforeAll, describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor } from "@notrelix/testing";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { createNotrelixClient } from "@notrelix/contracts";
import { I18nProvider } from "@notrelix/i18n";
import {
  MockStore,
  createMockFetch,
  defaultConfig,
  mockIds,
  type MockPersona,
} from "@notrelix/dev-mock-backend";
import { chatCatalog } from "../web/components/chat.catalog";
import { ChatWorkspace } from "../web/components/chat-workspace";
import { createChatHooks } from "../web/query/create-chat-hooks";

const WORKSPACE = mockIds.workspaces.primary;
const BOARD = mockIds.boards.roadmap;

beforeAll(() => {
  Element.prototype.scrollIntoView ??= () => undefined;
});

function setup(persona: MockPersona) {
  const store = new MockStore({ ...defaultConfig, persona });
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
  return {
    store,
    deps: { api: client.api, endpoints: client.endpoints },
    viewerId,
    people,
    wrapper,
  };
}

async function openGeneral(
  s: ReturnType<typeof setup>,
  role: "owner" | "member",
) {
  const channels = await createChatHooks(s.deps).service.listChannels(
    WORKSPACE,
  );
  const general = channels.find((channel) => channel.name === "general")!;
  render(
    <ChatWorkspace
      deps={s.deps}
      workspaceId={WORKSPACE}
      viewerId={s.viewerId}
      viewerRole={role}
      channelId={general.id}
      conversationId={null}
      onOpenConversation={vi.fn()}
      onOpenChannel={vi.fn()}
      people={s.people}
    />,
    { wrapper: s.wrapper },
  );
  return general;
}

describe("ChatWorkspace linked resource (UC-CHT-H10)", () => {
  it("an Owner links the channel to a Board after the preview, the chip appears, and unlink removes it", async () => {
    const s = setup("owner");
    const general = await openGeneral(s, "owner");
    await screen.findByRole("combobox", { name: /Message #general/ });

    fireEvent.keyDown(
      await screen.findByRole("button", { name: "Channel settings" }),
      { key: "Enter" },
    );
    fireEvent.click(
      await screen.findByRole("menuitem", { name: "Link a resource" }),
    );
    fireEvent.change(
      await screen.findByLabelText("Address or id of the resource"),
      {
        target: { value: `/workspaces/${WORKSPACE}/boards/${BOARD}` },
      },
    );
    fireEvent.click(screen.getByRole("button", { name: "Check" }));
    await screen.findByTestId("chat-link-preview");
    const confirm = screen.getByRole("button", {
      name: "Link",
    }) as HTMLButtonElement;
    await waitFor(() => expect(confirm.disabled).toBe(false));
    fireEvent.click(confirm);

    const title = s.store.getBoard(BOARD)!.title;
    const chip = await screen.findByRole("link", {
      name: `Open the linked Board ${title}`,
    });
    expect(chip.getAttribute("href")).toBe(
      `/workspaces/${WORKSPACE}/boards/${BOARD}`,
    );
    expect(s.store.chat.spaces.get(general.id)!.link).toEqual({
      kind: "work-management.board",
      resourceId: BOARD,
    });

    fireEvent.keyDown(
      screen.getByRole("button", { name: "Channel settings" }),
      { key: "Enter" },
    );
    fireEvent.click(
      await screen.findByRole("menuitem", { name: "Remove the link" }),
    );
    await waitFor(() =>
      expect(s.store.chat.spaces.get(general.id)!.link ?? null).toBeNull(),
    );
    await waitFor(() =>
      expect(
        screen.queryByRole("link", { name: /Open the linked Board/ }),
      ).toBeNull(),
    );
  }, 20_000);

  it("a member who is not a manager sees the chip but no way to change it", async () => {
    const s = setup("member");
    const channels = await createChatHooks(s.deps).service.listChannels(
      WORKSPACE,
    );
    const general = channels.find((channel) => channel.name === "general")!;
    s.store.chat.spaces.get(general.id)!.link = {
      kind: "work-management.board",
      resourceId: BOARD,
    };

    await openGeneral(s, "member");

    await screen.findByRole("link", { name: /Open the linked Board/ });
    expect(
      screen.queryByRole("button", { name: "Channel settings" }),
    ).toBeNull();
  });
});
