import { describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";
import { act, fireEvent, renderPureUi, screen } from "@notrelix/testing";
import type { CardFile } from "@notrelix/work-management-core";
import { en } from "../../card-popup.copy";
import {
  CardPopupProvider,
  type CardPopupModel,
} from "../../card-popup-context";
import { AttachmentsSection } from "../attachments-section";

// The linked pages of the item are read from the server; this test is about the files only.
vi.mock("@notrelix/work-management-state", async (importOriginal) => ({
  ...(await importOriginal<typeof import("@notrelix/work-management-state")>()),
  useItemLinkedPages: () => ({ pages: [], isError: false, isLoading: false }),
}));

const FILE = {
  id: "file-1",
  cardId: "item-1",
  name: "bao-cao.pdf",
  size: 2048,
  contentType: "application/pdf",
  url: "",
  source: "upload",
  kind: "file",
  createdBy: { id: "u-1", name: "Ada" },
  createdAt: "2026-11-01T09:00:00Z",
} as unknown as CardFile;

function show(canEdit: boolean) {
  const onDeleteFile = vi.fn(async () => true);
  const model = {
    mode: "view",
    copy: en,
    card: { id: "item-1", workspaceId: "ws-1" },
    readOnly: canEdit ? null : { reason: "viewer" },
    capabilities: { canEditFields: canEdit },
    timeZone: "UTC",
    openPicker: vi.fn(),
    callbacks: { onDeleteFile, onDownloadFile: vi.fn() },
    data: {
      files: [FILE],
      filesLoading: false,
      uploads: [],
      fileNotice: null,
    },
  } as unknown as CardPopupModel;
  renderPureUi(
    <I18nProvider catalogs={[]} initialLocale="en">
      <CardPopupProvider value={model}>
        <AttachmentsSection />
      </CardPopupProvider>
    </I18nProvider>,
  );
  return { onDeleteFile };
}

function openOptions() {
  const trigger = screen.getByRole("button", {
    name: "Options for bao-cao.pdf",
  });
  const pointerDown = new MouseEvent("pointerdown", {
    bubbles: true,
    button: 0,
  });
  Object.defineProperty(pointerDown, "isPrimary", { value: true });
  act(() => {
    fireEvent(trigger, pointerDown);
  });
}

describe("item attachments: Delete is editor-only (UC-COL-B11)", () => {
  it("offers Delete in a file's options to an editor, and deleting after confirmation calls the delete", async () => {
    const { onDeleteFile } = show(true);

    openOptions();
    fireEvent.click(await screen.findByRole("menuitem", { name: "Delete" }));
    fireEvent.click(
      await screen.findByRole("button", {
        name: en.related.attachments.deleteConfirm,
      }),
    );

    expect(onDeleteFile).toHaveBeenCalledWith(FILE);
  });

  it("offers no options menu and no Delete to a viewer", () => {
    show(false);

    expect(screen.getByText("bao-cao.pdf")).toBeTruthy();
    expect(
      screen.queryByRole("button", { name: "Options for bao-cao.pdf" }),
    ).toBeNull();
    expect(screen.queryByRole("menuitem", { name: "Delete" })).toBeNull();
  });
});
