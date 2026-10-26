import { beforeAll, describe, expect, it, vi } from "vitest";
import {
  fireEvent,
  renderPureUi,
  screen,
  waitFor,
  within,
} from "@notrelix/testing";
import { ItemPageSurface } from "../item-page-surface";
import { en, vi as viCopy, type ItemPageCopy } from "../item-page.copy";
import {
  en as sheetEn,
  vi as sheetVi,
} from "../../card-detail/item-sheet.copy";
import {
  commenterSheetCapabilities,
  editorSheetCapabilities,
} from "../../../verification/item-sheet-fixtures";
import {
  erpPageLongChildItems,
  erpPagePeople,
  erpPageUploads,
  ERP_PAGE_NOW,
  ERP_PAGE_TIME_ZONE,
  erpPageBoard,
  erpPageCommenterAccess,
  erpPageConflict,
  erpPageDetailData,
  erpPageItem,
  erpPageNavigation,
  erpPageSpace,
  erpPageViewer,
} from "../../../verification/item-page-fixtures";
import type { ItemPageSurfaceProps } from "../item-page-types";

/** The interaction-case marker, once per case: on the Vietnamese run only. */
const fui = (id: string, locale: string) =>
  locale === "vi" ? `FUI[${id}] ` : "";

function callbacksMock(): ItemPageSurfaceProps["callbacks"] {
  return {
    onClose: vi.fn(),
    onRenameTitle: vi.fn(),
    onUpdateItem: vi.fn(),
    onToggleWatch: vi.fn(),
    onDuplicate: vi.fn(),
    onArchive: vi.fn(),
    onUpdateFieldValue: vi.fn(),
    onRefreshActivity: vi.fn(),
    // Like a mutation: success, then settled.
    onCreateUpdate: vi.fn((_input, options) => {
      options?.onSuccess?.();
      options?.onSettled?.();
    }),
    onUpdateUpdate: vi.fn(),
    onDeleteUpdate: vi.fn(),
    onSelectTab: vi.fn(),
    onNext: vi.fn(),
    onCopyLink: vi.fn(),
  };
}

function props(
  copy: ItemPageCopy,
  overrides: Partial<ItemPageSurfaceProps> = {},
): ItemPageSurfaceProps {
  const item = erpPageItem();
  return {
    status: "ready",
    board: erpPageBoard(),
    card: item,
    capabilities: editorSheetCapabilities,
    detailData: erpPageDetailData(item),
    callbacks: callbacksMock(),
    navigation: erpPageNavigation({
      onBack: vi.fn(),
      onCollapse: vi.fn(),
      onOpenView: vi.fn(),
    }),
    copy,
    sheetCopy: copy === viCopy ? sheetVi : sheetEn,
    now: ERP_PAGE_NOW,
    timeZone: ERP_PAGE_TIME_ZONE,
    space: erpPageSpace,
    viewer: erpPageViewer,
    creatorName: "Nguyễn Thu Hà",
    ...overrides,
  };
}

function renderPage(
  copy: ItemPageCopy,
  overrides: Partial<ItemPageSurfaceProps> = {},
) {
  const pageProps = props(copy, overrides);
  const view = renderPureUi(<ItemPageSurface {...pageProps} />);
  return { ...view, props: pageProps, callbacks: pageProps.callbacks };
}

const TITLE = "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP";

