import { describe, expect, it, vi } from "vitest";
import {
  act,
  fireEvent,
  renderPureUi,
  screen,
  waitFor,
  within,
} from "@notrelix/testing";

import {
  DRILL_QUAN,
  PEOPLE,
  REPORTS_FIXTURE_COPY as copy,
  dashboardScenario,
  emptyReport,
  erpReport,
  listScenario,
  previewColumns,
  viewerReport,
  weeklyReport,
  zeroReport,
} from "../../../verification/workspace-reports-fixtures";
import { ReportDashboardSurface } from "../report-dashboard-surface";
import { ReportListSurface } from "../report-list-surface";

function openMenu(trigger: HTMLElement) {
  fireEvent.keyDown(trigger, { key: "Enter" });
}

const widgetOf = (title: string) =>
  screen.getByRole("heading", { name: title, level: 2 }).closest("section")!;

const order = () =>
  screen
    .getAllByRole("heading", { level: 2 })
    .map((heading) => heading.textContent);

describe("ReportDashboardSurface: view", () => {
  it("FUI[workspace.report-dashboard:view] draws the report header, its summary and every widget the way the mockup does", () => {
    renderPureUi(<ReportDashboardSurface {...dashboardScenario()} />);
    expect(
      screen.getByRole("heading", { level: 1, name: "Tổng quan triển khai ERP Q4" }),
    ).toBeTruthy();
    expect(screen.getByText("Lê Hoàng Phúc tạo")).toBeTruthy();
    expect(screen.getByText("Chia sẻ với Phòng Công nghệ, Ban giám đốc")).toBeTruthy();
    expect(screen.getByText(/Cập nhật 10:38 · số liệu đến 10:37 \(GMT\+7\)/)).toBeTruthy();
    const summary = screen.getByRole("list", {
      name: "Tóm tắt board Triển khai ERP Q4, số liệu đến 10:37",
    });
    expect(summary.textContent).toContain("5/16 việc hoàn thành · 31%");
    expect(summary.textContent).toContain("1 việc quá hạn");
    expect(summary.textContent).toContain("4 việc đến hạn trong 7 ngày");
    expect(summary.textContent).toContain("20 ngày tới go-live 01/11");
    expect(order()).toEqual([
      "Theo trạng thái",
      "Tốc độ hoàn thành",
      "Theo phân hệ",
      "Theo mức ưu tiên",
      "Đến hạn trong 7 ngày",
      "Khối lượng theo người phụ trách",
    ]);
    const velocity = within(widgetOf("Tốc độ hoàn thành"));
    expect(velocity.getByRole("img").getAttribute("aria-label")).toBe(
      "Tốc độ hoàn thành theo tuần: 21–27/09 2, 28/09–04/10 2, 05–11/10 1, tuần này 12–18/10 0 việc tính đến 10:37. Trung bình 3 tuần gần nhất 1,7 việc mỗi tuần. Nhịp cần từ tuần này tới 01/11 là 3,7 việc mỗi tuần.",
    );
    expect(velocity.getByRole("note").textContent).toContain(
      "Có nguy cơ trễ go-live. 3 tuần gần nhất: trung bình 1,7 việc/tuần (2 + 2 + 1). Còn 11 việc và 3 tuần tới 01/11 nên cần khoảng 3,7 việc/tuần.",
    );
    expect(
      within(widgetOf("Theo phân hệ")).getByRole("img").getAttribute("aria-label"),
    ).toBe("Việc chưa hoàn thành theo phân hệ: Kế toán 5, Kho 2, Mua hàng 2, Chưa đặt 2");
    const due = within(widgetOf("Đến hạn trong 7 ngày"));
    const first = due.getAllByRole("link")[0]!;
    expect(first.textContent).toContain("ERP-138");
    expect(first.textContent).toContain("14/10");
    expect(first.textContent).toContain("Thứ Tư");
    expect(first.getAttribute("href")).toBe(
      "/workspaces/w/boards/board-erp?item=item-ERP-138",
    );
    expect(screen.getByText("Việc chưa hoàn thành có hạn 13–19/10 · 4 việc · sắp theo hạn")).toBeTruthy();
  });

  it("FUI[workspace.report-dashboard:actions] reloads, narrows to a person, opens the export and starts the editor", () => {
    const onReload = vi.fn();
    const onAssignee = vi.fn();
    const onOpenExport = vi.fn();
    const onStartEdit = vi.fn();
    renderPureUi(
      <ReportDashboardSurface
        {...dashboardScenario({ onReload, onAssignee, onOpenExport, onStartEdit })}
      />,
    );
    fireEvent.click(screen.getByRole("button", { name: "Tải lại" }));
    expect(onReload).toHaveBeenCalledTimes(1);
    fireEvent.click(screen.getByRole("button", { name: "Xuất" }));
    expect(onOpenExport).toHaveBeenCalledTimes(1);
    fireEvent.click(screen.getByRole("button", { name: "Sửa" }));
    expect(onStartEdit).toHaveBeenCalledTimes(1);
    openMenu(screen.getByRole("button", { name: /Tất cả người phụ trách/ }));
    fireEvent.click(
      screen.getByRole("menuitemradio", { name: PEOPLE.quan.name }),
    );
    expect(onAssignee).toHaveBeenCalledWith(PEOPLE.quan.id);
    // The filter button hides the bar and shows it again.
    expect(screen.getByRole("group", { name: "Bộ lọc đang dùng" })).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Bộ lọc" }));
    expect(screen.queryByRole("group", { name: "Bộ lọc đang dùng" })).toBeNull();
  });

  it("FUI[workspace.report-dashboard:viewer] hides the editor from somebody who may only read", () => {
    renderPureUi(<ReportDashboardSurface {...dashboardScenario({ report: viewerReport() })} />);
    expect(screen.getByText("Chỉ xem")).toBeTruthy();
    expect(screen.queryByRole("button", { name: "Sửa" })).toBeNull();
    openMenu(screen.getByRole("button", { name: copy.page.more }));
    expect(screen.getByRole("menuitem", { name: copy.page.share })).toBeTruthy();
    expect(screen.queryByRole("menuitem", { name: copy.page.delete })).toBeNull();
    fireEvent.keyDown(screen.getByRole("menu"), { key: "Escape" });
    // A widget menu offers the definition but not the removal.
    openMenu(
      screen.getByRole("button", { name: "Tuỳ chọn widget Theo trạng thái" }),
    );
    expect(screen.getByRole("menuitem", { name: copy.widget.definition })).toBeTruthy();
    expect(screen.queryByRole("menuitem", { name: copy.widget.remove })).toBeNull();
  });

  it("FUI[workspace.report-dashboard:partial] says what was left out and holds a stale widget until it is reloaded", () => {
    const onReload = vi.fn();
    renderPureUi(
      <ReportDashboardSurface {...dashboardScenario({ report: weeklyReport(), onReload })} />,
    );
    expect(
      screen.getByText(
        "Không tính 1 board bạn không có quyền xem. Số liệu có thể khác với bản người khác thấy.",
      ),
    ).toBeTruthy();
    expect(screen.queryByRole("button", { name: "Sửa" })).toBeNull();
    const over = widgetOf("Việc quá hạn theo phòng");
    expect(over.getAttribute("aria-busy")).toBe("true");
    expect(within(over).getByRole("status").textContent).toContain(
      "Đang tính lại · số liệu đến 10:20, cũ hơn phần còn lại của báo cáo",
    );
    fireEvent.click(within(over).getByRole("button", { name: "Tải lại" }));
    expect(onReload).toHaveBeenCalledTimes(1);
    expect(
      within(widgetOf("Việc hoàn thành theo phòng")).getByText(
        "0 nghĩa là không có việc nào hoàn thành, không phải thiếu số liệu.",
      ),
    ).toBeTruthy();
    expect(within(widgetOf("Ghi chú tuần")).getByText("Trịnh Quốc Hưng viết · 09/10")).toBeTruthy();
    const link = within(widgetOf("Tiến độ Triển khai ERP Q4")).getByRole("link", {
      name: "Mở báo cáo chi tiết",
    });
    expect(link.getAttribute("href")).toBe("/workspaces/w/boards/board-erp?view=table");
  });

  it("FUI[workspace.report-dashboard:zero] draws a board with nothing on it without dividing by zero", () => {
    renderPureUi(<ReportDashboardSurface {...dashboardScenario({ report: zeroReport() })} />);
    expect(
      screen.getByRole("list", { name: /Tóm tắt board/ }).textContent,
    ).toContain("0/0 việc hoàn thành · 0%");
    expect(
      within(widgetOf("Đến hạn trong 7 ngày")).getByText(
        copy.widget.due.empty,
      ),
    ).toBeTruthy();
    expect(within(widgetOf("Theo trạng thái")).getByRole("img")).toBeTruthy();
  });
});

