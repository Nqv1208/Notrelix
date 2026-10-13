import { useState } from "react";
import { describe, expect, it, vi } from "vitest";
import {
  fireEvent,
  renderPureUi,
  screen,
  waitFor,
  within,
} from "@notrelix/testing";
import { viewerCapabilities } from "@notrelix/work-management-testing";
import { TaskDetailPanelSurface } from "../task-detail-panel-surface";
import { en, vi as viCopy, type ItemSheetCopy } from "../item-sheet.copy";
import {
  ERP_SHEET_NOW,
  ERP_SHEET_TIME_ZONE,
  commenterSheetCapabilities,
  editorSheetCapabilities,
  erpSheetBoard,
  erpSheetDetailData,
  erpSheetGroups,
  erpSheetItem,
  erpSheetMark,
} from "../../../verification/item-sheet-fixtures";

/** The interaction-case marker, once per case: on the Vietnamese run only. */
const fui = (id: string, locale: string) =>
  locale === "vi" ? `FUI[${id}] ` : "";

type SurfaceProps = Parameters<typeof TaskDetailPanelSurface>[0];

function callbacksMock(): SurfaceProps["callbacks"] {
  return {
    onClose: vi.fn(),
    onRenameTitle: vi.fn(),
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
    onPrevious: vi.fn(),
    onNext: vi.fn(),
    onCopyLink: vi.fn(),
    onMoveToGroup: vi.fn(),
  };
}

function props(
  copy: ItemSheetCopy,
  overrides: Partial<SurfaceProps> = {},
): SurfaceProps {
  const item = erpSheetItem();
  return {
    status: "ready",
    board: erpSheetBoard(),
    card: item,
    capabilities: editorSheetCapabilities,
    detailData: erpSheetDetailData(item),
    callbacks: callbacksMock(),
    copy,
    now: ERP_SHEET_NOW,
    timeZone: ERP_SHEET_TIME_ZONE,
    departmentMark: erpSheetMark,
    groups: erpSheetGroups,
    ...overrides,
  };
}

function renderSheet(
  copy: ItemSheetCopy,
  overrides: Partial<SurfaceProps> = {},
) {
  const sheetProps = props(copy, overrides);
  const view = renderPureUi(<TaskDetailPanelSurface {...sheetProps} />);
  return { ...view, props: sheetProps, callbacks: sheetProps.callbacks };
}

const TITLE = "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP";