describe.each([
  ["vi", viCopy],
  ["en", en],
] as const)("item page (%s)", (locale, copy) => {
  it(`${fui("wm.item-page.surface:loading", locale)}renders the loading state without QueryClient (${locale})`, () => {
    renderPage(copy, { status: "loading", card: null, detailData: undefined });
    expect(screen.getByRole("status", { name: copy.loading })).toBeTruthy();
  });

  it(`${fui("wm.item-page.surface:unavailable", locale)}renders the unavailable state and goes back from it (${locale})`, () => {
    const { props: pageProps } = renderPage(copy, {
      status: "error",
      card: null,
      detailData: undefined,
    });
    expect(screen.getByText(copy.unavailable.title)).toBeTruthy();
    fireEvent.click(
      screen.getByRole("button", { name: copy.unavailable.back }),
    );
    expect(pageProps.navigation.onBack).toHaveBeenCalledTimes(1);
  });

  it(`${fui("wm.item-page.surface:ready", locale)}renders the page at the mockup (${locale})`, () => {
    renderPage(copy);
    expect(screen.getByRole("heading", { level: 1, name: TITLE })).toBeTruthy();

    const crumb = screen.getByRole("navigation", { name: copy.topbar.crumb });
    expect(crumb.textContent).toContain("CN");
    expect(crumb.textContent).toContain("Phòng Công nghệ");
    expect(crumb.textContent).toContain("Triển khai ERP Q4");
    expect(
      within(crumb).getByText("ERP-142").getAttribute("aria-current"),
    ).toBe("page");
    expect(
      screen.getByText(
        locale === "vi" ? "Việc 1/3 trong Đang làm" : "Item 1/3 in Đang làm",
      ),
    ).toBeTruthy();

    // Sections of the left column, in the mockup order.
    const headings = screen
      .getAllByRole("heading", { level: 2 })
      .map((heading) => heading.textContent ?? "");
    expect(headings).toEqual([
      expect.stringContaining(copy.description.heading),
      expect.stringContaining(copy.children.heading),
      expect.stringContaining(copy.links.heading),
      expect.stringContaining(copy.files.heading),
      expect.stringContaining(copy.activity.heading),
    ]);
    expect(screen.getByRole("progressbar").getAttribute("aria-valuenow")).toBe(
      "60",
    );
    // The three "Xong khi" bullets and the five sub-items.
    expect(screen.getAllByRole("listitem")).toHaveLength(8);
    expect(screen.getByText("khach-hang-da-gop.xlsx")).toBeTruthy();
    expect(screen.getByText(/18,4 MB|18\.4 MB/)).toBeTruthy();
    expect(screen.getByText(/Xong khi:/)).toBeTruthy();

    // The right column.
    const rail = screen.getByRole("complementary", { name: copy.rail.label });
    const inRail = within(rail);
    expect(inRail.getByRole("button", { name: copy.rail.status })).toBeTruthy();
    expect(inRail.getByText("01/10/2026")).toBeTruthy();
    expect(
      inRail.getByRole("button", { name: copy.rail.due }).textContent,
    ).toMatch(
      locale === "vi"
        ? /^10\/10 · quá hạn 2 ngày$/
        : /^10\/10 · 2 days overdue$/,
    );
    expect(
      inRail.getByText(
        copy.rail.progressRollup
          .replace("{{done}}", "3")
          .replace("{{total}}", "5"),
      ),
    ).toBeTruthy();
    expect(inRail.getByText("ERP-142")).toBeTruthy();
    expect(inRail.getByText("Nguyễn Thu Hà")).toBeTruthy();
    expect(inRail.getAllByRole("link")).toHaveLength(4);
  });

  it(`${fui("wm.item-page.surface:back", locale)}goes back, collapses to the sheet and opens a view (${locale})`, () => {
    const { props: pageProps } = renderPage(copy);
    fireEvent.click(screen.getByRole("link", { name: copy.topbar.back }));
    expect(pageProps.navigation.onBack).toHaveBeenCalledTimes(1);
    fireEvent.click(screen.getByRole("button", { name: copy.topbar.collapse }));
    expect(pageProps.navigation.onCollapse).toHaveBeenCalledTimes(1);
    fireEvent.click(
      screen.getByRole("link", { name: new RegExp(`^${copy.views.table}`) }),
    );
    expect(pageProps.navigation.onOpenView).toHaveBeenCalledWith("table");
    // A modified click is the browser's: no route change from the page.
    document.addEventListener("click", (event) => event.preventDefault(), {
      once: true,
    });
    fireEvent.click(screen.getByRole("link", { name: copy.topbar.back }), {
      ctrlKey: true,
    });
    expect(pageProps.navigation.onBack).toHaveBeenCalledTimes(1);
  });

  it(`${fui("wm.item-page.surface:kj", locale)}steps with J and K, not while typing (${locale})`, () => {
    const { callbacks } = renderPage(copy);
    const root = screen.getByRole("heading", { level: 1 });
    fireEvent.keyDown(root, { key: "k" });
    expect(callbacks.onPrevious).toBeUndefined();
    fireEvent.keyDown(root, { key: "j" });
    expect(callbacks.onNext).toHaveBeenCalledTimes(1);
    fireEvent.keyDown(
      screen.getByRole("textbox", { name: copy.composer.label }),
      {
        key: "j",
      },
    );
    expect(callbacks.onNext).toHaveBeenCalledTimes(1);
    // The first item of its lane has no previous: the button says why.
    const previous = screen.getByRole("button", {
      name: new RegExp(copy.topbar.previousNone.split("{{")[0] ?? ""),
    });
    expect(previous.getAttribute("aria-disabled")).toBe("true");
    expect(screen.getByRole("button", { name: copy.topbar.next })).toBeTruthy();
  });

  it(`${fui("wm.item-page.surface:tabs", locale)}filters the activity by tab (${locale})`, () => {
    const onSelectTab = vi.fn();
    renderPage(copy, { onSelectTab, activeTab: "all" });
    const filter = screen.getByRole("navigation", {
      name: copy.activity.filter,
    });
    const tabs = within(filter).getAllByRole("link");
    expect(tabs.map((tab) => tab.textContent)).toEqual([
      `${copy.activity.all}8`,
      `${copy.activity.comments}3`,
      `${copy.activity.history}5`,
    ]);
    expect(tabs[0]?.getAttribute("aria-current")).toBe("page");
    expect(screen.getByText("Tự động hoá")).toBeTruthy();
    expect(screen.getByText(/Gửi file công nợ đầu kỳ tháng 9/)).toBeTruthy();
    fireEvent.click(tabs[2]!);
    expect(onSelectTab).toHaveBeenCalledWith("history");
    fireEvent.keyDown(tabs[0]!, { key: "ArrowRight" });
    expect(onSelectTab).toHaveBeenLastCalledWith("comments");
  });

  it(`${fui("wm.item-page.surface:history", locale)}shows only the history, without the composer (${locale})`, () => {
    renderPage(copy, { activeTab: "history", onSelectTab: vi.fn() });
    expect(screen.getByText("Tự động hoá")).toBeTruthy();
    expect(screen.queryByText(/Gửi file công nợ đầu kỳ tháng 9/)).toBeNull();
    expect(
      screen.queryByRole("textbox", { name: copy.composer.label }),
    ).toBeNull();
    // The history records no field, so it cannot be filtered by one yet.
    const filter = screen.getByRole("button", {
      name: new RegExp(copy.activity.allFields),
    });
    expect(filter.getAttribute("aria-disabled")).toBe("true");
    expect(filter.getAttribute("aria-describedby")).toBeTruthy();
    expect(screen.getByText(copy.activity.historyNote)).toBeTruthy();
  });

  it(`${fui("wm.item-page.surface:properties", locale)}edits the status and the start date through the shared editors (${locale})`, () => {
    const { callbacks } = renderPage(copy);
    const rail = within(screen.getByRole("complementary"));
    fireEvent.click(rail.getByRole("button", { name: copy.rail.status }));
    fireEvent.click(screen.getByRole("option", { name: /Chờ duyệt/ }));
    expect(callbacks.onUpdateFieldValue).toHaveBeenCalledWith({
      cardId: "item-erp-142",
      fieldDefinitionId: "field-status",
      value: "status-review",
    });
    const start = rail.getByRole("button", { name: copy.rail.startDate });
    expect(start.getAttribute("aria-disabled")).toBeNull();
  });

  it(`${fui("wm.item-page.surface:conflict", locale)}keeps both values of a refused write until one is picked (${locale})`, () => {
    const onResolveConflict = vi.fn();
    renderPage(copy, { conflict: erpPageConflict, onResolveConflict });
    const notice = screen.getByRole("alert");
    expect(notice.textContent).toContain(erpPageConflict.fieldName);
    expect(notice.textContent).toContain("12/10");
    expect(notice.textContent).toContain("14/10");
    expect(
      within(screen.getByRole("complementary")).getByText(
        locale === "vi"
          ? "12/10 · đã đổi thành 14/10"
          : "12/10 · changed to 14/10",
      ),
    ).toBeTruthy();
    fireEvent.click(within(notice).getByRole("button", { name: /14\/10/ }));
    expect(onResolveConflict).toHaveBeenLastCalledWith("theirs");
    fireEvent.click(within(notice).getByRole("button", { name: /12\/10/ }));
    expect(onResolveConflict).toHaveBeenLastCalledWith("mine");
    fireEvent.click(
      within(notice).getByRole("button", { name: copy.conflict.viewChanges }),
    );
    expect(
      screen
        .getAllByRole("link", { name: new RegExp(copy.activity.history) })[0]
        ?.getAttribute("aria-current"),
    ).toBe("page");
  });

  it(`${fui("wm.item-page.surface:readonly", locale)}shows the read-only notice and keeps the composer (${locale})`, () => {
    renderPage(copy, {
      capabilities: commenterSheetCapabilities,
      access: erpPageCommenterAccess,
    });
    const notice = screen.getByRole("status");
    expect(notice.textContent).toContain(copy.readOnly.levels.commenter);
    expect(notice.textContent).toContain("Lê Hoàng Phúc");
    expect(
      within(notice)
        .getByRole("button", { name: copy.readOnly.requestEdit })
        .getAttribute("aria-disabled"),
    ).toBe("true");
    const rail = within(screen.getByRole("complementary"));
    for (const name of [copy.rail.status, copy.rail.assignee, copy.rail.due]) {
      expect(
        rail.getByRole("button", { name }).getAttribute("aria-disabled"),
      ).toBe("true");
    }
    expect(rail.queryByText(copy.rail.manageFields)).toBeNull();
    expect(screen.queryByPlaceholderText(copy.children.add)).toBeNull();
    expect(screen.queryByText(copy.files.choose)).toBeNull();
    expect(
      (
        screen.getByRole("textbox", {
          name: copy.composer.label,
        }) as HTMLTextAreaElement
      ).disabled,
    ).toBe(false);
  });

  it(`${fui("wm.item-page.surface:composer", locale)}posts a comment with the button and with the shortcut (${locale})`, () => {
    const { callbacks } = renderPage(copy);
    const composer = screen.getByRole("textbox", { name: copy.composer.label });
    const send = screen.getByRole("button", { name: copy.composer.submit });
    expect((send as HTMLButtonElement).disabled).toBe(true);
    fireEvent.change(composer, { target: { value: "  Đã nhận file  " } });
    expect((send as HTMLButtonElement).disabled).toBe(false);
    fireEvent.click(send);
    expect(callbacks.onCreateUpdate).toHaveBeenCalledWith(
      {
        cardId: "item-erp-142",
        body: "Đã nhận file",
        mentionUserIds: [],
        attachmentIds: [],
      },
      expect.objectContaining({ onSuccess: expect.any(Function) }),
    );
    expect((composer as HTMLTextAreaElement).value).toBe("");
    fireEvent.change(composer, { target: { value: "Lần hai" } });
    fireEvent.keyDown(composer, { key: "Enter", metaKey: true });
    expect(callbacks.onCreateUpdate).toHaveBeenCalledTimes(2);
  });

  it(`${fui("wm.item-page.surface:unavailable-controls", locale)}keeps controls without a producer, disabled with the reason (${locale})`, () => {
    renderPage(copy);
    const cases: ReadonlyArray<
      readonly [Parameters<typeof screen.getByRole>[1], string]
    > = [
      [{ name: copy.rail.following }, copy.rail.followUnavailable],
      [{ name: copy.links.add }, copy.links.addUnavailable],
      [{ name: copy.composer.attach }, copy.composer.attachUnavailable],
    ];
    for (const [query, reason] of cases) {
      const control = screen.getByRole("button", query);
      expect(control.getAttribute("aria-disabled")).toBe("true");
      const described = control.getAttribute("aria-describedby");
      expect(described).toBeTruthy();
      expect(document.getElementById(described!)?.textContent).toBe(reason);
      // A disabled control never acts.
      fireEvent.click(control);
    }
    expect(screen.getAllByText(copy.activity.reply).length).toBeGreaterThan(0);
    expect(
      screen
        .getAllByRole("button", { name: copy.activity.reply })[0]
        ?.getAttribute("aria-disabled"),
    ).toBe("true");
  });

  it(`${fui("wm.item-page.surface:edge", locale)}renders an item with no data (${locale})`, async () => {
    const item = erpPageItem({
      descriptionMd: undefined,
      itemKey: null,
      dueAt: null,
      dueDate: undefined,
      startDate: undefined,
      priority: undefined,
      labels: [],
      members: [],
      watchers: [],
      isWatched: false,
      childProgress: null,
      statusOptionId: null,
      statusCategory: null,
      status: "",
      updates: [],
      files: [],
      activity: [],
    });
    renderPage(copy, {
      card: item,
      detailData: { ...erpPageDetailData(item), childItems: undefined },
    });
    await waitFor(() =>
      expect(screen.getByRole("heading", { level: 1 })).toBeTruthy(),
    );
    expect(screen.getByText(copy.states.empty.description)).toBeTruthy();
    expect(screen.getByText(copy.activity.emptyAll)).toBeTruthy();
    expect(screen.queryByRole("progressbar")).toBeNull();
    expect(
      screen.queryByRole("group", { name: /followers|theo dõi/ }),
    ).toBeNull();
  });

  it(`${fui("wm.item-page.surface:description-edit", locale)}writes, saves and clears the description from the page (${locale})`, async () => {
    const item = erpPageItem({ descriptionMd: undefined });
    const { callbacks } = renderPage(copy, {
      card: item,
      detailData: erpPageDetailData(item),
    });
    // An item without a description invites writing one.
    fireEvent.click(
      screen.getByRole("button", { name: copy.states.empty.description }),
    );
    const box = screen.getByRole("textbox", { name: copy.description.heading });
    fireEvent.change(box, { target: { value: "- First\n- Second" } });
    fireEvent.click(
      screen.getByRole("button", { name: copy.description.save }),
    );
    expect(callbacks.onRenameTitle).toHaveBeenCalledWith(item.id, {
      descriptionMd: "- First\n- Second",
    });
    expect(
      screen.queryByRole("textbox", { name: copy.description.heading }),
    ).toBeNull();
  });

  it(`${fui("wm.item-page.surface:description-keys", locale)}saves with Ctrl+Enter, cancels with Escape and sends nothing when unchanged (${locale})`, async () => {
    const item = erpPageItem({ descriptionMd: "Existing text" });
    const { callbacks } = renderPage(copy, {
      card: item,
      detailData: erpPageDetailData(item),
    });
    fireEvent.click(
      screen.getByRole("button", { name: copy.description.edit }),
    );
    let box = screen.getByRole("textbox", { name: copy.description.heading });
    fireEvent.change(box, { target: { value: "Dropped by Escape" } });
    fireEvent.keyDown(box, { key: "Escape" });
    expect(callbacks.onRenameTitle).not.toHaveBeenCalled();
    expect(screen.getByText("Existing text")).toBeTruthy();

    fireEvent.click(
      screen.getByRole("button", { name: copy.description.edit }),
    );
    box = screen.getByRole("textbox", { name: copy.description.heading });
    fireEvent.keyDown(box, { key: "Enter", ctrlKey: true });
    expect(callbacks.onRenameTitle).not.toHaveBeenCalled();

    fireEvent.click(
      screen.getByRole("button", { name: copy.description.edit }),
    );
    box = screen.getByRole("textbox", { name: copy.description.heading });
    fireEvent.change(box, { target: { value: "" } });
    fireEvent.keyDown(box, { key: "Enter", ctrlKey: true });
    expect(callbacks.onRenameTitle).toHaveBeenCalledWith(item.id, {
      descriptionMd: "",
    });
  });
});

