import { describe, it, expect, vi, beforeEach } from "vitest";
import { QueryClient } from "@tanstack/react-query";
import {
  executeOptimisticCommand,
  defineOptimisticUpdate,
} from "../optimistic-command";
import { AppError } from "@notrelix/kernel";

describe("executeOptimisticCommand with defineOptimisticUpdate", () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });
  });

  it("supports distinct per-key updaters for detail and list cache shapes", async () => {
    const detailKey = ["boards", "1"];
    const listKey = ["boards", "list"];

    queryClient.setQueryData(detailKey, { id: "1", title: "Old Title" });
    queryClient.setQueryData(listKey, [{ id: "1", title: "Old Title" }]);

    const mutationFn = vi
      .fn()
      .mockResolvedValue({ id: "1", title: "New Title" });

    const result = await executeOptimisticCommand({
      queryClient,
      commandId: "cmd-1",
      updates: [
        defineOptimisticUpdate(
          detailKey,
          (old: any, vars: { title: string }) => ({
            ...old,
            title: vars.title,
          }),
        ),
        defineOptimisticUpdate(
          listKey,
          (old: any[] | undefined, vars: { title: string }) =>
            (old ?? []).map((item) => ({ ...item, title: vars.title })),
        ),
      ],
      mutationFn,
      variables: { title: "New Title" },
    });

    expect(result).toEqual({ id: "1", title: "New Title" });
    expect(queryClient.getQueryData(detailKey)).toEqual({
      id: "1",
      title: "New Title",
    });
    expect(queryClient.getQueryData(listKey)).toEqual([
      { id: "1", title: "New Title" },
    ]);
  });

  it("rolls back all updates in reverse order on mutation failure", async () => {
    const detailKey = ["boards", "1"];
    queryClient.setQueryData(detailKey, { id: "1", title: "Original Title" });

    const mutationFn = vi.fn().mockRejectedValue(new Error("Network error"));

    await expect(
      executeOptimisticCommand({
        queryClient,
        commandId: "cmd-rollback",
        updates: [
          defineOptimisticUpdate(
            detailKey,
            (old: any, vars: { title: string }) => ({
              ...old,
              title: vars.title,
            }),
          ),
        ],
        mutationFn,
        variables: { title: "Optimistic Title" },
      }),
    ).rejects.toThrow("Network error");

    // Cache should be restored to pre-mutation state
    expect(queryClient.getQueryData(detailKey)).toEqual({
      id: "1",
      title: "Original Title",
    });
  });

  it("removes entries that did not exist prior to optimistic command on failure", async () => {
    const nonExistentKey = ["boards", "new-id"];

    expect(queryClient.getQueryState(nonExistentKey)).toBeUndefined();

    const mutationFn = vi.fn().mockRejectedValue(new Error("Creation failed"));

    await expect(
      executeOptimisticCommand({
        queryClient,
        commandId: "cmd-remove",
        updates: [
          defineOptimisticUpdate(
            nonExistentKey,
            (_old: any, vars: { title: string }) => ({
              id: "new-id",
              title: vars.title,
            }),
          ),
        ],
        mutationFn,
        variables: { title: "New Item" },
      }),
    ).rejects.toThrow("Creation failed");

    // Cache entry should be completely removed, not left as undefined/empty state
    expect(queryClient.getQueryState(nonExistentKey)).toBeUndefined();
  });

  it("passes command context into mutationFn and reconcile", async () => {
    const key = ["boards", "1"];
    queryClient.setQueryData(key, { id: "1", title: "Old" });

    const mutationFn = vi.fn().mockResolvedValue({ id: "1", title: "Server" });
    const reconcile = vi.fn((result, client: QueryClient) => {
      client.setQueryData(key, result);
    });

    await executeOptimisticCommand({
      queryClient,
      commandId: "cmd-context",
      correlationId: "corr-context",
      idempotencyKey: "idem-context",
      updates: [
        defineOptimisticUpdate(key, (old: any) => ({
          ...old,
          title: "Optimistic",
        })),
      ],
      mutationFn,
      reconcile,
      invalidate: [],
      variables: {},
    });

    expect(mutationFn).toHaveBeenCalledWith(
      {},
      {
        commandId: "cmd-context",
        correlationId: "corr-context",
        idempotencyKey: "idem-context",
      },
    );
    expect(reconcile).toHaveBeenCalled();
    expect(queryClient.getQueryData(key)).toEqual({ id: "1", title: "Server" });
  });

  it("supports explicit conflict refetch policy without local rollback", async () => {
    const key = ["boards", "1"];
    queryClient.setQueryData(key, { id: "1", title: "Old" });

    const conflict = new AppError({
      kind: "conflict",
      message: "Expected version mismatch.",
      status: 409,
    });

    await expect(
      executeOptimisticCommand({
        queryClient,
        commandId: "cmd-conflict",
        updates: [
          defineOptimisticUpdate(key, (old: any) => ({
            ...old,
            title: "Optimistic",
          })),
        ],
        mutationFn: vi.fn().mockRejectedValue(conflict),
        onConflict: () => "refetch",
        variables: {},
      }),
    ).rejects.toThrow("Expected version mismatch.");

    expect(queryClient.getQueryData(key)).toEqual({
      id: "1",
      title: "Optimistic",
    });
  });
});
