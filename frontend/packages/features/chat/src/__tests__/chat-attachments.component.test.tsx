import type { ReactNode } from "react";
import { afterEach, beforeAll, describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor, within } from "@notrelix/testing";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { createNotrelixClient } from "@notrelix/contracts";
import {
  MockStore,
  createMockFetch,
  defaultConfig,
  mockIds,
} from "@notrelix/dev-mock-backend";
import { createChatService } from "../core/api/chat.service";
import type { ChatUploadItem } from "../core/model/attachment-model";
import type { ChatMessage } from "../core/types/chat";
import { chatQueryKeys } from "../query/keys";
import { createChatHooks, flattenPages } from "../web/query/create-chat-hooks";
import { ChatMessageRow } from "../web/components/chat-message-row";
import { en } from "../web/components/chat.copy";
import { installBlobReaders } from "./blob-shim";

installBlobReaders();

const WORKSPACE = mockIds.workspaces.primary;

function setup() {
  const store = new MockStore({ ...defaultConfig });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: (input, init) => createMockFetch(store)(input, init),
  });
  const deps = { api: client.api, endpoints: client.endpoints };
  const hooks = createChatHooks(deps);
  const service = createChatService(deps.api, deps.endpoints);
  const viewer = store.getCurrentUser().id;
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
  return { hooks, service, viewer, queryClient, wrapper };
}

const PNG = new Uint8Array([
  0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, 0, 0, 0, 0x0d, 0x49, 0x48,
  0x44, 0x52,
]);

/** A PNG whose IHDR carries the given pixel size, so the server reads it at upload (UC-CHT-I01). */
function pngOf(width: number, height: number): Uint8Array<ArrayBuffer> {
  const bytes = new Uint8Array(new ArrayBuffer(24));
  bytes.set(PNG.subarray(0, 16), 0);
  const view = new DataView(bytes.buffer);
  view.setUint32(16, width);
  view.setUint32(20, height);
  return bytes;
}

/** A message of the caller with the given files stored, written through the service. */
async function messageWithFiles(
  s: ReturnType<typeof setup>,
  files: File[],
  content = "files here",
) {
  const channels = await s.service.listChannels(WORKSPACE);
  const general = channels.find((channel) => channel.name === "general")!;
  const sent = await s.service.sendMessage(WORKSPACE, general.id, {
    content,
    clientMessageId: crypto.randomUUID(),
  });
  for (const file of files) {
    await s.service.uploadAttachment(WORKSPACE, sent.id, file, {
      idempotencyKey: crypto.randomUUID(),
    });
  }
  const page = await s.service.listMessages(WORKSPACE, general.id, {}, 50);
  return page.items.find((item) => item.id === sent.id)!;
}

/** Renders the row from the message cache, so a removal that patches the cache shows. */
function Row({
  s,
  messageId,
  spaceId,
  isOwn,
  readOnly,
}: {
  s: ReturnType<typeof setup>;
  messageId: string;
  spaceId: string;
  isOwn: boolean;
  readOnly?: boolean;
}) {
  const list = s.hooks.useMessages(WORKSPACE, spaceId, { kind: "latest" });
  const message = flattenPages(list.data).find((item) => item.id === messageId);
  if (!message) return null;
  return (
    <ChatMessageRow
      message={message}
      author={{ name: "Ada" }}
      continued={false}
      isOwn={isOwn}
      locale="en"
      timeZone="UTC"
      copy={en}
      workspaceId={WORKSPACE}
      hooks={s.hooks}
      authorName={() => "Ada"}
      attachmentsReadOnly={readOnly}
    />
  );
}

let createObjectURL: ReturnType<typeof vi.fn>;
let anchorClick: ReturnType<typeof vi.spyOn>;

beforeAll(() => {
  createObjectURL = vi.fn(() => "blob:preview");
  Object.defineProperty(URL, "createObjectURL", {
    configurable: true,
    value: createObjectURL,
  });
  Object.defineProperty(URL, "revokeObjectURL", {
    configurable: true,
    value: vi.fn(),
  });
});