describe("item page sections with data (vi)", () => {
  beforeAll(() => {
    // The link picker (cmdk) scrolls its highlighted row into view.
    Element.prototype.scrollIntoView = () => undefined;
  });

  const detail = (overrides: Record<string, unknown> = {}) => ({
    ...erpPageDetailData(erpPageItem()),
    ...overrides,
  });

  it("adds a sub-item with Enter and ticks one", () => {
    const callbacks = {
      ...callbacksMock(),
      onAddChildItem: vi.fn(),
      onToggleChildItem: vi.fn(),
    };
    renderPage(viCopy, {
      callbacks,
      detailData: detail({
        childItems: [
          {
            id: "c1",
            title: "Xuất dữ liệu",
            done: false,
            itemKey: "ERP-142.1",
          },
        ],
      }),
    });
    fireEvent.click(screen.getByRole("checkbox", { name: "Xuất dữ liệu" }));
    expect(callbacks.onToggleChildItem).toHaveBeenCalledWith("c1", true);
    const input = screen.getByRole("textbox", { name: "Việc con" });
    fireEvent.change(input, { target: { value: "Kiểm tra lại" } });
    fireEvent.keyDown(input, { key: "Enter" });
    expect(callbacks.onAddChildItem).toHaveBeenCalledWith("Kiểm tra lại");
  });

  it("groups the links, adds one through the picker and removes one", () => {
    const callbacks = {
      ...callbacksMock(),
      onAddLink: vi.fn(),
      onRemoveLink: vi.fn(),
    };
    renderPage(viCopy, {
      callbacks,
      detailData: detail({
        links: [
          {
            id: "l1",
            relation: "blocked-by",
            item: {
              id: "i129",
              key: "ERP-129",
              title: "Chốt danh mục",
              statusName: "Hoàn thành",
              statusCategory: "done",
            },
          },
          {
            id: "l2",
            relation: "related",
            item: {
              id: "i151",
              key: "ERP-151",
              title: "Cấu hình quyền",
              statusName: "Cần làm",
              statusCategory: "todo",
            },
          },
        ],
        linkCandidates: [{ id: "i150", key: "ERP-150", title: "Cảnh báo" }],
      }),
    });
    expect(screen.getByText("Đang chờ")).toBeTruthy();
    expect(screen.getByText("Liên quan")).toBeTruthy();
    fireEvent.click(
      screen.getByRole("button", { name: "Gỡ liên kết với ERP-129" }),
    );
    expect(callbacks.onRemoveLink).toHaveBeenCalledWith("l1");
    fireEvent.click(screen.getByRole("button", { name: "Thêm" }));
    fireEvent.click(screen.getByRole("radio", { name: "Đang chặn" }));
    fireEvent.click(screen.getByText("Cảnh báo"));
    expect(callbacks.onAddLink).toHaveBeenCalledWith("i150", "blocks");
  });

  it("registers a chosen file and tells which file came with a comment", () => {
    const callbacks = { ...callbacksMock(), onAddFile: vi.fn() };
    const { container } = renderPage(viCopy, { callbacks });
    const input =
      container.querySelector<HTMLInputElement>('input[type="file"]')!;
    const file = new File(["x"], "ghi-chu.pdf", { type: "application/pdf" });
    fireEvent.change(input, { target: { files: [file] } });
    expect(callbacks.onAddFile).toHaveBeenCalledWith(file);
  });

  it("follows and unfollows from the rail", () => {
    const callbacks = callbacksMock();
    renderPage(viCopy, {
      callbacks,
      detailData: detail({
        following: true,
        followers: [{ userId: "u1", name: "Nguyễn Thu Hà", initials: "HA" }],
      }),
    });
    fireEvent.click(screen.getByRole("button", { name: "Đang theo dõi" }));
    expect(callbacks.onToggleWatch).toHaveBeenCalledWith(false);
  });
});

