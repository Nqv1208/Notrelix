import type { ReactNode } from "react";
import { beforeAll, describe, expect, it, vi } from "vitest";
import {
  act,
  fireEvent,
  render,
  renderHook,
  screen,
  waitFor,
} from "@notrelix/testing";
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
import { en, vi as viCopy } from "../web/components/chat.copy";
import { createChatHooks } from "../web/query/create-chat-hooks";
import { ChatHeader } from "../web/components/chat-header";
import {
  ChatLinkChip,
  ChatLinkDialog,
} from "../web/components/chat-resource-link";
import { DiscussButton } from "../web/components/chat-discuss-button";
import { chatFailureOf } from "../core/api/chat-failure";
import type { ChatChannel, ChatChannelLink } from "../core/types/chat";

const WORKSPACE = mockIds.workspaces.primary;
const BOARD = mockIds.boards.roadmap;
const PAGE = "mock-doc-invoice-process";

beforeAll(() => {
  Element.prototype.scrollIntoView ??= () => undefined;
});

function setup(persona: MockPersona = "owner") {
  const store = new MockStore({ ...defaultConfig, persona });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: (input, init) => createMockFetch(store)(input, init),
  });
  const hooks = createChatHooks({
    api: client.api,
    endpoints: client.endpoints,
  });
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
  return { store, hooks, queryClient, wrapper };
}

async function generalChannel(hooks: ReturnType<typeof setup>["hooks"]) {
  const channels = await hooks.service.listChannels(WORKSPACE);
  return channels.find((channel) => channel.name === "general")!;
}

function link(over: Partial<ChatChannelLink> = {}): ChatChannelLink {
  return {
    kind: "work-management.board",
    resourceId: "b-1",
    accessible: true,
    title: "Roadmap",
    ...over,
  };
}

describe("ChatLinkChip (UC-CHT-H10)", () => {
  it("opens the resource when the viewer can see it, with its title and kind", () => {
    render(<ChatLinkChip link={link()} workspaceId={WORKSPACE} copy={en} />);
    const chip = screen.getByRole("link", {
      name: "Open the linked Board Roadmap",
    });
    expect(chip.getAttribute("href")).toBe(
      `/workspaces/${WORKSPACE}/boards/b-1`,
    );
    expect(chip.getAttribute("data-channel-link")).toBe("resolved");
    expect(screen.getByText("Board")).toBeTruthy();
  });

  it("is a generic chip with no title and no link when the viewer cannot see the resource", () => {
    const { container } = render(
      <ChatLinkChip
        link={link({ accessible: false, title: null })}
        workspaceId={WORKSPACE}
        copy={en}
      />,
    );
    expect(screen.queryByRole("link")).toBeNull();
    expect(screen.queryByText("Roadmap")).toBeNull();
    expect(screen.getByText("Board you cannot view")).toBeTruthy();
    expect(
      container
        .querySelector("[data-channel-link]")
        ?.getAttribute("data-channel-link"),
    ).toBe("no-access");
  });

  it("is generic and unavailable for a kind with no route here (a Team), in Vietnamese too", () => {
    const { container } = render(
      <ChatLinkChip
        link={link({
          kind: "governance.team",
          accessible: true,
          title: "Platform",
        })}
        workspaceId={WORKSPACE}
        copy={viCopy}
      />,
    );
    expect(screen.queryByRole("link")).toBeNull();
    expect(
      container
        .querySelector("[data-channel-link]")
        ?.getAttribute("data-channel-link"),
    ).toBe("unavailable");
    expect(screen.getByText("Nhóm đã liên kết")).toBeTruthy();
  });
});

describe("ChatHeader link entries (UC-CHT-H10)", () => {
  const channel: ChatChannel = {
    id: "c1",
    name: "general",
    topic: null,
    kind: "channel_public",
    groupId: null,
    position: 0,
    isArchived: false,
    isMember: true,
    role: "manager",
    notificationLevel: "all",
    version: 3,
    createdAt: "2026-11-01T08:00:00Z",
    link: link(),
  };
  const props = {
    channel,
    members: [],
    viewerId: "me",
    canManage: true,
    joining: false,
    authorName: () => "x",
    copy: en,
    onOpenList: () => undefined,
    onRename: () => undefined,
    onToggleArchive: () => undefined,
    onJoin: () => undefined,
    onSetLevel: () => undefined,
    workspaceId: WORKSPACE,
  };

  it("shows the chip and, for a manager, change and remove entries in the manage menu", async () => {
    const onLinkResource = vi.fn();
    const onUnlinkResource = vi.fn();
    render(
      <ChatHeader
        {...props}
        onLinkResource={onLinkResource}
        onUnlinkResource={onUnlinkResource}
      />,
    );
    expect(
      screen.getByRole("link", { name: /Open the linked Board Roadmap/ }),
    ).toBeTruthy();

    fireEvent.keyDown(screen.getByRole("button", { name: en.header.manage }), {
      key: "Enter",
    });
    fireEvent.click(
      await screen.findByRole("menuitem", {
        name: "Change the linked resource",
      }),
    );
    expect(onLinkResource).toHaveBeenCalledTimes(1);
  });

  it("offers no link entries without the handlers (a viewer), but still shows the chip", () => {
    render(<ChatHeader {...props} canManage={false} />);
    expect(screen.getByRole("link", { name: /Roadmap/ })).toBeTruthy();
    expect(screen.queryByRole("button", { name: en.header.manage })).toBeNull();
  });
});