describe("ReportDashboardSurface: drill-down", () => {
  it("FUI[workspace.report-dashboard:drill] opens a list behind a workload row, read-only, with links to the board", () => {
    const onDrill = vi.fn();
    const { rerender } = renderPureUi(
      <ReportDashboardSurface {...dashboardScenario({ onDrill })} />,
    );
    fireEvent.click(
      screen.getByRole("button", {
        name: "Trần Minh Quân · 2 việc chưa hoàn thành, 1 quá hạn. Xem danh sách việc",
      }),
    );
    expect(onDrill).toHaveBeenCalledWith(PEOPLE.quan.id);
    rerender(
      <ReportDashboardSurface
        {...dashboardScenario({ onDrill, drillKey: PEOPLE.quan.id, drill: DRILL_QUAN })}
      />,
    );
    const sheet = screen.getByRole("dialog", {
      name: "Trần Minh Quân · 2 việc chưa hoàn thành",
    });
    expect(sheet.getAttribute("aria-modal")).toBe("false");
    expect(sheet.textContent).toContain("Quá hạn 2 ngày · hạn 10/10");
    expect(sheet.textContent).toContain("Phân hệ Kế toán");
    expect(sheet.textContent).toContain("Đang chờ bạn duyệt");
    expect(sheet.textContent).toContain("Báo cáo chỉ đọc. Sửa việc trên board.");
    const open = within(sheet).getAllByRole("link", { name: "Mở trên board" });
    expect(open[0]!.getAttribute("href")).toBe(
      "/workspaces/w/boards/board-erp?item=item-ERP-142",
    );
    expect(
      within(sheet)
        .getByRole("link", { name: "Mở danh sách này trên board" })
        .getAttribute("href"),
    ).toBe("/workspaces/w/boards/board-erp?view=table");
    fireEvent.keyDown(sheet, { key: "Escape" });
    expect(onDrill).toHaveBeenLastCalledWith(null);
    fireEvent.click(within(sheet).getByRole("button", { name: "Đóng danh sách" }));
    expect(onDrill).toHaveBeenCalledTimes(3);
  });

  it("FUI[workspace.report-dashboard:drill-state] says loading and failure and retries", () => {
    const onRetryDrill = vi.fn();
    const { rerender } = renderPureUi(
      <ReportDashboardSurface
        {...dashboardScenario({ drillKey: "x", drill: { status: "loading" } })}
      />,
    );
    expect(
      within(screen.getByRole("dialog")).getByRole("status").textContent,
    ).toContain(copy.drill.loading);
    rerender(
      <ReportDashboardSurface
        {...dashboardScenario({ drillKey: "x", drill: { status: "error" }, onRetryDrill })}
      />,
    );
    fireEvent.click(screen.getByRole("button", { name: copy.drill.retry }));
    expect(onRetryDrill).toHaveBeenCalledTimes(1);
  });
});

