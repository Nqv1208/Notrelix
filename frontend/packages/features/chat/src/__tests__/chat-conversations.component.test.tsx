import type { ReactNode } from "react";
import { fireEvent, render, screen, waitFor, within } from "@notrelix/testing";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { describe, expect, it, vi } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import { I18nProvider } from "@notrelix/i18n";
import { chatCatalog } from "../web/components/chat.catalog";
import {
  MockStore,
  createMockFetch,
  defaultConfig,
  mockIds,
} from "@notrelix/dev-mock-backend";
import { ChatNewMessageDialog } from "../web/components/chat-new-message-dialog";
import {
  ChatConversationSection,
  activityLabel,
  previewOf,
  type ChatConversationListView,
} from "../web/components/chat-conversation-list";
import { ChatConversationDetails } from "../web/components/chat-conversation-view";
import { ChatWorkspace } from "../web/components/chat-workspace";
import type { ChatConversation } from "../core/types/chat";
import { en } from "../web/components/chat.copy";

const WORKSPACE = mockIds.workspaces.primary;

function conversation(
  overrides: Partial<ChatConversation> = {},
): ChatConversation {
  return {
    id: "conv-1",
    kind: "direct",
    name: null,
    participants: [
      { userId: "me", role: "member", joinedAt: "2026-11-01T08:00:00Z" },
      { userId: "quan", role: "member", joinedAt: "2026-11-01T08:00:00Z" },
    ],
    isHidden: false,
    notificationLevel: "all",
    unreadCount: 0,
    lastMessage: null,
    lastActivityAt: "2026-11-10T09:00:00Z",
    version: 1,
    createdAt: "2026-11-01T08:00:00Z",
    ...overrides,
  };
}

const NAMES: Record<string, string> = {
  me: "Thu Hà",
  quan: "Trần Minh Quân",
  an: "Lê Hoài An",
  linh: "Đặng Khánh Linh",
};
const nameOf = (id: string) => NAMES[id] ?? "?";

function listView(
  overrides: Partial<ChatConversationListView> = {},
): ChatConversationListView {
  return {
    conversations: [],
    status: "ready",
    activeId: null,
    viewerId: "me",
    locale: "en",
    timeZone: "UTC",
    nameOf,
    unreadOf: (entry) => entry.unreadCount,
    onSelect: vi.fn(),
    onNewMessage: vi.fn(),
    onRetry: vi.fn(),
    ...overrides,
  };
}

describe("conversation section of the sidebar", () => {
  it("lists a direct conversation by the other person, a group by its members, with preview and unread count", () => {
    const direct = conversation({
      unreadCount: 2,
      lastMessage: {
        sequence: 4,
        authorId: "quan",
        kind: "user",
        text: "Chị duyệt giúp em",
        createdAt: "2026-11-10T09:00:00Z",
      },
    });
    const group = conversation({
      id: "conv-2",
      kind: "group",
      participants: [
        ...direct.participants,
        { userId: "an", role: "member", joinedAt: "2026-11-01T08:00:00Z" },
      ],
      lastMessage: {
        sequence: 2,
        authorId: "an",
        kind: "user",
        text: "Đã cập nhật",
        createdAt: "2026-11-10T08:00:00Z",
      },
    });
    render(
      <ChatConversationSection
        copy={en}
        view={listView({ conversations: [direct, group], activeId: "conv-2" })}
      />,
    );
    expect(
      screen.getByRole("heading", { name: "Direct messages" }),
    ).toBeTruthy();
    const row = screen.getByRole("button", {
      name: /^Direct message: Trần Minh Quân/,
    });
    expect(row.getAttribute("aria-label")).toContain("2 unread messages");
    expect(within(row).getByText("Chị duyệt giúp em")).toBeTruthy();
    const groupRow = screen.getByRole("button", {
      name: /Group conversation/,
    });
    expect(groupRow.getAttribute("aria-current")).toBe("page");
    expect(within(groupRow).getByText("Lê Hoài An: Đã cập nhật")).toBeTruthy();
  });

  it("opens a conversation and starts a new message from its buttons", () => {
    const view = listView({ conversations: [conversation()] });
    render(<ChatConversationSection copy={en} view={view} />);
    fireEvent.click(
      screen.getByRole("button", { name: /^Direct message: Trần Minh Quân/ }),
    );
    expect(view.onSelect).toHaveBeenCalledWith("conv-1");
    fireEvent.click(screen.getByRole("button", { name: "New message" }));
    expect(view.onNewMessage).toHaveBeenCalled();
  });

  it("says when there are no conversations, and offers a retry when the list failed", () => {
    const { rerender } = render(
      <ChatConversationSection copy={en} view={listView()} />,
    );
    expect(screen.getByText("No direct messages yet.")).toBeTruthy();
    const view = listView({ status: "error" });
    rerender(<ChatConversationSection copy={en} view={view} />);
    fireEvent.click(screen.getByRole("button", { name: "Try again" }));
    expect(view.onRetry).toHaveBeenCalled();
  });

  it("prefixes the caller's own last message with You, and shows the time of the day for today", () => {
    const own = conversation({
      lastMessage: {
        sequence: 1,
        authorId: "me",
        kind: "user",
        text: "Cảm ơn nhé",
        createdAt: "2026-11-10T09:05:00Z",
      },
    });
    expect(previewOf(own, "me", nameOf, en)).toBe("You: Cảm ơn nhé");
    expect(
      activityLabel(
        "2026-11-10T09:05:00Z",
        new Date("2026-11-10T20:00:00Z"),
        "en",
        "UTC",
      ),
    ).toMatch(/09:05/);
  });
});

