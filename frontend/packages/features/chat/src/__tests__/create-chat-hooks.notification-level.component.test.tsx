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
import { createChatHooks } from "../web/query/create-chat-hooks";

const WORKSPACE = mockIds.workspaces.primary;

describe("useSetNotificationLevel", () => {
  it("keeps the chosen level on the open channel, now and after a refetch", async () => {
    const store = new MockStore({ ...defaultConfig });
    const client = createNotrelixClient({
      baseUrl: "http://localhost:8000/api/v1",
      fetchImpl: (input, init) => createMockFetch(store)(input, init),
    });
    const hooks = createChatHooks({
      api: client.api,
      endpoints: client.endpoints,
    });
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    const wrapper = ({ children }: { children: ReactNode }) => (
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    );
    const channels = await hooks.service.listChannels(WORKSPACE);
    const general = channels.find((c) => c.name === "general")!.id;

    const { result } = renderHook(
      () => ({
        detail: hooks.useChannel(WORKSPACE, general),
        writer: hooks.useSetNotificationLevel(WORKSPACE),
      }),
      { wrapper },
    );
    await waitFor(() => expect(result.current.detail.isSuccess).toBe(true));
    expect(result.current.detail.data?.channel.notificationLevel).toBe("all");

    await act(async () => {
      await result.current.writer.mutateAsync({
        spaceId: general,
        level: "mentions",
      });
    });
    await waitFor(() =>
      expect(result.current.detail.data?.channel.notificationLevel).toBe(
        "mentions",
      ),
    );
    await waitFor(() => expect(result.current.detail.isFetching).toBe(false));
    expect(result.current.detail.data?.channel.notificationLevel).toBe(
      "mentions",
    );
  });
});
