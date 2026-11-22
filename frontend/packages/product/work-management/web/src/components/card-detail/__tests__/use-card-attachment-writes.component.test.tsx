import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import { act, renderHook, waitFor } from "@notrelix/testing";
import {
  WorkManagementTestScope,
  useCardAttachmentWrites,
  useCardFiles,
  type WorkManagementServices,
} from "@notrelix/work-management-state";

function setup(createCardLink: ReturnType<typeof vi.fn>) {
  const getCardFiles = vi.fn(async () => []);
  const services = {
    cards: { createCardLink, getCardFiles },
    itemExtras: {},
  } as unknown as WorkManagementServices;
  const wrapper = ({ children }: { children: ReactNode }) => (
    <WorkManagementTestScope services={services}>
      {children}
    </WorkManagementTestScope>
  );
  // The item's open file list is read again after every attempt.
  const hook = renderHook(
    () => {
      useCardFiles("item-1", "ws-1");
      return useCardAttachmentWrites("item-1", "ws-1");
    },
    { wrapper },
  );
  return { hook, getCardFiles };
}

const keysOf = (fn: ReturnType<typeof vi.fn>) =>
  fn.mock.calls.map(
    (call) => (call[2] as { idempotencyKey: string }).idempotencyKey,
  );

describe("useCardAttachmentWrites.addWebLink (UC-COL-B07)", () => {
  it("attaches the link and reads the item's files and history again", async () => {
    const createCardLink = vi.fn(async () => undefined);
    const { hook, getCardFiles } = setup(createCardLink);
    await waitFor(() => expect(getCardFiles).toHaveBeenCalledTimes(1));

    const result = await act(() =>
      hook.result.current.addWebLink("https://example.com/a", "Spec"),
    );

    expect(result).toEqual({ ok: true });
    expect(createCardLink).toHaveBeenCalledWith(
      "item-1",
      { url: "https://example.com/a", name: "Spec" },
      { idempotencyKey: expect.any(String) },
    );
    await waitFor(() => expect(getCardFiles).toHaveBeenCalledTimes(2));
  });

  it.each([
    ["javascript:alert(1)"],
    ["/relative"],
    ["ftp://example.com/x"],
    ["not a url"],
  ])("refuses %s as invalid without asking the server", async (url) => {
    const createCardLink = vi.fn(async () => undefined);
    const { hook } = setup(createCardLink);

    const result = await act(() => hook.result.current.addWebLink(url, "Spec"));

    expect(result).toEqual({ ok: false, reason: "invalid" });
    expect(createCardLink).not.toHaveBeenCalled();
  });

  it.each([
    [{ status: 400 }, "rejected"],
    [{ status: 409, code: "attachment.limit_exceeded" }, "limitReached"],
    [{ status: 403 }, "forbidden"],
    [{ status: 404 }, "notFound"],
    [{ kind: "network" }, "offline"],
    [{ status: 500 }, "failed"],
    [new Error("boom"), "failed"],
  ])("maps %j to %s", async (error, reason) => {
    const createCardLink = vi.fn(async () => {
      throw error;
    });
    const { hook } = setup(createCardLink);

    const result = await act(() =>
      hook.result.current.addWebLink("https://example.com", "Spec"),
    );

    expect(result).toEqual({ ok: false, reason });
  });

  it("reuses the key to retry the same address and name, and takes a new one after success", async () => {
    const createCardLink = vi
      .fn()
      .mockRejectedValueOnce({ kind: "network" })
      .mockResolvedValue(undefined);
    const { hook } = setup(createCardLink);
    const add = (name: string) =>
      act(() => hook.result.current.addWebLink("https://example.com", name));

    await add("Spec");
    await add("Spec");
    await add("Spec");

    const [first, retry, again] = keysOf(createCardLink);
    expect(retry).toBe(first);
    expect(again).not.toBe(first);
  });

  it("takes another key for another name or address", async () => {
    const createCardLink = vi.fn().mockRejectedValue({ status: 500 });
    const { hook } = setup(createCardLink);

    await act(() => hook.result.current.addWebLink("https://example.com", "A"));
    await act(() => hook.result.current.addWebLink("https://example.com", "B"));
    await act(() => hook.result.current.addWebLink("https://example.org", "A"));

    expect(new Set(keysOf(createCardLink)).size).toBe(3);
  });
});