describe("ReportDashboardSurface: editing", () => {
  it("FUI[workspace.report-dashboard:edit] moves a widget with the keyboard, cancels with Esc, resizes and saves the layout", async () => {
    const onSaveLayout = vi.fn(() => Promise.resolve(true));
    const onCancelEdit = vi.fn();
    renderPureUi(
      <ReportDashboardSurface
        {...dashboardScenario({ editing: true, onSaveLayout, onCancelEdit })}
      />,
    );
    const bar = screen.getByRole("region", { name: copy.edit.label });
    expect(bar.textContent).toContain("Đang sửa bố cục");
    // Esc puts a move back.
    const velocity = widgetOf("Tốc độ hoàn thành");
    fireEvent.click(within(velocity).getByRole("button", { name: "Di chuyển" }));
    expect(within(velocity).getByRole("status").textContent).toContain(
      "Đang di chuyển · vị trí 2/6",
    );
    fireEvent.keyDown(document, { key: "ArrowUp" });
    expect(within(velocity).getByRole("status").textContent).toContain("vị trí 1/6");
    fireEvent.keyDown(document, { key: "Escape" });
    expect(within(velocity).queryByRole("status")).toBeNull();
    expect(order().slice(0, 2)).toEqual(["Theo trạng thái", "Tốc độ hoàn thành"]);
    // Move and place.
    fireEvent.click(within(velocity).getByRole("button", { name: "Di chuyển" }));
    fireEvent.keyDown(document, { key: "ArrowUp" });
    fireEvent.keyDown(document, { key: "Enter" });
    expect(order().slice(0, 2)).toEqual(["Tốc độ hoàn thành", "Theo trạng thái"]);
    // A widget in the narrow column resizes to a wide one.
    openMenu(
      screen.getByRole("button", { name: "Đổi kích thước widget Theo phân hệ" }),
    );
    fireEvent.click(screen.getByRole("menuitemradio", { name: "Rộng" }));
    await act(async () => {
      fireEvent.click(within(bar).getByRole("button", { name: "Lưu" }));
    });
    expect(onSaveLayout).toHaveBeenCalledTimes(1);
    const saved = (onSaveLayout.mock.calls[0] as unknown as [readonly { id: string; size: string }[]])[0];
    expect(saved.map((widget) => widget.id)).toEqual([
      "w-velocity",
      "w-status",
      "w-module",
      "w-priority",
      "w-due",
      "w-workload",
    ]);
    expect(saved.find((widget) => widget.id === "w-module")?.size).toBe("wide");
    fireEvent.click(within(bar).getByRole("button", { name: "Huỷ" }));
    expect(onCancelEdit).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.report-dashboard:edit-failure] keeps the draft and says so when the layout is not saved", async () => {
    const onSaveLayout = vi.fn(() => Promise.resolve(false));
    renderPureUi(
      <ReportDashboardSurface {...dashboardScenario({ editing: true, onSaveLayout })} />,
    );
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: "Lưu" }));
    });
    expect((await screen.findByRole("alert")).textContent).toContain(copy.edit.saveFailed);
  });
});

