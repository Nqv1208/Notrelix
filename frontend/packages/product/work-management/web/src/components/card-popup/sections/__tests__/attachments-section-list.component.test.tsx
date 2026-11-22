import { describe, expect, it } from "vitest";
import { fireEvent, screen, within } from "@notrelix/testing";
import {
  fileOf,
  linkOf,
  openOptions,
  showSection,
  T,
} from "./attachments-fixtures";

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

describe("item attachments: list and open (UC-COL-B10, item side)", () => {
  it("shows the loading state while the first read is on its way", () => {
    showSection({ data: { filesLoading: true } });

    expect(screen.getByRole("status", { name: T.loading })).toBeTruthy();
    expect(screen.queryByRole("list")).toBeNull();
  });

  it("folds to Nothing yet with an Add action when nothing is attached", () => {
    const { openPicker } = showSection();

    expect(screen.getByText(/Nothing yet/)).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: T.addLabel }));
    expect(openPicker).toHaveBeenCalledWith("attach", expect.any(HTMLElement));
  });

  it("lists files and links as a named list of items", () => {
    showSection({
      data: {
        files: [fileOf(), linkOf("https://example.com/spec", "Spec")],
      },
    });

    const list = screen.getByRole("list", { name: T.title });
    expect(within(list).getAllByRole("listitem")).toHaveLength(2);
  });

  it("downloads a file through the download callback", () => {
    const file = fileOf();
    const { callbacks } = showSection({ data: { files: [file] } });

    fireEvent.click(
      screen.getByRole("button", { name: "Download bao-cao.pdf" }),
    );

    expect(callbacks.onDownloadFile).toHaveBeenCalledWith(file);
  });

  it("shows a download failure with a Dismiss control that clears it", () => {
    const { callbacks } = showSection({
      data: {
        files: [fileOf()],
        fileNotice: "Could not download bao-cao.pdf. Try again.",
      },
    });

    expect(screen.getByRole("alert").textContent).toContain(
      "Could not download bao-cao.pdf",
    );
    fireEvent.click(screen.getByRole("button", { name: T.dismiss }));
    expect(callbacks.onDismissFileNotice).toHaveBeenCalledTimes(1);
  });

  it("says who is unknown in words when the author is missing", () => {
    showSection({
      data: { files: [fileOf({ createdBy: { id: "u", name: "" } as never })] },
    });

    expect(screen.getByText(/· someone$/)).toBeTruthy();
  });

  describe("a failed read of the files", () => {
    it("shows an alert with Retry and keeps the list it has", () => {
      const { callbacks } = showSection({
        data: { files: [fileOf()], filesError: "failed" },
      });

      expect(screen.getByRole("alert").textContent).toContain(T.filesError);
      expect(screen.getByText("bao-cao.pdf")).toBeTruthy();
      fireEvent.click(screen.getByRole("button", { name: T.retry }));
      expect(callbacks.onRefreshFiles).toHaveBeenCalledTimes(1);
    });

    it("is not the Nothing yet empty state when nothing was read", () => {
      showSection({ data: { filesError: "failed" } });

      expect(screen.queryByText(/Nothing yet/)).toBeNull();
      expect(screen.getByRole("alert").textContent).toContain(T.filesError);
    });

    it.each([
      ["forbidden", T.filesForbidden],
      ["notFound", T.filesGone],
    ] as const)(
      "tells %s, hides the stale rows and offers neither Retry nor Add",
      (filesError, message) => {
        showSection({ data: { files: [fileOf()], filesError } });

        expect(screen.getByRole("alert").textContent).toBe(message);
        expect(screen.queryByText("bao-cao.pdf")).toBeNull();
        expect(screen.queryByRole("button", { name: T.retry })).toBeNull();
        expect(screen.queryByRole("button", { name: T.addLabel })).toBeNull();
      },
    );
  });

  describe("a link attachment (UC-COL-B07, item side)", () => {
    it("is an anchor only for an absolute http or https address", () => {
      showSection({
        data: {
          files: [
            linkOf("https://example.com/a", "Safe"),
            linkOf("javascript:alert(1)", "Script"),
            linkOf("/relative/path", "Relative"),
            linkOf("ftp://example.com/x", "Ftp"),
          ],
        },
      });

      expect(
        screen.getByRole("link", { name: "Safe" }).getAttribute("href"),
      ).toBe("https://example.com/a");
      for (const name of ["Script", "Relative", "Ftp"]) {
        expect(screen.getByText(name)).toBeTruthy();
        expect(screen.queryByRole("link", { name })).toBeNull();
      }
    });

    it("offers Open link in the menu for a safe address only", async () => {
      showSection({
        data: {
          files: [
            linkOf("https://example.com/a", "Safe"),
            linkOf("javascript:alert(1)", "Script"),
          ],
        },
      });

      openOptions("Safe");
      expect(
        await screen.findByRole("menuitem", { name: T.openLink }),
      ).toBeTruthy();
    });

    it("offers an unsafe link no Open link item", () => {
      showSection({
        canEdit: false,
        data: { files: [linkOf("javascript:alert(1)", "Script")] },
      });

      expect(
        screen.queryByRole("button", { name: "Options for Script" }),
      ).toBeNull();
    });
  });
});
