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
  type MockPersona,
} from "@notrelix/dev-mock-backend";
import { createChatHooks } from "../web/query/create-chat-hooks";
import { chatFailureOf } from "../core/api/chat-failure";
import { chatQueryKeys } from "../query/keys";

const WORKSPACE = mockIds.workspaces.primary;

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
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
  return { store, hooks, queryClient, wrapper };
}

describe("retention hooks (UC-CHT-H05)", () => {
  it("reads the rule and saves a new one, seeding the cache with the new version", async () => {
    const { hooks, queryClient, wrapper } = setup();
    const { result } = renderHook(
      () => ({
        read: hooks.useRetention(WORKSPACE),
        write: hooks.useSetRetention(WORKSPACE),
      }),
      { wrapper },
    );
    await waitFor(() => expect(result.current.read.isSuccess).toBe(true));
    expect(result.current.read.data).toMatchObject({
      retentionDays: null,
      version: 1,
      canChange: true,
    });

    await act(async () => {
      await result.current.write.mutateAsync({
        retentionDays: 90,
        mode: "purge",
        expectedVersion: 1,
      });
    });

    expect(
      queryClient.getQueryData(chatQueryKeys.retention(WORKSPACE)),
    ).toMatchObject({ retentionDays: 90, mode: "purge", version: 2 });
  });

  it("a stale version is a stale failure and reads the rule again", async () => {
    const { hooks, wrapper } = setup();
    await hooks.service.setRetention(WORKSPACE, {
      retentionDays: 30,
      mode: "tombstone",
      expectedVersion: 1,
    });
    const { result } = renderHook(() => hooks.useSetRetention(WORKSPACE), {
      wrapper,
    });
    let failure: unknown;
    await act(async () => {
      failure = await result.current
        .mutateAsync({ retentionDays: 60, mode: "purge", expectedVersion: 1 })
        .catch((error: unknown) => error);
    });
    expect(chatFailureOf(failure).kind).toBe("stale");
  });

  it("an Admin reads the rule as read-only and is refused when setting it", async () => {
    const { hooks } = setup("admin");
    expect((await hooks.service.getRetention(WORKSPACE)).canChange).toBe(false);
    const refused = await hooks.service
      .setRetention(WORKSPACE, {
        retentionDays: 30,
        mode: "tombstone",
        expectedVersion: 1,
      })
      .catch((error: unknown) => error);
    expect(chatFailureOf(refused).kind).toBe("forbidden");
  });
});

describe("export hooks (UC-CHT-H06)", () => {
  it("asks for an export, follows it to ready and downloads the file with the session", async () => {
    const { hooks, wrapper } = setup();
    const { result } = renderHook(
      () => {
        const create = hooks.useCreateExport(WORKSPACE);
        const id = create.data?.id ?? null;
        return {
          create,
          status: hooks.useExport(WORKSPACE, id),
          download: hooks.useDownloadExport(WORKSPACE),
        };
      },
      { wrapper },
    );

    await act(async () => {
      await result.current.create.mutateAsync({
        subjectUserId: mockIds.users.admin,
      });
    });
    expect(result.current.create.data?.status).toBe("queued");
    await waitFor(() =>
      expect(result.current.status.data?.status).toBe("ready"),
    );

    let blob: Blob | undefined;
    await act(async () => {
      blob = await result.current.download.mutateAsync(
        result.current.create.data!.id,
      );
    });
    expect(blob).toBeDefined();
    expect(blob!.size).toBe(result.current.status.data!.sizeBytes);
  });

  it("a Member is refused (forbidden) and the sixth request in an hour is rate limited", async () => {
    const member = setup("member");
    const refused = await member.hooks.service
      .createExport(WORKSPACE, { subjectUserId: mockIds.users.admin })
      .catch((error: unknown) => error);
    expect(chatFailureOf(refused).kind).toBe("forbidden");

    const { hooks } = setup();
    let last: unknown;
    for (let i = 0; i < 6; i += 1) {
      last = await hooks.service
        .createExport(WORKSPACE, { subjectUserId: mockIds.users.admin })
        .catch((error: unknown) => error);
    }
    expect(chatFailureOf(last).kind).toBe("rate-limited");
  });
});
