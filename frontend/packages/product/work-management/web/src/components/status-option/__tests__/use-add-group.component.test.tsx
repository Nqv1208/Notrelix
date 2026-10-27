import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ReactNode } from "react";
import { I18nProvider } from "@notrelix/i18n";
import { act, renderHook } from "@notrelix/testing";
import { useNoGroupNotice } from "../../item-create/use-no-group-notice";
import { useAddGroup } from "../use-add-group";

const mutateAsync = vi.fn();
const toast = vi.hoisted(() => {
  const fn = vi.fn() as ReturnType<typeof vi.fn> & {
    success: ReturnType<typeof vi.fn>;
    error: ReturnType<typeof vi.fn>;
  };
  fn.success = vi.fn();
  fn.error = vi.fn();
  return fn;
});

vi.mock("sonner", () => ({ toast }));
vi.mock("@notrelix/work-management-state", () => ({
  useCreateGroup: () => ({ mutateAsync, isPending: false }),
}));

function wrapper({ children }: { children: ReactNode }) {
  return (
    <I18nProvider catalogs={[]} initialLocale="vi">
      {children}
    </I18nProvider>
  );
}

beforeEach(() => {
  mutateAsync.mockReset();
  toast.success.mockReset();
  toast.error.mockReset();
});

describe("useAddGroup", () => {
  it("creates the group with its colour and names it", async () => {
    mutateAsync.mockResolvedValue(undefined);
    const { result } = renderHook(() => useAddGroup("b-1", "ws-1"), {
      wrapper,
    });
    await act(() =>
      result.current.addGroup({ title: "Sprint 2", color: "#C8143F" }),
    );
    expect(mutateAsync).toHaveBeenCalledWith({
      title: "Sprint 2",
      color: "#C8143F",
    });
    expect(toast.success).toHaveBeenCalledWith(
      expect.stringContaining("Sprint 2"),
    );
  });

  it("says it failed and rejects so a form can stay open", async () => {
    mutateAsync.mockRejectedValue(new Error("refused"));
    const { result } = renderHook(() => useAddGroup("b-1", "ws-1"), {
      wrapper,
    });
    await expect(
      act(() => result.current.addGroup({ title: "Sprint 2" })),
    ).rejects.toThrow("refused");
    expect(toast.error).toHaveBeenCalledTimes(1);
    expect(toast.success).not.toHaveBeenCalled();
  });

  it("stays quiet for a caller that reports the result itself", async () => {
    mutateAsync.mockResolvedValue(undefined);
    const { result } = renderHook(() => useAddGroup("b-1", "ws-1"), {
      wrapper,
    });
    await act(() => result.current.addGroup({ title: "Sprint 2" }, false));
    expect(toast.success).not.toHaveBeenCalled();
    expect(toast.error).not.toHaveBeenCalled();
  });
});

describe("useNoGroupNotice", () => {
  it("offers a group through the same create flow", async () => {
    mutateAsync.mockResolvedValue(undefined);
    const { result } = renderHook(() => useNoGroupNotice("b-1", "ws-1"), {
      wrapper,
    });
    act(() => result.current.noGroup());
    const [, options] = toast.error.mock.calls[0]!;
    await act(async () => options.action.onClick());
    expect(mutateAsync).toHaveBeenCalledWith({ title: expect.any(String) });
    expect(toast.success).toHaveBeenCalledTimes(1);
  });
});
