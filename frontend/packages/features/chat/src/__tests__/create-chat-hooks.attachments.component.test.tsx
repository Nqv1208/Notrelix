import type { ReactNode } from "react";
import { act, renderHook, waitFor } from "@notrelix/testing";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { describe, expect, it } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import {
  MockStore,
  createMockFetch,
  defaultConfig,
  mockIds,
} from "@notrelix/dev-mock-backend";
import { createChatService } from "../core/api/chat.service";
import { createChatHooks, flattenPages } from "../web/query/create-chat-hooks";

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
  return { hooks, service, viewer, wrapper, store };
}

async function generalId(service: ReturnType<typeof setup>["service"]) {
  const channels = await service.listChannels(WORKSPACE);
  const found = channels.find((channel) => channel.name === "general");
  if (!found) throw new Error("no general channel");
  return found.id;
}

function file(name: string, body = "hello", type = "text/plain") {
  return new File([body], name, { type });
}

function mount(s: ReturnType<typeof setup>, general: string) {
  return renderHook(
    () => ({
      list: s.hooks.useMessages(WORKSPACE, general, { kind: "latest" }),
      send: s.hooks.useSendMessage(WORKSPACE, general, s.viewer),
      uploads: s.hooks.useUploads(WORKSPACE),
      actions: s.hooks.useAttachmentActions(WORKSPACE),
    }),
    { wrapper: s.wrapper },
  );
}

function sentMessage(
  messages: ReturnType<typeof flattenPages>,
  content: string,
) {
  return messages.find((message) => message.content === content);
}

describe("sending a message with files", () => {
  it("stores the message first, then uploads each file to it and shows them on the message", async () => {
    const s = setup();
    const general = await generalId(s.service);
    const { result } = mount(s, general);
    await waitFor(() => expect(result.current.list.isSuccess).toBe(true));

    act(() => {
      result.current.send.send("with two files", null, undefined, [
        file("a.txt"),
        file("b.txt", "second"),
      ]);
    });
    await waitFor(() => {
      const message = sentMessage(
        flattenPages(result.current.list.data),
        "with two files",
      );
      expect(message?.attachments?.map((item) => item.fileName)).toEqual([
        "a.txt",
        "b.txt",
      ]);
    });
    expect(result.current.uploads.data).toEqual([]);
    const message = sentMessage(
      flattenPages(result.current.list.data),
      "with two files",
    )!;
    // The server holds the same files.
    const stored = await s.service.listAttachments(WORKSPACE, message.id);
    expect(stored).toHaveLength(2);
    expect(stored[0]!.downloadPath).toContain(
      `/messages/${message.id}/attachments/${stored[0]!.id}/content`,
    );
  });

  it("keeps the message when a file is refused, shows the reason for that file and lets the author dismiss it", async () => {
    const s = setup();
    const general = await generalId(s.service);
    const { result } = mount(s, general);
    await waitFor(() => expect(result.current.list.isSuccess).toBe(true));

    act(() => {
      result.current.send.send("one bad file", null, undefined, [
        file("run.exe", "MZ", "application/x-msdownload"),
        file("ok.txt"),
      ]);
    });
    await waitFor(() => {
      const message = sentMessage(
        flattenPages(result.current.list.data),
        "one bad file",
      );
      expect(message?.attachments?.map((item) => item.fileName)).toEqual([
        "ok.txt",
      ]);
    });
    expect(
      sentMessage(flattenPages(result.current.list.data), "one bad file"),
    ).toBeDefined();
    expect(result.current.uploads.data).toHaveLength(1);
    expect(result.current.uploads.data[0]).toMatchObject({
      status: "failed",
      reason: "blocked",
    });
    act(() =>
      result.current.actions.dismiss(result.current.uploads.data[0]!.id),
    );
    await waitFor(() => expect(result.current.uploads.data).toEqual([]));
  });

  it("answers the 11th file with the limit reason (409) after the 10 stored ones", async () => {
    const s = setup();
    // Eleven uploads in one minute would meet the upload rate limit (10 a minute) first; this test is about the per-message cap.
    s.store.chat.limitOverrides.set("upload", {
      person: 100,
      space: 100,
      windowSeconds: 60,
    });
    const general = await generalId(s.service);
    const { result } = mount(s, general);
    await waitFor(() => expect(result.current.list.isSuccess).toBe(true));

    act(() => {
      result.current.send.send(
        "eleven files",
        null,
        undefined,
        Array.from({ length: 11 }, (_, index) => file(`f${index}.txt`)),
      );
    });
    await waitFor(() => expect(result.current.uploads.data).toHaveLength(1));
    const message = sentMessage(
      flattenPages(result.current.list.data),
      "eleven files",
    )!;
    expect(message.attachments).toHaveLength(10);
    expect(result.current.uploads.data[0]).toMatchObject({
      status: "failed",
      reason: "limit",
    });
  });
});