describe("ReportDashboardSurface: add widget", () => {
  it("FUI[workspace.report-dashboard:add] previews a widget, names it and puts it at the end of the draft", async () => {
    const onPreviewWidget = vi.fn(() => Promise.resolve(previewColumns()));
    const onCloseAdd = vi.fn();
    const onSaveLayout = vi.fn(() => Promise.resolve(true));
    const { rerender } = renderPureUi(
      <ReportDashboardSurface
        {...dashboardScenario({ editing: true, addOpen: true, onPreviewWidget, onCloseAdd, onSaveLayout })}
      />,
    );
    const dialog = screen.getByRole("dialog", { name: "Thêm widget" });
    expect(dialog.textContent).toContain("Vào báo cáo Tổng quan triển khai ERP Q4");
    expect(
      (within(dialog).getByLabelText("Tên widget") as HTMLInputElement).value,
    ).toBe("Việc chưa hoàn thành theo người phụ trách");
    expect(
      within(dialog).getByRole("radio", { name: "Đường" }).getAttribute("aria-disabled"),
    ).toBe("true");
    expect(dialog.textContent).toContain("Số việc · định nghĩa: mỗi việc chưa lưu trữ đếm 1");
    await waitFor(() =>
      expect(within(dialog).getByRole("img").getAttribute("aria-label")).toContain(
        "Trần Minh Quân 2",
      ),
    );
    expect(onPreviewWidget).toHaveBeenCalled();
    // The line chart needs the week dimension.
    fireEvent.change(within(dialog).getByLabelText("Nhóm theo"), {
      target: { value: "week" },
    });
    expect(
      within(dialog).getByRole("radio", { name: "Đường" }).getAttribute("aria-disabled"),
    ).toBeNull();
    fireEvent.change(within(dialog).getByLabelText("Nhóm theo"), {
      target: { value: "assignee" },
    });
    fireEvent.change(within(dialog).getByLabelText("Tên widget"), {
      target: { value: "Việc mở theo người" },
    });
    // The filter token comes off and a new one goes on.
    fireEvent.click(
      within(dialog).getByRole("button", { name: "Bỏ điều kiện lọc Trạng thái" }),
    );
    openMenu(within(dialog).getByRole("button", { name: "Thêm điều kiện" }));
    fireEvent.click(screen.getByRole("menuitem", { name: "Quá hạn" }));
    await waitFor(() =>
      expect(onPreviewWidget.mock.calls.length).toBeGreaterThan(1),
    );
    const lastSpec = (
      onPreviewWidget.mock.calls.at(-1) as unknown as [{ filters: string[]; kind: string }]
    )[0];
    expect(lastSpec.filters).toEqual(["overdue"]);
    expect(lastSpec.kind).toBe("columns");
    await act(async () => {
      fireEvent.click(within(dialog).getByRole("button", { name: "Thêm" }));
    });
    expect(onCloseAdd).toHaveBeenCalledTimes(1);
    rerender(
      <ReportDashboardSurface
        {...dashboardScenario({ editing: true, addOpen: false, onSaveLayout })}
      />,
    );
    expect(order().at(-1)).not.toBeNull();
    expect(screen.getByRole("heading", { name: "Việc mở theo người" })).toBeTruthy();
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: "Lưu" }));
    });
    const saved = (
      onSaveLayout.mock.calls[0] as unknown as [{ title?: string; kind: string; groupBy?: string }[]]
    )[0];
    expect(saved).toHaveLength(7);
    expect(saved.at(-1)).toMatchObject({
      kind: "columns",
      title: "Việc mở theo người",
      groupBy: "assignee",
    });
  });

  it("FUI[workspace.report-dashboard:add-note] asks for the text of a note before it adds one", async () => {
    const onCloseAdd = vi.fn();
    renderPureUi(
      <ReportDashboardSurface
        {...dashboardScenario({ editing: true, addOpen: true, onCloseAdd })}
      />,
    );
    const dialog = screen.getByRole("dialog", { name: "Thêm widget" });
    fireEvent.click(within(dialog).getByRole("radio", { name: "Ghi chú" }));
    fireEvent.click(within(dialog).getByRole("button", { name: "Thêm" }));
    expect(within(dialog).getByText(copy.add.noteRequired)).toBeTruthy();
    expect(onCloseAdd).not.toHaveBeenCalled();
    fireEvent.change(within(dialog).getByLabelText(copy.add.noteLabel), {
      target: { value: "Chốt dữ liệu trước thứ Sáu." },
    });
    fireEvent.click(within(dialog).getByRole("button", { name: "Thêm" }));
    expect(onCloseAdd).toHaveBeenCalledTimes(1);
  });
});

