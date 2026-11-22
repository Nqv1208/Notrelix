import { describe, expect, it, vi } from "vitest";
import { fireEvent, screen, within } from "@notrelix/testing";
import { en as pageCopy } from "../../../item-page/item-page.copy";
import { showSection, T } from "./attachments-fixtures";

// The linked pages of the item are read from the server; these tests are about the files only.
vi.mock("@notrelix/work-management-state", async (importOriginal) => ({
  ...(await importOriginal<typeof import("@notrelix/work-management-state")>()),
  useItemLinkedPages: () => ({
    pages: [],
    hiddenCount: 0,
    isError: false,
    isLoading: false,
  }),
}));

const upload = (extra: Record<string, unknown>) => ({
  id: "up-1",
  name: "so-lieu.xlsx",
  size: 10,
  status: "uploading",
  progress: 40,
  error: null,
  retryable: false,
  ...extra,
});

describe("item attachments: uploads on their way (UC-COL-B06)", () => {
  it("shows the percent as a polite status on a busy row", () => {
    showSection({ data: { uploads: [upload({})] } });

    expect(screen.getByRole("status").textContent).toBe("Uploading 40%");
    const row = screen.getByText("so-lieu.xlsx").closest("[role=listitem]");
    expect(row?.getAttribute("aria-busy")).toBe("true");
  });

  it("says only Uploading when the transfer reports no progress", () => {
    showSection({ data: { uploads: [upload({ progress: null })] } });

    expect(screen.getByRole("status").textContent).toBe(T.uploadingPlain);
  });

  it("cancels a sending file: the control aborts through the dismiss callback", () => {
    const { callbacks } = showSection({ data: { uploads: [upload({})] } });

    fireEvent.click(
      screen.getByRole("button", { name: "Cancel the upload of so-lieu.xlsx" }),
    );

    expect(callbacks.onDismissUpload).toHaveBeenCalledWith("up-1");
  });

  it.each([
    ["tooBig", pageCopy.files.tooBig.replace("{{limit}}", "25 MB"), false],
    ["blockedType", pageCopy.files.blockedType, false],
    ["serverTooLarge", pageCopy.files.serverTooLarge, false],
    ["unsupportedType", pageCopy.files.unsupportedType, false],
    ["limitReached", pageCopy.files.limitReached, true],
    ["forbidden", pageCopy.files.forbidden, false],
    ["notFound", pageCopy.files.notFound, false],
    ["failed", pageCopy.files.failed, true],
  ] as const)(
    "tells %s as an alert, with Retry only when a retry can change it",
    (error, message, retryable) => {
      const { callbacks } = showSection({
        data: {
          uploads: [
            upload({
              status: "error",
              progress: null,
              error,
              retryable,
              limit: "25 MB",
            }),
          ],
        },
      });

      expect(screen.getByRole("alert").textContent).toBe(message);
      const retry = screen.queryByRole("button", { name: T.retry });
      expect(Boolean(retry)).toBe(retryable);
      if (retry) {
        fireEvent.click(retry);
        expect(callbacks.onRetryUpload).toHaveBeenCalledWith("up-1");
      }
    },
  );

  it("lets a failed row be dismissed", () => {
    const { callbacks } = showSection({
      data: {
        uploads: [upload({ status: "error", error: "blockedType" })],
      },
    });

    fireEvent.click(
      screen.getByRole("button", { name: "Cancel the upload of so-lieu.xlsx" }),
    );

    expect(callbacks.onDismissUpload).toHaveBeenCalledWith("up-1");
  });

  it("offers a viewer no Add button but still lists what is already attached", () => {
    showSection({ canEdit: false, data: { uploads: [upload({})] } });

    expect(screen.queryByRole("button", { name: T.addLabel })).toBeNull();
    expect(
      within(screen.getByRole("list", { name: T.title })).getAllByRole(
        "listitem",
      ),
    ).toHaveLength(1);
  });
});
