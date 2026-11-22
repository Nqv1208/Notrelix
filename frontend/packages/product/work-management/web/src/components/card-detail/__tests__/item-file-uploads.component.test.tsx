import type { ReactNode } from "react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { act, renderHook } from "@notrelix/testing";
import { MAX_ITEM_FILE_BYTES } from "@notrelix/work-management-core";
import {
  ITEM_FILE_UPLOAD_DONE_MS,
  WorkManagementTestScope,
  canRetryItemFileUpload,
  useCardFiles,
  useItemFileUploads,
  type WorkManagementServices,
} from "@notrelix/work-management-state";

interface Call {
  readonly key: string;
  readonly signal: AbortSignal;
  readonly progress: (progress: { loaded: number; total: number }) => void;
  readonly resolve: () => void;
  readonly reject: (error: unknown) => void;
}

function setup() {
  const calls: Call[] = [];
  const getCardFiles = vi.fn(async () => []);
  const uploadCardFile = vi.fn(
    (
      _id: string,
      _file: File,
      options: {
        idempotencyKey: string;
        signal: AbortSignal;
        onProgress: Call["progress"];
      },
    ) =>
      new Promise<void>((resolve, reject) =>
        calls.push({
          key: options.idempotencyKey,
          signal: options.signal,
          progress: options.onProgress,
          resolve,
          reject,
        }),
      ),
  );
  const services = {
    cards: { uploadCardFile, getCardFiles },
  } as unknown as WorkManagementServices;
  const wrapper = ({ children }: { children: ReactNode }) => (
    <WorkManagementTestScope services={services}>
      {children}
    </WorkManagementTestScope>
  );
  const hook = renderHook(
    () => {
      useCardFiles("item-1", "ws-1");
      return useItemFileUploads("item-1", "ws-1");
    },
    { wrapper },
  );
  return { hook, calls, uploadCardFile, getCardFiles };
}

const pdf = () => new File(["x"], "bao-cao.pdf");
const only = (hook: ReturnType<typeof setup>["hook"]) =>
  hook.result.current.uploads[0]!;

afterEach(() => vi.useRealTimers());

describe("useItemFileUploads (UC-COL-B06)", () => {
  it("reports progress while the file is sending", async () => {
    const { hook, calls } = setup();
    act(() => hook.result.current.start(pdf()));

    expect(only(hook)).toMatchObject({ status: "uploading", progress: null });
    act(() => calls[0]!.progress({ loaded: 25, total: 100 }));
    expect(only(hook).progress).toBe(25);
  });

  it("refuses a file over the limit before sending anything", () => {
    const { hook, uploadCardFile } = setup();
    const big = new File(["x"], "big.pdf");
    Object.defineProperty(big, "size", { value: MAX_ITEM_FILE_BYTES + 1 });

    act(() => hook.result.current.start(big));

    expect(only(hook)).toMatchObject({ status: "error", error: "tooBig" });
    expect(canRetryItemFileUpload(only(hook))).toBe(false);
    expect(uploadCardFile).not.toHaveBeenCalled();
  });

  it("refuses a blocked type before sending anything", () => {
    const { hook, uploadCardFile } = setup();

    act(() => hook.result.current.start(new File(["x"], "setup.exe")));

    expect(only(hook)).toMatchObject({ status: "error", error: "blockedType" });
    expect(uploadCardFile).not.toHaveBeenCalled();
  });

  it.each([
    [
      { status: 413, code: "attachment.payload_too_large" },
      "serverTooLarge",
      false,
    ],
    [
      { status: 415, code: "attachment.unsupported_media_type" },
      "unsupportedType",
      false,
    ],
    [{ status: 409, code: "attachment.limit_exceeded" }, "limitReached", true],
    [{ status: 403 }, "forbidden", false],
    [{ status: 404 }, "notFound", false],
    [{ kind: "network" }, "failed", true],
  ] as const)(
    "keeps the row with the reason when the server answers %j",
    async (answer, reason, retryable) => {
      const { hook, calls } = setup();
      act(() => hook.result.current.start(pdf()));

      await act(async () => calls[0]!.reject(answer));

      expect(only(hook)).toMatchObject({ status: "error", error: reason });
      expect(canRetryItemFileUpload(only(hook))).toBe(retryable);
    },
  );

  it("retries with the same idempotency key and finishes", async () => {
    const { hook, calls } = setup();
    act(() => hook.result.current.start(pdf()));
    await act(async () => calls[0]!.reject({ kind: "network" }));

    act(() => hook.result.current.retry(only(hook).id));
    expect(only(hook)).toMatchObject({ status: "uploading", error: null });
    await act(async () => calls[1]!.resolve());

    expect(calls[1]!.key).toBe(calls[0]!.key);
    expect(only(hook).status).toBe("done");
  });

  it("does not retry a refusal that a retry cannot change", async () => {
    const { hook, uploadCardFile } = setup();
    act(() => hook.result.current.start(new File(["x"], "setup.exe")));

    act(() => hook.result.current.retry(only(hook).id));

    expect(uploadCardFile).not.toHaveBeenCalled();
  });

  it("uses a new key for each user action", () => {
    const { hook, calls } = setup();

    act(() => hook.result.current.start(pdf()));
    act(() => hook.result.current.start(pdf()));

    expect(calls[1]!.key).not.toBe(calls[0]!.key);
  });

  it("aborts the transfer and drops the row when it is cancelled", async () => {
    const { hook, calls } = setup();
    act(() => hook.result.current.start(pdf()));

    act(() => hook.result.current.dismiss(only(hook).id));

    expect(calls[0]!.signal.aborted).toBe(true);
    expect(hook.result.current.uploads).toEqual([]);
    await act(async () => calls[0]!.reject({ kind: "aborted" }));
    expect(hook.result.current.uploads).toEqual([]);
  });

  it("reads the item's files again after the file arrived, then drops the done row", async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
    const { hook, calls, getCardFiles } = setup();
    await act(async () => {});
    act(() => hook.result.current.start(pdf()));

    await act(async () => calls[0]!.resolve());
    expect(getCardFiles.mock.calls.length).toBeGreaterThanOrEqual(2);

    act(() => {
      vi.advanceTimersByTime(ITEM_FILE_UPLOAD_DONE_MS + 1);
    });
    expect(hook.result.current.uploads).toEqual([]);
  });
});