describe("ReportDashboardSurface: export", () => {
  const job = (patch = {}) => ({
    id: "job-1",
    format: "csv" as const,
    status: "running" as const,
    startedAt: "2026-10-12T03:41:00.000Z",
    filename: "tong-quan.csv",
    downloadUrl: null,
    expiresAt: null,
    ...patch,
  });

  it("FUI[workspace.report-dashboard:export] chooses a format, starts a file, waits and offers the download", async () => {
    const onStartExport = vi.fn(() => Promise.resolve(true));
    const onCheckExport = vi.fn();
    const { rerender } = renderPureUi(
      <ReportDashboardSurface
        {...dashboardScenario({ exportOpen: true, onStartExport, onCheckExport })}
      />,
    );
    const dialog = screen.getByRole("dialog", { name: "Xuất báo cáo" });
    expect(dialog.textContent).toContain(
      "Bộ lọc đang dùng: board Triển khai ERP Q4 · đến 01/11 · tất cả người phụ trách",
    );
    expect(dialog.textContent).toContain("Số liệu đến 10:37 (GMT+7) · tuần bắt đầu Thứ Hai");
    expect(
      within(dialog).getByRole("radio", { name: /PDF/ }).getAttribute("aria-checked"),
    ).toBe("true");
    fireEvent.click(within(dialog).getByRole("radio", { name: /CSV/ }));
    await act(async () => {
      fireEvent.click(within(dialog).getByRole("button", { name: "Tạo tệp" }));
    });
    expect(onStartExport).toHaveBeenCalledWith("csv");
    rerender(
      <ReportDashboardSurface
        {...dashboardScenario({ exportOpen: true, exportJob: job(), onCheckExport })}
      />,
    );
    expect(dialog.textContent).toContain("Đang tạo tệp… thường dưới 1 phút");
    expect(dialog.textContent).toContain("Bắt đầu 10:41.");
    expect(
      within(dialog).getByRole("button", { name: "Đang tạo…" }).getAttribute("aria-disabled"),
    ).toBe("true");
    fireEvent.click(within(dialog).getByRole("button", { name: "Kiểm tra lại" }));
    expect(onCheckExport).toHaveBeenCalledTimes(1);
    rerender(
      <ReportDashboardSurface
        {...dashboardScenario({
          exportOpen: true,
          exportJob: job({ status: "done", downloadUrl: "data:text/csv;charset=utf-8,a" }),
        })}
      />,
    );
    const link = within(dialog).getByRole("link", { name: /Tải xuống tong-quan\.csv/ });
    expect(link.getAttribute("download")).toBe("tong-quan.csv");
    expect(dialog.textContent).toContain("Tệp đã sẵn sàng");
  });

  it("FUI[workspace.report-dashboard:export-failure] says the file was not created and lets the person try again", async () => {
    const onStartExport = vi.fn(() => Promise.resolve(false));
    renderPureUi(
      <ReportDashboardSurface {...dashboardScenario({ exportOpen: true, onStartExport })} />,
    );
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: "Tạo tệp" }));
    });
    expect((await screen.findByRole("alert")).textContent).toContain(copy.export.failed);
    expect(screen.getByRole("button", { name: "Tạo tệp" })).toBeTruthy();
  });
});

