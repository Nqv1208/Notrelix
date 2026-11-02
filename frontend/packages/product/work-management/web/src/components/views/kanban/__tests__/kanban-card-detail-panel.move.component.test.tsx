import { describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";
import { itemCapabilitiesStub } from "@notrelix/work-management-testing";
import {
  act,
  fireEvent,
  renderPureUi,
  screen,
  waitFor,
  within,
} from "@notrelix/testing";
import {
  WorkManagementTestScope,
  type WorkManagementServices,
} from "@notrelix/work-management-state";
import type { Board, CardDetail } from "@notrelix/work-management-core";
import { KanbanCardDetailPanel } from "../kanban-card-detail-panel";
import { itemSheetCatalog } from "../../../card-detail/item-sheet.catalog";
import type {
  TaskDetailCallbacks,
  TaskDetailCapabilities,
} from "../../../card-detail/task-detail-types";
import {
  ERP_SHEET_NOW,
  commenterSheetCapabilities,
  editorSheetCapabilities,
  erpSheetBoard,
  erpSheetDetailData,
  erpSheetGroups,
  erpSheetItem,
} from "../../../../verification/item-sheet-fixtures";

interface Setup {
  board?: Board;
  card?: CardDetail;
  capabilities?: TaskDetailCapabilities;
  groupField?: "priority";
  callbacks?: Partial<TaskDetailCallbacks>;
}

function renderPanel(setup: Setup = {}) {
  const board = setup.board ?? erpSheetBoard();
  const card = setup.card ?? erpSheetItem();
  const callbacks = {
    onClose: vi.fn(),
    onRenameTitle: vi.fn(),
    onToggleWatch: vi.fn(),
    onDuplicate: vi.fn(),
    onArchive: vi.fn(),
    onUpdateFieldValue: vi.fn(),
    onMoveToGroup: vi.fn(),
    onRefreshActivity: vi.fn(),
    onCreateUpdate: vi.fn(),
    onUpdateUpdate: vi.fn(),
    onDeleteUpdate: vi.fn(),
    onSelectTab: vi.fn(),
    ...setup.callbacks,
  };
  renderPureUi(
    <WorkManagementTestScope
      services={
        {
          itemCapabilities: itemCapabilitiesStub(),
        } as unknown as WorkManagementServices
      }
    >
      <I18nProvider catalogs={[itemSheetCatalog]} initialLocale="en">
        <KanbanCardDetailPanel
          board={board}
          card={card}
          detailData={erpSheetDetailData(card)}
          capabilities={setup.capabilities ?? editorSheetCapabilities}
          now={ERP_SHEET_NOW}
          groups={erpSheetGroups}
          groupField={
            setup.groupField
              ? board.fieldDefinitions.find(
                  (field) => field.id === "field-priority",
                )
              : undefined
          }
          callbacks={callbacks}
        />
      </I18nProvider>
    </WorkManagementTestScope>,
  );
  return callbacks;
}

/** A board with no Status and no Select field: its columns are the board's own groups. */
const groupsOnlyBoard = () => {
  const board = erpSheetBoard();
  return {
    ...board,
    fieldDefinitions: board.fieldDefinitions.filter(
      (field) => field.fieldType !== "select",
    ),
  };
};

/** The header's column picker: the first button that opens a listbox. */
const picker = () =>
  screen
    .getAllByRole("button")
    .find((button) => button.getAttribute("aria-haspopup") === "listbox")!;
const openPicker = () => fireEvent.click(picker());
const option = (name: string) => screen.getByRole("option", { name });

describe("the popup's column picker", () => {
  it("on a board grouped by Status is the Status: it marks the current lane and writes the Status", async () => {
    const callbacks = renderPanel();
    expect(picker().getAttribute("title")).toBe("Move card to another column");
    openPicker();
    expect(
      screen.getAllByRole("option").map((entry) => entry.textContent),
    ).toEqual(["Cần làm", "Đang làm", "Chờ duyệt", "Hoàn thành"]);
    expect(option("Đang làm").getAttribute("aria-selected")).toBe("true");
    fireEvent.click(option("Chờ duyệt"));
    await waitFor(() =>
      expect(callbacks.onUpdateFieldValue).toHaveBeenCalledWith({
        cardId: "item-erp-142",
        fieldDefinitionId: "field-status",
        value: "status-review",
      }),
    );
    expect(callbacks.onMoveToGroup).not.toHaveBeenCalled();
  });

  it("on a board grouped by another field changes that field and leaves the Status alone", async () => {
    const card = erpSheetItem({ fieldValues: { "field-priority": "high" } });
    const callbacks = renderPanel({ card, groupField: "priority" });
    expect(picker().textContent).toContain("Cao");
    openPicker();
    expect(option("Cao").getAttribute("aria-selected")).toBe("true");
    fireEvent.click(option("Khẩn"));
    await waitFor(() =>
      expect(callbacks.onUpdateFieldValue).toHaveBeenCalledWith({
        cardId: "item-erp-142",
        fieldDefinitionId: "field-priority",
        value: "urgent",
      }),
    );
    expect(callbacks.onMoveToGroup).not.toHaveBeenCalled();
  });

  it("on a board whose columns are its own groups offers Move to group and moves the card there", async () => {
    const callbacks = renderPanel({ board: groupsOnlyBoard() });
    expect(picker().getAttribute("title")).toBe("Move card to another group");
    openPicker();
    expect(screen.getByRole("listbox", { name: "Move to group" })).toBeTruthy();
    expect(option("Chuyển đổi dữ liệu").getAttribute("aria-selected")).toBe(
      "true",
    );
    fireEvent.click(option("Triển khai"));
    await waitFor(() =>
      expect(callbacks.onMoveToGroup).toHaveBeenCalledWith(
        "item-erp-142",
        "group-erp-rollout",
      ),
    );
    expect(callbacks.onUpdateFieldValue).not.toHaveBeenCalled();
  });

  it("choosing the column the card is in writes nothing", () => {
    const callbacks = renderPanel({ board: groupsOnlyBoard() });
    openPicker();
    fireEvent.click(option("Chuyển đổi dữ liệu"));
    expect(callbacks.onMoveToGroup).not.toHaveBeenCalled();
  });

  it("is busy while the move is saved, then free again", async () => {
    let finish: () => void = () => undefined;
    const onMoveToGroup = vi.fn(
      () => new Promise<void>((resolve) => (finish = resolve)),
    );
    renderPanel({ board: groupsOnlyBoard(), callbacks: { onMoveToGroup } });
    openPicker();
    fireEvent.click(option("Triển khai"));
    await waitFor(() =>
      expect(picker().getAttribute("aria-busy")).toBe("true"),
    );
    expect((picker() as HTMLButtonElement).disabled).toBe(true);
    expect(within(picker()).getByText("Saving…")).toBeTruthy();
    await act(async () => finish());
    await waitFor(() =>
      expect(picker().getAttribute("aria-busy")).toBe("false"),
    );
    expect((picker() as HTMLButtonElement).disabled).toBe(false);
  });

  it("says someone else saved first when the version is refused, and offers no blind retry", async () => {
    const onMoveToGroup = vi.fn(() =>
      Promise.reject({ kind: "conflict", status: 412 }),
    );
    renderPanel({ board: groupsOnlyBoard(), callbacks: { onMoveToGroup } });
    openPicker();
    fireEvent.click(option("Triển khai"));
    const notice = await screen.findByRole("alert");
    expect(notice.textContent).toContain("just edited this card");
    expect(
      within(notice).queryByRole("button", { name: "Try again" }),
    ).toBeNull();
  });

  it("when offline says the card stays where it was and retries on Try again", async () => {
    const onMoveToGroup = vi
      .fn()
      .mockRejectedValueOnce({ kind: "network" })
      .mockResolvedValue(undefined);
    renderPanel({ board: groupsOnlyBoard(), callbacks: { onMoveToGroup } });
    openPicker();
    fireEvent.click(option("Triển khai"));
    const notice = await screen.findByRole("alert");
    expect(notice.textContent).toContain("Could not move the card");
    expect(notice.textContent).toContain("You are offline");
    fireEvent.click(within(notice).getByRole("button", { name: "Try again" }));
    await waitFor(() => expect(onMoveToGroup).toHaveBeenCalledTimes(2));
    await waitFor(() => expect(screen.queryByRole("alert")).toBeNull());
  });

  it("when the server says forbidden names the refusal and does not offer a retry", async () => {
    const onMoveToGroup = vi.fn(() =>
      Promise.reject({ kind: "forbidden", status: 403 }),
    );
    renderPanel({ board: groupsOnlyBoard(), callbacks: { onMoveToGroup } });
    openPicker();
    fireEvent.click(option("Triển khai"));
    const notice = await screen.findByRole("alert");
    expect(notice.textContent).toContain("no longer have the right");
    expect(within(notice).queryByRole("button")).toBeNull();
  });

  it("without the right to move is shown as a label, never a control, and the notice says the card is view-only", () => {
    renderPanel({ capabilities: commenterSheetCapabilities });
    expect(screen.queryByTitle(/^Move card to another/)).toBeNull();
    expect(screen.getAllByText("Đang làm").length).toBeGreaterThan(0);
    expect(screen.getByText("You can only view this card")).toBeTruthy();
  });
});