describe.each([
  ["vi", viCopy],
  ["en", en],
] as const)("item side sheet (%s)", (locale, copy) => {
  it(`${fui("wm.task-detail.panel:panel", locale)}renders the loading state without QueryClient (${locale})`, () => {
    renderSheet(copy, { status: "loading", card: null, detailData: undefined });
    expect(screen.getByRole("status", { name: copy.loading })).toBeTruthy();
  });

  it(`${fui("wm.task-detail.panel:unavailable", locale)}renders the unavailable state and closes from it (${locale})`, () => {
    const { callbacks } = renderSheet(copy, {
      status: "error",
      card: null,
      detailData: undefined,
    });
    expect(screen.getByText(copy.unavailable.title)).toBeTruthy();
    fireEvent.click(
      screen.getByRole("button", { name: copy.unavailable.close }),
    );
    expect(callbacks.onClose).toHaveBeenCalledTimes(1);
  });

  it(`${fui("wm.task-detail.panel:ready", locale)}renders the non-modal sheet at the mockup (${locale})`, () => {
    renderSheet(copy);
    const dialog = screen.getByRole("dialog", { name: TITLE });
    expect(dialog.getAttribute("aria-modal")).toBe("false");
    expect(screen.getByRole("heading", { level: 2, name: TITLE })).toBeTruthy();

    const crumb = screen.getByRole("navigation", { name: copy.head.crumb });
    expect(crumb.textContent).toContain("CN");
    expect(crumb.textContent).toContain("Triển khai ERP Q4");
    expect(crumb.textContent).toContain("ERP-142");

    for (const name of [
      copy.head.previous,
      copy.head.next,
      copy.head.copyLink,
      copy.head.openFullPage,
      copy.head.more,
      copy.head.close,
    ]) {
      expect(screen.getByRole("button", { name })).toBeTruthy();
    }

    const properties = screen.getByRole("group", {
      name: copy.properties.group,
    });
    const pills = within(properties);
    expect(pills.getByRole("button", { name: /Đang làm/ })).toBeTruthy();
    expect(pills.getByRole("button", { name: /Trần Minh Quân/ })).toBeTruthy();
    expect(
      pills.getByText(
        locale === "vi" ? "10/10 · quá hạn 2 ngày" : "10/10 · 2 days overdue",
      ),
    ).toBeTruthy();
    expect(pills.getByText(copy.properties.priorities.urgent)).toBeTruthy();
    expect(pills.getByText("Dữ liệu")).toBeTruthy();
    expect(
      pills.getByRole("button", { name: copy.properties.addProperty }),
    ).toBeTruthy();

    expect(
      screen.getByRole("heading", {
        level: 3,
        name: copy.sections.description,
      }),
    ).toBeTruthy();
    expect(screen.getByText(copy.sections.descriptionUnavailable)).toBeTruthy();
    expect(
      screen.queryByRole("textbox", { name: copy.sections.description }),
    ).toBeNull();
    expect(
      screen.getByRole("heading", {
        level: 3,
        name: new RegExp(`${copy.sections.childItems}.*3/5`),
      }),
    ).toBeTruthy();
    expect(screen.getAllByRole("listitem").length).toBeGreaterThanOrEqual(5);

    expect(
      screen
        .getByRole("tab", {
          name: copy.sections.comments.replace("{{count}}", "2"),
        })
        .getAttribute("aria-selected"),
    ).toBe("true");
    expect(
      screen.getByRole("tab", { name: copy.sections.history }),
    ).toBeTruthy();
    expect(screen.getByText(/Bên mình cần file công nợ đầu kỳ/)).toBeTruthy();
    expect(
      screen.getByText(locale === "vi" ? "Hôm qua, 16:40" : "Yesterday, 16:40"),
    ).toBeTruthy();
    expect(
      screen.getByText(locale === "vi" ? "2 giờ trước" : "2 hours ago"),
    ).toBeTruthy();

    const composer = screen.getByRole("textbox", { name: copy.composer.label });
    expect(composer.getAttribute("placeholder")).toBe(
      copy.composer.placeholder,
    );
    expect(screen.getByText("⌘↵")).toBeTruthy();
  });

  it(`${fui("wm.task-detail.panel:focus-in", locale)}moves focus into the sheet on open (${locale})`, () => {
    const opener = document.createElement("button");
    document.body.append(opener);
    opener.focus();
    renderSheet(copy);
    expect(document.activeElement).toBe(screen.getByRole("dialog"));
    opener.remove();
  });

  it(`${fui("wm.task-detail.panel:focus-return", locale)}returns focus to the opener on close (${locale})`, async () => {
    function Harness() {
      const [open, setOpen] = useState(false);
      return (
        <>
          <button type="button" onClick={() => setOpen(true)}>
            opener
          </button>
          {open ? (
            <TaskDetailPanelSurface
              {...props(copy, {
                callbacks: {
                  ...callbacksMock(),
                  onClose: () => setOpen(false),
                },
              })}
            />
          ) : null}
        </>
      );
    }
    renderPureUi(<Harness />);
    const opener = screen.getByRole("button", { name: "opener" });
    opener.focus();
    fireEvent.click(opener);
    expect(document.activeElement).toBe(screen.getByRole("dialog"));
    fireEvent.click(screen.getByRole("button", { name: copy.head.close }));
    await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
    expect(document.activeElement).toBe(opener);
  });

  it(`lets the container place focus instead (${locale})`, () => {
    const onReturnFocus = vi.fn((event: Event) => event.preventDefault());
    const opener = document.createElement("button");
    document.body.append(opener);
    opener.focus();
    const { unmount } = renderSheet(copy, { onReturnFocus });
    unmount();
    expect(onReturnFocus).toHaveBeenCalledTimes(1);
    expect(document.activeElement).not.toBe(opener);
    opener.remove();
  });

  it(`${fui("wm.task-detail.panel:close", locale)}closes from Escape and the close button (${locale})`, () => {
    const { callbacks } = renderSheet(copy);
    fireEvent.keyDown(screen.getByRole("dialog"), { key: "Escape" });
    fireEvent.click(screen.getByRole("button", { name: copy.head.close }));
    expect(callbacks.onClose).toHaveBeenCalledTimes(2);
  });

  it(`${fui("wm.task-detail.panel:escape", locale)}closes on Escape from inside the composer (${locale})`, () => {
    const { callbacks } = renderSheet(copy);
    fireEvent.keyDown(
      screen.getByRole("textbox", { name: copy.composer.label }),
      { key: "Escape" },
    );
    expect(callbacks.onClose).toHaveBeenCalledTimes(1);
  });

  it(`${fui("wm.task-detail.panel:kj", locale)}moves to the previous and next item with K/J and the head buttons (${locale})`, () => {
    const { callbacks } = renderSheet(copy);
    const dialog = screen.getByRole("dialog");
    fireEvent.keyDown(dialog, { key: "k" });
    fireEvent.keyDown(dialog, { key: "j" });
    fireEvent.click(screen.getByRole("button", { name: copy.head.previous }));
    fireEvent.click(screen.getByRole("button", { name: copy.head.next }));
    expect(callbacks.onPrevious).toHaveBeenCalledTimes(2);
    expect(callbacks.onNext).toHaveBeenCalledTimes(2);

    // Typing in the composer never navigates.
    fireEvent.keyDown(
      screen.getByRole("textbox", { name: copy.composer.label }),
      { key: "j" },
    );
    expect(callbacks.onNext).toHaveBeenCalledTimes(2);
  });

  it(`disables previous/next at the ends of the lane (${locale})`, () => {
    const callbacks = { ...callbacksMock(), onPrevious: undefined };
    renderSheet(copy, { callbacks });
    expect(
      (
        screen.getByRole("button", {
          name: copy.head.previous,
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(true);
    fireEvent.keyDown(screen.getByRole("dialog"), { key: "k" });
  });

  it(`${fui("wm.task-detail.panel:head", locale)}copies the link, explains the full page and runs the more menu (${locale})`, () => {
    const { callbacks } = renderSheet(copy);
    fireEvent.click(screen.getByRole("button", { name: copy.head.copyLink }));
    expect(callbacks.onCopyLink).toHaveBeenCalledWith("item-erp-142");

    const fullPage = screen.getByRole("button", {
      name: copy.head.openFullPage,
    });
    expect(fullPage.getAttribute("aria-disabled")).toBe("true");
    expect(fullPage.getAttribute("aria-describedby")).toBeTruthy();
    expect(
      document.getElementById(fullPage.getAttribute("aria-describedby")!)
        ?.textContent,
    ).toBe(copy.head.openFullPageUnavailable);

    fireEvent.keyDown(screen.getByRole("button", { name: copy.head.more }), {
      key: "Enter",
    });
    fireEvent.click(
      screen.getByRole("menuitem", { name: copy.head.duplicate }),
    );
    expect(callbacks.onDuplicate).toHaveBeenCalledWith("item-erp-142");

    fireEvent.keyDown(screen.getByRole("button", { name: copy.head.more }), {
      key: "Enter",
    });
    fireEvent.click(screen.getByRole("menuitem", { name: copy.head.archive }));
    expect(callbacks.onArchive).toHaveBeenCalledWith("item-erp-142");
  });

  it(`${fui("wm.task-detail.panel:edit", locale)}routes title and status edits through callbacks (${locale})`, () => {
    const { callbacks } = renderSheet(copy);
    const title = screen.getByRole("textbox", { name: copy.title.edit });
    title.textContent = "Chuyển dữ liệu khách hàng";
    fireEvent.blur(title);
    expect(callbacks.onRenameTitle).toHaveBeenCalledWith("item-erp-142", {
      title: "Chuyển dữ liệu khách hàng",
    });

    fireEvent.keyDown(screen.getByRole("button", { name: /Đang làm/ }), {
      key: "Enter",
    });
    fireEvent.click(screen.getByRole("menuitem", { name: "Chờ duyệt" }));
    expect(callbacks.onUpdateFieldValue).toHaveBeenCalledWith({
      cardId: "item-erp-142",
      fieldDefinitionId: "field-status",
      value: "status-review",
    });

    fireEvent.click(screen.getByRole("tab", { name: copy.sections.history }));
    expect(callbacks.onSelectTab).toHaveBeenCalledWith("activity");
  });

  it(`${fui("wm.task-detail.panel:submit", locale)}submits a comment with the button and with ⌘↵ (${locale})`, () => {
    const { callbacks } = renderSheet(copy);
    const composer = screen.getByRole("textbox", { name: copy.composer.label });
    fireEvent.change(composer, { target: { value: "Đã nhận file" } });
    fireEvent.click(screen.getByRole("button", { name: copy.composer.submit }));
    fireEvent.change(composer, { target: { value: "Chốt thứ Tư" } });
    fireEvent.keyDown(composer, { key: "Enter", metaKey: true });

    expect(callbacks.onCreateUpdate).toHaveBeenNthCalledWith(
      1,
      {
        cardId: "item-erp-142",
        body: "Đã nhận file",
        mentionUserIds: [],
        attachmentIds: [],
      },
      expect.objectContaining({ onSuccess: expect.any(Function) }),
    );
    expect(callbacks.onCreateUpdate).toHaveBeenNthCalledWith(
      2,
      expect.objectContaining({ body: "Chốt thứ Tư" }),
      expect.anything(),
    );
    expect((composer as HTMLTextAreaElement).value).toBe("");
  });

  it(`${fui("wm.task-detail.panel:readonly", locale)}a commenter sees read-only properties and keeps the composer (${locale})`, () => {
    const { callbacks } = renderSheet(copy, {
      capabilities: commenterSheetCapabilities,
    });
    const properties = screen.getByRole("group", {
      name: copy.properties.group,
    });
    const pills = within(properties).getAllByRole("button");
    expect(pills.length).toBeGreaterThan(0);
    for (const pill of pills) {
      expect(pill.getAttribute("aria-disabled")).toBe("true");
      expect(pill.getAttribute("aria-haspopup")).toBeNull();
    }
    // "Thêm thuộc tính" stays visible but inert, as in the chi-xem mockup.
    expect(
      within(properties)
        .getByRole("button", { name: copy.properties.addProperty })
        .getAttribute("aria-disabled"),
    ).toBe("true");
    expect(properties.querySelector("[data-pill-chevron]")).toBeNull();

    fireEvent.click(
      within(properties).getByRole("button", { name: /Đang làm/ }),
    );
    expect(screen.queryByRole("menu")).toBeNull();
    expect(callbacks.onUpdateFieldValue).not.toHaveBeenCalled();

    expect(screen.queryByRole("textbox", { name: copy.title.edit })).toBeNull();
    expect(screen.queryByRole("button", { name: copy.head.more })).toBeNull();
    expect(screen.queryByRole("checkbox")).toBeNull();

    const composer = screen.getByRole("textbox", { name: copy.composer.label });
    expect((composer as HTMLTextAreaElement).disabled).toBe(false);
    fireEvent.change(composer, { target: { value: "Đã gửi file" } });
    fireEvent.keyDown(composer, { key: "Enter", ctrlKey: true });
    expect(callbacks.onCreateUpdate).toHaveBeenCalledTimes(1);
  });

  it(`a viewer cannot comment either (${locale})`, () => {
    renderSheet(copy, { capabilities: viewerCapabilities });
    expect(
      (
        screen.getByRole("textbox", {
          name: copy.composer.label,
        }) as HTMLTextAreaElement
      ).disabled,
    ).toBe(true);
  });

  it(`${fui("wm.task-detail.panel:edge", locale)}handles an item without key, due, priority, labels or children (${locale})`, () => {
    const item = erpSheetItem({
      itemKey: null,
      dueAt: null,
      dueDate: undefined,
      priority: undefined,
      labels: [],
      members: [],
      descriptionMd: undefined,
      childProgress: null,
      statusOptionId: null,
      statusCategory: null,
      status: "",
      updates: [],
    });
    renderSheet(copy, {
      card: item,
      detailData: erpSheetDetailData(item, { childItems: undefined }),
      departmentMark: undefined,
    });
    const crumb = screen.getByRole("navigation", { name: copy.head.crumb });
    expect(crumb.textContent).toBe("Triển khai ERP Q4");
    expect(screen.getByText(copy.properties.noStatus)).toBeTruthy();
    expect(screen.getByText(copy.properties.unassigned)).toBeTruthy();
    expect(screen.getByText(copy.properties.noDue)).toBeTruthy();
    expect(screen.getByText(copy.properties.noPriority)).toBeTruthy();
    expect(screen.getByText(copy.sections.descriptionUnavailable)).toBeTruthy();
    expect(screen.getByText(copy.sections.noChildItems)).toBeTruthy();
    expect(screen.getByText(copy.sections.noComments)).toBeTruthy();
  });
  it(`keeps the comment and allows a retry when posting fails (${locale})`, () => {
    const onCreateUpdate = vi.fn(
      (
        _input: Parameters<SurfaceProps["callbacks"]["onCreateUpdate"]>[0],
        options?: Parameters<SurfaceProps["callbacks"]["onCreateUpdate"]>[1],
      ) => options?.onSettled?.(),
    );
    renderSheet(copy, { callbacks: { ...callbacksMock(), onCreateUpdate } });
    const composer = screen.getByRole("textbox", { name: copy.composer.label });
    fireEvent.change(composer, { target: { value: "Đã nhận file" } });
    fireEvent.keyDown(composer, { key: "Enter", metaKey: true });
    expect((composer as HTMLTextAreaElement).value).toBe("Đã nhận file");
    const submit = screen.getByRole("button", { name: copy.composer.submit });
    expect((submit as HTMLButtonElement).disabled).toBe(false);
    fireEvent.click(submit);
    expect(onCreateUpdate).toHaveBeenCalledTimes(2);
  });

  it(`can render without its own dialog role inside a host dialog (${locale})`, () => {
    renderSheet(copy, { asDialog: false });
    expect(screen.queryByRole("dialog")).toBeNull();
    expect(screen.getByRole("heading", { level: 2, name: TITLE })).toBeTruthy();
  });
});

describe("item sheet fields on the generated board read", () => {
  it("finds the Status and priority fields by their role, not by an id suffix", () => {
    const board = erpSheetBoard();
    const STATUS_ID = "0b6f2c1e-6d9a-4f55-9a43-2f1d0c9e7a11";
    const PRIORITY_ID = "5e1d7b42-3c8f-4a0e-b6a1-9d2c4e8f1a22";
    const generated = {
      ...board,
      fieldDefinitions: board.fieldDefinitions.map((field) =>
        field.id === "field-status"
          ? { ...field, id: STATUS_ID, semanticRole: "status" as const }
          : field.id === "field-priority"
            ? { ...field, id: PRIORITY_ID, semanticRole: "priority" as const }
            : field,
      ),
    };
    const { callbacks } = renderSheet(viCopy, { board: generated });
    fireEvent.keyDown(screen.getByRole("button", { name: /Đang làm/ }), {
      key: "Enter",
    });
    fireEvent.click(screen.getByRole("menuitem", { name: "Chờ duyệt" }));
    expect(callbacks.onUpdateFieldValue).toHaveBeenCalledWith({
      cardId: "item-erp-142",
      fieldDefinitionId: STATUS_ID,
      value: "status-review",
    });
    fireEvent.keyDown(screen.getByRole("button", { name: /Khẩn/ }), {
      key: "Enter",
    });
    fireEvent.click(screen.getByRole("menuitem", { name: "Cao" }));
    expect(callbacks.onUpdateFieldValue).toHaveBeenCalledWith(
      expect.objectContaining({ fieldDefinitionId: PRIORITY_ID }),
    );
  });
});

describe("comment actions on touch screens", () => {
  it("keeps the comment actions button visible without hover on a coarse pointer", () => {
    renderSheet(viCopy);
    const triggers = screen.getAllByRole("button", {
      name: viCopy.comment.actions,
    });
    expect(triggers.length).toBeGreaterThan(0);
    for (const trigger of triggers) {
      expect(trigger.className).toContain("pointer-coarse:opacity-100");
    }
  });
});
