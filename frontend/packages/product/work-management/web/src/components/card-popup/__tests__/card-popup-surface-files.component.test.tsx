import { describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";
import {
  createEvent,
  fireEvent,
  renderPureUi,
  screen,
} from "@notrelix/testing";
import { en } from "../card-popup.copy";
import type { CardPopupModel } from "../card-popup-context";
import { CardPopupSurface } from "../card-popup-surface";

// Only the surface's own drop and paste handling is under test: its parts are stubs.
vi.mock("../states/popup-states", () => ({
  PopupBanners: () => null,
  PopupDeleted: () => null,
  PopupGone: () => null,
  PopupLoadError: () => null,
  PopupMoved: () => null,
  PopupSkeleton: () => null,
}));
vi.mock("../popup-header", () => ({ PopupHeader: () => null }));
vi.mock("../popup-title-row", () => ({ PopupTitleRow: () => null }));
vi.mock("../popup-quick-row", () => ({ PopupQuickRow: () => null }));
vi.mock("../chips-row", () => ({ ChipsRow: () => null }));
vi.mock("../sections/description-section", () => ({
  DescriptionSection: () => null,
}));
vi.mock("../sections/fields-section", () => ({ FieldsSection: () => null }));
vi.mock("../sections/checklists-section", () => ({
  ChecklistsSection: () => null,
}));
vi.mock("../sections/attachments-section", () => ({
  AttachmentsSection: () => <input aria-label="inside the card" />,
}));
vi.mock("../sections/links-section", () => ({ LinksSection: () => null }));
vi.mock("../approval/approval-section", () => ({
  ApprovalSection: () => null,
}));
vi.mock("../comments/comments-column", () => ({
  CommentsBar: () => null,
  CommentsColumn: () => null,
}));

function show(canEdit: boolean) {
  const onAddFile = vi.fn();
  const model = {
    mode: "view",
    copy: en,
    card: { id: "item-1", title: "Plan" },
    readOnly: canEdit ? null : "permission",
    capabilities: { canEditFields: canEdit },
    callbacks: { onAddFile },
    slots: {},
  } as unknown as CardPopupModel;
  renderPureUi(
    <I18nProvider catalogs={[]} initialLocale="en">
      <CardPopupSurface
        status="ready"
        model={model}
        presentation="pane"
        onClose={vi.fn()}
      />
    </I18nProvider>,
  );
  return { onAddFile, target: screen.getByLabelText("inside the card") };
}

const FILES = [new File(["a"], "a.pdf"), new File(["b"], "b.png")];

/** fireEvent answers false when the handler called preventDefault. */
function drop(target: HTMLElement, files: File[]) {
  return !fireEvent.drop(target, { dataTransfer: { files, types: ["Files"] } });
}

function paste(target: HTMLElement, files: File[]) {
  return !fireEvent.paste(target, { clipboardData: { files } });
}

describe("card popup: dropping and pasting files (UC-COL-B06)", () => {
  it("attaches each file an editor drops on the card", () => {
    const { onAddFile, target } = show(true);

    const prevented = drop(target, FILES);

    expect(onAddFile.mock.calls.map((call) => call[0])).toEqual(FILES);
    expect(prevented).toBe(true);
  });

  it("attaches each file an editor pastes on the card", () => {
    const { onAddFile, target } = show(true);

    paste(target, FILES);

    expect(onAddFile.mock.calls.map((call) => call[0])).toEqual(FILES);
  });

  it("ignores a drop or paste without files, so pasted text still pastes", () => {
    const { onAddFile, target } = show(true);

    drop(target, []);
    const prevented = paste(target, []);

    expect(onAddFile).not.toHaveBeenCalled();
    expect(prevented).toBe(false);
  });

  it("ignores a viewer's drop and paste", () => {
    const { onAddFile, target } = show(false);

    const prevented = drop(target, FILES);
    paste(target, FILES);

    expect(onAddFile).not.toHaveBeenCalled();
    expect(prevented).toBe(false);
  });
});
