import { describe, expect, it, vi } from "vitest";
import {
  fireEvent,
  renderPureUi,
  screen,
  waitFor,
  within,
} from "@notrelix/testing";
import { BoardSettingsSurface } from "../board-settings-surface";
import {
  en,
  vi as viCopy,
  type BoardSettingsCopy,
} from "../board-settings.copy";
import { fillCopy } from "../board-settings-format";
import type {
  BoardSettingsActions,
  BoardSettingsSurfaceProps,
} from "../board-settings-types";
import {
  ERP_SETTINGS_TIME_ZONE,
  emptyBoardSettings,
  erpBoardSettings,
  erpSettingsSpace,
  noopSettingsActions,
} from "../../../verification/board-settings-fixtures";

/** The interaction-case marker, once per case: on the Vietnamese run only. */
const fui = (id: string, locale: string) =>
  locale === "vi" ? `FUI[${id}] ` : "";

function props(
  copy: BoardSettingsCopy,
  overrides: Partial<BoardSettingsSurfaceProps> = {},
): BoardSettingsSurfaceProps {
  return {
    status: "ready",
    settings: erpBoardSettings(),
    copy,
    boardTitle: "Triển khai ERP Q4",
    space: erpSettingsSpace,
    askName: "Lê Hoàng Phúc",
    timeZone: ERP_SETTINGS_TIME_ZONE,
    section: "fields",
    onSelectSection: vi.fn(),
    fieldId: null,
    onSelectField: vi.fn(),
    viewId: null,
    onSelectView: vi.fn(),
    actions: noopSettingsActions(),
    onReload: vi.fn(),
    onBack: vi.fn(),
    boardHref: "/workspaces/w/boards/b",
    onOpenAccess: vi.fn(),
    onLeave: vi.fn(),
    ...overrides,
  };
}

function renderPage(
  copy: BoardSettingsCopy,
  overrides: Partial<BoardSettingsSurfaceProps> = {},
) {
  const pageProps = props(copy, overrides);
  const view = renderPureUi(<BoardSettingsSurface {...pageProps} />);
  return { ...view, props: pageProps };
}

function spyActions(): {
  [K in keyof BoardSettingsActions]: ReturnType<typeof vi.fn>;
} {
  const base = noopSettingsActions();
  return Object.fromEntries(
    Object.entries(base).map(([key, fn]) => [key, vi.fn(fn)]),
  ) as never;
}

/** Opens the styled Select behind a label and picks one option by its text. */
function choose(trigger: HTMLElement, option: string | RegExp) {
  fireEvent.keyDown(trigger, { key: "Enter" });
  fireEvent.click(screen.getByRole("option", { name: option }));
}

