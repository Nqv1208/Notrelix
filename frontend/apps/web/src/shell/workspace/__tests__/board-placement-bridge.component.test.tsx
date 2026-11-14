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
import {
  useBoardPlacements,
  useMoveBoardToSpace,
  type BoardPlacement,
} from "../board-placement-bridge";

(
  globalThis as typeof globalThis & { IS_REACT_ACT_ENVIRONMENT?: boolean }
).IS_REACT_ACT_ENVIRONMENT = true;

const runtime = vi.hoisted(() => ({ api: null as unknown }));
vi.mock("@notrelix/runtime-web", () => ({
  useAppRuntime: () => ({ api: runtime.api }),
}));

const BOARD = mockIds.boards.erp;
const WORKSPACE = mockIds.workspaces.primary;

let root: Root | null = null;
let host: HTMLDivElement | null = null;

afterEach(() => {
  act(() => root?.unmount());
  host?.remove();
  root = null;
  host = null;
});

function render() {
  const store = new MockStore({ ...defaultConfig });
  const mockFetch = createMockFetch(store);
  const sent: { path: string; key: string | null; body: unknown }[] = [];
  runtime.api = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: async (input, init) => {
      const request = input instanceof Request ? input : null;
      const url = new URL(String(request?.url ?? input));
      if (url.pathname.endsWith("/move-to-space")) {
        const headers = new Headers(init?.headers ?? request?.headers);
        sent.push({
          path: url.pathname,
          key: headers.get("Idempotency-Key"),
          body: JSON.parse(String(init?.body ?? "null")),
        });
      }
      return mockFetch(input, init);
    },
  });
  const result: {
    placements: ReturnType<typeof useBoardPlacements>;
    move: ReturnType<typeof useMoveBoardToSpace>;
  } = { placements: undefined as never, move: undefined as never };
  function Probe() {
    result.placements = useBoardPlacements(WORKSPACE);
    result.move = useMoveBoardToSpace(WORKSPACE);
    return null;
  }
  const queries = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  const wrap = (children: ReactNode) => (
    <QueryClientProvider client={queries}>{children}</QueryClientProvider>
  );
  host = document.createElement("div");
  document.body.append(host);
  root = createRoot(host);
  act(() => root!.render(wrap(<Probe />)));
  return { result, sent };
}

async function settled(check: () => boolean) {
  for (let attempt = 0; attempt < 100 && !check(); attempt += 1)
    await act(async () => {
      await new Promise((resolve) => setTimeout(resolve, 10));
    });
  expect(check()).toBe(true);
}

const placementOf = (rows: BoardPlacement[] | undefined) =>
  rows?.find((row) => row.id === BOARD);

describe("useMoveBoardToSpace", () => {
  it("sends the read version and one idempotency key, and the Board reads in the new Space after", async () => {
    const { result, sent } = render();
    await settled(() => result.placements.data !== undefined);
    const before = placementOf(result.placements.data)!;
    expect(before.version).toBeGreaterThan(0);

    await act(async () => {
      await result.move.mutateAsync({
        boardId: BOARD,
        spaceId: mockIds.spaces.marketing,
        expectedVersion: before.version,
        attemptKey: "attempt-1",
      });
    });

    expect(sent).toEqual([
      {
        path: `/api/v1/boards/${BOARD}/move-to-space`,
        key: "attempt-1",
        body: {
          spaceId: mockIds.spaces.marketing,
          expectedVersion: before.version,
        },
      },
    ]);
    await settled(
      () =>
        placementOf(result.placements.data)?.spaceId ===
        mockIds.spaces.marketing,
    );
    expect(placementOf(result.placements.data)!.version).toBeGreaterThan(
      before.version,
    );
  });

  it("rolls the placement back and changes nothing when the version is stale", async () => {
    const { result } = render();
    await settled(() => result.placements.data !== undefined);
    const before = placementOf(result.placements.data)!;

    const failure = await act(async () =>
      result.move
        .mutateAsync({
          boardId: BOARD,
          spaceId: mockIds.spaces.marketing,
          expectedVersion: before.version + 5,
          attemptKey: "attempt-stale",
        })
        .then(
          () => null,
          (error: unknown) => error,
        ),
    );

    expect((failure as { status?: number } | null)?.status).toBe(412);
    await settled(() => result.move.isError);
    expect(placementOf(result.placements.data)).toMatchObject({
      spaceId: before.spaceId,
      version: before.version,
    });
  });

  it("rolls the placement back when the target Space does not exist", async () => {
    const { result } = render();
    await settled(() => result.placements.data !== undefined);
    const before = placementOf(result.placements.data)!;

    const failure = await act(async () =>
      result.move
        .mutateAsync({
          boardId: BOARD,
          spaceId: "mock-space-of-another-workspace",
          expectedVersion: before.version,
          attemptKey: "attempt-foreign",
        })
        .then(
          () => null,
          (error: unknown) => error,
        ),
    );

    expect(
      (failure as { status?: number } | null)?.status,
    ).toBeGreaterThanOrEqual(400);
    await settled(() => result.move.isError);
    expect(placementOf(result.placements.data)?.spaceId).toBe(before.spaceId);
  });
});