describe("ReportDashboardSurface: widget and report menus", () => {
  it("FUI[workspace.report-dashboard:menus] explains how a widget counts and removes it after a confirmation", async () => {
    const onRemoveWidget = vi.fn(() => Promise.resolve(true));
    renderPureUi(<ReportDashboardSurface {...dashboardScenario({ onRemoveWidget })} />);
    openMenu(screen.getByRole("button", { name: "Tuỳ chọn widget Theo trạng thái" }));
    fireEvent.click(screen.getByRole("menuitem", { name: copy.widget.definition }));
    expect(screen.getByRole("dialog").textContent).toContain(
      "Mỗi việc chưa lưu trữ đếm một lần, theo trạng thái hiện tại.",
    );
    fireEvent.click(screen.getByRole("button", { name: "Đóng" }));
    openMenu(screen.getByRole("button", { name: "Tuỳ chọn widget Theo mức ưu tiên" }));
    fireEvent.click(screen.getByRole("menuitem", { name: copy.widget.remove }));
    const confirm = screen.getByRole("alertdialog");
    expect(onRemoveWidget).not.toHaveBeenCalled();
    await act(async () => {
      fireEvent.click(within(confirm).getByRole("button", { name: copy.edit.removeConfirm }));
    });
    expect(onRemoveWidget).toHaveBeenCalledWith("w-priority");
  });

  it("FUI[workspace.report-dashboard:report-menu] shares, renames and deletes the report", async () => {
    const onShare = vi.fn(() => Promise.resolve(true));
    const onRename = vi.fn(() => Promise.resolve(true));
    const onDelete = vi.fn(() => Promise.resolve(true));
    renderPureUi(
      <ReportDashboardSurface {...dashboardScenario({ onShare, onRename, onDelete })} />,
    );
    const menu = () => openMenu(screen.getByRole("button", { name: copy.page.more }));
    menu();
    fireEvent.click(screen.getByRole("menuitem", { name: copy.page.share }));
    const share = screen.getByRole("dialog", { name: copy.share.title });
    fireEvent.click(within(share).getByRole("checkbox", { name: /Phòng Kế toán/ }));
    await act(async () => {
      fireEvent.click(within(share).getByRole("button", { name: copy.share.save }));
    });
    expect(onShare).toHaveBeenCalledWith(["space-tech", "space-exec", "space-acc"]);
    menu();
    fireEvent.click(screen.getByRole("menuitem", { name: copy.page.rename }));
    const rename = screen.getByRole("dialog", { name: copy.page.rename });
    fireEvent.change(within(rename).getByLabelText(copy.page.renameLabel), {
      target: { value: "  Báo cáo ERP  " },
    });
    await act(async () => {
      fireEvent.click(within(rename).getByRole("button", { name: copy.page.renameSave }));
    });
    expect(onRename).toHaveBeenCalledWith("Báo cáo ERP");
    menu();
    fireEvent.click(screen.getByRole("menuitem", { name: copy.page.delete }));
    expect(onDelete).not.toHaveBeenCalled();
    await act(async () => {
      fireEvent.click(
        within(screen.getByRole("alertdialog")).getByRole("button", { name: copy.remove.confirm }),
      );
    });
    expect(onDelete).toHaveBeenCalledTimes(1);
  });
});

