import { QueryClient } from "@tanstack/react-query";
import type {
  Block,
  CreateBlockPayload,
  ReorderBlocksInput,
} from "@notrelix/docs-core";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { docsQueryKeys } from "../query/keys";
import { createUseCreateBlock } from "../query/hooks/use-create-block";
import { createUseDeleteBlock } from "../query/hooks/use-delete-block";
import { createUseReorderBlocks } from "../query/hooks/use-reorder-blocks";
import { testEndpoints } from "./test-endpoints";

// `useMutation` is replaced by its options, so each hook's optimistic patch,
// rollback and settle callbacks can be driven without a React tree.
const harness = vi.hoisted(() => ({
  client: undefined as unknown as QueryClient,
}));

vi.mock("@tanstack/react-query", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@tanstack/react-query")>();
  return {
    ...actual,
    useMutation: (options: unknown) => options,
    useQueryClient: () => harness.client,
  };
});

interface MutationOptions<TVariables, TResult> {
  mutationFn: (variables: TVariables) => Promise<TResult>;
  onMutate?: (
    variables: TVariables,
  ) => Promise<{ previous?: Block[]; pendingId?: string }>;
  onSuccess?: (
    result: TResult,
    variables: TVariables,
    context?: { previous?: Block[]; pendingId?: string },
  ) => void;
  onError?: (
    error: unknown,
    variables: TVariables,
    context?: { previous?: Block[] },
  ) => void;
  onSettled?: () => void;
}

const WORKSPACE = "ws-1";
const PAGE = "page-1";
const key = docsQueryKeys.blocks(WORKSPACE, PAGE);

function block(
  id: string,
  position: number,
  parentId: string | null = null,
): Block {
  return {
    id,
    pageId: PAGE,
    type: "paragraph",
    properties: { text: id },
    version: 1,
    position,
    parentId,
    createdById: "u1",
    updatedById: "u1",
    createdAt: "2026-11-01T00:00:00Z",
    updatedAt: "2026-11-01T00:00:00Z",
  };
}

function api() {
  return {
    get: vi.fn(),
    post: vi.fn(),
    patch: vi.fn(),
    delete: vi.fn(),
  };
}

function stored(id: string): Record<string, unknown> {
  return {
    id,
    pageId: PAGE,
    type: "Text",
    content: {},
    properties: {},
    position: "a0",
    version: 1,
    createdAt: "2026-11-09T00:00:00Z",
  };
}

const stableBlocks = () => [
  block("a", 1000),
  block("b", 2000),
  block("c", 3000),
];

beforeEach(() => {
  harness.client = new QueryClient();
  harness.client.setQueryData(key, stableBlocks());
});

function options<TVariables, TResult>(hook: () => unknown) {
  return hook() as MutationOptions<TVariables, TResult>;
}

describe("useCreateBlock optimistic patch", () => {
  const payload: CreateBlockPayload = { type: "paragraph", afterBlockId: "b" };

  it("shows the new block at once and replaces it with the stored block on success", async () => {
    const client = api();
    client.post.mockResolvedValue(stored("new-1"));
    const mutation = options<CreateBlockPayload, Block>(() =>
      createUseCreateBlock(client, testEndpoints)(WORKSPACE, PAGE),
    );

    const context = await mutation.onMutate!(payload);
    const pending = harness.client.getQueryData<Block[]>(key)!;
    expect(pending).toHaveLength(4);
    expect(pending.at(-1)?.id).toBe(context.pendingId);

    const saved = await mutation.mutationFn(payload);
    mutation.onSuccess!(saved, payload, context);

    const after = harness.client.getQueryData<Block[]>(key)!;
    expect(after.map((item) => item.id)).toContain(saved.id);
    expect(after.map((item) => item.id)).not.toContain(context.pendingId);
  });

  it("goes back to the last stored blocks when the producer refuses the create", async () => {
    const client = api();
    client.post.mockRejectedValue(new Error("400 Bad Request"));
    const mutation = options<CreateBlockPayload, Block>(() =>
      createUseCreateBlock(client, testEndpoints)(WORKSPACE, PAGE),
    );

    const context = await mutation.onMutate!(payload);
    const error = await mutation
      .mutationFn(payload)
      .catch((reason: unknown) => reason);
    mutation.onError!(error, payload, context);

    expect(harness.client.getQueryData<Block[]>(key)).toEqual(stableBlocks());
  });
});

describe("useDeleteBlock optimistic patch", () => {
  it("removes the block and the blocks under it at once", async () => {
    harness.client.setQueryData(key, [
      block("a", 1000),
      block("child", 1500, "a"),
      block("grandchild", 1600, "child"),
      block("b", 2000),
    ]);
    const mutation = options<Pick<Block, "id" | "version">, unknown>(() =>
      createUseDeleteBlock(api(), testEndpoints)(WORKSPACE, PAGE),
    );

    await mutation.onMutate!({ id: "a", version: 1 });

    expect(
      harness.client.getQueryData<Block[]>(key)?.map((item) => item.id),
    ).toEqual(["b"]);
  });

  it("brings the blocks back when the producer refuses the delete", async () => {
    const client = api();
    client.delete.mockRejectedValue(new Error("412 Precondition Failed"));
    const mutation = options<Pick<Block, "id" | "version">, unknown>(() =>
      createUseDeleteBlock(client, testEndpoints)(WORKSPACE, PAGE),
    );

    const context = await mutation.onMutate!({ id: "b", version: 1 });
    expect(
      harness.client.getQueryData<Block[]>(key)?.map((item) => item.id),
    ).toEqual(["a", "c"]);
    const error = await mutation
      .mutationFn({ id: "b", version: 1 })
      .catch((reason: unknown) => reason);
    mutation.onError!(error, { id: "b", version: 1 }, context);

    expect(harness.client.getQueryData<Block[]>(key)).toEqual(stableBlocks());
  });
});

describe("useReorderBlocks optimistic patch", () => {
  const moves: ReorderBlocksInput = {
    pageId: PAGE,
    moves: [
      { blockId: "c", expectedVersion: 1, parentId: null, afterBlockId: "a" },
    ],
  };

  it("moves the block next to its named sibling at once", async () => {
    const mutation = options<ReorderBlocksInput, unknown>(() =>
      createUseReorderBlocks(api(), testEndpoints)(WORKSPACE, PAGE),
    );

    await mutation.onMutate!(moves);

    const order = [...harness.client.getQueryData<Block[]>(key)!].sort(
      (x, y) => x.position - y.position,
    );
    expect(order.map((item) => item.id)).toEqual(["a", "c", "b"]);
  });

  it("goes back to the last stored order when the producer refuses the move", async () => {
    const client = api();
    client.post.mockRejectedValue(new Error("412 Precondition Failed"));
    const mutation = options<ReorderBlocksInput, unknown>(() =>
      createUseReorderBlocks(client, testEndpoints)(WORKSPACE, PAGE),
    );

    const context = await mutation.onMutate!(moves);
    const error = await mutation
      .mutationFn(moves)
      .catch((reason: unknown) => reason);
    mutation.onError!(error, moves, context);

    expect(harness.client.getQueryData<Block[]>(key)).toEqual(stableBlocks());
  });
});
