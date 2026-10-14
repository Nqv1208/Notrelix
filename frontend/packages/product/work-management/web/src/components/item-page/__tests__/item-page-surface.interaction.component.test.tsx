import { describe, expect, it, vi } from "vitest";
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
    fireEvent.keyDown(rail.getByRole("button", { name: copy.rail.status }), {
      key: "Enter",
    });
    fireEvent.click(screen.getByRole("menuitem", { name: "Chờ duyệt" }));
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
    expect(screen.getByText(copy.description.empty)).toBeTruthy();
    expect(screen.getByText(copy.activity.emptyAll)).toBeTruthy();
    expect(screen.queryByRole("progressbar")).toBeNull();
    expect(
      screen.queryByRole("group", { name: /followers|theo dõi/ }),
    ).toBeNull();
  });
});
