import { act, type ReactNode } from "react";
import { createRoot, type Root } from "react-dom/client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { afterEach, describe, expect, it, vi } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import {
  MockStore,
  createMockFetch,
  defaultConfig,
  mockIds,
} from "@notrelix/dev-mock-backend";
import { useBoardSharing } from "../use-board-sharing";

(
  globalThis as typeof globalThis & { IS_REACT_ACT_ENVIRONMENT?: boolean }
).IS_REACT_ACT_ENVIRONMENT = true;

const runtime = vi.hoisted(() => ({ api: null as unknown }));
vi.mock("@notrelix/runtime-web", () => ({
  useAppRuntime: () => ({ api: runtime.api }),
}));

const BOARD = mockIds.boards.erp;
const WORKSPACE = "mock-workspace-primary";

let root: Root | null = null;
let host: HTMLDivElement | null = null;

afterEach(() => {
  act(() => root?.unmount());
  host?.remove();
  root = null;
  host = null;
});

function renderSharing(options: { failFirstRead?: boolean } = {}) {
  const store = new MockStore({ ...defaultConfig });
  const requests: string[] = [];
  const mockFetch = createMockFetch(store);
  let failures = options.failFirstRead ? 1 : 0;
  runtime.api = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: async (input, init) => {
      const path = new URL(String(input instanceof Request ? input.url : input))
        .pathname;
      if (failures > 0 && path.endsWith(`/boards/${BOARD}/sharing`)) {
        failures -= 1;
        return new Response(null, { status: 500 });
      }
      requests.push(
        `${init?.method ?? "GET"} ${new URL(String(input instanceof Request ? input.url : input)).pathname}`,
      );
      return mockFetch(input, init);
    },
  });
  const result: { current: ReturnType<typeof useBoardSharing> } = {
    current: undefined as never,
  };
  function Probe() {
    result.current = useBoardSharing(WORKSPACE, BOARD, true);
    return null;
  }
  const queries = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  const wrap = (children: ReactNode) => (
    <QueryClientProvider client={queries}>{children}</QueryClientProvider>
  );
  host = document.createElement("div");
  document.body.append(host);
  root = createRoot(host);
  act(() => root!.render(wrap(<Probe />)));
  return { result, requests, store };
}

async function settled(check: () => boolean) {
  for (let attempt = 0; attempt < 50 && !check(); attempt += 1)
    await act(async () => {
      await new Promise((resolve) => setTimeout(resolve, 10));
    });
  expect(check()).toBe(true);
}

describe("useBoardSharing", () => {
  it("is loading until the read answers, then ready", async () => {
    const { result } = renderSharing();
    expect(result.current.loading).toBe(true);
    await settled(() => result.current.data !== null);
    expect(result.current.loading).toBe(false);
    expect(result.current.failed).toBe(false);
  });

  it("reports a failed read and recovers when it is read again", async () => {
    const { result } = renderSharing({ failFirstRead: true });
    await settled(() => result.current.failed);
    expect(result.current.data).toBeNull();
    act(() => result.current.reload());
    await settled(() => result.current.data !== null);
    expect(result.current.failed).toBe(false);
  });

  it("reads the Board's sharing from the producer and maps it", async () => {
    const { result } = renderSharing();
    await settled(() => result.current.data !== null);
    const data = result.current.data!;
    expect(data).toMatchObject({
      canManage: true,
      entriesDisclosed: true,
      visibility: "Workspace",
    });
    expect(data.entries.map((entry) => entry.name)).toContain("Đặng Minh Tuấn");
    expect(data.entries.find((entry) => entry.kind === "guest")).toMatchObject({
      level: "viewer",
      levelCapped: false,
    });
  });

  it("changes a Team's level with the grant operation and reads it back", async () => {
    const { result, requests } = renderSharing();
    await settled(() => result.current.data !== null);
    await act(async () => {
      await result.current.changeLevel("mock-team-erp-accounting", "editor");
    });
    expect(requests).toContain(
      `POST /api/v1/resources/work-management.board/${BOARD}/permissions`,
    );
    await settled(
      () =>
        result.current.data?.entries.find(
          (entry) => entry.id === "mock-team-erp-accounting",
        )?.level === "editor",
    );
  });

  it("removes a grant with Revoke and a membership with RemoveMember", async () => {
    const { result, requests } = renderSharing();
    await settled(() => result.current.data !== null);
    await act(async () => {
      await result.current.remove("mock-user-erp-tuan");
    });
    expect(
      requests.some((request) =>
        request.startsWith(
          `DELETE /api/v1/resources/work-management.board/${BOARD}/permissions/`,
        ),
      ),
    ).toBe(true);
    await act(async () => {
      await result.current.makePrivate(["mock-user-erp-quan"], false);
    });
    await settled(
      () =>
        result.current.data?.entries.some(
          (entry) => entry.id === "mock-user-erp-quan",
        ) ?? false,
    );
    await act(async () => {
      await result.current.remove("mock-user-erp-quan");
    });
    expect(requests).toContain(
      `DELETE /api/v1/boards/${BOARD}/members/mock-user-erp-quan`,
    );
    await settled(
      () =>
        !(
          result.current.data?.entries.some(
            (entry) => entry.id === "mock-user-erp-quan",
          ) ?? true
        ),
    );
  });

  it("makes the Board Private in one call with the chosen people", async () => {
    const { result, requests } = renderSharing();
    await settled(() => result.current.data !== null);
    await act(async () => {
      await result.current.makePrivate(["mock-user-erp-quan"], false);
    });
    expect(requests).toContain(`POST /api/v1/boards/${BOARD}/sharing/private`);
    await settled(() => result.current.data?.visibility === "Private");
  });
});
