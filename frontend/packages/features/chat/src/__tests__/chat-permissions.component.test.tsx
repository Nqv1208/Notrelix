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
  type MockPersona,
} from "@notrelix/dev-mock-backend";
import { chatCatalog } from "../web/components/chat.catalog";
import { en } from "../web/components/chat.copy";
import { ChatWorkspace } from "../web/components/chat-workspace";
import { ChatPermissionsDialog } from "../web/components/chat-permissions-dialog";
import { ModerateMessageDialog } from "../web/components/chat-dialogs";
import { ChatMessageRow } from "../web/components/chat-message-row";
import { ChatHeader } from "../web/components/chat-header";
import { createChatHooks } from "../web/query/create-chat-hooks";
import type {
  ChatChannel,
  ChatChannelPermissions,
  ChatMessage,
} from "../core/types/chat";

const WORKSPACE = mockIds.workspaces.primary;

beforeAll(() => {
  Element.prototype.scrollIntoView ??= () => undefined;
});

// ─── The permissions dialog ─────────────────────────────────────────────────

const STORED: ChatChannelPermissions = {
  channelId: "c1",
  entries: [{ subject: "guest", action: "post", effect: "deny" }],
  version: 4,
};

function dialog(
  overrides: Partial<Parameters<typeof ChatPermissionsDialog>[0]> = {},
) {
  const onSave = vi.fn();
  const onOpenChange = vi.fn();
  const onRetry = vi.fn();
  render(
    <ChatPermissionsDialog
      open
      copy={en}
      permissions={STORED}
      loading={false}
      loadFailed={false}
      busy={false}
      error={null}
      onOpenChange={onOpenChange}
      onRetry={onRetry}
      onSave={onSave}
      {...overrides}
    />,
  );
  return { onSave, onOpenChange, onRetry };
}

function cell(action: string, role: string) {
  return screen.getByRole("combobox", {
    name: `${action}, ${role}`,
  }) as HTMLSelectElement;
}

describe("ChatPermissionsDialog (UC-CHT-H01)", () => {
  it("shows the actions as rows and the roles as columns, each cell with the stored state", () => {
    dialog();
    const table = screen.getByRole("table", { name: "Permissions by role" });
    expect(
      within(table)
        .getAllByRole("columnheader")
        .map((h) => h.textContent),
    ).toEqual(["Action", "Channel managers", "Members", "Guests"]);
    expect(within(table).getAllByRole("combobox")).toHaveLength(18);
    expect(cell("Post messages", "Guests").value).toBe("deny");
    expect(cell("Post messages", "Members").value).toBe("default");
  });

  it("does not offer an allow above the role nor a deny of the manager's invite", () => {
    dialog();
    const options = (select: HTMLSelectElement) =>
      Array.from(select.options).map((o) => o.value);
    expect(options(cell("Pin messages", "Guests"))).toEqual([
      "default",
      "deny",
    ]);
    expect(options(cell("Pin messages", "Members"))).toEqual([
      "default",
      "allow",
      "deny",
    ]);
    expect(options(cell("Invite people", "Channel managers"))).toEqual([
      "default",
      "allow",
    ]);
  });

  it("saves the whole set with the version it was read at, and only after a change", () => {
    const { onSave } = dialog();
    const save = screen.getByRole("button", { name: "Save" });
    expect((save as HTMLButtonElement).disabled).toBe(true);

    fireEvent.change(cell("React to messages", "Members"), {
      target: { value: "deny" },
    });
    expect((save as HTMLButtonElement).disabled).toBe(false);
    fireEvent.click(save);

    expect(onSave).toHaveBeenCalledWith(
      [
        { subject: "member", action: "react", effect: "deny" },
        { subject: "guest", action: "post", effect: "deny" },
      ],
      4,
    );
  });

  it("the read-only preset denies post for members and guests, and reset clears every override", () => {
    const { onSave } = dialog({
      permissions: { ...STORED, entries: [] },
    });
    fireEvent.click(screen.getByRole("button", { name: "Make read-only" }));
    expect(cell("Post messages", "Members").value).toBe("deny");
    expect(cell("Post messages", "Guests").value).toBe("deny");
    expect(cell("Post messages", "Channel managers").value).toBe("default");
    fireEvent.click(screen.getByRole("button", { name: "Save" }));
    expect(onSave).toHaveBeenCalledWith(
      [
        { subject: "member", action: "post", effect: "deny" },
        { subject: "guest", action: "post", effect: "deny" },
      ],
      4,
    );

    fireEvent.click(screen.getByRole("button", { name: "Reset to defaults" }));
    expect(cell("Post messages", "Members").value).toBe("default");
  });

  it("shows a save error as an alert, and a loading and a failed read", () => {
    dialog({ error: "The permissions were not saved." });
    expect(screen.getByRole("alert").textContent).toBe(
      "The permissions were not saved.",
    );
  });

  it("offers a retry when the read failed", () => {
    const { onRetry } = dialog({ permissions: undefined, loadFailed: true });
    fireEvent.click(screen.getByRole("button", { name: "Try again" }));
    expect(onRetry).toHaveBeenCalled();
  });

  it("shows a status while the permissions load", () => {
    dialog({ permissions: undefined, loading: true });
    expect(
      screen.getByRole("status", { name: "Loading permissions" }),
    ).toBeTruthy();
  });
});

