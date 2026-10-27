import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";

import {
  homeCopyFixture,
  myWorkByBoardScenario,
  myWorkCreateScenario,
  myWorkDefaultScenario,
  myWorkDetailScenario,
  myWorkDoneScenario,
  myWorkEdgeDataScenario,
  myWorkEmptyScenario,
  myWorkErrorScenario,
  myWorkGroupMenuScenario,
  myWorkHighDensityScenario,
  myWorkLoadingScenario,
  myWorkProducedOnlyScenario,
  myWorkSelectingScenario,
  myWorkStatusPopScenario,
} from "../../../verification/workspace-home-fixtures";
import { shellNavigationFixture } from "../../../verification/workspace-shell-fixtures";
import {
  WorkspaceMyWorkSurface,
  type WorkspaceMyWorkSurfaceProps,
} from "../workspace-my-work-surface";

const copy = homeCopyFixture("vi");

function renderMyWork(
  scenario: ReturnType<typeof myWorkDefaultScenario> = myWorkDefaultScenario(),
  overrides: Partial<WorkspaceMyWorkSurfaceProps> = {},
) {
  const props: WorkspaceMyWorkSurfaceProps = {
    ...scenario,
    navigation: shellNavigationFixture(vi.fn()),
    onComplete: vi.fn(),
    onCreate: null,
    onOpenItem: vi.fn(),
    onLoadMore: vi.fn(),
    onRetry: vi.fn(),
    onChangeStatus: vi.fn(),
    ...overrides,
  };
  renderPureUi(<WorkspaceMyWorkSurface {...props} />);
  return props;
}

const rowOf = (key: string) =>
  screen
    .getAllByRole("listitem")
    .find((row) => row.textContent?.includes(key)) as HTMLElement;