describe.each([
  ["vi", viCopy],
  ["en", en],
] as const)("board settings (%s)", (locale, copy) => {
  it(`${fui("wm.board-settings:loading", locale)}renders the loading state (${locale})`, () => {
    renderPage(copy, { status: "loading", settings: null });
    expect(screen.getByRole("status", { name: copy.loading })).toBeTruthy();
  });

  it(`${fui("wm.board-settings:error", locale)}renders the error state and reloads from it (${locale})`, () => {
    const { props: pageProps } = renderPage(copy, {
      status: "error",
      settings: null,
    });
    expect(screen.getByText(copy.error.title)).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: copy.error.retry }));
    expect(pageProps.onReload).toHaveBeenCalledTimes(1);
  });

  it(`${fui("wm.board-settings:no-access", locale)}shows the no-access card to a viewer who is not a manager (${locale})`, () => {
    const { props: pageProps } = renderPage(copy, {
      status: "forbidden",
      settings: null,
    });
    expect(
      screen.getByRole("heading", { level: 2, name: copy.denied.title }),
    ).toBeTruthy();
    expect(
      screen.getByText(
        (text) =>
          text.includes("Lê Hoàng Phúc") && text.includes(copy.denied.rest),
      ),
    ).toBeTruthy();
    expect(
      screen.queryByRole("navigation", { name: copy.menu.aria }),
    ).toBeNull();
    fireEvent.click(
      screen.getAllByRole("link", { name: copy.header.back }).at(-1)!,
    );
    expect(pageProps.onBack).toHaveBeenCalled();
  });

  it(`${fui("wm.board-settings:ready", locale)}renders the fields at the mockup (${locale})`, () => {
    renderPage(copy);
    expect(
      screen.getByRole("heading", { level: 1, name: copy.title }),
    ).toBeTruthy();
    const crumb = screen.getByRole("navigation", { name: copy.header.crumb });
    expect(crumb.textContent).toContain("Triển khai ERP Q4");

    const menu = screen.getByRole("navigation", { name: copy.menu.aria });
    expect(
      within(menu).getByRole("button", { name: new RegExp(copy.menu.fields) })
        .textContent,
    ).toContain("16");
    expect(
      within(menu).getByRole("button", { name: new RegExp(copy.menu.views) })
        .textContent,
    ).toContain("7");
    expect(
      within(menu).getByRole("button", { name: new RegExp(copy.menu.groups) })
        .textContent,
    ).toContain("4");
    expect(
      within(menu).getByRole("button", {
        name: new RegExp(copy.menu.automations),
      }).textContent,
    ).toContain("3");
    expect(
      within(menu).getByRole("button", { name: new RegExp(copy.menu.archived) })
        .textContent,
    ).toContain("1");

    expect(screen.getByText("status")).toBeTruthy();
    expect(screen.getByText("estimate_hours")).toBeTruthy();
    expect(screen.getByText("created_by…")).toBeTruthy();
    expect(screen.getByText("handover_date")).toBeTruthy();
    expect(screen.getByText(copy.fields.renameOnly)).toBeTruthy();
  });

  it(`${fui("wm.board-settings:menu", locale)}moves between sections and opens access (${locale})`, () => {
    const { props: pageProps } = renderPage(copy);
    const menu = screen.getByRole("navigation", { name: copy.menu.aria });
    fireEvent.click(
      within(menu).getByRole("button", { name: new RegExp(copy.menu.views) }),
    );
    expect(pageProps.onSelectSection).toHaveBeenCalledWith("views");
    fireEvent.click(
      within(menu).getByRole("button", { name: copy.menu.access }),
    );
    expect(pageProps.onOpenAccess).toHaveBeenCalledTimes(1);
    fireEvent.click(
      within(menu).getByRole("button", {
        name: new RegExp(copy.menu.archived),
      }),
    );
    expect(pageProps.onSelectSection).toHaveBeenCalledWith("fields");
  });

  it(`${fui("wm.board-settings:field-panel", locale)}edits a field and saves the changed keys only (${locale})`, async () => {
    const actions = spyActions();
    renderPage(copy, {
      fieldId: "sf-priority",
      actions: actions as unknown as BoardSettingsActions,
    });
    const panel = screen.getByRole("complementary", {
      name: fillCopy(copy.panel.label, { name: "Mức ưu tiên" }),
    });
    expect(within(panel).getByText("priority")).toBeTruthy();
    expect(within(panel).getByDisplayValue("Khẩn")).toBeTruthy();
    expect(
      within(panel).getByText(
        fillCopy(copy.panel.optionCount_other, { count: 5 }),
      ),
    ).toBeTruthy();

    const name = within(panel).getByLabelText(copy.panel.name);
    fireEvent.change(name, { target: { value: "Độ ưu tiên" } });
    fireEvent.click(
      within(panel).getByRole("switch", { name: copy.panel.required }),
    );
    fireEvent.click(
      within(panel).getByRole("button", { name: copy.common.save }),
    );
    await waitFor(() =>
      expect(actions.updateField).toHaveBeenCalledWith("sf-priority", {
        name: "Độ ưu tiên",
        required: true,
      }),
    );
  });

  it(`${fui("wm.board-settings:options", locale)}adds, renames and deletes an option at once (${locale})`, async () => {
    const actions = spyActions();
    renderPage(copy, {
      fieldId: "sf-priority",
      actions: actions as unknown as BoardSettingsActions,
    });
    const panel = screen.getByRole("complementary", {
      name: fillCopy(copy.panel.label, { name: "Mức ưu tiên" }),
    });
    fireEvent.click(
      within(panel).getByRole("button", { name: copy.panel.addOption }),
    );
    const input = within(panel).getByLabelText(copy.panel.optionName, {
      selector: "input[placeholder]",
    });
    fireEvent.change(input, { target: { value: "Chưa xác định" } });
    fireEvent.keyDown(input, { key: "Enter" });
    await waitFor(() =>
      expect(actions.addOption).toHaveBeenCalledWith("sf-priority", {
        name: "Chưa xác định",
        color: expect.any(String),
      }),
    );

    const rename = within(panel).getAllByLabelText(copy.panel.optionName)[3]!;
    fireEvent.change(rename, { target: { value: "Rất thấp" } });
    fireEvent.blur(rename);
    await waitFor(() =>
      expect(actions.updateOption).toHaveBeenCalledWith(
        "sf-priority",
        "p-low",
        {
          name: "Rất thấp",
        },
      ),
    );

    fireEvent.click(
      within(panel).getByRole("button", {
        name: fillCopy(copy.panel.removeOption, { name: "Cao" }),
      }),
    );
    await waitFor(() =>
      expect(actions.deleteOption).toHaveBeenCalledWith(
        "sf-priority",
        "p-high",
      ),
    );
  });

  it(`${fui("wm.board-settings:select-field", locale)}opens a field from its row and closes its panel (${locale})`, () => {
    const { props: pageProps } = renderPage(copy, { fieldId: "sf-priority" });
    fireEvent.click(
      screen.getByRole("button", {
        name: fillCopy(copy.fields.open, { name: "Hạn chót" }),
      }),
    );
    expect(pageProps.onSelectField).toHaveBeenCalledWith("sf-due");
    fireEvent.click(screen.getByRole("button", { name: copy.common.close }));
    expect(pageProps.onSelectField).toHaveBeenCalledWith(null);
  });

  it(`${fui("wm.board-settings:add-field", locale)}picks a field type to add (${locale})`, async () => {
    const actions = spyActions();
    renderPage(copy, { actions: actions as unknown as BoardSettingsActions });
    fireEvent.click(screen.getByRole("button", { name: copy.fields.add }));
    const picker = await screen.findByRole("dialog", {
      name: copy.picker.label,
    });
    expect(within(picker).getByText(copy.picker.groups.basic)).toBeTruthy();
    fireEvent.change(within(picker).getByLabelText(copy.picker.search), {
      target: { value: copy.picker.names.date },
    });
    fireEvent.click(
      within(picker).getByRole("button", {
        name: new RegExp(copy.picker.names.date),
      }),
    );
    await waitFor(() =>
      expect(actions.createField).toHaveBeenCalledWith("date"),
    );
  });

  it(`${fui("wm.board-settings:archive-field", locale)}shows what archiving touches before it archives (${locale})`, async () => {
    const actions = spyActions();
    const { props: pageProps } = renderPage(copy, {
      fieldId: "sf-priority",
      actions: actions as unknown as BoardSettingsActions,
    });
    const panel = screen.getByRole("complementary", {
      name: fillCopy(copy.panel.label, { name: "Mức ưu tiên" }),
    });
    fireEvent.click(
      within(panel).getByRole("button", { name: copy.panel.archive }),
    );
    const dialog = await screen.findByRole("alertdialog");
    expect(
      within(dialog).getByText(
        fillCopy(copy.archiveDialog.views_other, { count: 3 }),
      ),
    ).toBeTruthy();
    expect(
      within(dialog).getByText(
        fillCopy(copy.archiveDialog.rules_one, { count: 1 }),
      ),
    ).toBeTruthy();
    fireEvent.click(
      within(dialog).getByRole("button", { name: copy.archiveDialog.confirm }),
    );
    await waitFor(() =>
      expect(actions.archiveField).toHaveBeenCalledWith("sf-priority"),
    );
    await waitFor(() =>
      expect(pageProps.onSelectField).toHaveBeenCalledWith(null),
    );
  });

  it(`${fui("wm.board-settings:delete-field", locale)}asks before it deletes a field and offers no delete without the capability (${locale})`, async () => {
    const without = renderPage(copy, { fieldId: "sf-priority" });
    const withoutPanel = screen.getByRole("complementary", {
      name: fillCopy(copy.panel.label, { name: "Mức ưu tiên" }),
    });
    expect(
      within(withoutPanel).queryByRole("button", { name: copy.panel.delete }),
    ).toBeNull();
    without.unmount();

    const actions = spyActions();
    const base = erpBoardSettings();
    const { props: pageProps } = renderPage(copy, {
      fieldId: "sf-priority",
      settings: {
        ...base,
        unavailable: { ...base.unavailable, fieldDelete: false },
      },
      actions: actions as unknown as BoardSettingsActions,
    });
    const panel = screen.getAllByRole("complementary", {
      name: fillCopy(copy.panel.label, { name: "Mức ưu tiên" }),
    })[0]!;
    fireEvent.click(
      within(panel).getByRole("button", { name: copy.panel.delete }),
    );
    const dialog = await screen.findByRole("alertdialog");
    fireEvent.click(
      within(dialog).getByRole("button", { name: copy.deleteDialog.confirm }),
    );
    await waitFor(() =>
      expect(actions.deleteField).toHaveBeenCalledWith("sf-priority"),
    );
    await waitFor(() =>
      expect(pageProps.onSelectField).toHaveBeenCalledWith(null),
    );
  });

  it(`${fui("wm.board-settings:restore-field", locale)}restores an archived field (${locale})`, async () => {
    const actions = spyActions();
    renderPage(copy, { actions: actions as unknown as BoardSettingsActions });
    fireEvent.click(
      screen.getByRole("button", {
        name: fillCopy(copy.fields.restoreLabel, { name: "Ngày bàn giao" }),
      }),
    );
    await waitFor(() =>
      expect(actions.restoreField).toHaveBeenCalledWith("sf-handover"),
    );
  });

  it(`${fui("wm.board-settings:status-field", locale)}does not archive the status field (${locale})`, () => {
    renderPage(copy, { fieldId: "sf-status" });
    const panel = screen.getByRole("complementary", {
      name: fillCopy(copy.panel.label, { name: "Trạng thái" }),
    });
    expect(
      within(panel)
        .getByRole("button", { name: copy.panel.archive })
        .getAttribute("aria-disabled"),
    ).toBe("true");
  });

  it(`${fui("wm.board-settings:views", locale)}lists the views and edits the Kanban view (${locale})`, async () => {
    const actions = spyActions();
    const { props: pageProps } = renderPage(copy, {
      section: "views",
      viewId: "sv-kanban",
      actions: actions as unknown as BoardSettingsActions,
    });
    expect(screen.getByText(copy.views.default)).toBeTruthy();
    expect(screen.getByText(copy.views.needsFix)).toBeTruthy();
    fireEvent.click(
      screen.getByRole("button", {
        name: fillCopy(copy.views.open, { name: "Lịch" }),
      }),
    );
    expect(pageProps.onSelectView).toHaveBeenCalledWith("sv-calendar");

    const panel = screen.getByRole("complementary", {
      name: fillCopy(copy.viewPanel.label, { name: "Kanban" }),
    });
    fireEvent.click(
      within(panel).getByRole("checkbox", { name: "Ước lượng (giờ)" }),
    );
    fireEvent.click(
      within(panel).getByRole("radio", { name: copy.viewPanel.whoMe }),
    );
    fireEvent.click(
      within(panel).getByRole("button", { name: copy.viewPanel.save }),
    );
    await waitFor(() =>
      expect(actions.updateView).toHaveBeenCalledWith("sv-kanban", {
        visibility: "me",
        cardFieldIds: [
          "sf-owner",
          "sf-due",
          "sf-priority",
          "sf-module",
          "sf-progress",
          "sf-estimate",
        ],
      }),
    );
  });

  it(`${fui("wm.board-settings:view-needs-fix", locale)}explains a view whose date field is archived and fixes it (${locale})`, async () => {
    const actions = spyActions();
    renderPage(copy, {
      section: "views",
      viewId: "sv-handover",
      actions: actions as unknown as BoardSettingsActions,
    });
    const panel = screen.getByRole("complementary", {
      name: fillCopy(copy.viewPanel.label, { name: "Lịch bàn giao" }),
    });
    expect(within(panel).getByRole("alert").textContent).toContain(
      copy.viewPanel.brokenTitle,
    );
    expect(
      within(panel)
        .getByRole("button", { name: copy.viewPanel.save })
        .hasAttribute("disabled"),
    ).toBe(true);
    fireEvent.click(
      within(panel).getByRole("button", {
        name: copy.viewPanel.fixRestoreAction,
      }),
    );
    await waitFor(() =>
      expect(actions.restoreField).toHaveBeenCalledWith("sf-handover"),
    );
  });

  it(`${fui("wm.board-settings:general", locale)}saves the general settings and guards the danger zone (${locale})`, async () => {
    const actions = spyActions();
    const { props: pageProps } = renderPage(copy, {
      section: "general",
      actions: actions as unknown as BoardSettingsActions,
    });
    fireEvent.change(screen.getByLabelText(copy.general.name), {
      target: { value: "Triển khai ERP Q1" },
    });
    fireEvent.change(screen.getByLabelText(new RegExp(copy.general.key)), {
      target: { value: "erq" },
    });
    fireEvent.click(screen.getByRole("button", { name: copy.common.save }));
    await waitFor(() =>
      expect(actions.updateGeneral).toHaveBeenCalledWith({
        title: "Triển khai ERP Q1",
        itemKeyPrefix: "ERQ",
      }),
    );

    const remove = screen.getByRole("button", {
      name: copy.general.deleteAction,
    });
    expect(remove.hasAttribute("disabled")).toBe(true);
    fireEvent.change(
      screen.getByLabelText(new RegExp(copy.general.deleteConfirm)),
      {
        target: { value: "Triển khai ERP Q4" },
      },
    );
    fireEvent.click(remove);
    await waitFor(() =>
      expect(actions.deleteBoard).toHaveBeenCalledWith("Triển khai ERP Q4"),
    );
    await waitFor(() =>
      expect(pageProps.onLeave).toHaveBeenCalledWith("deleted"),
    );
  });

  it(`${fui("wm.board-settings:groups", locale)}deletes a group by moving its items first (${locale})`, async () => {
    const actions = spyActions();
    renderPage(copy, {
      section: "groups",
      actions: actions as unknown as BoardSettingsActions,
    });
    expect(
      screen.getByText(fillCopy(copy.groups.summary, { groups: 4, items: 16 })),
    ).toBeTruthy();
    fireEvent.keyDown(
      screen.getByRole("button", {
        name: fillCopy(copy.groups.more, { name: "Kiểm thử" }),
      }),
      { key: "Enter" },
    );
    fireEvent.click(
      await screen.findByRole("menuitem", { name: copy.groups.delete }),
    );
    const confirm = screen.getByRole("listitem", {
      name: fillCopy(copy.groups.deleteQuestion, { name: "Kiểm thử" }),
    });
    const submit = within(confirm).getByRole("button", {
      name: copy.groups.deleteConfirm,
    });
    // The items move to the next group unless another is chosen.
    expect(within(confirm).getByRole("combobox").textContent).toBe(
      "Đào tạo và vận hành",
    );
    choose(within(confirm).getByRole("combobox"), "Dữ liệu");
    fireEvent.click(submit);
    await waitFor(() =>
      expect(actions.deleteGroup).toHaveBeenCalledWith("g-test", "g-data"),
    );
  });

  it(`${fui("wm.board-settings:automations", locale)}turns a rule off and keeps a paused rule off (${locale})`, async () => {
    const actions = spyActions();
    renderPage(copy, {
      section: "automations",
      actions: actions as unknown as BoardSettingsActions,
    });
    fireEvent.click(
      screen.getByRole("switch", {
        name: fillCopy(copy.automations.toggle, { name: "Nhắc việc quá hạn" }),
      }),
    );
    await waitFor(() =>
      expect(actions.setAutomationEnabled).toHaveBeenCalledWith(
        "sa-overdue",
        false,
      ),
    );
    expect(
      screen
        .getByRole("switch", {
          name: fillCopy(copy.automations.toggle, {
            name: "Gắn nhóm Dữ liệu cho việc kho",
          }),
        })
        .hasAttribute("disabled"),
    ).toBe(true);
    expect(screen.getByText("lỗi một phần")).toBeTruthy();
  });

  it(`${fui("wm.board-settings:conflict", locale)}tells a refused write and reloads the settings (${locale})`, async () => {
    const actions = spyActions();
    actions.restoreField!.mockResolvedValue({ ok: false, reason: "conflict" });
    const { props: pageProps } = renderPage(copy, {
      actions: actions as unknown as BoardSettingsActions,
    });
    fireEvent.click(
      screen.getByRole("button", {
        name: fillCopy(copy.fields.restoreLabel, { name: "Ngày bàn giao" }),
      }),
    );
    const alert = await screen.findByRole("alert");
    expect(alert.textContent).toContain(copy.feedback.conflictTitle);
    // The latest settings were already read again when the write was refused.
    expect(pageProps.onReload).toHaveBeenCalledTimes(1);
    fireEvent.click(
      within(alert).getByRole("button", { name: copy.common.close }),
    );
    await waitFor(() => expect(screen.queryByRole("alert")).toBeNull());
  });

  it(`${fui("wm.board-settings:failed", locale)}tells a write that failed and keeps the page (${locale})`, async () => {
    const actions = spyActions();
    actions.restoreField!.mockResolvedValue({ ok: false, reason: "failed" });
    renderPage(copy, { actions: actions as unknown as BoardSettingsActions });
    fireEvent.click(
      screen.getByRole("button", {
        name: fillCopy(copy.fields.restoreLabel, { name: "Ngày bàn giao" }),
      }),
    );
    expect((await screen.findByRole("alert")).textContent).toContain(
      copy.feedback.failed,
    );
    expect(
      screen.getByRole("heading", { level: 1, name: copy.title }),
    ).toBeTruthy();
  });

  it(`${fui("wm.board-settings:empty", locale)}renders a board with no fields, rules or extra groups (${locale})`, () => {
    renderPage(copy, { settings: emptyBoardSettings() });
    expect(screen.getByText(copy.fields.empty.title)).toBeTruthy();
  });
});
