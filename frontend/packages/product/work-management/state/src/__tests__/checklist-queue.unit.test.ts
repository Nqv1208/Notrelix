import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { QueryClient } from "@tanstack/react-query";
import type { CardDetail, Checklist } from "@notrelix/work-management-core";
import { wmQueryKeys } from "../queries/keys";
import {
  CHECKLIST_UNDO_MS,
  ChecklistQueue,
  type ChecklistOp,
} from "../mutations/checklist-queue";

const WS = "ws-1";
const BOARD = "b-1";
const CARD = "c-1";

const list = (id: string, version: number, done: boolean[]): Checklist => ({
  id,
  title: id,
  version,
  position: 1,
  items: done.map((isDone, i) => ({
    id: `${id}-${i}`,
    title: `item ${i}`,
    isDone,
    position: i + 1,
  })),
});

function setup() {
  const queryClient = new QueryClient();
  const detail = {
    id: CARD,
    checklists: [list("l1", 4, [true, false])],
    _count: { comments: 0, attachments: 0, checklistItems: 2 },
  } as unknown as CardDetail;
  queryClient.setQueryData(wmQueryKeys.cardDetail(WS, CARD), detail);
  const refetch = vi.fn(() => Promise.resolve());
  queryClient.invalidateQueries = refetch as never;
  const queue = new ChecklistQueue(queryClient, WS, BOARD, CARD);
  const lists = () =>
    queryClient.getQueryData<CardDetail>(wmQueryKeys.cardDetail(WS, CARD))!
      .checklists;
  return { queryClient, queue, lists, refetch };
}

const toggleOp = (send: ChecklistOp["send"], itemId = "l1-1"): ChecklistOp => ({
  id: `t-${itemId}`,
  kind: "toggle-item",
  checklistId: "l1",
  itemId,
  apply: (lists) =>
    lists.map((l) => ({
      ...l,
      items: l.items.map((i) =>
        i.id === itemId ? { ...i, isDone: !i.isDone } : i,
      ),
    })),
  send,
});

describe("ChecklistQueue", () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it("shows the change at once, sends the list's version, then bumps it", async () => {
    const { queue, lists, refetch } = setup();
    const send = vi.fn(() => Promise.resolve());
    queue.schedule(toggleOp(send));
    expect(lists()[0]!.items[1]!.isDone).toBe(true);
    await vi.runAllTimersAsync();
    expect(send).toHaveBeenCalledWith(4);
    expect(lists()[0]!.version).toBe(5);
    expect(refetch).toHaveBeenCalled();
    expect(queue.getSnapshot().busy).toBe(false);
  });

  it("sends writes one after another with the version the last one left", async () => {
    const { queue } = setup();
    const versions: Array<number | undefined> = [];
    const send = (v: number | undefined) => {
      versions.push(v);
      return Promise.resolve();
    };
    queue.schedule(toggleOp(send, "l1-0"));
    queue.schedule(toggleOp(send, "l1-1"));
    await vi.runAllTimersAsync();
    expect(versions).toEqual([4, 5]);
  });

  it("keeps a refused write on screen with Retry and holds the ones behind it", async () => {
    const { queue, lists } = setup();
    const send = vi
      .fn<ChecklistOp["send"]>()
      .mockRejectedValueOnce(new Error("offline"))
      .mockResolvedValue(undefined);
    const behind = vi.fn(() => Promise.resolve());
    queue.schedule(toggleOp(send, "l1-1"));
    queue.schedule(toggleOp(behind, "l1-0"));
    await vi.runAllTimersAsync();
    expect(queue.getSnapshot().failure?.op.itemId).toBe("l1-1");
    expect(lists()[0]!.items[1]!.isDone).toBe(true); // no silent revert
    expect(behind).not.toHaveBeenCalled();
    queue.retry();
    await vi.runAllTimersAsync();
    expect(send).toHaveBeenCalledTimes(2);
    expect(behind).toHaveBeenCalledTimes(1);
    expect(queue.getSnapshot().failure).toBeNull();
  });

  it("reads again on a version conflict and drops the queued writes", async () => {
    const { queue, refetch } = setup();
    const conflict = Object.assign(new Error("409"), { kind: "conflict" });
    const behind = vi.fn(() => Promise.resolve());
    queue.schedule(toggleOp(() => Promise.reject(conflict), "l1-1"));
    queue.schedule(toggleOp(behind, "l1-0"));
    await vi.runAllTimersAsync();
    expect(queue.getSnapshot().conflict).toBe(true);
    expect(queue.getSnapshot().failure).toBeNull();
    expect(behind).not.toHaveBeenCalled();
    expect(refetch).toHaveBeenCalled();
    queue.dismissConflict();
    expect(queue.getSnapshot().conflict).toBe(false);
  });

  it("discard gives up the refused write and reads the server list", async () => {
    const { queue, refetch } = setup();
    queue.schedule(toggleOp(() => Promise.reject(new Error("offline"))));
    await vi.runAllTimersAsync();
    refetch.mockClear();
    queue.discard();
    await vi.runAllTimersAsync();
    expect(queue.getSnapshot().failure).toBeNull();
    expect(refetch).toHaveBeenCalled();
  });

  it("holds a delete for undo, sends it after the window", async () => {
    const { queue, lists } = setup();
    const send = vi.fn(() => Promise.resolve());
    const removed = lists()[0]!.items[0]!;
    queue.schedule(
      {
        id: "d",
        kind: "delete-item",
        checklistId: "l1",
        itemId: removed.id,
        text: removed.title,
        apply: (ls) => ls.map((l) => ({ ...l, items: l.items.slice(1) })),
        send,
      },
      { delayMs: CHECKLIST_UNDO_MS },
    );
    expect(lists()[0]!.items).toHaveLength(1);
    expect(queue.getSnapshot().pendingDelete?.text).toBe("item 0");
    await vi.advanceTimersByTimeAsync(CHECKLIST_UNDO_MS - 1);
    expect(send).not.toHaveBeenCalled();
    await vi.advanceTimersByTimeAsync(2);
    expect(send).toHaveBeenCalledWith(4);
    expect(queue.getSnapshot().pendingDelete).toBeNull();
  });

  it("undo puts the entry back at its place and sends nothing", async () => {
    const { queue, lists } = setup();
    const send = vi.fn(() => Promise.resolve());
    const removed = lists()[0]!.items[0]!;
    queue.schedule(
      {
        id: "d",
        kind: "delete-item",
        checklistId: "l1",
        itemId: removed.id,
        apply: (ls) => ls.map((l) => ({ ...l, items: l.items.slice(1) })),
        send,
      },
      {
        delayMs: CHECKLIST_UNDO_MS,
        restore: (ls) =>
          ls.map((l) => ({ ...l, items: [removed, ...l.items] })),
      },
    );
    queue.undo();
    await vi.advanceTimersByTimeAsync(CHECKLIST_UNDO_MS * 2);
    expect(lists()[0]!.items.map((i) => i.id)).toEqual(["l1-0", "l1-1"]);
    expect(send).not.toHaveBeenCalled();
  });

  it("keeps the face's progress in step with the list", async () => {
    const { queue, queryClient } = setup();
    queue.schedule(toggleOp(() => Promise.resolve(), "l1-1"));
    const detail = queryClient.getQueryData<CardDetail>(
      wmQueryKeys.cardDetail(WS, CARD),
    )!;
    expect(detail.checklistProgress).toEqual({ done: 2, total: 2 });
    await vi.runAllTimersAsync();
  });
});
