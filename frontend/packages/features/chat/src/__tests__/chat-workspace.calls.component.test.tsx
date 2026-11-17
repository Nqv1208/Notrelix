import type { ReactNode } from "react";
import { fireEvent, render, screen, waitFor, within } from "@notrelix/testing";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { describe, expect, it, vi } from "vitest";
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
import { createChatService } from "../core/api/chat.service";
import { en } from "../web/components/chat.copy";

const WS = mockIds.workspaces.primary;

async function open(role: "owner" | "guest" = "owner") {
  const store = new MockStore({ ...defaultConfig });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: (input, init) => createMockFetch(store)(input, init),
  });
  const deps = { api: client.api, endpoints: client.endpoints };
  const viewerId = store.getCurrentUser().id;
  const members = store.getWorkspaceMembers(WS);
  const people = new Map(members.map((m) => [m.userId, `Person ${m.userId}`]));
  const other = members.map((m) => m.userId).find((id) => id !== viewerId)!;
  const dm = await createChatService(
    deps.api,
    deps.endpoints,
  ).startConversation(WS, { kind: "direct", userId: other }, "calls-ws-key");
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
  render(
    <ChatWorkspace
      deps={deps}
      workspaceId={WS}
      viewerId={viewerId}
      viewerRole={role}
      channelId={null}
      conversationId={dm.conversation.id}
      onOpenConversation={vi.fn()}
      onOpenChannel={vi.fn()}
      people={people}
    />,
    { wrapper },
  );
  await screen.findByRole("heading", { level: 1 });
  return { store, name: people.get(other)! };
}

describe("ChatWorkspace: the two places a call starts", () => {
  it("starts a voice call from the header button and rings out with Cancel", async () => {
    const { store, name } = await open();
    const header = document.querySelector("header") as HTMLElement;
    fireEvent.click(
      await within(header).findByRole("button", { name: en.panel.voiceCall }),
    );
    const card = await screen.findByTestId("chat-call-card");
    expect(card.getAttribute("data-phase")).toBe("outgoing-ringing");
    expect(card.textContent).toContain(`Calling ${name}`);
    expect(Array.from(store.chat.calls.values())[0]).toMatchObject({
      kind: "voice",
      state: "ringing",
    });
    fireEvent.click(screen.getByRole("button", { name: en.calls.cancel }));
    await waitFor(() =>
      expect(screen.getByTestId("chat-call-card").textContent).toContain(
        en.calls.endedCancelled,
      ),
    );
  });

  it("starts a video call from the quick action of the details panel", async () => {
    const { store } = await open();
    const header = document.querySelector("header") as HTMLElement;
    fireEvent.click(
      within(header).getByRole("button", { name: en.dm.view.details }),
    );
    const panel = await screen.findByTestId("chat-side-panel");
    fireEvent.click(
      await within(panel).findByRole("button", { name: en.panel.videoCall }),
    );
    await screen.findByTestId("chat-call-card");
    expect(Array.from(store.chat.calls.values())[0]).toMatchObject({
      kind: "video",
      state: "ringing",
    });
  });

  it("offers no call button to a Guest", async () => {
    await open("guest");
    const header = document.querySelector("header") as HTMLElement;
    expect(
      within(header).queryByRole("button", { name: en.panel.voiceCall }),
    ).toBeNull();
  });
});
