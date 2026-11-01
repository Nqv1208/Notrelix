import { describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";
import { itemCapabilitiesStub } from "@notrelix/work-management-testing";
import { renderPureUi, screen } from "@notrelix/testing";
import {
  WorkManagementTestScope,
  type WorkManagementServices,
} from "@notrelix/work-management-state";
import { KanbanCardDetailPanel } from "../kanban-card-detail-panel";
import { itemSheetCatalog } from "../../../card-detail/item-sheet.catalog";
import {
  ERP_SHEET_NOW,
  commenterSheetCapabilities,
  editorSheetCapabilities,
  erpSheetBoard,
  erpSheetDetailData,
  erpSheetItem,
} from "../../../../verification/item-sheet-fixtures";
import type { TaskDetailCapabilities } from "../../../card-detail/task-detail-types";

function renderPanel(
  capabilities: TaskDetailCapabilities,
  overrides: { isArchived?: boolean } = {},
) {
  const item = { ...erpSheetItem(), ...overrides };
  return renderPureUi(
    <WorkManagementTestScope
      services={
        {
          itemCapabilities: itemCapabilitiesStub(),
        } as unknown as WorkManagementServices
      }
    >
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
      </I18nProvider>
    </WorkManagementTestScope>,
  );
}

describe("KanbanCardDetailPanel capabilities", () => {
  it("passes a commenter's capabilities to the popup: read-only card, comment composer kept", () => {
    renderPanel(commenterSheetCapabilities);
    expect(
      (screen.getByRole("textbox", { name: "Tên thẻ" }) as HTMLTextAreaElement)
        .readOnly,
    ).toBe(true);
    expect(screen.queryByRole("button", { name: "Thêm" })).toBeNull();
    expect(
      screen.getAllByRole("button", { name: "Viết bình luận…" }).length,
    ).toBeGreaterThan(0);
  });

  it("lets an editor change the card", () => {
    renderPanel(editorSheetCapabilities);
    expect(
      (screen.getByRole("textbox", { name: "Tên thẻ" }) as HTMLTextAreaElement)
        .readOnly,
    ).toBe(false);
    const status = screen.getByRole("button", { name: /Đang làm/ });
    expect(status.getAttribute("aria-disabled")).toBeNull();
    expect(status.getAttribute("aria-haspopup")).toBe("listbox");
    expect(screen.getByRole("button", { name: "Thêm" })).toBeTruthy();
  });

  it("reads an archived card: view-only, with the archived notice, an editor included", () => {
    renderPanel(editorSheetCapabilities, { isArchived: true });
    expect(screen.getByText("Thẻ này đã lưu trữ")).toBeTruthy();
    expect(
      (screen.getByRole("textbox", { name: "Tên thẻ" }) as HTMLTextAreaElement)
        .readOnly,
    ).toBe(true);
    expect(screen.queryByRole("button", { name: "Thêm" })).toBeNull();
  });
});
