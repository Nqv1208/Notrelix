import { beforeAll, describe, expect, it, vi } from "vitest";
import { act, fireEvent, render, screen, waitFor } from "@notrelix/testing";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { createNotrelixClient } from "@notrelix/contracts";
import { I18nProvider } from "@notrelix/i18n";
import {
  MockStore,
  createMockFetch,
  defaultConfig,
  mockIds,
} from "@notrelix/dev-mock-backend";
import type { ChatEphemeralPort } from "../core/model/ephemeral-model";
import { chatCatalog } from "../web/components/chat.catalog";
import { ChatWorkspace } from "../web/components/chat-workspace";
import { en } from "../web/components/chat.copy";
import { createChatHooks } from "../web/query/create-chat-hooks";

const WORKSPACE = mockIds.workspaces.primary;

function fakePort() {
  const port: ChatEphemeralPort = {
    subscribe: vi.fn(() => () => undefined),
    sendTyping: vi.fn(),
    sendPresence: vi.fn(),
  };
  return port;
}

async function renderChat(workspaceArchived: boolean, port: ChatEphemeralPort) {
  const store = new MockStore({ ...defaultConfig, persona: "owner" });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: (input, init) => createMockFetch(store)(input, init),
  });
  const deps = { api: client.api, endpoints: client.endpoints };
  const hooks = createChatHooks(deps);
  const general = (await hooks.service.listChannels(WORKSPACE)).find(
    (c) => c.name === "general",
  )!;
  render(
    <QueryClientProvider
      client={
        new QueryClient({ defaultOptions: { queries: { retry: false } } })
      }
    >
      <I18nProvider catalogs={[chatCatalog]} initialLocale="en">
        <ChatWorkspace
          deps={deps}
          workspaceId={WORKSPACE}
          viewerId={store.getCurrentUser().id}
          viewerRole="owner"
          channelId={general.id}
          onOpenChannel={vi.fn()}
          people={new Map<string, string>()}
          ephemeral={port}
          workspaceArchived={workspaceArchived}
        />
      </I18nProvider>
    </QueryClientProvider>,
  );
  return screen.findByLabelText(/Message #general/);
}

describe("Chat in an archived workspace", () => {
  beforeAll(() => {
    Element.prototype.scrollIntoView ??= () => undefined;
  });

  it("reads as off, sends no typing and no presence, and still listens", async () => {
    const port = fakePort();
    const box = (await renderChat(true, port)) as HTMLTextAreaElement;

    expect(box.disabled).toBe(true);
    expect(screen.getByText(en.composer.workspaceArchived)).not.toBeNull();
    fireEvent.change(box, { target: { value: "hello" } });
    await act(async () => undefined);
    expect(port.sendTyping).not.toHaveBeenCalled();
    expect(port.sendPresence).not.toHaveBeenCalled();
    expect(port.subscribe).toHaveBeenCalled();
    expect(screen.queryByRole("alert")).toBeNull();
  });

  it("sends presence and typing in a live workspace (the control)", async () => {
    const port = fakePort();
    const box = (await renderChat(false, port)) as HTMLTextAreaElement;

    expect(box.disabled).toBe(false);
    expect(screen.queryByText(en.composer.workspaceArchived)).toBeNull();
    await waitFor(() => expect(port.sendPresence).toHaveBeenCalled());
    fireEvent.change(box, { target: { value: "hello" } });
    await waitFor(() => expect(port.sendTyping).toHaveBeenCalled());
  });
});
