import { describe, expect, it, vi } from "vitest";
import {
  createPreferenceSaver,
  type PreferenceStatus,
} from "../queries/preference-saver";

const target = { workspaceId: "w1", boardId: "b1", viewId: "v1" };

function setup() {
  const jobs: Array<{
    viewId: string;
    json: string;
    resolve: () => void;
    reject: (error: unknown) => void;
  }> = [];
  const send = vi.fn(
    (t: { viewId: string }, json: string) =>
      new Promise<void>((resolve, reject) => {
        jobs.push({ viewId: t.viewId, json, resolve, reject });
      }),
  );
  const reports: PreferenceStatus[] = [];
  const saver = createPreferenceSaver({
    send,
    cache: vi.fn(),
    report: (status) => reports.push(status),
  });
  const flushMicrotasks = () => new Promise((r) => setTimeout(r, 0));
  return { saver, send, jobs, reports, flushMicrotasks };
}

describe("createPreferenceSaver", () => {
  it("sends the next save only after the previous one settled, last state last", async () => {
    const { saver, send, jobs, flushMicrotasks } = setup();
    saver.save({ groupBy: "first" }, target);
    saver.save({ groupBy: "second" }, target);
    await flushMicrotasks();
    expect(send).toHaveBeenCalledTimes(1);
    expect(jobs[0]!.json).toContain("first");
    jobs[0]!.resolve();
    await flushMicrotasks();
    expect(send).toHaveBeenCalledTimes(2);
    expect(jobs[1]!.json).toContain("second");
  });

  it("a failed save does not block the next one", async () => {
    const { saver, send, jobs, flushMicrotasks } = setup();
    saver.save({ groupBy: "first" }, target);
    saver.save({ groupBy: "second" }, target);
    await flushMicrotasks();
    jobs[0]!.reject({ kind: "network" });
    await flushMicrotasks();
    expect(send).toHaveBeenCalledTimes(2);
  });

  it("sends to the view each save was made on", async () => {
    const { saver, jobs, flushMicrotasks } = setup();
    saver.save({ groupBy: "x" }, target);
    await flushMicrotasks();
    jobs[0]!.resolve();
    saver.save(
      { groupBy: "y" },
      { workspaceId: "w2", boardId: "b2", viewId: "v9" },
    );
    await flushMicrotasks();
    expect(jobs[1]!.viewId).toBe("v9");
  });

  it("never sends criteria over 8192 UTF-8 bytes and names the reason", async () => {
    const { saver, send, reports } = setup();
    // 2800 three-byte characters: fewer than 8192 characters, more than 8192 bytes.
    saver.save({ filter: "€".repeat(2800) }, target);
    expect(send).not.toHaveBeenCalled();
    expect(reports.at(-1)).toMatchObject({ issue: "too_large", failed: false });
  });

  it("a server validation error is a reason, not a retry", async () => {
    const { saver, jobs, reports, flushMicrotasks } = setup();
    saver.save({ groupBy: "x" }, target);
    await flushMicrotasks();
    jobs[0]!.reject({ kind: "validation" });
    await flushMicrotasks();
    expect(reports.at(-1)).toMatchObject({ issue: "rejected", failed: false });
  });

  it("a network failure offers retry and resends the last state", async () => {
    const { saver, send, jobs, reports, flushMicrotasks } = setup();
    saver.save({ groupBy: "x" }, target);
    await flushMicrotasks();
    jobs[0]!.reject({ kind: "network" });
    await flushMicrotasks();
    expect(reports.at(-1)).toMatchObject({ failed: true, issue: null });
    saver.retry();
    await flushMicrotasks();
    expect(send).toHaveBeenCalledTimes(2);
  });

  it("a view deleted by someone else is a refusal with a reason, not a retry", async () => {
    const { saver, jobs, reports, flushMicrotasks } = setup();
    saver.save({ groupBy: "x" }, target);
    await flushMicrotasks();
    jobs[0]!.reject({ kind: "not_found" });
    await flushMicrotasks();
    expect(reports.at(-1)).toMatchObject({ issue: "gone", failed: false });
  });

  it("a pending save flushed on a workspace switch caches and reports under the workspace it was made in", async () => {
    const cached: Array<{ workspaceId: string }> = [];
    const reports: Array<{ target: { workspaceId: string } }> = [];
    const saver = createPreferenceSaver({
      send: () => Promise.resolve(),
      cache: (t) => cached.push(t),
      report: (status) => reports.push(status),
    });
    saver.save({ groupBy: "x" }, { ...target, workspaceId: "w-old" });
    expect(cached).toEqual([{ ...target, workspaceId: "w-old" }]);
    expect(reports.every((r) => r.target.workspaceId === "w-old")).toBe(true);
  });
});
