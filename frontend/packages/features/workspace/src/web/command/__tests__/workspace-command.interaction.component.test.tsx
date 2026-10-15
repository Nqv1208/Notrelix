import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";

import {
  commandCommandScenario,
  commandCopyFixture,
  commandEdgeDataScenario,
  commandNoneScenario,
  commandProducedOnlyScenario,
  commandRecentScenario,
  commandRunningScenario,
  commandSearchScenario,
} from "../../../verification/workspace-command-fixtures";
import {
  WorkspaceCommandPaletteSurface,
  type WorkspaceCommandPaletteSurfaceProps,
} from "../command-palette-surface";

const copy = commandCopyFixture("vi");

function renderPalette(
  scenario: ReturnType<typeof commandSearchScenario> = commandRecentScenario(),
  overrides: Partial<WorkspaceCommandPaletteSurfaceProps> = {},
) {
  const props: WorkspaceCommandPaletteSurfaceProps = {
    ...scenario,
    onOpenChange: vi.fn(),
    onSelect: vi.fn(),
    ...overrides,
  };
  renderPureUi(<WorkspaceCommandPaletteSurface {...props} />);
  return props;
}

const input = () => screen.getByRole("combobox");
const options = () => screen.getAllByRole("option");
const selected = () =>
  options().find((option) => option.getAttribute("aria-selected") === "true");
const tabs = () => screen.getAllByRole("tab");