describe("ChatLinkDialog (UC-CHT-H10)", () => {
  function open(
    s: ReturnType<typeof setup>,
    over: Partial<Parameters<typeof ChatLinkDialog>[0]> = {},
  ) {
    const onConfirm = vi.fn();
    const onOpenChange = vi.fn();
    render(
      <ChatLinkDialog
        open
        copy={en}
        workspaceId={WORKSPACE}
        hooks={s.hooks}
        busy={false}
        error={null}
        onOpenChange={onOpenChange}
        onConfirm={onConfirm}
        {...over}
      />,
      { wrapper: s.wrapper },
    );
    return { onConfirm, onOpenChange };
  }

  const type = (text: string) =>
    fireEvent.change(screen.getByLabelText("Address or id of the resource"), {
      target: { value: text },
    });
  const confirm = () =>
    screen.getByRole("button", { name: "Link" }) as HTMLButtonElement;

  it("shows what a pasted Board address resolves to and links only after the preview", async () => {
    const s = setup("owner");
    const { onConfirm } = open(s);
    expect(confirm().disabled).toBe(true);

    type(`/workspaces/${WORKSPACE}/boards/${BOARD}`);
    fireEvent.click(screen.getByRole("button", { name: "Check" }));

    const title = s.store.getBoard(BOARD)!.title;
    expect(await screen.findByTestId("chat-link-preview")).toBeTruthy();
    expect(screen.getByText(title)).toBeTruthy();
    await waitFor(() => expect(confirm().disabled).toBe(false));
    fireEvent.click(confirm());
    expect(onConfirm).toHaveBeenCalledWith({
      kind: "work-management.board",
      resourceId: BOARD,
    });
  });

  it("takes a Page address too, and a bare id with the kind chosen", async () => {
    const s = setup("owner");
    const { onConfirm } = open(s);
    fireEvent.click(screen.getByRole("radio", { name: "Page" }));
    type(PAGE);
    fireEvent.click(screen.getByRole("button", { name: "Check" }));
    await waitFor(() => expect(confirm().disabled).toBe(false));
    fireEvent.click(confirm());
    expect(onConfirm).toHaveBeenCalledWith({
      kind: "documents.page",
      resourceId: PAGE,
    });
  });

  it("refuses an address that is not a Board or Page of this Workspace, without asking the server", () => {
    const s = setup("owner");
    open(s);
    type("https://elsewhere.test/x");
    fireEvent.click(screen.getByRole("button", { name: "Check" }));
    expect(screen.getByRole("alert").textContent).toBe(
      en.resourceLink.dialog.invalid,
    );
    expect(confirm().disabled).toBe(true);
  });

  it("says a resource the manager cannot view cannot be linked, and keeps Link off", async () => {
    const s = setup("owner");
    s.store.getPage(PAGE)!.trashedAt = s.store.getClock().isoNow();
    open(s);
    type(`/workspaces/${WORKSPACE}/docs/${PAGE}`);
    fireEvent.click(screen.getByRole("button", { name: "Check" }));
    expect(
      await screen.findByText(en.resourceLink.dialog.previewNoAccess),
    ).toBeTruthy();
    expect(confirm().disabled).toBe(true);
  });

  it("shows a save error", () => {
    const s = setup("owner");
    open(s, { error: "The link could not be saved. Try again." });
    expect(screen.getByRole("alert").textContent).toContain(
      "could not be saved",
    );
  });
});

