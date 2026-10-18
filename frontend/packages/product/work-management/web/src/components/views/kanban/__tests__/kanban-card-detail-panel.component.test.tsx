import { describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";
import { renderPureUi, screen, within } from "@notrelix/testing";
import { KanbanCardDetailPanel } from "../kanban-card-detail-panel";
import { itemSheetCatalog } from "../../../card-detail/item-sheet.catalog";
import { vi as viCopy } from "../../../card-detail/item-sheet.copy";
import {
  ERP_SHEET_NOW,
  commenterSheetCapabilities,
  editorSheetCapabilities,
  erpSheetBoard,
  erpSheetDetailData,
  erpSheetItem,
} from "../../../../verification/item-sheet-fixtures";
import type { TaskDetailCapabilities } from "../../../card-detail/task-detail-types";

function renderPanel(capabilities: TaskDetailCapabilities) {
  const item = erpSheetItem();
  return renderPureUi(
    <I18nProvider catalogs={[itemSheetCatalog]} initialLocale="vi">
      <KanbanCardDetailPanel
        board={erpSheetBoard()}
        card={item}
        detailData={erpSheetDetailData(item)}
        capabilities={capabilities}
        now={ERP_SHEET_NOW}
        callbacks={{
          onClose: vi.fn(),
          onRenameTitle: vi.fn(),
          onToggleWatch: vi.fn(),
          onDuplicate: vi.fn(),
          onArchive: vi.fn(),
          onUpdateFieldValue: vi.fn(),
          onRefreshActivity: vi.fn(),
          onCreateUpdate: vi.fn(),
          onUpdateUpdate: vi.fn(),
          onDeleteUpdate: vi.fn(),
          onSelectTab: vi.fn(),
        }}
      />
    </I18nProvider>,
  );
}

describe("KanbanCardDetailPanel capabilities", () => {
  it("passes a commenter's capabilities to the sheet: read-only properties, composer kept", () => {
    renderPanel(commenterSheetCapabilities);
    const properties = screen.getByRole("group", {
      name: viCopy.properties.group,
    });
    for (const pill of within(properties).getAllByRole("button")) {
      expect(pill.getAttribute("aria-disabled")).toBe("true");
    }
    expect(
      (
        screen.getByRole("textbox", {
          name: viCopy.composer.label,
        }) as HTMLTextAreaElement
      ).disabled,
    ).toBe(false);
  });

  it("lets an editor change properties", () => {
    renderPanel(editorSheetCapabilities);
    const status = screen.getByRole("button", { name: /Đang làm/ });
    expect(status.getAttribute("aria-disabled")).toBeNull();
    expect(status.getAttribute("aria-haspopup")).toBe("menu");
  });
});