describe("new message dialog", () => {
  const people = [
    { id: "quan", name: "Trần Minh Quân" },
    { id: "an", name: "Lê Hoài An" },
    { id: "linh", name: "Đặng Khánh Linh" },
  ];

  function open(onSubmit = vi.fn()) {
    render(
      <ChatNewMessageDialog
        open
        busy={false}
        people={people}
        error={null}
        copy={en}
        onOpenChange={vi.fn()}
        onSubmit={onSubmit}
      />,
    );
    return onSubmit;
  }

  it("starts a direct conversation when one person is picked", () => {
    const onSubmit = open();
    expect(
      (
        screen.getByRole("button", {
          name: "Start conversation",
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(true);
    fireEvent.click(screen.getByRole("checkbox", { name: /Lê Hoài An/ }));
    fireEvent.click(screen.getByRole("button", { name: "Start conversation" }));
    expect(onSubmit).toHaveBeenCalledWith({ kind: "direct", userId: "an" });
  });

  it("starts a group, with its optional name, when several people are picked", () => {
    const onSubmit = open();
    fireEvent.click(screen.getByRole("checkbox", { name: /Lê Hoài An/ }));
    fireEvent.click(screen.getByRole("checkbox", { name: /Đặng Khánh Linh/ }));
    fireEvent.change(screen.getByLabelText("Group name (optional)"), {
      target: { value: "Dự án ERP" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Create group" }));
    expect(onSubmit).toHaveBeenCalledWith({
      kind: "group",
      userIds: ["an", "linh"],
      name: "Dự án ERP",
    });
  });

  it("finds people without caring about case or diacritics", () => {
    open();
    fireEvent.change(screen.getByLabelText("Find people"), {
      target: { value: "quan" },
    });
    expect(
      screen.getByRole("checkbox", { name: /Trần Minh Quân/ }),
    ).toBeTruthy();
    expect(screen.queryByRole("checkbox", { name: /Lê Hoài An/ })).toBeNull();
    fireEvent.change(screen.getByLabelText("Find people"), {
      target: { value: "zzz" },
    });
    expect(screen.getByText("No one matches.")).toBeTruthy();
  });

  it("stops picking at the group cap, the caller included", () => {
    const many = Array.from({ length: 9 }, (_, index) => ({
      id: `p${index}`,
      name: `Person ${index}`,
    }));
    render(
      <ChatNewMessageDialog
        open
        busy={false}
        people={many}
        error="You cannot message this person right now."
        copy={en}
        onOpenChange={vi.fn()}
        onSubmit={vi.fn()}
      />,
    );
    for (let index = 0; index < 7; index++) {
      fireEvent.click(
        screen.getByRole("checkbox", { name: `Person ${index}` }),
      );
    }
    expect(
      (screen.getByRole("checkbox", { name: "Person 8" }) as HTMLButtonElement)
        .disabled,
    ).toBe(true);
    expect(screen.getByRole("alert").textContent).toContain("cannot message");
  });
});

describe("conversation details panel", () => {
  function details(
    overrides: Partial<Parameters<typeof ChatConversationDetails>[0]> = {},
  ) {
    const handlers = {
      onBlock: vi.fn(),
      onUnblock: vi.fn(),
      onAddPeople: vi.fn(),
      onLeave: vi.fn(),
      onHide: vi.fn(),
    };
    render(
      <ChatConversationDetails
        conversation={conversation()}
        viewerId="me"
        nameOf={nameOf}
        copy={en}
        locale="en"
        blocked={false}
        busy={false}
        {...handlers}
        {...overrides}
      />,
    );
    return handlers;
  }

  it("shows the profile of the other person with hide and block", () => {
    const handlers = details();
    expect(
      screen.getByRole("heading", { name: "Trần Minh Quân" }),
    ).toBeTruthy();
    expect(screen.getByText("Conversation started")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Block person" }));
    expect(handlers.onBlock).toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button", { name: "Hide conversation" }));
    expect(handlers.onHide).toHaveBeenCalled();
  });

  it("offers to unblock a blocked person", () => {
    const handlers = details({ blocked: true });
    expect(screen.getByText("Blocked")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Unblock person" }));
    expect(handlers.onUnblock).toHaveBeenCalled();
  });

  it("lists the members of a group, marks the caller, and offers add and leave", () => {
    const handlers = details({
      conversation: conversation({
        kind: "group",
        participants: [
          { userId: "me", role: "member", joinedAt: "2026-11-01T08:00:00Z" },
          { userId: "quan", role: "member", joinedAt: "2026-11-01T08:00:00Z" },
          { userId: "an", role: "member", joinedAt: "2026-11-01T08:00:00Z" },
        ],
      }),
    });
    expect(screen.getByText("Members, 3")).toBeTruthy();
    expect(screen.getByText("You")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Add people" }));
    expect(handlers.onAddPeople).toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button", { name: "Leave group" }));
    expect(handlers.onLeave).toHaveBeenCalled();
  });

  it("disables adding people when the group is full", () => {
    details({
      conversation: conversation({
        kind: "group",
        participants: Array.from({ length: 8 }, (_, index) => ({
          userId: `p${index}`,
          role: "member" as const,
          joinedAt: "2026-11-01T08:00:00Z",
        })),
      }),
    });
    expect(
      (screen.getByRole("button", { name: "Add people" }) as HTMLButtonElement)
        .disabled,
    ).toBe(true);
  });
});

// ─── The workspace with a conversation open (mock backend) ──────────────────

function workspaceSetup() {
  const store = new MockStore({ ...defaultConfig });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: (input, init) => createMockFetch(store)(input, init),
  });
  const viewerId = store.getCurrentUser().id;
  const members = store.getWorkspaceMembers(WORKSPACE);
  const people = new Map(
    members.map((member) => [member.userId, `Person ${member.userId}`]),
  );
  const others = members
    .map((member) => member.userId)
    .filter((id) => id !== viewerId);
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
  return { store, deps, viewerId, people, others, wrapper, queryClient };
}

describe("ChatWorkspace with a conversation", () => {
  it("opens a direct conversation: intro for an empty history, details, hide with show again, and block with confirmation", async () => {
    const { deps, viewerId, people, others, wrapper } = workspaceSetup();
    const service = (
      await import("../web/query/create-chat-hooks")
    ).createChatHooks(deps).service;
    const started = await service.startConversation(
      WORKSPACE,
      { kind: "direct", userId: others[0]! },
      "workspace-test-key",
    );
    const otherName = people.get(others[0]!)!;
    render(
      <ChatWorkspace
        deps={deps}
        workspaceId={WORKSPACE}
        viewerId={viewerId}
        viewerRole="owner"
        channelId={null}
        conversationId={started.conversation.id}
        onOpenConversation={vi.fn()}
        onOpenChannel={vi.fn()}
        people={people}
      />,
      { wrapper },
    );
    expect(
      await screen.findByRole("heading", { level: 1, name: otherName }),
    ).toBeTruthy();
    expect(
      await screen.findByText(
        `This is the start of your conversation with ${otherName}.`,
      ),
    ).toBeTruthy();
    expect(
      screen.getByRole("combobox", { name: `Message ${otherName}` }),
    ).toBeTruthy();

    // Details panel: the profile of the other person.
    fireEvent.click(
      screen.getByRole("button", { name: "Conversation details" }),
    );
    fireEvent.click(
      await screen.findByRole("button", { name: "Block person" }),
    );
    const confirm = await screen.findByRole("alertdialog");
    expect(within(confirm).getByText(`Block ${otherName}?`)).toBeTruthy();
    fireEvent.click(within(confirm).getByRole("button", { name: "Block" }));

    // Blocked state: a notice, a read-only composer and an unblock action.
    expect(
      await screen.findByText(new RegExp(`You blocked ${otherName}`)),
    ).toBeTruthy();
    await waitFor(() =>
      expect(
        screen
          .getByRole("combobox", { name: `Message ${otherName}` })
          .hasAttribute("disabled"),
      ).toBe(true),
    );

    // Hide: the conversation is marked hidden here and can be shown again.
    fireEvent.click(
      screen.getAllByRole("button", { name: "Hide conversation" })[0]!,
    );
    const showAgain = await screen.findByRole("button", { name: "Show again" });
    fireEvent.click(showAgain);
    await waitFor(() =>
      expect(screen.queryByRole("button", { name: "Show again" })).toBeNull(),
    );
  }, 20_000);

  it("shows a conversation that is not in the list as not found, with a way to start a message", async () => {
    const { deps, viewerId, people, wrapper } = workspaceSetup();
    render(
      <ChatWorkspace
        deps={deps}
        workspaceId={WORKSPACE}
        viewerId={viewerId}
        viewerRole="owner"
        channelId={null}
        conversationId="missing-conversation"
        onOpenConversation={vi.fn()}
        onOpenChannel={vi.fn()}
        people={people}
      />,
      { wrapper },
    );
    expect(
      await screen.findByText("This conversation is not in your list"),
    ).toBeTruthy();
    expect(
      screen.getAllByRole("button", { name: "New message" }).length,
    ).toBeGreaterThan(1);
  });
});
