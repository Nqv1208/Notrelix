import type { ReactNode } from "react";
import { beforeAll, describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, within } from "@notrelix/testing";
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
import { installBlobReaders } from "./blob-shim";

installBlobReaders();

const WORKSPACE = mockIds.workspaces.primary;

beforeAll(() => {
  Element.prototype.scrollIntoView ??= () => undefined;
});

function setup(failUploads = false) {
  const store = new MockStore({ ...defaultConfig });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: (input, init) => {
      const url = new URL(String(input instanceof Request ? input.url : input));
      if (
        failUploads &&
        url.pathname.endsWith("/attachments") &&
        init?.method === "POST"
      ) {
        return Promise.reject(new TypeError("offline"));
      }
      return createMockFetch(store)(input, init);
    },
  });
  const viewerId = store.getCurrentUser().id;
  const members = store.getWorkspaceMembers(WORKSPACE);
  const people = new Map(
    members.map((member) => [member.userId, `Person ${member.userId}`]),
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
  return { deps, viewerId, people, wrapper };
}

async function openGeneral(s: ReturnType<typeof setup>) {
  const channels = await createChatHooks(s.deps).service.listChannels(
    WORKSPACE,
  );
  const general = channels.find((channel) => channel.name === "general")!;
  render(
    <ChatWorkspace
      deps={s.deps}
      workspaceId={WORKSPACE}
      viewerId={s.viewerId}
      viewerRole="owner"
      channelId={general.id}
      conversationId={null}
      onOpenConversation={vi.fn()}
      onOpenChannel={vi.fn()}
      people={s.people}
    />,
    { wrapper: s.wrapper },
  );
  return screen.findByRole("combobox", { name: /Message #general/ });
}

function pick(files: File[]) {
  fireEvent.change(screen.getByTestId("chat-file-input"), {
    target: { files },
  });
}

describe("ChatWorkspace attach flow", () => {
  it("sends the message first, then uploads the picked file and shows it on the message", async () => {
    const s = setup();
    const box = await openGeneral(s);
    pick([new File(["hello"], "notes.txt", { type: "text/plain" })]);
    expect(screen.getByText("notes.txt")).toBeTruthy();
    fireEvent.change(box, { target: { value: "with a file" } });
    fireEvent.keyDown(box, { key: "Enter" });

    const log = await screen.findByText("with a file");
    const row = log.closest("[data-message-id]") as HTMLElement;
    // The file card replaces the upload row once the server stored the file.
    expect(await within(row).findByText("notes.txt")).toBeTruthy();
    expect(within(row).queryByTestId("chat-upload-progress")).toBeNull();
    expect(screen.queryByTestId("chat-pending-files")).toBeNull();
  });

  it("keeps the message when the upload fails and retries only that file", async () => {
    const s = setup(true);
    const box = await openGeneral(s);
    pick([new File(["hello"], "later.txt", { type: "text/plain" })]);
    fireEvent.change(box, { target: { value: "upload will fail" } });
    fireEvent.keyDown(box, { key: "Enter" });

    const log = await screen.findByText("upload will fail");
    const row = log.closest("[data-message-id]") as HTMLElement;
    const failed = await within(row).findByTestId("chat-upload-failed");
    expect(failed.textContent).toContain("later.txt was not uploaded");
    expect(
      within(failed).getByRole("button", { name: "Retry uploading later.txt" }),
    ).toBeTruthy();
    // The message is stored and still shown.
    expect(screen.getByText("upload will fail")).toBeTruthy();
  });
});