describe("ReportDashboardSurface: empty report", () => {
  it("FUI[workspace.report-dashboard:empty] offers the starting layouts and names the report", async () => {
    const onApplyLayout = vi.fn(() => Promise.resolve(true));
    const onOpenAdd = vi.fn();
    const onRename = vi.fn(() => Promise.resolve(true));
    renderPureUi(
      <ReportDashboardSurface
        {...dashboardScenario({ report: emptyReport(), onApplyLayout, onOpenAdd, onRename })}
      />,
    );
    expect(
      screen.getByRole("heading", { level: 1, name: "Báo cáo chưa đặt tên" }),
    ).toBeTruthy();
    expect(screen.getByText("Bạn tạo lúc 10:40")).toBeTruthy();
    expect(screen.getByText("Chưa chia sẻ, chỉ bạn thấy")).toBeTruthy();
    expect(screen.getByText("Chưa có widget")).toBeTruthy();
    expect(screen.getByRole("heading", { name: "Bắt đầu từ một bố cục" })).toBeTruthy();
    const use = screen.getAllByRole("button", { name: "Dùng bố cục" });
    expect(use).toHaveLength(3);
    await act(async () => {
      fireEvent.click(use[1]!);
    });
    expect(onApplyLayout).toHaveBeenCalledWith("team");
    fireEvent.click(screen.getByRole("button", { name: "Hoặc thêm từng widget" }));
    expect(onOpenAdd).toHaveBeenCalledTimes(1);
    fireEvent.click(screen.getByRole("button", { name: "Đổi tên báo cáo" }));
    const rename = screen.getByRole("dialog", { name: copy.page.rename });
    fireEvent.change(within(rename).getByLabelText(copy.page.renameLabel), {
      target: { value: "Báo cáo tuần" },
    });
    await act(async () => {
      fireEvent.click(within(rename).getByRole("button", { name: copy.page.renameSave }));
    });
    expect(onRename).toHaveBeenCalledWith("Báo cáo tuần");
  });
});

