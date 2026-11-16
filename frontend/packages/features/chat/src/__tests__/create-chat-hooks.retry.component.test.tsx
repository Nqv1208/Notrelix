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
import { markVoiceFile } from "../core/model/voice-model";
import { createChatHooks, flattenPages } from "../web/query/create-chat-hooks";

import { installBlobReaders } from "./blob-shim";

installBlobReaders();

const WORKSPACE = mockIds.workspaces.primary;

/** A backend whose message POST fails while `down`, recording the body of every message POST. */
function setup() {
  const store = new MockStore({ ...defaultConfig });
  const state = { down: false, bodies: [] as Record<string, unknown>[] };
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: async (input, init) => {
      const request = input instanceof Request ? input : null;
      const url = new URL(String(request ? request.url : input));
      const method = (request?.method ?? init?.method ?? "GET").toUpperCase();
      if (method === "POST" && url.pathname.endsWith("/messages")) {
        const text = request
          ? await request.clone().text()
          : String(init?.body ?? "{}");
        state.bodies.push(JSON.parse(text) as Record<string, unknown>);
        if (state.down) throw new TypeError("offline");
      }
      return createMockFetch(store)(input, init);
    },
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
  return { hooks, service, viewer, wrapper, state };
}

async function generalId(service: ReturnType<typeof setup>["service"]) {
  const channels = await service.listChannels(WORKSPACE);
  return channels.find((channel) => channel.name === "general")!.id;
}

function mount(s: ReturnType<typeof setup>, general: string) {
  return renderHook(
    () => ({
      list: s.hooks.useMessages(WORKSPACE, general, { kind: "latest" }),
      pending: s.hooks.usePendingSends(WORKSPACE, general),
      send: s.hooks.useSendMessage(WORKSPACE, general, s.viewer),
    }),
    { wrapper: s.wrapper },
  );
}

describe("retrying a failed send", () => {
  it("keeps the quote target and the voice flag of a failed reply or voice message", async () => {
    const s = setup();
    const general = await generalId(s.service);
    const { result } = mount(s, general);
    await waitFor(() => expect(result.current.list.isSuccess).toBe(true));
    const target = flattenPages(result.current.list.data)[0]!;

    s.state.down = true;
    const voice = markVoiceFile(
      new File(["v"], "voice.webm", { type: "audio/webm" }),
      {
        durationMs: 1000,
        waveform: [1],
      },
    );
    act(() => {
      result.current.send.send("a reply", target.id);
      result.current.send.send("", null, undefined, [voice]);
    });
    await waitFor(() =>
      expect(
        result.current.pending.data.every((entry) => entry.status === "failed"),
      ).toBe(true),
    );
    expect(result.current.pending.data).toHaveLength(2);

    s.state.down = false;
    s.state.bodies.length = 0;
    act(() => {
      for (const entry of result.current.pending.data) {
        result.current.send.retry(entry);
      }
    });
    await waitFor(() => expect(s.state.bodies).toHaveLength(2));
    const reply = s.state.bodies.find((body) => body.content === "a reply")!;
    const voiced = s.state.bodies.find((body) => body.content !== "a reply")!;
    expect(reply.replyToId).toBe(target.id);
    expect(voiced.voiceMessage).toBe(true);
  });

  it("keeps the files of a failed send when the channel is left and opened again", async () => {
    const s = setup();
    const general = await generalId(s.service);
    const first = mount(s, general);
    await waitFor(() => expect(first.result.current.list.isSuccess).toBe(true));

    s.state.down = true;
    act(() => {
      first.result.current.send.send("with a file", null, undefined, [
        new File(["bytes"], "keep.txt", { type: "text/plain" }),
      ]);
    });
    await waitFor(() =>
      expect(first.result.current.pending.data[0]?.status).toBe("failed"),
    );
    first.unmount();

    s.state.down = false;
    const second = mount(s, general);
    await waitFor(() =>
      expect(second.result.current.list.isSuccess).toBe(true),
    );
    act(() =>
      second.result.current.send.retry(second.result.current.pending.data[0]!),
    );
    await waitFor(() => {
      const stored = flattenPages(second.result.current.list.data).find(
        (message) => message.content === "with a file",
      );
      expect(stored?.attachments?.map((item) => item.fileName)).toEqual([
        "keep.txt",
      ]);
    });
  });
});