describe("link hooks and the Discuss button (UC-CHT-H10)", () => {
  it("links, reads the channel with its link, and a stale version is refused", async () => {
    const s = setup("owner");
    const channel = await generalChannel(s.hooks);
    const { result } = renderHook(
      () => s.hooks.useLinkChannel(WORKSPACE, channel.id),
      { wrapper: s.wrapper },
    );

    let answer: ChatChannel | undefined;
    await act(async () => {
      answer = await result.current.mutateAsync({
        kind: "work-management.board",
        resourceId: BOARD,
        expectedVersion: channel.version,
      });
    });
    expect(answer?.link).toMatchObject({
      kind: "work-management.board",
      accessible: true,
    });
    expect(
      (await s.hooks.service.getChannel(WORKSPACE, channel.id)).channel.link
        ?.resourceId,
    ).toBe(BOARD);

    let failure: unknown;
    await act(async () => {
      try {
        await result.current.mutateAsync({
          kind: "documents.page",
          resourceId: PAGE,
          expectedVersion: channel.version,
        });
      } catch (error) {
        failure = error;
      }
    });
    expect(chatFailureOf(failure).kind).toBe("stale");
  });

  it("unlinks with the version it was read at", async () => {
    const s = setup("owner");
    const channel = await generalChannel(s.hooks);
    const linked = await s.hooks.service.linkChannel(WORKSPACE, channel.id, {
      kind: "documents.page",
      resourceId: PAGE,
      expectedVersion: channel.version,
    });
    const { result } = renderHook(
      () => s.hooks.useUnlinkChannel(WORKSPACE, channel.id),
      { wrapper: s.wrapper },
    );
    await act(async () => {
      await result.current.mutateAsync(linked.version);
    });
    expect(
      (await s.hooks.service.getChannel(WORKSPACE, channel.id)).channel.link ??
        null,
    ).toBeNull();
  });

  it("Discuss renders nothing when no channel is linked, opens the only channel directly, and lists several", async () => {
    const s = setup("owner");
    const onOpen = vi.fn();
    const { container, rerender } = render(
      <DiscussButton
        hooks={s.hooks}
        workspaceId={WORKSPACE}
        resource={{ kind: "work-management.board", resourceId: BOARD }}
        onOpenChannel={onOpen}
      />,
      { wrapper: s.wrapper },
    );
    await waitFor(() => expect(s.queryClient.isFetching()).toBe(0));
    expect(container.querySelector("button")).toBeNull();

    const channel = await generalChannel(s.hooks);
    await s.hooks.service.linkChannel(WORKSPACE, channel.id, {
      kind: "work-management.board",
      resourceId: BOARD,
      expectedVersion: channel.version,
    });
    await act(async () => {
      await s.queryClient.invalidateQueries();
    });
    const button = await screen.findByRole("button", {
      name: "Discuss in #general",
    });
    fireEvent.click(button);
    expect(onOpen).toHaveBeenCalledWith(channel.id);

    // A second channel on the same Board turns the button into a menu.
    const other = (await s.hooks.service.listChannels(WORKSPACE)).find(
      (c) => c.name === "engineering",
    )!;
    await s.hooks.service.linkChannel(WORKSPACE, other.id, {
      kind: "work-management.board",
      resourceId: BOARD,
      expectedVersion: other.version,
    });
    await act(async () => {
      await s.queryClient.invalidateQueries();
    });
    rerender(
      <DiscussButton
        hooks={s.hooks}
        workspaceId={WORKSPACE}
        resource={{ kind: "work-management.board", resourceId: BOARD }}
        onOpenChannel={onOpen}
      />,
    );
    const menu = await screen.findByRole("button", { name: "Discuss" });
    fireEvent.keyDown(menu, { key: "Enter" });
    expect(
      await screen.findByRole("menuitem", { name: "Open #engineering" }),
    ).toBeTruthy();
    expect(
      screen.getByRole("menuitem", { name: "Open #general" }),
    ).toBeTruthy();
  });

  it("Discuss shows nothing to a person who cannot view the Board", async () => {
    const s = setup("owner");
    const channel = await generalChannel(s.hooks);
    await s.hooks.service.linkChannel(WORKSPACE, channel.id, {
      kind: "work-management.board",
      resourceId: BOARD,
      expectedVersion: channel.version,
    });
    s.store.getBoard(BOARD)!.visibility = "private";
    (
      s.store as unknown as { config: { persona: MockPersona } }
    ).config.persona = "member";
    const { container } = render(
      <DiscussButton
        hooks={s.hooks}
        workspaceId={WORKSPACE}
        resource={{ kind: "work-management.board", resourceId: BOARD }}
        onOpenChannel={() => undefined}
      />,
      { wrapper: s.wrapper },
    );
    await waitFor(() => expect(s.queryClient.isFetching()).toBe(0));
    expect(container.querySelector("button")).toBeNull();
  });
});