describe("ReportListSurface", () => {
  it("FUI[workspace.report-list:list] lists the reports with their source, sharing, access and update", () => {
    const onFilter = vi.fn();
    const onCreate = vi.fn();
    renderPureUi(<ReportListSurface {...listScenario({ onFilter, onCreate })} />);
    const table = screen.getByRole("table", { name: "Báo cáo" });
    expect(within(table).getAllByRole("row")).toHaveLength(4);
    expect(table.textContent).toContain(
      "Nguồn: board Triển khai ERP Q4 · 6 widget · chia sẻ với Phòng Công nghệ, Ban giám đốc",
    );
    expect(table.textContent).toContain(
      "Nguồn: board Đóng sổ tháng 10 · chia sẻ với Phòng Kế toán và bạn",
    );
    expect(table.textContent).toContain(
      "Nguồn: board ở nhiều phòng ban · không tính 1 board bạn không có quyền xem",
    );
    expect(within(table).getAllByText("Chỉ xem")).toHaveLength(2);
    expect(within(table).getByText("Có thể sửa")).toBeTruthy();
    expect(within(table).getByText("10:38 hôm nay")).toBeTruthy();
    const rows = within(table).getAllByRole("row");
    expect(rows[1]!.getAttribute("href")).toBe("/workspaces/w/reports?report=report-erp");
    const group = screen.getByRole("group", { name: "Lọc báo cáo" });
    expect(group.textContent).toContain("Của tôi0");
    expect(group.textContent).toContain("Được chia sẻ3");
    expect(group.textContent).toContain("Tất cả3");
    fireEvent.click(within(group).getByRole("button", { name: /Của tôi/ }));
    expect(onFilter).toHaveBeenCalledWith("mine");
    fireEvent.click(screen.getByRole("button", { name: "Tạo báo cáo" }));
    expect(onCreate).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.report-list:states] says loading, failure, nothing yet and nothing in a list", () => {
    const onRetry = vi.fn();
    const { rerender } = renderPureUi(
      <ReportListSurface {...listScenario({ state: { status: "loading" } })} />,
    );
    expect(screen.getByRole("status").textContent).toContain(copy.list.loading);
    rerender(<ReportListSurface {...listScenario({ state: { status: "error" }, onRetry })} />);
    fireEvent.click(screen.getByRole("button", { name: copy.list.retry }));
    expect(onRetry).toHaveBeenCalledTimes(1);
    rerender(<ReportListSurface {...listScenario({ state: { status: "ready", data: [] } })} />);
    expect(screen.getByText(copy.list.emptyTitle)).toBeTruthy();
    rerender(<ReportListSurface {...listScenario({ filter: "mine" })} />);
    expect(screen.getByText(copy.list.emptyFilter)).toBeTruthy();
    rerender(<ReportListSurface {...listScenario({ createFailed: true })} />);
    expect(screen.getByRole("alert").textContent).toContain(copy.list.createFailed);
  });
});

describe("report fixtures", () => {
  it("the weekly report is the stale one until it is recomputed", () => {
    expect(weeklyReport().widgets.some((widget) => widget.staleAsOf)).toBe(true);
    expect(
      weeklyReport({ recomputed: true }).widgets.some((widget) => widget.staleAsOf),
    ).toBe(false);
    expect(erpReport().widgets).toHaveLength(6);
  });
});
