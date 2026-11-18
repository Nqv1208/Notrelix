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
import { ChatWorkspace } from "../web/components/chat-workspace";
import { createChatHooks } from "../web/query/create-chat-hooks";

const WORKSPACE = mockIds.workspaces.primary;

beforeAll(() => {
  Element.prototype.scrollIntoView ??= () => undefined;
});

async function setup(isPrivate: boolean, addOther = true) {
  const store = new MockStore({ ...defaultConfig, persona: "owner" });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: (input, init) => createMockFetch(store)(input, init),
  });
  const deps = { api: client.api, endpoints: client.endpoints };
  const service = createChatHooks(deps).service;
  const viewerId = store.getCurrentUser().id;
  const other = store
    .getWorkspaceMembers(WORKSPACE)
    .find((member) => member.userId !== viewerId)!;
  const name = isPrivate ? "secret-room" : "open-room";
  const created = await service.createChannel(
    WORKSPACE,
    { name, topic: null, isPrivate },
    crypto.randomUUID(),
  );
  if (addOther) await service.addMembers(WORKSPACE, created.id, [other.userId]);
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
  const onOpenChannel = vi.fn();
  const onCloseConversation = vi.fn();
  render(
    <ChatWorkspace
      deps={deps}
      workspaceId={WORKSPACE}
      viewerId={viewerId}
      viewerRole="owner"
      channelId={created.id}
      onOpenChannel={onOpenChannel}
      onCloseConversation={onCloseConversation}
      people={new Map([[other.userId, "Other Person"]])}
    />,
    { wrapper },
  );
  await screen.findByRole("combobox", { name: `Message #${name}` });
  return {
    service,
    created,
    other,
    viewerId,
    name,
    onOpenChannel,
    onCloseConversation,
  };
}

const openSettings = async () =>
  fireEvent.keyDown(await screen.findByRole("button", { name: "More" }), {
    key: "Enter",
  });

describe("ChatWorkspace membership and deletion (UC-CHT-A07, UC-CHT-A08)", () => {
  it("a manager removes another member from the member list and the person is gone from the channel", async () => {
    const s = await setup(true);
    fireEvent.click(
      await screen.findByRole("button", { name: "Channel info" }),
    );
    fireEvent.click(
      await screen.findByRole("button", { name: "Remove Other Person" }),
    );
    fireEvent.click(
      within(await screen.findByRole("alertdialog")).getByRole("button", {
        name: "Remove",
      }),
    );
    await waitFor(async () =>
      expect(
        (await s.service.getChannel(WORKSPACE, s.created.id)).members.map(
          (member) => member.userId,
        ),
      ).not.toContain(s.other.userId),
    );
  });

  it("the last manager cannot leave and is told why; the channel stays", async () => {
    const s = await setup(true);
    await openSettings();
    fireEvent.click(
      await screen.findByRole("menuitem", { name: "Leave channel" }),
    );
    fireEvent.click(
      within(await screen.findByRole("alertdialog")).getByRole("button", {
        name: "Leave",
      }),
    );
    await screen.findByText(/last manager of this channel/);
    expect(s.onOpenChannel).not.toHaveBeenCalled();
    expect(
      (await s.service.listChannels(WORKSPACE)).some(
        (channel) => channel.id === s.created.id,
      ),
    ).toBe(true);
  });

  it("deleting needs the typed name, then the channel is gone and the view moves on", async () => {
    const s = await setup(false);
    await openSettings();
    fireEvent.click(
      await screen.findByRole("menuitem", { name: "Delete channel" }),
    );
    const dialog = await screen.findByRole("dialog");
    expect(within(dialog).getByText(/kept for 30 days/)).toBeTruthy();
    const confirm = within(dialog).getByRole("button", {
      name: "Delete channel",
    }) as HTMLButtonElement;
    expect(confirm.disabled).toBe(true);
    fireEvent.change(within(dialog).getByLabelText("Channel name"), {
      target: { value: "wrong-name" },
    });
    expect(confirm.disabled).toBe(true);
    fireEvent.change(within(dialog).getByLabelText("Channel name"), {
      target: { value: s.name },
    });
    expect(confirm.disabled).toBe(false);
    fireEvent.click(confirm);
    await waitFor(async () =>
      expect(
        (await s.service.listChannels(WORKSPACE)).some(
          (channel) => channel.id === s.created.id,
        ),
      ).toBe(false),
    );
    await waitFor(() =>
      expect(
        s.onOpenChannel.mock.calls.length +
          s.onCloseConversation.mock.calls.length,
      ).toBeGreaterThan(0),
    );
  });

  it("a manager invites a Workspace member from the member list and the person joins the private channel (UC-CHT-A06)", async () => {
    const s = await setup(true, false);
    fireEvent.click(
      await screen.findByRole("button", { name: "Channel info" }),
    );
    fireEvent.click(
      await screen.findByRole("button", { name: "Invite people" }),
    );
    const dialog = await screen.findByRole("dialog");
    expect(
      within(dialog).getByText("Invite people to #secret-room"),
    ).toBeTruthy();
    const submit = within(dialog).getByRole("button", {
      name: "Invite",
    }) as HTMLButtonElement;
    expect(submit.disabled).toBe(true);
    fireEvent.click(within(dialog).getByRole("checkbox"));
    fireEvent.click(
      within(dialog).getByRole("button", { name: "Invite 1 person" }),
    );
    await waitFor(async () =>
      expect(
        (await s.service.getChannel(WORKSPACE, s.created.id)).members.map(
          (member) => member.userId,
        ),
      ).toContain(s.other.userId),
    );
    await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
  });

  it("the invite dialog says when everyone is already in the channel", async () => {
    await setup(true, true);
    fireEvent.click(
      await screen.findByRole("button", { name: "Channel info" }),
    );
    fireEvent.click(
      await screen.findByRole("button", { name: "Invite people" }),
    );
    await screen.findByText("Everyone is already in this channel");
    await screen.findByText(
      "No one is left to invite. Add new people to the Workspace in member settings.",
    );
  });

  it("a manager names a successor from the member list, then the last manager may leave (UC-CHT-A07)", async () => {
    const s = await setup(true);
    fireEvent.click(
      await screen.findByRole("button", { name: "Channel info" }),
    );
    fireEvent.click(
      await screen.findByRole("button", {
        name: "Make Other Person a manager",
      }),
    );
    await waitFor(async () =>
      expect(
        (await s.service.getChannel(WORKSPACE, s.created.id)).members.find(
          (member) => member.userId === s.other.userId,
        )?.role,
      ).toBe("manager"),
    );
    await screen.findByRole("button", { name: "Make Other Person a member" });
    await s.service.setMemberRole(
      WORKSPACE,
      s.created.id,
      s.other.userId,
      "member",
    );
    await expect(
      s.service.setMemberRole(WORKSPACE, s.created.id, s.viewerId, "member"),
    ).rejects.toBeTruthy();
  });
});