afterEach(() => {
  anchorClick?.mockRestore();
  createObjectURL.mockClear();
});

describe("message attachments", () => {
  it("shows a card with the name and size, and downloads through the session", async () => {
    const s = setup();
    const message = await messageWithFiles(s, [
      new File(["1234567890"], "report.pdf", { type: "application/pdf" }),
    ]);
    anchorClick = vi
      .spyOn(HTMLAnchorElement.prototype, "click")
      .mockImplementation(() => undefined);
    render(
      <Row s={s} messageId={message.id} spaceId={message.spaceId} isOwn />,
      { wrapper: s.wrapper },
    );
    const group = await screen.findByRole("group", { name: "Files" });
    expect(within(group).getByText("report.pdf")).not.toBeNull();
    expect(within(group).getByText("10 B")).not.toBeNull();

    fireEvent.click(
      within(group).getAllByRole("button", { name: "Download report.pdf" })[0]!,
    );
    await waitFor(() => expect(anchorClick).toHaveBeenCalled());
    const blob = createObjectURL.mock.calls[0]![0] as Blob;
    expect(await blob.text()).toBe("1234567890");
  });

  it("previews an image inline from bytes read with the session", async () => {
    const s = setup();
    const message = await messageWithFiles(s, [
      new File([PNG], "pic.png", { type: "image/png" }),
    ]);
    render(
      <Row s={s} messageId={message.id} spaceId={message.spaceId} isOwn />,
      { wrapper: s.wrapper },
    );
    const image = await screen.findByRole("img", { name: "pic.png" });
    expect(image.getAttribute("src")).toBe("blob:preview");
  });

  it("shows several images as one grid with a +N tile, and one image alone without a grid", async () => {
    const s = setup();
    const five = await messageWithFiles(
      s,
      ["a", "b", "c", "d", "e"].map(
        (name) => new File([PNG], `${name}.png`, { type: "image/png" }),
      ),
    );
    render(
      <Row s={s} messageId={five.id} spaceId={five.spaceId} isOwn={false} />,
      { wrapper: s.wrapper },
    );
    const grid = await screen.findByTestId("chat-gallery");
    expect(grid.getAttribute("data-count")).toBe("5");
    // Four tiles, the last one says one more.
    expect(grid.querySelectorAll("li")).toHaveLength(4);
    expect(
      within(grid).getByRole("button", {
        name: "1 more images, open the gallery",
      }),
    ).not.toBeNull();
    expect(within(grid).getByText("+1")).not.toBeNull();
  });

  it("reserves each gallery tile at its image's own ratio, clamped to 3:4 to 2:1", async () => {
    const s = setup();
    const message = await messageWithFiles(s, [
      new File([pngOf(640, 480)], "photo.png", { type: "image/png" }),
      new File([pngOf(4000, 1000)], "panorama.png", { type: "image/png" }),
      new File([pngOf(300, 1000)], "tall.png", { type: "image/png" }),
    ]);
    render(
      <Row s={s} messageId={message.id} spaceId={message.spaceId} isOwn />,
      { wrapper: s.wrapper },
    );
    const grid = await screen.findByTestId("chat-gallery");
    const tiles = within(grid).getAllByRole("listitem");

    expect(tiles.map((tile) => tile.style.aspectRatio)).toEqual([
      String(640 / 480),
      "2",
      "0.75",
    ]);
  });

  it("opens an image in the viewer, pages with the arrow keys and the buttons, and closes on Escape", async () => {
    const s = setup();
    const message = await messageWithFiles(
      s,
      ["one", "two", "three"].map(
        (name) => new File([PNG], `${name}.png`, { type: "image/png" }),
      ),
    );
    render(
      <Row s={s} messageId={message.id} spaceId={message.spaceId} isOwn />,
      { wrapper: s.wrapper },
    );
    const grid = await screen.findByTestId("chat-gallery");
    expect(grid.querySelectorAll("li")).toHaveLength(3);
    fireEvent.click(
      within(grid).getAllByRole("button", { name: /^Open .*\.png$/ })[1]!,
    );
    const viewer = await screen.findByTestId("chat-image-viewer");
    expect(within(viewer).getByText("2 of 3")).not.toBeNull();
    const previous = within(viewer).getByRole("button", {
      name: "Previous image",
    });
    const next = within(viewer).getByRole("button", { name: "Next image" });
    fireEvent.keyDown(viewer, { key: "ArrowRight" });
    await waitFor(() =>
      expect(within(viewer).getByText("3 of 3")).not.toBeNull(),
    );
    // At the end there is no next; at the start there is no previous.
    expect((next as HTMLButtonElement).disabled).toBe(true);
    fireEvent.click(previous);
    await waitFor(() =>
      expect(within(viewer).getByText("2 of 3")).not.toBeNull(),
    );
    fireEvent.keyDown(viewer, { key: "ArrowLeft" });
    await waitFor(() =>
      expect(within(viewer).getByText("1 of 3")).not.toBeNull(),
    );
    expect((previous as HTMLButtonElement).disabled).toBe(true);
    fireEvent.keyDown(viewer, { key: "Escape" });
    await waitFor(() =>
      expect(screen.queryByTestId("chat-image-viewer")).toBeNull(),
    );
  });

  it("offers remove only to the author, asks first, then removes the file", async () => {
    const s = setup();
    const message = await messageWithFiles(s, [
      new File(["x"], "mine.txt", { type: "text/plain" }),
    ]);
    const other = render(
      <Row
        s={s}
        messageId={message.id}
        spaceId={message.spaceId}
        isOwn={false}
      />,
      { wrapper: s.wrapper },
    );
    await screen.findByText("mine.txt");
    expect(
      screen.queryByRole("button", { name: /Remove mine.txt/ }),
    ).toBeNull();
    other.unmount();

    render(
      <Row s={s} messageId={message.id} spaceId={message.spaceId} isOwn />,
      { wrapper: s.wrapper },
    );
    fireEvent.click(
      await screen.findByRole("button", {
        name: "Remove mine.txt from the message",
      }),
    );
    const dialog = await screen.findByRole("alertdialog");
    expect(within(dialog).getByText("Remove this file?")).not.toBeNull();
    // Nothing is removed until the author confirms.
    expect(
      (await s.service.listAttachments(WORKSPACE, message.id)).length,
    ).toBe(1);
    fireEvent.click(
      within(dialog).getByRole("button", { name: "Remove file" }),
    );
    await waitFor(() => expect(screen.queryByText("mine.txt")).toBeNull());
    expect(await s.service.listAttachments(WORKSPACE, message.id)).toEqual([]);
    expect(screen.getByText("mine.txt was removed.")).not.toBeNull();
  });

  it("offers no remove in a read-only (archived) channel", async () => {
    const s = setup();
    const message = await messageWithFiles(s, [
      new File(["x"], "locked.txt", { type: "text/plain" }),
    ]);
    render(
      <Row
        s={s}
        messageId={message.id}
        spaceId={message.spaceId}
        isOwn
        readOnly
      />,
      { wrapper: s.wrapper },
    );
    await screen.findByText("locked.txt");
    expect(screen.queryByRole("button", { name: /Remove/ })).toBeNull();
    expect(
      screen.getAllByRole("button", { name: "Download locked.txt" }).length,
    ).toBeGreaterThan(0);
  });

  it("shows no files on a deleted message", () => {
    const s = setup();
    const tombstone: ChatMessage = {
      id: "m-del",
      spaceId: "s",
      sequence: 1,
      kind: "user",
      authorId: s.viewer,
      content: null,
      clientMessageId: null,
      replyToId: null,
      edited: false,
      deleted: true,
      createdAt: "2026-11-10T09:00:00.000Z",
      editedAt: null,
      deletedAt: "2026-11-10T10:00:00.000Z",
      version: 2,
      attachments: [
        {
          id: "a",
          fileName: "stale.txt",
          sizeBytes: 1,
          contentType: "text/plain",
          uploadedBy: s.viewer,
          createdAt: "2026-11-10T09:00:00.000Z",
          downloadPath: "/x",
        },
      ],
    };
    render(
      <ChatMessageRow
        message={tombstone}
        author={{ name: "Ada" }}
        continued={false}
        isOwn
        locale="en"
        timeZone="UTC"
        copy={en}
        workspaceId={WORKSPACE}
        hooks={s.hooks}
        authorName={() => "Ada"}
      />,
      { wrapper: s.wrapper },
    );
    expect(screen.getByText(en.list.deleted)).not.toBeNull();
    expect(screen.queryByText("stale.txt")).toBeNull();
    expect(screen.queryByTestId("chat-attachments")).toBeNull();
  });
});