describe("attachment actions", () => {
  it("retries a failed upload with the same file once the connection is back", async () => {
    const store = new MockStore({ ...defaultConfig });
    let down = false;
    const client = createNotrelixClient({
      baseUrl: "http://localhost:8000/api/v1",
      fetchImpl: (input, init) => {
        const url = new URL(
          String(input instanceof Request ? input.url : input),
        );
        if (down && url.pathname.endsWith("/attachments")) {
          return Promise.reject(new TypeError("offline"));
        }
        return createMockFetch(store)(input, init);
      },
    });
    const deps = { api: client.api, endpoints: client.endpoints };
    const hooks = createChatHooks(deps);
    const service = createChatService(deps.api, deps.endpoints);
    const viewer = store.getCurrentUser().id;
    const queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
        mutations: { retry: false },
      },
    });
    const wrapper = ({ children }: { children: ReactNode }) => (
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    );
    const general = await generalId(service);
    const { result } = renderHook(
      () => ({
        list: hooks.useMessages(WORKSPACE, general, { kind: "latest" }),
        send: hooks.useSendMessage(WORKSPACE, general, viewer),
        uploads: hooks.useUploads(WORKSPACE),
        actions: hooks.useAttachmentActions(WORKSPACE),
      }),
      { wrapper },
    );
    await waitFor(() => expect(result.current.list.isSuccess).toBe(true));

    down = true;
    act(() => {
      result.current.send.send("flaky", null, undefined, [file("c.txt")]);
    });
    await waitFor(() =>
      expect(result.current.uploads.data[0]?.status).toBe("failed"),
    );
    expect(result.current.uploads.data[0]!.reason).toBe("retryable");
    expect(
      sentMessage(flattenPages(result.current.list.data), "flaky"),
    ).toBeDefined();

    down = false;
    act(() => result.current.actions.retry(result.current.uploads.data[0]!.id));
    await waitFor(() =>
      expect(
        sentMessage(flattenPages(result.current.list.data), "flaky")
          ?.attachments?.[0]?.fileName,
      ).toBe("c.txt"),
    );
    expect(result.current.uploads.data).toEqual([]);
  });

  it("removes a stored file from the message and the server, and reads the bytes back with the session", async () => {
    const s = setup();
    const general = await generalId(s.service);
    const { result } = mount(s, general);
    await waitFor(() => expect(result.current.list.isSuccess).toBe(true));
    act(() => {
      result.current.send.send("remove me", null, undefined, [
        file("gone.txt", "bytes"),
      ]);
    });
    await waitFor(() =>
      expect(
        sentMessage(flattenPages(result.current.list.data), "remove me")
          ?.attachments,
      ).toHaveLength(1),
    );
    const message = sentMessage(
      flattenPages(result.current.list.data),
      "remove me",
    )!;
    const attachment = message.attachments![0]!;

    const blob = await s.service.getAttachmentContent(
      WORKSPACE,
      message.id,
      attachment.id,
    );
    expect(await blob.text()).toBe("bytes");

    await act(async () => {
      await result.current.actions.remove.mutateAsync({
        messageId: message.id,
        attachmentId: attachment.id,
      });
    });
    await waitFor(() =>
      expect(
        sentMessage(flattenPages(result.current.list.data), "remove me")
          ?.attachments,
      ).toEqual([]),
    );
    expect(await s.service.listAttachments(WORKSPACE, message.id)).toEqual([]);
  });
});