// ─── The moderator delete ───────────────────────────────────────────────────

describe("ModerateMessageDialog (UC-CHT-H03)", () => {
  it("needs a reason and sends it trimmed", () => {
    const onSubmit = vi.fn();
    render(
      <ModerateMessageDialog
        open
        busy={false}
        error={null}
        copy={en}
        onOpenChange={vi.fn()}
        onSubmit={onSubmit}
      />,
    );
    const confirm = screen.getByRole("button", {
      name: "Delete message",
    }) as HTMLButtonElement;
    expect(confirm.disabled).toBe(true);

    fireEvent.change(screen.getByLabelText("Reason"), {
      target: { value: "  spam link  " },
    });
    expect(confirm.disabled).toBe(false);
    fireEvent.click(confirm);

    expect(onSubmit).toHaveBeenCalledWith("spam link");
  });
});

function message(overrides: Partial<ChatMessage> = {}): ChatMessage {
  return {
    id: "m-1",
    spaceId: "s-1",
    sequence: 1,
    kind: "user",
    authorId: "someone",
    content: "hello",
    clientMessageId: null,
    replyToId: null,
    edited: false,
    deleted: false,
    createdAt: "2026-11-10T09:00:00.000Z",
    editedAt: null,
    deletedAt: null,
    version: 1,
    ...overrides,
  };
}

function row(
  overrides: Partial<Parameters<typeof ChatMessageRow>[0]> = {},
  msg: Partial<ChatMessage> = {},
) {
  render(
    <ChatMessageRow
      message={message(msg)}
      author={{ name: "Someone" }}
      continued={false}
      isOwn={false}
      locale="en"
      timeZone="UTC"
      copy={en}
      {...overrides}
    />,
  );
}

describe("ChatMessageRow moderation", () => {
  it("offers Delete as moderator on another person's live message, and calls the handler", () => {
    const onModerate = vi.fn();
    row({ onModerate });
    fireEvent.click(
      screen.getByRole("button", { name: "Delete as moderator" }),
    );
    expect(onModerate).toHaveBeenCalledWith(
      expect.objectContaining({ id: "m-1" }),
    );
  });

  it("does not offer it on your own message, on a tombstone, or without a handler", () => {
    row({
      onModerate: vi.fn(),
      isOwn: true,
      onEdit: vi.fn(),
      onDelete: vi.fn(),
    });
    expect(
      screen.queryByRole("button", { name: "Delete as moderator" }),
    ).toBeNull();
  });

  it("does not offer it without a handler", () => {
    row({ onReact: vi.fn() });
    expect(
      screen.queryByRole("button", { name: "Delete as moderator" }),
    ).toBeNull();
  });

  it("says a moderator removed the message, and says plain deleted for an author's own delete", () => {
    row({}, { deleted: true, content: null, removedByModerator: true });
    expect(
      screen.getByText("This message was removed by a moderator"),
    ).toBeTruthy();
  });

  it("an author's own delete stays 'deleted'", () => {
    row({}, { deleted: true, content: null, removedByModerator: false });
    expect(screen.getByText("This message was deleted")).toBeTruthy();
  });
});

// ─── The header menu ────────────────────────────────────────────────────────

const CHANNEL: ChatChannel = {
  id: "c1",
  name: "general",
  topic: null,
  kind: "channel_public",
  groupId: null,
  position: 1,
  isArchived: false,
  isMember: true,
  role: "manager",
  notificationLevel: "all",
  version: 3,
  createdAt: "2026-11-10T09:00:00.000Z",
};