describe.each([
  ["vi", viCopy],
  ["en", en],
] as const)("item page states (%s)", (locale, copy) => {
  it(`${fui("wm.item-page.surface:not-found", locale)}says the item is not there, with the key, and offers search and home (${locale})`, () => {
    const onOpenSearch = vi.fn();
    const onGoHome = vi.fn();
    renderPage(copy, {
      status: "notFound",
      card: null,
      detailData: undefined,
      itemKey: "ERP-171",
      onOpenSearch,
      onGoHome,
    });
    expect(
      screen.getByRole("heading", {
        level: 1,
        name: copy.states.notFound.title.replace("{{key}}", "ERP-171"),
      }),
    ).toBeTruthy();
    fireEvent.click(
      screen.getByRole("button", { name: copy.states.notFound.search }),
    );
    fireEvent.click(
      screen.getByRole("button", { name: copy.states.notFound.home }),
    );
    expect(onOpenSearch).toHaveBeenCalledTimes(1);
    expect(onGoHome).toHaveBeenCalledTimes(1);
  });

  it(`${fui("wm.item-page.surface:loading-slow", locale)}keeps the frame while loading and says when it is slow (${locale})`, () => {
    const { rerender } = renderPage(copy, {
      status: "loading",
      card: null,
      detailData: undefined,
    });
    expect(screen.queryByText(copy.states.loadingSlow)).toBeNull();
    rerender(
      <ItemPageSurface
        {...props(copy, {
          status: "loading",
          card: null,
          detailData: undefined,
          slow: true,
        })}
      />,
    );
    expect(screen.getByText(copy.states.loadingSlow)).toBeTruthy();
  });

  it(`${fui("wm.item-page.surface:lifecycle", locale)}shows a done item's notice and reopens it; an archived one is read-only and restores (${locale})`, () => {
    const onReopen = vi.fn();
    const done = renderPage(copy, {
      lifecycle: {
        completed: { at: "2026-10-06T09:00:00.000Z", byName: "Vũ Thanh" },
        onReopen,
      },
    });
    const banner = document.querySelector("[data-item-banner=completed]");
    expect(banner?.textContent).toContain("Vũ Thanh");
    fireEvent.click(
      within(banner as HTMLElement).getByRole("button", {
        name: copy.states.completed.reopen,
      }),
    );
    expect(onReopen).toHaveBeenCalledTimes(1);
    done.unmount();

    const onRestore = vi.fn();
    renderPage(copy, {
      lifecycle: {
        archived: { at: "2026-10-08T03:00:00.000Z", byName: "Nguyễn Thu Hà" },
        onRestore,
      },
    });
    const archived = document.querySelector("[data-item-banner=archived]");
    expect(archived?.textContent).toContain("Nguyễn Thu Hà");
    // Read-only whatever the board grants: the composer is off.
    expect(
      screen
        .getByRole("textbox", { name: copy.composer.label })
        .hasAttribute("disabled"),
    ).toBe(true);
    fireEvent.click(
      within(archived as HTMLElement).getByRole("button", {
        name: copy.states.archived.restore,
      }),
    );
    expect(onRestore).toHaveBeenCalledTimes(1);
  });

  it(`${fui("wm.item-page.surface:offline", locale)}says what waits while offline and marks the values (${locale})`, () => {
    renderPage(copy, {
      offline: { pendingCount: 2, properties: ["due"], childItems: true },
    });
    const notice = document.querySelector("[data-item-banner=offline]");
    expect(notice?.textContent).toContain(copy.states.offline.title);
    expect(screen.getAllByText(copy.states.offline.pending).length).toBe(2);
  });

  it(`${fui("wm.item-page.surface:empty", locale)}folds the empty fields and invites the first sub-item (${locale})`, () => {
    const item = erpPageItem({
      descriptionMd: undefined,
      dueAt: null,
      dueDate: undefined,
      startDate: undefined,
      labels: [],
      childProgress: null,
    });
    renderPage(copy, {
      card: item,
      detailData: { ...erpPageDetailData(item), childItems: [] },
      callbacks: { ...callbacksMock(), onAddChildItem: vi.fn() },
    });
    const rail = within(screen.getByRole("complementary"));
    expect(rail.queryByRole("button", { name: copy.rail.due })).toBeNull();
    fireEvent.click(
      rail.getByRole("button", {
        name: new RegExp(
          copy.states.empty.showFields_other.replace("{{count}}", "\\d+"),
        ),
      }),
    );
    expect(rail.getByRole("button", { name: copy.rail.due })).toBeTruthy();
    expect(screen.getByText(copy.states.empty.children)).toBeTruthy();
    fireEvent.click(
      screen.getByRole("button", { name: copy.states.empty.addChild }),
    );
    expect(
      screen.getByRole("textbox", { name: copy.children.heading }),
    ).toBeTruthy();
  });

  it(`${fui("wm.item-page.surface:long", locale)}shows a long list of sub-items in part and expands it (${locale})`, () => {
    const item = erpPageItem({ childProgress: { done: 12, total: 48 } });
    renderPage(copy, {
      card: item,
      detailData: {
        ...erpPageDetailData(item),
        childItems: erpPageLongChildItems,
      },
    });
    const more = screen.getByRole("button", {
      name: copy.states.long.showChildren.replace("{{count}}", "40"),
    });
    expect(screen.queryByText("Đối chiếu mã kho chi nhánh 20")).toBeNull();
    fireEvent.click(more);
    expect(screen.getByText("Đối chiếu mã kho chi nhánh 20")).toBeTruthy();
    fireEvent.click(
      screen.getByRole("button", { name: copy.states.long.hideChildren }),
    );
    expect(screen.queryByText("Đối chiếu mã kho chi nhánh 20")).toBeNull();
  });

  it(`${fui("wm.item-page.surface:pickers", locale)}picks Status with the keyboard and a number, Priority with none, and a day with a quick chip (${locale})`, () => {
    const { callbacks } = renderPage(copy, { people: erpPagePeople });
    const rail = within(screen.getByRole("complementary"));

    // Status: type to filter, Enter chooses the highlighted option.
    fireEvent.click(rail.getByRole("button", { name: copy.rail.status }));
    const search = screen.getByRole("combobox", {
      name: copy.pickers.status.label,
    });
    fireEvent.change(search, { target: { value: "duy" } });
    expect(screen.getAllByRole("option")).toHaveLength(1);
    fireEvent.keyDown(search, { key: "Enter" });
    expect(callbacks.onUpdateFieldValue).toHaveBeenLastCalledWith({
      cardId: "item-erp-142",
      fieldDefinitionId: "field-status",
      value: "status-review",
    });

    // Status again: a digit chooses while the field is empty.
    fireEvent.click(rail.getByRole("button", { name: copy.rail.status }));
    fireEvent.keyDown(
      screen.getByRole("combobox", { name: copy.pickers.status.label }),
      { key: "4" },
    );
    expect(callbacks.onUpdateFieldValue).toHaveBeenLastCalledWith({
      cardId: "item-erp-142",
      fieldDefinitionId: "field-status",
      value: "status-done",
    });

    // Priority: "none" clears it.
    fireEvent.click(rail.getByRole("button", { name: copy.rail.priority }));
    fireEvent.click(
      screen.getByRole("option", {
        name: new RegExp(copy.pickers.priority.none),
      }),
    );
    expect(callbacks.onUpdateFieldValue).toHaveBeenLastCalledWith({
      cardId: "item-erp-142",
      fieldDefinitionId: "field-priority",
      value: null,
    });

    // Due: a quick chip sets the day (and closes the picker).
    fireEvent.click(rail.getByRole("button", { name: copy.rail.due }));
    fireEvent.click(
      screen.getByRole("button", { name: copy.pickers.date.tomorrow }),
    );
    const write = vi.mocked(callbacks.onUpdateFieldValue).mock.lastCall?.[0];
    expect(write?.fieldDefinitionId).toBe("field-due-date");
    // The day moves, the time of day (17:00 Hanoi) stays.
    expect(write?.value).toBe("2026-10-13T10:00:00.000Z");
  });

  it(`${fui("wm.item-page.surface:assignee-picker", locale)}lists who sees the board first, invites or asks for the rest, clears and assigns me (${locale})`, () => {
    const onInvite = vi.fn();
    const { callbacks } = renderPage(copy, {
      people: { ...erpPagePeople, onInvite },
    });
    const rail = within(screen.getByRole("complementary"));
    fireEvent.click(rail.getByRole("button", { name: copy.rail.assignee }));
    const search = screen.getByRole("combobox", {
      name: copy.pickers.assignee.label,
    });
    fireEvent.change(search, { target: { value: "ph" } });
    expect(screen.getByText(copy.pickers.assignee.members)).toBeTruthy();
    expect(screen.getByText(copy.pickers.assignee.outside)).toBeTruthy();
    // Who is not on the board is invited by a manager.
    fireEvent.click(
      screen.getByRole("button", { name: copy.pickers.assignee.invite }),
    );
    expect(onInvite).toHaveBeenCalledWith("user-thinh");
    // Backspace on an empty field clears the assignee.
    fireEvent.change(search, { target: { value: "" } });
    fireEvent.keyDown(search, { key: "Backspace" });
    expect(callbacks.onUpdateFieldValue).toHaveBeenLastCalledWith({
      cardId: "item-erp-142",
      fieldDefinitionId: "field-person",
      value: [],
    });
    // I assigns the signed-in person.
    fireEvent.keyDown(
      screen.getByRole("combobox", { name: copy.pickers.assignee.label }),
      { key: "i" },
    );
    expect(callbacks.onUpdateFieldValue).toHaveBeenLastCalledWith({
      cardId: "item-erp-142",
      fieldDefinitionId: "field-person",
      value: [erpPageViewer.userId],
    });
  });

  it(`${fui("wm.item-page.surface:assignee-ask", locale)}tells a member who is not a manager whom to ask (${locale})`, () => {
    renderPage(copy, { people: { ...erpPagePeople, canInvite: false } });
    fireEvent.click(
      within(screen.getByRole("complementary")).getByRole("button", {
        name: copy.rail.assignee,
      }),
    );
    expect(
      screen.queryByRole("button", { name: copy.pickers.assignee.invite }),
    ).toBeNull();
    expect(
      screen.getAllByText(copy.pickers.assignee.askManager).length,
    ).toBeGreaterThan(0);
  });

  it(`${fui("wm.item-page.surface:mention", locale)}lists people and teams after @, warns about who cannot see the item and inserts the pick (${locale})`, async () => {
    const { callbacks } = renderPage(copy, { people: erpPagePeople });
    const field = screen.getByRole("textbox", { name: copy.composer.label });
    fireEvent.change(field, {
      target: { value: "Chị @Kh", selectionStart: 7 },
    });
    // Khánh Linh sees the item; Văn Tài does not and is not notified.
    const list = await screen.findByRole("listbox", {
      name: copy.composer.mentionList,
    });
    expect(within(list).getByText("Văn Tài", { exact: false })).toBeTruthy();
    expect(list.textContent).toContain(
      copy.composer.mentionNoAccess.replace("{{name}}", "Tài"),
    );
    fireEvent.keyDown(field, { key: "Escape" });
    expect(
      screen.queryByRole("listbox", { name: copy.composer.mentionList }),
    ).toBeNull();
    fireEvent.click(field);
    fireEvent.keyDown(field, { key: "Enter" });
    expect((field as HTMLTextAreaElement).value).toBe("Chị @Đỗ Khánh Linh ");
    fireEvent.click(screen.getByRole("button", { name: copy.composer.submit }));
    expect(callbacks.onCreateUpdate).toHaveBeenCalledWith(
      expect.objectContaining({ mentionUserIds: ["user-linh"] }),
      expect.anything(),
    );
  });

  it(`${fui("wm.item-page.surface:request-approval", locale)}lists the approvals asked, offers the board members but not the viewer in the item menu, and asks the chosen one (${locale})`, async () => {
    const item = erpPageItem();
    const data = erpPageDetailData(item);
    const board = erpPageBoard();
    const asked = board.members.find(
      (member) => member.userId !== erpPageViewer.userId,
    )!;
    const onRequestApproval = vi.fn();
    renderPage(copy, {
      card: item,
      callbacks: { ...callbacksMock(), onRequestApproval },
      detailData: {
        ...data,
        approvals: [
          {
            id: "approval-1",
            status: "approved",
            approverUserId: asked.userId,
            approverName: asked.name,
            decisionNote: "Looks good",
          },
        ],
      },
    });
    const section = screen.getByRole("region", { name: copy.approval.heading });
    expect(
      within(section).getByText(
        copy.approval.approved.replace("{{name}}", asked.name),
      ),
    ).toBeTruthy();
    expect(within(section).getByText("Looks good")).toBeTruthy();
    fireEvent.keyDown(screen.getByRole("button", { name: copy.topbar.more }), {
      key: "Enter",
    });
    const ask = await screen.findByRole("menuitem", {
      name: copy.approval.request,
    });
    fireEvent.keyDown(ask, { key: "ArrowRight" });
    const viewer = board.members.find(
      (member) => member.userId === erpPageViewer.userId,
    );
    if (viewer)
      expect(
        screen.queryByRole("menuitem", { name: new RegExp(viewer.name) }),
      ).toBeNull();
    fireEvent.click(
      await screen.findByRole("menuitem", { name: new RegExp(asked.name) }),
    );
    expect(onRequestApproval).toHaveBeenCalledWith(asked.userId);
  });

  it(`${fui("wm.item-page.surface:link-picker", locale)}picks the relation, lists linked items as taken and explains a loop (${locale})`, () => {
    const item = erpPageItem();
    const data = erpPageDetailData(item);
    renderPage(copy, {
      card: item,
      callbacks: { ...callbacksMock(), onAddLink: vi.fn() },
      detailData: {
        ...data,
        links: [
          {
            id: "link-129",
            relation: "blocked-by",
            item: {
              id: "item-129",
              key: "ERP-129",
              title: "Chốt danh mục tài khoản kế toán dùng chung",
              statusName: "Hoàn thành",
              statusCategory: "done",
            },
          },
        ],
        linkCandidates: [
          {
            id: "item-129",
            key: "ERP-129",
            title: "Chốt danh mục tài khoản kế toán dùng chung",
          },
          { id: "item-144", key: "ERP-144", title: "Kiểm thử phân hệ công nợ" },
        ],
      },
    });
    fireEvent.click(screen.getByRole("button", { name: copy.links.add }));
    // Already linked: listed, marked, not choosable.
    expect(screen.getByText(copy.links.picker.linked)).toBeTruthy();
    // On "blocking" the item it waits for would make a loop.
    fireEvent.click(
      screen.getByRole("radio", { name: copy.links.picker.relations.blocks }),
    );
    expect(
      screen.getByText(
        copy.links.picker.cycleBlocks
          .replace("{{key}}", "ERP-142")
          .replace("{{other}}", "ERP-129"),
      ),
    ).toBeTruthy();
  });

  it(`${fui("wm.item-page.surface:uploads", locale)}lists files being sent and refused, retries, skips and takes a drop anywhere on the item (${locale})`, () => {
    const onRetry = vi.fn();
    const onDismiss = vi.fn();
    const onAddFile = vi.fn();
    renderPage(copy, {
      uploads: erpPageUploads,
      onRetryUpload: onRetry,
      onDismissUpload: onDismiss,
      callbacks: { ...callbacksMock(), onAddFile },
    });
    expect(
      screen
        .getByRole("progressbar", { name: "bien-ban-doi-chieu.pdf" })
        .getAttribute("aria-valuenow"),
    ).toBe("64");
    fireEvent.click(screen.getByRole("button", { name: copy.files.retry }));
    expect(onRetry).toHaveBeenCalledWith("upload-zip");
    fireEvent.click(screen.getByRole("button", { name: copy.files.skip }));
    expect(onDismiss).toHaveBeenCalledWith("upload-zip");

    // Dragging a file over the item shows the drop zone; dropping sends it.
    const page = screen.getByRole("heading", { level: 1 }).closest("section")!;
    const file = new File(["x"], "bien-ban.pdf", { type: "application/pdf" });
    fireEvent.dragEnter(page, {
      dataTransfer: { types: ["Files"], files: [file] },
    });
    expect(
      document.querySelector("[data-item-drop-zone]")?.textContent,
    ).toContain(copy.files.dropHere.replace("{{key}}", "ERP-142"));
    fireEvent.drop(page, { dataTransfer: { types: ["Files"], files: [file] } });
    expect(onAddFile).toHaveBeenCalledWith(file);
    expect(document.querySelector("[data-item-drop-zone]")).toBeNull();
  });
});