describe("WorkspaceMyWorkSurface", () => {
  it("FUI[workspace.my-work:render] draws the title, tabs with counts, the due windows and their rows", () => {
    renderMyWork();
    expect(
      screen.getByRole("heading", { level: 1, name: "Việc của tôi" }),
    ).toBeTruthy();
    expect(
      screen.getByText("7 việc đang mở trên 3 phòng ban · cập nhật 10:40"),
    ).toBeTruthy();
    const tabs = screen.getAllByRole("tab");
    expect(tabs.map((tab) => tab.textContent)).toEqual([
      "Tất cả7",
      "Giao cho tôi4",
      "Cần tôi duyệt1",
      "Đang theo dõi2",
      "Đã xong",
    ]);

    const headings = screen
      .getAllByRole("heading", { level: 2 })
      .filter((heading) => heading.id.startsWith("g-"))
      .map((heading) => heading.textContent);
    expect(headings).toEqual([
      "Quá hạn1",
      "Hôm nay2Thứ Hai 12/10",
      "Tuần này2đến Chủ nhật 18/10",
      "Sau đó2từ tuần sau",
    ]);

    const row = rowOf("ERP-142");
    expect(row.textContent).toContain("Triển khai ERP Q4 ·");
    expect(row.textContent).toContain("Đang theo dõi");
    expect(row.textContent).toContain("Đang làm");
    expect(row.textContent).toContain("Quá hạn 2 ngày");
    expect(row.textContent).toContain("hạn 10/10");
    expect(row.textContent).toContain("Khẩn");
    expect(within(rowOf("KT-88")).getByText("17:00")).toBeTruthy();
    const approval = rowOf("ERP-138");
    expect(approval.textContent).toContain("gửi duyệt lúc 10:28");
    expect(
      within(approval).getByRole("button", { name: "Duyệt ERP-138" }),
    ).toBeTruthy();
  });

  it("FUI[workspace.my-work:tabs] filters by relationship and keeps the tabs with no data inert", () => {
    renderMyWork();
    const list = () => within(screen.getByRole("tabpanel"));
    fireEvent.click(screen.getByRole("tab", { name: /^Cần tôi duyệt/ }));
    expect(list().queryByText("ERP-142")).toBeNull();
    expect(list().getByText("ERP-138")).toBeTruthy();
    fireEvent.click(screen.getByRole("tab", { name: /^Đang theo dõi/ }));
    expect(list().getByText("ERP-142")).toBeTruthy();
    expect(list().getByText("ERP-144")).toBeTruthy();
    expect(list().queryByText("KT-88")).toBeNull();

    // A host without the data draws the tabs inert with the reason.
    renderMyWork(myWorkProducedOnlyScenario());
    const inert = screen.getAllByRole("tab", { name: "Cần tôi duyệt" })[0]!;
    expect(inert.getAttribute("aria-disabled")).toBe("true");
    expect(inert.getAttribute("title")).toBe(copy.list.tabUnavailable.approve);
  });

  it("FUI[workspace.my-work:group] groups by Board and says Status is unavailable when items have none", () => {
    renderMyWork(myWorkByBoardScenario());
    const headings = screen
      .getAllByRole("heading", { level: 2 })
      .filter((heading) => heading.id.startsWith("g-"))
      .map((heading) => heading.textContent);
    expect(headings).toEqual([
      "CNTriển khai ERP Q44Phòng Công nghệ",
      "KTĐóng sổ tháng 102Phòng Kế toán",
      "MRa mắt sản phẩm Q41Phòng Marketing",
    ]);
    // Grouped by Board, a row leaves its Board out.
    expect(rowOf("ERP-142").textContent).not.toContain("Triển khai ERP Q4 ·");
  });

  it("FUI[workspace.my-work:group-menu] opens the group menu with three choices and picks one", () => {
    renderMyWork(myWorkGroupMenuScenario());
    const menu = screen.getByRole("menu", { name: copy.list.groupMenuLabel });
    const items = within(menu).getAllByRole("menuitemradio");
    expect(items.map((item) => item.textContent)).toEqual([
      "Theo hạn",
      "Theo board",
      "Theo trạng thái",
    ]);
    expect(items[0]!.getAttribute("aria-checked")).toBe("true");
    fireEvent.click(items[1]!);
    expect(screen.queryByRole("menu")).toBeNull();
    expect(screen.getByRole("tabpanel").getAttribute("aria-label")).toContain(
      "theo board",
    );
  });

  it("FUI[workspace.my-work:search-filter] searches by title, key and Board, and filters by department", () => {
    renderMyWork();
    fireEvent.change(
      screen.getByRole("searchbox", { name: copy.list.searchLabel }),
      { target: { value: "ket noi" } },
    );
    expect(screen.getByText("Không có việc nào khớp.")).toBeTruthy();
    fireEvent.change(
      screen.getByRole("searchbox", { name: copy.list.searchLabel }),
      { target: { value: "sao ke" } },
    );
    expect(screen.getByText("KT-88")).toBeTruthy();
    expect(screen.queryByText("ERP-142")).toBeNull();
    fireEvent.change(
      screen.getByRole("searchbox", { name: copy.list.searchLabel }),
      { target: { value: "" } },
    );

    fireEvent.click(screen.getByRole("button", { name: /^Phòng ban/ }));
    fireEvent.click(
      within(screen.getByRole("listbox", { name: "Phòng ban" })).getByRole(
        "option",
        { name: "Kế toán" },
      ),
    );
    expect(screen.getByText("KT-88")).toBeTruthy();
    expect(screen.getByText("KT-95")).toBeTruthy();
    expect(screen.queryByText("MK-21")).toBeNull();
  });

  it("FUI[workspace.my-work:select] selects rows, shows the bulk bar with the reason Bỏ theo dõi is unavailable, completes the selection and clears with Escape", () => {
    const props = renderMyWork(myWorkSelectingScenario());
    const bar = screen.getByRole("toolbar", {
      name: "Thao tác với 2 việc đã chọn",
    });
    expect(bar.textContent).toContain("2 đã chọn");
    expect(
      within(bar)
        .getByRole("button", { name: /Bỏ theo dõi/ })
        .getAttribute("aria-disabled"),
    ).toBe("true");
    expect(screen.getByRole("tooltip").textContent).toContain(
      "KT-88 được giao cho bạn nên không bỏ theo dõi được. Bỏ chọn KT-88 để bỏ theo dõi ERP-144.",
    );

    fireEvent.click(within(bar).getByRole("button", { name: "Xong" }));
    expect(props.onComplete).toHaveBeenCalledTimes(2);
    expect(screen.queryByRole("toolbar")).toBeNull();

    fireEvent.click(screen.getByRole("checkbox", { name: "Chọn ERP-142" }));
    expect(screen.getByRole("toolbar").textContent).toContain("1 đã chọn");
    fireEvent.keyDown(window, { key: "Escape" });
    expect(screen.queryByRole("toolbar")).toBeNull();
  });

  it("FUI[workspace.my-work:keys] moves with J and K, selects with X and completes with E", () => {
    const props = renderMyWork();
    const links = screen
      .getAllByRole("link")
      .filter((link) => link.closest('[data-f="rt"]'));
    fireEvent.keyDown(window, { key: "j" });
    expect(document.activeElement).toBe(links[0]);
    fireEvent.keyDown(window, { key: "j" });
    expect(document.activeElement).toBe(links[1]);
    fireEvent.keyDown(window, { key: "k" });
    expect(document.activeElement).toBe(links[0]);

    fireEvent.keyDown(window, { key: "x" });
    expect(
      screen
        .getByRole("checkbox", { name: "Chọn ERP-142" })
        .getAttribute("aria-checked"),
    ).toBe("true");
    fireEvent.keyDown(window, { key: "e" });
    expect(props.onComplete).toHaveBeenCalledWith(
      expect.objectContaining({ id: "item-erp-142" }),
    );
  });

  it("FUI[workspace.my-work:complete] completes inline and blocks a second request while one is in flight", () => {
    const props = renderMyWork(myWorkDefaultScenario(), {
      completingIds: new Set(["item-erp-144"]),
    });
    fireEvent.click(
      within(rowOf("KT-88")).getByRole("button", {
        name: "Đánh dấu xong KT-88",
      }),
    );
    expect(props.onComplete).toHaveBeenCalledWith(
      expect.objectContaining({ id: "item-kt-88" }),
    );
    const busy = within(rowOf("ERP-144")).getByRole("button", {
      name: "Đánh dấu xong ERP-144",
    });
    expect(busy.getAttribute("aria-disabled")).toBe("true");
    fireEvent.click(busy);
    expect(props.onComplete).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.my-work:open] hands a title click to the host and marks the open row", () => {
    const props = renderMyWork();
    const link = within(rowOf("MK-21")).getByRole("link", {
      name: /Góp ý nội dung/,
    });
    expect(link.getAttribute("href")).toBe(
      "/workspaces/ws-product/boards/board-mk",
    );
    fireEvent.click(link, { button: 0 });
    expect(props.onOpenItem).toHaveBeenCalledWith(
      expect.objectContaining({ id: "item-mk-21" }),
    );
    // A modified click keeps the link's own meaning.
    fireEvent.click(link, { button: 0, ctrlKey: true });
    expect(props.onOpenItem).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.my-work:detail] narrows the list for the sheet and keeps the open row marked", () => {
    renderMyWork(myWorkDetailScenario(), {
      sheet: <aside aria-label="Item sheet" />,
    });
    expect(rowOf("ERP-142").getAttribute("aria-current")).toBe("true");
    // The right rail, Priority and the person make room for the sheet.
    expect(
      screen.queryByRole("complementary", { name: copy.list.rail.label }),
    ).toBeNull();
    expect(screen.queryByText("Khẩn")).toBeNull();
    expect(
      screen.getByRole("complementary", { name: "Item sheet" }),
    ).toBeTruthy();
  });

  it("FUI[workspace.my-work:status] opens the Status popover, picks with the keyboard and never changes without a handler", () => {
    const props = renderMyWork(myWorkStatusPopScenario());
    const pop = screen.getByRole("listbox", { name: "Đổi trạng thái KT-88" });
    const options = within(pop).getAllByRole("option");
    expect(options.map((option) => option.textContent)).toEqual([
      "Cần làm",
      "Đang làm",
      "Chờ duyệt",
      "Hoàn thành",
    ]);
    expect(options[0]!.getAttribute("aria-selected")).toBe("true");
    fireEvent.keyDown(pop, { key: "3" });
    expect(props.onChangeStatus).toHaveBeenCalledWith(
      expect.objectContaining({ id: "item-kt-88" }),
      "review",
    );
    expect(
      screen.queryByRole("listbox", { name: /Đổi trạng thái/ }),
    ).toBeNull();
  });

  it("FUI[workspace.my-work:no-status] draws no Status pill or popover when the list carries no Status", () => {
    renderMyWork(myWorkProducedOnlyScenario());
    expect(
      screen.queryAllByRole("button", { name: /đổi trạng thái/ }),
    ).toHaveLength(0);
    expect(screen.queryByRole("listbox")).toBeNull();
  });

  it("FUI[workspace.my-work:create] opens the create popover with the Boards and says nothing is created without a write", () => {
    renderMyWork(myWorkCreateScenario());
    const dialog = screen.getByRole("dialog", { name: copy.list.addTitle });
    const boards = within(dialog).getAllByRole("radio");
    expect(boards).toHaveLength(3);
    expect(boards[0]!.getAttribute("aria-checked")).toBe("true");
    fireEvent.click(boards[1]!);
    expect(boards[1]!.getAttribute("aria-checked")).toBe("true");
    fireEvent.click(
      within(dialog).getByRole("button", { name: copy.list.addBoardOther }),
    );
    expect(within(dialog).getAllByRole("radio")).toHaveLength(4);
    const submit = within(dialog).getByRole("button", {
      name: new RegExp(copy.list.addSubmit),
    });
    expect(submit.getAttribute("aria-disabled")).toBe("true");
    expect(submit.getAttribute("title")).toBe(copy.list.addUnavailable);
    fireEvent.keyDown(window, { key: "Escape" });
    expect(screen.queryByRole("dialog")).toBeNull();
  });

  it("FUI[workspace.my-work:create-write] creates through the host once a Board and a name are given", () => {
    const onCreate = vi.fn();
    renderMyWork(myWorkCreateScenario(), { onCreate });
    const dialog = screen.getByRole("dialog", { name: copy.list.addTitle });
    fireEvent.change(within(dialog).getByLabelText(copy.list.addName), {
      target: { value: "Gửi biên bản họp UAT" },
    });
    fireEvent.click(
      within(dialog).getByRole("button", {
        name: new RegExp(copy.list.addSubmit),
      }),
    );
    expect(onCreate).toHaveBeenCalledWith({
      boardId: "board-erp",
      name: "Gửi biên bản họp UAT",
    });
  });

  it("FUI[workspace.my-work:create-form] hands the name and Board to the full item form from More details", () => {
    const onOpenCreateForm = vi.fn();
    renderMyWork(myWorkCreateScenario(), { onOpenCreateForm });
    const dialog = screen.getByRole("dialog", { name: copy.list.addTitle });
    fireEvent.change(within(dialog).getByLabelText(copy.list.addName), {
      target: { value: "Soạn kế hoạch quý" },
    });
    fireEvent.click(
      within(dialog).getByRole("button", { name: copy.list.addMore }),
    );
    expect(onOpenCreateForm).toHaveBeenCalledWith({
      boardId: "board-erp",
      name: "Soạn kế hoạch quý",
    });
    expect(screen.queryByRole("dialog")).toBeNull();
  });

  it("FUI[workspace.my-work:rail] adds up the week by due day and by department, and filters by a day", () => {
    renderMyWork();
    const rail = screen.getByRole("complementary", {
      name: copy.list.rail.label,
    });
    expect(rail.textContent).toContain("12–18/10");
    const days = within(rail).getAllByRole("button", {
      name: /^(Thứ|Chủ nhật)/,
    });
    expect(days).toHaveLength(7);
    expect(days[0]!.getAttribute("aria-label")).toBe(
      "Thứ Hai 12/10, hôm nay: 2 việc đến hạn",
    );
    expect(rail.textContent).toContain("1 việc quá hạn");
    expect(rail.textContent).toContain("· từ Thứ Bảy 10/10");
    const deps = [...rail.querySelectorAll<HTMLElement>('[data-f="deprow"]')];
    expect(deps.map((dep) => dep.getAttribute("aria-label"))).toEqual([
      "Công nghệ: 4 việc đang mở",
      "Kế toán: 2 việc đang mở",
      "Marketing: 1 việc đang mở",
    ]);
    expect(deps[0]!.getAttribute("href")).toBe(
      "/workspaces/ws-product/spaces/space-cn",
    );
    expect(rail.textContent).toContain("Đang chờ người khác");

    const list = () => within(screen.getByRole("tabpanel"));
    fireEvent.click(days[0]!);
    expect(days[0]!.getAttribute("aria-pressed")).toBe("true");
    expect(list().getByText("ERP-144")).toBeTruthy();
    expect(list().queryByText("ERP-142")).toBeNull();
    fireEvent.click(days[0]!);
    expect(list().getByText("ERP-142")).toBeTruthy();
  });

  it("FUI[workspace.my-work:done] lists completed items by window and reopens nothing without a write", () => {
    renderMyWork(myWorkDoneScenario());
    expect(screen.getByText(/Ẩn việc đã xong sau 30 ngày/)).toBeTruthy();
    const headings = screen
      .getAllByRole("heading", { level: 2 })
      .filter((heading) => heading.id.startsWith("g-"))
      .map((heading) => heading.textContent);
    expect(headings).toEqual(["Hôm nay1", "Tuần trước1"]);
    const reopen = screen.getByRole("button", { name: "Mở lại KT-86" });
    expect(reopen.getAttribute("aria-disabled")).toBe("true");
    expect(rowOf("KT-86").textContent).toContain("10:05");
    expect(rowOf("ERP-136").textContent).toContain("Thứ Sáu 09/10");
  });

  it("FUI[workspace.my-work:collapse] collapses and expands a group", () => {
    renderMyWork();
    const toggle = screen.getByRole("button", { name: "Thu gọn nhóm Hôm nay" });
    fireEvent.click(toggle);
    expect(screen.queryByText("KT-88")).toBeNull();
    expect(toggle.getAttribute("aria-expanded")).toBe("false");
    fireEvent.click(
      screen.getByRole("button", { name: "Mở rộng nhóm Hôm nay" }),
    );
    expect(screen.getByText("KT-88")).toBeTruthy();
  });

  it("FUI[workspace.my-work:load-more] loads the next page and shows the plus in the count", () => {
    const props = renderMyWork(myWorkDefaultScenario(), { hasMore: true });
    expect(screen.getByText(/^7\+ việc đang mở/)).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: copy.list.loadMore }));
    expect(props.onLoadMore).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.my-work:empty] teaches the empty state with suggestions", () => {
    renderMyWork(myWorkEmptyScenario());
    expect(screen.getByText(copy.list.empty.title)).toBeTruthy();
    expect(
      screen.getByText("Chưa có việc nào đang mở · cập nhật 10:40"),
    ).toBeTruthy();
    const open = screen.getByRole("link", { name: /Mở Công nghệ/ });
    expect(open.getAttribute("href")).toBe(
      "/workspaces/ws-product/spaces/space-cn",
    );
    const follow = screen.getByRole("button", { name: /Theo dõi một việc/ });
    expect(follow.getAttribute("aria-disabled")).toBe("true");
    fireEvent.click(screen.getByRole("button", { name: /Tạo việc đầu tiên/ }));
    expect(
      screen.getByRole("dialog", { name: copy.list.addTitle }),
    ).toBeTruthy();
    // No filters and no rail without items.
    expect(screen.queryByRole("button", { name: /^Phòng ban/ })).toBeNull();
    expect(
      screen.queryByRole("complementary", { name: copy.list.rail.label }),
    ).toBeNull();
  });

  it("FUI[workspace.my-work:loading] shows stable skeleton rows while loading", () => {
    renderMyWork(myWorkLoadingScenario());
    expect(screen.getByText(copy.list.sublineLoading)).toBeTruthy();
    expect(
      document.querySelectorAll('[data-f="row"]').length,
    ).toBeGreaterThanOrEqual(3);
    expect(screen.queryByText("ERP-142")).toBeNull();
  });

  it("FUI[workspace.my-work:states] retries a failed load", () => {
    const props = renderMyWork(myWorkErrorScenario());
    expect(screen.getByRole("alert").textContent).toContain(copy.list.error);
    fireEvent.click(screen.getByRole("button", { name: copy.list.retry }));
    expect(props.onRetry).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.my-work:read-only] draws every tick inert when the host offers no completion", () => {
    renderMyWork(myWorkDefaultScenario(), { onComplete: null });
    const tick = within(rowOf("KT-88")).getByRole("button", {
      name: "Đánh dấu xong KT-88",
    });
    expect(tick.getAttribute("aria-disabled")).toBe("true");
    fireEvent.click(tick);
    // Opening an item still works.
    expect(
      within(rowOf("KT-88")).getByRole("link", { name: /Đối chiếu sao kê/ }),
    ).toBeTruthy();
  });

  it("FUI[workspace.my-work:produced-only] draws only what the host fills in, with no empty columns", () => {
    renderMyWork(myWorkProducedOnlyScenario());
    expect(screen.queryByText("ERP-142")).toBeNull();
    expect(screen.queryByText("Đang làm")).toBeNull();
    expect(screen.queryByText("Khẩn")).toBeNull();
    expect(screen.queryByText(/gửi duyệt lúc/)).toBeNull();
    expect(
      screen.queryByRole("complementary", { name: /Item sheet/ }),
    ).toBeNull();
    // The relationship the read states: assigned to the member.
    expect(screen.getAllByText("Giao cho tôi").length).toBeGreaterThan(0);
    // The waiting-on card and the personal calendar have no producer.
    expect(screen.queryByText("Đang chờ người khác")).toBeNull();
    expect(
      screen
        .getByRole("button", { name: "Lịch của tôi" })
        .getAttribute("aria-disabled"),
    ).toBe("true");
    // The Priority pill is inert with the reason.
    const priority = screen.getByRole("button", { name: /Mức ưu tiên/ });
    expect(priority.getAttribute("aria-disabled")).toBe("true");
    expect(priority.getAttribute("title")).toBe(
      copy.list.filters.priorityUnavailable,
    );
  });

  it("FUI[workspace.my-work:waiting-bare] draws a waiting-on entry without a note or due date with no dangling separators", () => {
    const scenario = myWorkDefaultScenario();
    const entry = scenario.waiting![0]!;
    renderMyWork({
      ...scenario,
      waiting: [{ ...entry, note: "", dueNote: "" }],
    });

    const keyInRail = screen
      .getAllByText(entry.key)
      .find((node) => !node.hasAttribute("data-f"))!;
    const line = keyInRail.parentElement!;
    expect(line.textContent?.trim()).toBe(entry.key);
  });

  it("FUI[workspace.my-work:edge] keeps long titles and a Board the member cannot open readable", () => {
    renderMyWork(myWorkEdgeDataScenario());
    const row = rowOf("VH-1204");
    expect(row.textContent).toContain("Chuẩn bị tài liệu bàn giao");
    expect(row.textContent).not.toContain("board-unknown");
    expect(
      within(row).getByRole("img", {
        name: "Người phụ trách: Nguyễn Thị Phương Thảo Nguyên",
      }),
    ).toBeTruthy();
  });

  // Heavy interaction sequence: many role queries in jsdom exceed the 5 s default on a loaded runner.
  it("FUI[workspace.my-work:high-density] renders 120 items with a completing row and a page still loading", () => {
    renderMyWork(myWorkHighDensityScenario());
    expect(screen.getAllByRole("listitem").length).toBeGreaterThanOrEqual(120);
    expect(
      screen
        .getByRole("button", { name: /^Đánh dấu xong Việc 004/ })
        .getAttribute("aria-busy"),
    ).toBe("true");
    expect(
      screen.getByRole("button", { name: copy.list.loadingMore }),
    ).toBeTruthy();
  }, 20_000);
});