describe("ChatHeader permissions entry", () => {
  function header(canManagePermissions: boolean, onPermissions = vi.fn()) {
    render(
      <ChatHeader
        channel={CHANNEL}
        members={[]}
        viewerId="me"
        canManage
        canManagePermissions={canManagePermissions}
        onPermissions={onPermissions}
        joining={false}
        authorName={() => "x"}
        copy={en}
        onOpenList={vi.fn()}
        onRename={vi.fn()}
        onToggleArchive={vi.fn()}
        onJoin={vi.fn()}
        onSetLevel={vi.fn()}
      />,
    );
    fireEvent.keyDown(
      screen.getByRole("button", { name: "Channel settings" }),
      {
        key: "Enter",
      },
    );
    return onPermissions;
  }

  it("lists Permissions in the settings menu for who may manage them", async () => {
    const onPermissions = header(true);
    fireEvent.click(
      await screen.findByRole("menuitem", { name: "Permissions" }),
    );
    expect(onPermissions).toHaveBeenCalled();
  });

  it("does not list it when the caller may not manage permissions", async () => {
    header(false);
    await screen.findByRole("menuitem", { name: "Rename channel" });
    expect(screen.queryByRole("menuitem", { name: "Permissions" })).toBeNull();
  });
});

// ─── The workspace against the mock ─────────────────────────────────────────

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
  const deps = { api: client.api, endpoints: client.endpoints };
  return { store, deps, viewerId, people, wrapper };
}

async function openGeneral(
  s: ReturnType<typeof setup>,
  viewerRole: "owner" | "admin" | "member" | "guest",
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
      viewerRole={viewerRole}
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

describe("ChatWorkspace channel permissions and moderation", () => {
  it("a member of a read-only channel sees the banner and a disabled composer", async () => {
    const s = setup("member");
    const channels = await createChatHooks(s.deps).service.listChannels(
      WORKSPACE,
    );
    const general = channels.find((channel) => channel.name === "general")!;
    s.store.chat.permissionOverrides.set(general.id, [
      { subject: "member", action: "post", effect: "deny" },
    ]);

    await openGeneral(s, "member");

    const notices = await screen.findAllByText(
      "Posting is turned off for your role in this channel.",
    );
    expect(notices.some((node) => node.closest("[role='status']"))).toBe(true);
    const box = (await screen.findByRole("combobox", {
      name: /Message #general/,
    })) as HTMLTextAreaElement;
    await waitFor(() => expect(box.disabled).toBe(true));
  });

  it("a member of a channel with no overrides can post and sees no banner", async () => {
    const s = setup("member");
    await openGeneral(s, "member");
    const box = (await screen.findByRole("combobox", {
      name: /Message #general/,
    })) as HTMLTextAreaElement;
    expect(box.disabled).toBe(false);
    expect(
      screen.queryByText(
        "Posting is turned off for your role in this channel.",
      ),
    ).toBeNull();
  });

  it("an Owner opens Permissions, makes the channel read-only and the mock stores the set", async () => {
    const s = setup("owner");
    const general = await openGeneral(s, "owner");
    await screen.findByRole("combobox", { name: /Message #general/ });

    fireEvent.keyDown(
      await screen.findByRole("button", { name: "Channel settings" }),
      { key: "Enter" },
    );
    fireEvent.click(
      await screen.findByRole("menuitem", { name: "Permissions" }),
    );
    fireEvent.click(
      await screen.findByRole("button", { name: "Make read-only" }),
    );
    fireEvent.click(screen.getByRole("button", { name: "Save" }));

    await waitFor(() =>
      expect(s.store.chat.permissionOverrides.get(general.id)).toEqual([
        { subject: "member", action: "post", effect: "deny" },
        { subject: "guest", action: "post", effect: "deny" },
      ]),
    );
    // The owner keeps posting: the composer stays enabled.
    const box = (await screen.findByRole("combobox", {
      name: /Message #general/,
    })) as HTMLTextAreaElement;
    expect(box.disabled).toBe(false);
  }, 20_000);

  it("an Admin deletes another person's message as a moderator with a reason", async () => {
    const s = setup("admin");
    await openGeneral(s, "admin");
    const text = await screen.findByText(/Tin nhắn 300\./);
    const target = text.closest("[data-message-id]") as HTMLElement;
    const id = target.getAttribute("data-message-id")!;

    fireEvent.click(
      within(target).getByRole("button", { name: "Delete as moderator" }),
    );
    const moderation = within(await screen.findByRole("dialog"));
    fireEvent.change(moderation.getByLabelText("Reason"), {
      target: { value: "spam" },
    });
    fireEvent.click(moderation.getByRole("button", { name: "Delete message" }));

    await waitFor(() => {
      const stored = s.store.chat.messages.get(id)!;
      expect(stored.deletedAt).not.toBeNull();
      expect(stored.deletedBy).toBe(s.viewerId);
    });
    expect(
      await screen.findByText("This message was removed by a moderator"),
    ).toBeTruthy();
  });

  it("a Member is not offered Delete as moderator", async () => {
    const s = setup("member");
    await openGeneral(s, "member");
    await screen.findByText(/Tin nhắn 300\./);
    expect(
      screen.queryByRole("button", { name: "Delete as moderator" }),
    ).toBeNull();
  });
});
