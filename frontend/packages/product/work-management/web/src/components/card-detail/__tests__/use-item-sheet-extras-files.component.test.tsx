import type { ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";
import { act, renderHook, waitFor } from "@notrelix/testing";
import {
  WorkManagementTestScope,
  type WorkManagementServices,
} from "@notrelix/work-management-state";
import type { CardDetail, CardFile } from "@notrelix/work-management-core";
import { en as pageCopy } from "../../item-page/item-page.copy";
import { useItemSheetExtras } from "../use-item-sheet-extras";

// The other reads of the sheet are not under test: they answer nothing.
vi.mock("@notrelix/work-management-state", async (importOriginal) => ({
  ...(await importOriginal<typeof import("@notrelix/work-management-state")>()),
  useItemFollowing: () => ({
    following: false,
    followers: [],
    setFollowing: vi.fn(),
  }),
  useItemChildItems: () => ({ data: [], toggle: vi.fn(), addChild: vi.fn() }),
  usePeopleDirectory: () => ({ directory: undefined }),
  useCreateItemField: () => vi.fn(),
}));
const saveBlobAs = vi.fn();
vi.mock("../../item-page/save-blob", () => ({
  saveBlobAs: (...args: unknown[]) => saveBlobAs(...args),
}));
vi.mock("sonner", () => ({ toast: { success: vi.fn(), error: vi.fn() } }));

const CARD = {
  id: "item-1",
  workspaceId: "ws-1",
  boardId: "b-1",
  title: "Plan",
} as unknown as CardDetail;
const FILE = { id: "att-1", name: "bao-cao.pdf" } as unknown as CardFile;

function setup(cards: Record<string, unknown>) {
  const services = { cards } as unknown as WorkManagementServices;
  const wrapper = ({ children }: { children: ReactNode }) => (
    <I18nProvider catalogs={[]} initialLocale="en">
      <WorkManagementTestScope services={services}>
        {children}
      </WorkManagementTestScope>
    </I18nProvider>
  );
  return renderHook(() => useItemSheetExtras(CARD), { wrapper });
}

beforeEach(() => saveBlobAs.mockReset());

describe("useItemSheetExtras: files of the item sheet (UC-COL-B10, B11)", () => {
  it("saves the downloaded bytes under the file's name and tells no failure", async () => {
    const blob = new Blob(["x"]);
    const downloadCardFile = vi.fn(async () => blob);
    const hook = setup({ downloadCardFile });

    act(() => hook.result.current.callbacks.onDownloadFile?.(FILE));

    await waitFor(() =>
      expect(saveBlobAs).toHaveBeenCalledWith(blob, "bao-cao.pdf"),
    );
    expect(downloadCardFile).toHaveBeenCalledWith("item-1", "att-1");
    expect(hook.result.current.detailData.fileNotice).toBeNull();
  });

  it("words a failed download with the file's name and clears it on dismiss", async () => {
    const hook = setup({
      downloadCardFile: vi.fn(async () => {
        throw new Error("network");
      }),
    });

    act(() => hook.result.current.callbacks.onDownloadFile?.(FILE));

    await waitFor(() =>
      expect(hook.result.current.detailData.fileNotice).toBe(
        pageCopy.files.downloadFailed.replace("{{name}}", "bao-cao.pdf"),
      ),
    );
    expect(saveBlobAs).not.toHaveBeenCalled();
    act(() => hook.result.current.callbacks.onDismissFileNotice?.());
    expect(hook.result.current.detailData.fileNotice).toBeNull();
  });

  it("clears an old failure when the next download starts", async () => {
    const downloadCardFile = vi
      .fn()
      .mockRejectedValueOnce(new Error("network"))
      .mockResolvedValueOnce(new Blob(["x"]));
    const hook = setup({ downloadCardFile });

    act(() => hook.result.current.callbacks.onDownloadFile?.(FILE));
    await waitFor(() =>
      expect(hook.result.current.detailData.fileNotice).not.toBeNull(),
    );
    act(() => hook.result.current.callbacks.onDownloadFile?.(FILE));

    await waitFor(() => expect(saveBlobAs).toHaveBeenCalledTimes(1));
    expect(hook.result.current.detailData.fileNotice).toBeNull();
  });

  it("answers a delete with true when it went through and false when it was refused", async () => {
    const deleteCardFile = vi
      .fn()
      .mockResolvedValueOnce(undefined)
      .mockRejectedValueOnce({ status: 403 });
    const hook = setup({ deleteCardFile });

    const ok = await act(() =>
      hook.result.current.callbacks.onDeleteFile!(FILE),
    );
    const refused = await act(() =>
      hook.result.current.callbacks.onDeleteFile!(FILE),
    );

    expect([ok, refused]).toEqual([true, false]);
  });
});