describe("WorkspaceCommandPaletteSurface", () => {
  it("FUI[workspace.command-palette:render] is a labelled modal dialog with a focused combobox over a listbox", () => {
    renderPalette();
    const dialog = screen.getByRole("dialog", { name: copy.title });
    expect(document.activeElement).toBe(input());
    const listbox = within(dialog).getByRole("listbox");
    expect(input().getAttribute("aria-controls")).toBe(listbox.id);
    expect(input().getAttribute("aria-expanded")).toBe("true");
    expect(input().getAttribute("aria-activedescendant")).toBe(selected()!.id);
    expect(
      within(dialog).getByRole("button", { name: /Minh An Group/ }),
    ).toBeTruthy();
  });

  it("FUI[workspace.command-palette:recent] lists recents then quick commands and starts on the first row that can run", () => {
    renderPalette();
    expect(screen.getByText("Mở gần đây")).toBeTruthy();
    expect(screen.getByText("Lệnh nhanh")).toBeTruthy();
    expect(options().map((option) => option.textContent)).toEqual([
      expect.stringContaining("Triển khai ERP Q4"),
      expect.stringContaining("Thiết lập đăng nhập một lần"),
      expect.stringContaining("Quy trình nhập hoá đơn đầu vào"),
      expect.stringContaining("Tạo công việc"),
      expect.stringContaining("Tạo tài liệu"),
      expect.stringContaining("Mở Hộp thư"),
      expect.stringContaining("Đổi giao diện sang Đêm"),
    ]);
    // Nothing typed: no filter tabs.
    expect(screen.queryAllByRole("tab")).toHaveLength(0);
    expect(selected()!.textContent).toContain("Triển khai ERP Q4");
  });

  it("FUI[workspace.command-palette:search] searches while typing, ignoring diacritics, with counts per type", () => {
    renderPalette();
    fireEvent.change(input(), { target: { value: "doi chieu" } });
    expect(tabs().map((tab) => tab.textContent)).toEqual([
      "Tất cả7",
      "Việc3",
      "Tài liệu2",
      "Board1",
      "Người",
      "Lệnh1",
    ]);
    expect(screen.getByText("Công việc")).toBeTruthy();
    const first = options()[0]!;
    expect(first.textContent).toContain("Đối chiếu sao kê ngân hàng tháng 9");
    expect(first.textContent).toContain("KT-88");
    expect(first.textContent).toContain("giao cho bạn");
    expect(first.textContent).toContain("Cần làm");
    expect(first.textContent).toContain("Hôm nay 17:00");
    // The term is highlighted in the title.
    expect(
      within(first).getByText("Đối chiếu", { selector: "b" }),
    ).toBeTruthy();
  });

  it("FUI[workspace.command-palette:filters] narrows to one type by click and cycles with Tab and Shift+Tab", () => {
    renderPalette(commandSearchScenario());
    fireEvent.click(screen.getByRole("tab", { name: /Tài liệu/ }));
    expect(
      screen
        .getByRole("tab", { name: /Tài liệu/ })
        .getAttribute("aria-selected"),
    ).toBe("true");
    expect(options()).toHaveLength(2);
    expect(options()[0]!.textContent).toContain("Hướng dẫn đối chiếu");

    // Tab from the input moves to the next tab, not out of the dialog.
    fireEvent.keyDown(input(), { key: "Tab" });
    expect(
      screen.getByRole("tab", { name: /Board/ }).getAttribute("aria-selected"),
    ).toBe("true");
    fireEvent.keyDown(input(), { key: "Tab", shiftKey: true });
    expect(
      screen
        .getByRole("tab", { name: /Tài liệu/ })
        .getAttribute("aria-selected"),
    ).toBe("true");
    expect(document.activeElement).toBe(input());
  });

  it("FUI[workspace.command-palette:keyboard] moves the selection with the arrows (wrapping) and opens it with Enter", () => {
    const props = renderPalette(commandSearchScenario());
    const ids = () => options().map((option) => option.id);
    expect(selected()!.id).toBe(ids()[0]);
    fireEvent.keyDown(input(), { key: "ArrowDown" });
    expect(selected()!.id).toBe(ids()[1]);
    expect(input().getAttribute("aria-activedescendant")).toBe(ids()[1]);
    fireEvent.keyDown(input(), { key: "ArrowUp" });
    fireEvent.keyDown(input(), { key: "ArrowUp" });
    expect(selected()!.id).toBe(ids()[ids().length - 1]);
    fireEvent.keyDown(input(), { key: "ArrowDown" });
    fireEvent.keyDown(input(), { key: "Enter" });
    expect(props.onOpenChange).toHaveBeenCalledWith(false);
    expect(props.onSelect).toHaveBeenCalledWith({
      type: "navigate",
      destination: {
        section: "board",
        boardId: "board-dong-so",
        item: "item-kt-88",
      },
    });
  });

  it("FUI[workspace.command-palette:select] opens a clicked Doc or Board", () => {
    const props = renderPalette(commandSearchScenario());
    fireEvent.click(
      screen.getByRole("option", { name: /Hướng dẫn đối chiếu ngân hàng/ }),
    );
    expect(props.onSelect).toHaveBeenLastCalledWith({
      type: "navigate",
      destination: { section: "doc", docId: "doc-doi-chieu" },
    });
    fireEvent.click(
      options().find(
        (o) => o.getAttribute("data-row-id") === "board:board-dong-so",
      )!,
    );
    expect(props.onSelect).toHaveBeenLastCalledWith({
      type: "navigate",
      destination: { section: "board", boardId: "board-dong-so" },
    });
  });

  it("FUI[workspace.command-palette:commands] reads > as commands only and shows real shortcuts", () => {
    const props = renderPalette(commandCommandScenario());
    expect(screen.queryAllByRole("tab")).toHaveLength(0);
    expect(
      screen.getByText("Lệnh khớp “chuyển”", { exact: false }),
    ).toBeTruthy();
    // `>` is drawn as the mode prefix, the input holds the term.
    expect((input() as HTMLInputElement).value).toBe("chuyển");
    const home = screen.getByRole("option", { name: /Chuyển tới Trang chủ/ });
    expect(within(home).getByText("G")).toBeTruthy();
    expect(within(home).getByText("H")).toBeTruthy();
    fireEvent.click(home);
    expect(props.onSelect).toHaveBeenCalledWith({
      type: "navigate",
      destination: { section: "home" },
    });
  });

  it("FUI[workspace.command-palette:inert] draws commands without a producer inert with their reason and never runs them", () => {
    const props = renderPalette(commandCommandScenario());
    const status = screen.getByRole("option", {
      name: /Chuyển trạng thái sang/,
    });
    expect(status.getAttribute("aria-disabled")).toBe("true");
    expect(status.getAttribute("title")).toBe(copy.commands.unavailableItem);
    fireEvent.click(status);
    expect(props.onSelect).not.toHaveBeenCalled();
    expect(props.onOpenChange).not.toHaveBeenCalled();
    // Enter on an inert selected row does nothing either.
    for (let step = 0; step < 3; step += 1) {
      fireEvent.keyDown(input(), { key: "ArrowUp" });
    }
    expect(selected()!.getAttribute("aria-disabled")).toBe("true");
    fireEvent.keyDown(input(), { key: "Enter" });
    expect(props.onSelect).not.toHaveBeenCalled();
    // The scope picker is inert too.
    expect(
      screen
        .getByRole("button", { name: /Minh An Group/ })
        .getAttribute("aria-disabled"),
    ).toBe("true");
  });

  it("FUI[workspace.command-palette:people] reads @ as people and # as Boards", () => {
    const props = renderPalette(commandSearchScenario());
    fireEvent.change(input(), { target: { value: "@thu" } });
    expect(options().map((option) => option.textContent)).toEqual([
      expect.stringContaining("Nguyễn Thu Hà"),
      expect.stringContaining("Lý Thu Trang"),
    ]);
    fireEvent.click(options()[1]!);
    expect(props.onSelect).toHaveBeenCalledWith({
      type: "navigate",
      destination: { section: "members" },
    });
    // Backspace on an empty term removes the `@` chip, then `#` starts Boards.
    fireEvent.change(input(), { target: { value: "" } });
    fireEvent.keyDown(input(), { key: "Backspace" });
    expect((input() as HTMLInputElement).value).toBe("");
    fireEvent.change(input(), { target: { value: "#dong" } });
    expect(options()).toHaveLength(1);
    expect(options()[0]!.textContent).toContain("Đóng sổ tháng 10");
  });

  it("FUI[workspace.command-palette:actions] runs shell actions", () => {
    const props = renderPalette(commandRecentScenario());
    fireEvent.click(
      screen.getByRole("option", { name: /Đổi giao diện sang Đêm/ }),
    );
    expect(props.onSelect).toHaveBeenLastCalledWith({
      type: "action",
      action: "toggle-theme",
    });
    fireEvent.click(screen.getByRole("option", { name: /Tạo tài liệu/ }));
    expect(props.onSelect).toHaveBeenLastCalledWith({
      type: "action",
      action: "create-doc",
    });
  });

  it("FUI[workspace.command-palette:empty] explains when nothing matches and offers to create the Item and an inert search of every workspace", () => {
    const props = renderPalette(commandNoneScenario());
    expect(
      screen.getByText(
        "Không tìm thấy “hợp đồng bảo trì thang máy” trong Minh An Group",
      ),
    ).toBeTruthy();
    expect(screen.queryAllByRole("tab")).toHaveLength(0);
    expect(options().map((option) => option.textContent)).toEqual([
      expect.stringContaining("Tạo việc “hợp đồng bảo trì thang máy”"),
      expect.stringContaining("Tìm ở mọi workspace"),
    ]);
    expect(options()[0]!.getAttribute("aria-disabled")).not.toBe("true");
    expect(options()[1]!.getAttribute("aria-disabled")).toBe("true");
    fireEvent.keyDown(input(), { key: "Enter" });
    expect(props.onSelect).toHaveBeenCalledTimes(1);
    expect(props.onSelect.mock.calls[0]![0]).toMatchObject({
      type: "create-item",
      name: "hợp đồng bảo trì thang máy",
    });
  });

  it("FUI[workspace.command-palette:running] shows the running command with an undo and no results", () => {
    const onUndo = vi.fn();
    renderPalette(commandRunningScenario(), {
      running: { ...commandRunningScenario().running!, onUndo },
    });
    const status = screen.getByRole("status");
    expect(status.textContent).toContain("Đang chuyển ERP-142 sang Chờ duyệt…");
    expect(screen.queryAllByRole("option")).toHaveLength(0);
    expect((input() as HTMLInputElement).readOnly).toBe(true);
    fireEvent.click(within(status).getByRole("button", { name: "Hoàn tác" }));
    expect(onUndo).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.command-palette:running-inert] draws the undo inert without a handler", () => {
    renderPalette(commandRunningScenario());
    expect(
      screen
        .getByRole("button", { name: "Hoàn tác" })
        .getAttribute("aria-disabled"),
    ).toBe("true");
  });

  it("FUI[workspace.command-palette:dismiss] closes with Escape", () => {
    const props = renderPalette();
    fireEvent.keyDown(input(), { key: "Escape" });
    expect(props.onOpenChange).toHaveBeenCalledWith(false);
  });

  it("FUI[workspace.command-palette:produced-only] lists what the member's lists hold and nothing more", () => {
    renderPalette(commandProducedOnlyScenario());
    expect(tabs().map((tab) => tab.textContent)).toEqual([
      "Tất cả3",
      "Việc",
      "Tài liệu1",
      "Board1",
      "Người",
      "Lệnh1",
    ]);
    // No second line without a department, key, parent or editor.
    const doc = screen.getByRole("option", { name: /Kế hoạch quý/ });
    expect(doc.textContent).toContain("Tài liệu");
    expect(screen.queryByText("KT-88")).toBeNull();
  });

  it("FUI[workspace.command-palette:edge] truncates long titles, caps each type and keeps the list scrollable", () => {
    renderPalette(commandEdgeDataScenario());
    expect(screen.getByText("Công việc").parentElement!.textContent).toContain(
      "9",
    );
    expect(
      options().filter((option) => option.textContent?.includes("KH-10")),
    ).toHaveLength(6);
    const title = options()[0]!.querySelector('[data-f="t"]')!;
    expect(title.className).toContain("truncate");
    expect(screen.getByRole("listbox").className).toContain("overflow-auto");
  });

  it("FUI[workspace.command-palette:i18n] renders in English", () => {
    renderPalette(commandSearchScenario("en"));
    expect(
      screen.getByRole("dialog", { name: "Search and commands" }),
    ).toBeTruthy();
    expect(screen.getByRole("tab", { name: /Docs/ })).toBeTruthy();
  });
});
