import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import { act, renderHook, waitFor } from "@notrelix/testing";
import {
  WorkManagementTestScope,
  useCardFiles,
  useDeleteCardFile,
  type WorkManagementServices,
} from "@notrelix/work-management-state";

function setup(deleteCardFile: ReturnType<typeof vi.fn>) {
  const getCardFiles = vi.fn(async () => []);
  const services = {
    cards: { deleteCardFile, getCardFiles },
  } as unknown as WorkManagementServices;
  const wrapper = ({ children }: { children: ReactNode }) => (
    <WorkManagementTestScope services={services}>
      {children}
    </WorkManagementTestScope>
  );
  // The item's open file list: a read again shows as one more read.
  const hook = renderHook(
    () => {
      useCardFiles("item-1", "ws-1");
      return useDeleteCardFile("item-1", "ws-1");
    },
    { wrapper },
  );
  return { hook, getCardFiles };
}

describe("useDeleteCardFile (UC-COL-B11)", () => {
  it("deletes the attachment and reads the files again", async () => {
    const deleteCardFile = vi.fn(async () => undefined);
    const { hook, getCardFiles } = setup(deleteCardFile);
    await waitFor(() => expect(getCardFiles).toHaveBeenCalledTimes(1));

    await act(() => hook.result.current.mutateAsync("att-1"));

    expect(deleteCardFile).toHaveBeenCalledWith("item-1", "att-1");
    await waitFor(() => expect(getCardFiles).toHaveBeenCalledTimes(2));
  });

  it.each([403, 404])(
    "reads the files again after a %s so the list shows what the server holds",
    async (status) => {
      const deleteCardFile = vi.fn(async () => {
        throw { status };
      });
      const { hook, getCardFiles } = setup(deleteCardFile);
      await waitFor(() => expect(getCardFiles).toHaveBeenCalledTimes(1));

      await act(() => hook.result.current.mutateAsync("att-1").catch(() => {}));

      await waitFor(() => expect(hook.result.current.isError).toBe(true));
      await waitFor(() => expect(getCardFiles).toHaveBeenCalledTimes(2));
    },
  );
});