describe("uploads on a message", () => {
  function upload(
    messageId: string,
    overrides: Partial<ChatUploadItem>,
  ): ChatUploadItem {
    return {
      id: crypto.randomUUID(),
      messageId,
      spaceId: "s",
      file: new File(["abc"], "slow.zip", { type: "application/zip" }),
      status: "uploading",
      progress: 0.4,
      reason: null,
      idempotencyKey: "key-aaaa-bbbb",
      ...overrides,
    };
  }

  async function mountWithUploads(
    items: (messageId: string) => ChatUploadItem[],
    isOwn = true,
  ) {
    const s = setup();
    const message = await messageWithFiles(s, []);
    s.queryClient.setQueryData(
      chatQueryKeys.uploads(WORKSPACE),
      items(message.id),
    );
    // The message needs a file or an upload to render the group: the uploads are the group here.
    render(
      <Row
        s={s}
        messageId={message.id}
        spaceId={message.spaceId}
        isOwn={isOwn}
      />,
      { wrapper: s.wrapper },
    );
    return { s, message };
  }

  it("shows progress with the progressbar semantics", async () => {
    await mountWithUploads((id) => [upload(id, {})]);
    const bar = await screen.findByRole("progressbar", {
      name: "Uploading slow.zip",
    });
    expect(bar.getAttribute("aria-valuenow")).toBe("40");
    expect(bar.getAttribute("aria-valuemin")).toBe("0");
    expect(bar.getAttribute("aria-valuemax")).toBe("100");
  });

  it("shows a queued file as waiting with an indeterminate bar", async () => {
    await mountWithUploads((id) => [
      upload(id, { status: "queued", progress: 0 }),
    ]);
    const bar = await screen.findByRole("progressbar", {
      name: "Waiting to upload slow.zip",
    });
    expect(bar.getAttribute("aria-valuenow")).toBeNull();
  });

  it("shows a failed file with the server's reason and Retry only when a retry can work", async () => {
    const { s } = await mountWithUploads((id) => [
      upload(id, { status: "failed", progress: 0, reason: "retryable" }),
      upload(id, {
        status: "failed",
        progress: 0,
        reason: "blocked",
        file: new File(["MZ"], "run.exe"),
      }),
    ]);
    const alerts = await screen.findAllByTestId("chat-upload-failed");
    expect(alerts).toHaveLength(2);
    expect(alerts[0]!.textContent).toContain(
      "slow.zip was not uploaded. Check the connection and try again.",
    );
    expect(
      within(alerts[0]!).getByRole("button", {
        name: "Retry uploading slow.zip",
      }),
    ).not.toBeNull();
    expect(alerts[1]!.textContent).toContain(
      "run.exe was not uploaded: this type of file cannot be attached.",
    );
    expect(
      within(alerts[1]!).queryByRole("button", { name: /Retry/ }),
    ).toBeNull();

    fireEvent.click(
      within(alerts[1]!).getByRole("button", { name: "Dismiss run.exe" }),
    );
    await waitFor(() =>
      expect(screen.getAllByTestId("chat-upload-failed")).toHaveLength(1),
    );
    expect(
      s.queryClient.getQueryData<ChatUploadItem[]>(
        chatQueryKeys.uploads(WORKSPACE),
      ),
    ).toHaveLength(1);
  });

  it("does not show another person's uploads", async () => {
    await mountWithUploads((id) => [upload(id, {})], false);
    expect(screen.queryByRole("progressbar")).toBeNull();
  });
});
