import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import { act, renderHook } from "@notrelix/testing";
import {
  WorkManagementTestScope,
  useItemFileUploads,
  type WorkManagementServices,
} from "@notrelix/work-management-state";

function setup(uploadCardFile: ReturnType<typeof vi.fn>) {
  const services = {
    cards: { uploadCardFile },
  } as unknown as WorkManagementServices;
  const wrapper = ({ children }: { children: ReactNode }) => (
    <WorkManagementTestScope services={services}>
      {children}
    </WorkManagementTestScope>
  );
  return wrapper;
}

describe("useItemFileUploads when the sheet closes", () => {
  it("lets a sending file finish and reports its result", async () => {
    let finish!: () => void;
    let signal: AbortSignal | undefined;
    const upload = vi.fn(
      (_id: string, _file: File, options: { signal: AbortSignal }) => {
        signal = options.signal;
        return new Promise<void>((resolve) => (finish = resolve));
      },
    );
    const onFinished = vi.fn();
    const hook = renderHook(
      () => useItemFileUploads("item-1", "ws-1", onFinished),
      { wrapper: setup(upload) },
    );
    act(() => hook.result.current.start(new File(["x"], "bao-cao.pdf")));
    hook.unmount();
    expect(signal?.aborted).toBe(false);
    await act(async () => finish());
    expect(onFinished).toHaveBeenCalledWith({
      name: "bao-cao.pdf",
      error: null,
    });
  });

  it("reports a failure after close with its reason", async () => {
    let fail!: (error: unknown) => void;
    const upload = vi.fn(
      () => new Promise<void>((_resolve, reject) => (fail = reject)),
    );
    const onFinished = vi.fn();
    const hook = renderHook(
      () => useItemFileUploads("item-1", "ws-1", onFinished),
      { wrapper: setup(upload) },
    );
    act(() => hook.result.current.start(new File(["x"], "bao-cao.pdf")));
    hook.unmount();
    await act(async () => fail({ status: 413 }));
    expect(onFinished).toHaveBeenCalledWith({
      name: "bao-cao.pdf",
      error: "serverTooLarge",
    });
  });
});
