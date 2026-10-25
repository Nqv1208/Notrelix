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
  BoardSettingsSurfaceProps,
  WorkspaceRulesPanelProps,
} from "../board-settings-types";
import {
  ERP_SETTINGS_TIME_ZONE,
  erpBoardSettings,
  noopSettingsActions,
} from "../../../verification/board-settings-fixtures";
import {
  WORKSPACE_RULE_RUNS,
  workspaceRulesPanel,
} from "../../../verification/workspace-rules-fixtures";

/** The interaction-case marker, once per case: on the Vietnamese run only. */
const fui = (id: string, locale: string) =>
  locale === "vi" ? `FUI[${id}] ` : "";

function renderRules(
  copy: BoardSettingsCopy,
  rules: Partial<WorkspaceRulesPanelProps> = {},
) {
  const workspaceRules = workspaceRulesPanel(rules);
  const props: BoardSettingsSurfaceProps = {
    status: "ready",
    settings: {
      ...erpBoardSettings(),
      automations: [],
      unavailable: { automations: true },
    },
    copy,
    boardTitle: "Triển khai ERP Q4",
    timeZone: ERP_SETTINGS_TIME_ZONE,
    section: "automations",
    onSelectSection: vi.fn(),
    fieldId: null,
    onSelectField: vi.fn(),
    viewId: null,
    onSelectView: vi.fn(),
    actions: noopSettingsActions(),
    workspaceRules,
    onReload: vi.fn(),
    onBack: vi.fn(),
  };
  renderPureUi(<BoardSettingsSurface {...props} />);
  return workspaceRules;
}

describe.each([
  ["vi", viCopy],
  ["en", en],
] as const)("workspace automation rules (%s)", (locale, copy) => {
  const t = copy.automations.rules;

  it(`${fui("wm.board-settings.workspace-rules:workspace-rules", locale)}lists each rule as one sentence with its conditions and its actions in order, and the system's reason for a rule turned off (${locale})`, () => {
    renderRules(copy);
    expect(
      screen.getByRole("heading", { level: 2, name: t.title }),
    ).toBeTruthy();
    expect(
      screen.getByText(
        fillCopy(t.line, {
          trigger: fillCopy(t.triggerMovedTo, { group: "“Hoàn thành”" }),
          conditions: fillCopy(t.ifPart, {
            conditions: fillCopy(t.conditionLabels.status, {
              status: "“Đang làm”",
            }),
          }),
          actions: [
            fillCopy(t.actionLabels.ChangeStatus, { status: "“Chờ duyệt”" }),
            fillCopy(t.actionLabels.AssignPerson, { person: "“Lan Phạm”" }),
            fillCopy(t.actionLabels.NotifyMember, { person: "“Lan Phạm”" }),
          ].join(t.thenJoin),
        }),
      ),
    ).toBeTruthy();
    expect(
      screen.getByText(
        fillCopy(t.disabledBy, {
          reason: "Disabled after 5 consecutive failed executions.",
        }),
      ),
    ).toBeTruthy();
    expect(screen.getByText(fillCopy(t.version, { version: 3 }))).toBeTruthy();
    const running = screen.getByRole("switch", {
      name: fillCopy(t.toggle, {
        name: "Chuyển việc xong cho người duyệt",
      }),
    });
    expect(running.getAttribute("aria-checked")).toBe("true");
  });

  it(`${fui("wm.board-settings.workspace-rules:workspace-rules-toggle", locale)}turns a rule on from its switch and says why when the write fails (${locale})`, async () => {
    const onSetEnabled = vi
      .fn()
      .mockResolvedValueOnce({ ok: false, reason: "limit" });
    renderRules(copy, { onSetEnabled });
    fireEvent.click(
      screen.getByRole("switch", {
        name: fillCopy(t.toggle, { name: "Báo khi giao việc" }),
      }),
    );
    expect(onSetEnabled).toHaveBeenCalledWith("r-2", true);
    await waitFor(() =>
      expect(screen.getByRole("alert").textContent).toContain(t.feedback.limit),
    );
  });

  it(`${fui("wm.board-settings.workspace-rules:workspace-rules-create", locale)}builds a rule from a trigger, a condition and several ordered actions, and caps the actions at five (${locale})`, async () => {
    const onCreate = vi.fn().mockResolvedValue({ ok: true });
    renderRules(copy, { onCreate });
    fireEvent.click(screen.getByRole("button", { name: t.create }));
    const form = screen.getByRole("form", { name: t.form.title });
    const submit = within(form).getByRole("button", { name: t.form.submit });
    expect(submit.hasAttribute("disabled")).toBe(true);
    fireEvent.change(within(form).getByLabelText(t.form.name), {
      target: { value: "Cất việc" },
    });
    fireEvent.change(within(form).getByLabelText(t.form.trigger), {
      target: { value: "ItemMovedToGroup" },
    });
    fireEvent.change(within(form).getByLabelText(t.form.triggerGroup), {
      target: { value: "g-todo" },
    });
    // The first action: move the item.
    fireEvent.change(within(form).getByLabelText(t.form.group), {
      target: { value: "g-done" },
    });
    // A condition on the status.
    fireEvent.click(
      within(form).getByRole("button", { name: t.form.conditionTypes.status }),
    );
    fireEvent.change(within(form).getByLabelText(t.form.status), {
      target: { value: "s-open" },
    });
    // A second action: assign a person.
    fireEvent.click(
      within(form).getByRole("button", { name: t.form.addAction }),
    );
    const second = within(form).getByRole("group", {
      name: fillCopy(t.form.action, { index: 2 }),
    });
    fireEvent.change(
      within(second).getByLabelText(fillCopy(t.form.action, { index: 2 })),
      { target: { value: "AssignPerson" } },
    );
    fireEvent.change(within(second).getByLabelText(t.form.person), {
      target: { value: "u-minh" },
    });
    fireEvent.click(submit);
    await waitFor(() =>
      expect(onCreate).toHaveBeenCalledWith({
        name: "Cất việc",
        trigger: "ItemMovedToGroup",
        boardId: "b-1",
        triggerGroupId: "g-todo",
        conditions: [{ type: "status", optionId: "s-open" }],
        actions: [
          { type: "MoveItem", groupId: "g-done" },
          { type: "AssignPerson", userId: "u-minh" },
        ],
      }),
    );
  });

  it(`${fui("wm.board-settings.workspace-rules:workspace-rules-limit", locale)}stops adding actions at five (${locale})`, () => {
    renderRules(copy);
    fireEvent.click(screen.getByRole("button", { name: t.create }));
    const form = screen.getByRole("form", { name: t.form.title });
    for (let n = 0; n < 4; n += 1) {
      fireEvent.click(
        within(form).getByRole("button", { name: t.form.addAction }),
      );
    }
    expect(
      within(form)
        .getByRole("button", { name: t.form.addAction })
        .hasAttribute("disabled"),
    ).toBe(true);
    expect(within(form).getByText(t.form.actionLimit)).toBeTruthy();
  });

  it(`${fui("wm.board-settings.workspace-rules:workspace-rules-edit", locale)}edits a rule in place and saves it as a new version, reordering its actions (${locale})`, async () => {
    const onUpdate = vi.fn().mockResolvedValue({ ok: true });
    renderRules(copy, { onUpdate });
    fireEvent.click(
      screen.getByRole("button", {
        name: fillCopy(t.editName, {
          name: "Chuyển việc xong cho người duyệt",
        }),
      }),
    );
    const form = screen.getByRole("form", { name: t.form.editTitle });
    fireEvent.click(
      within(form).getByRole("button", {
        name: fillCopy(t.form.moveUp, { index: 2 }),
      }),
    );
    fireEvent.click(within(form).getByRole("button", { name: t.form.save }));
    await waitFor(() =>
      expect(onUpdate).toHaveBeenCalledWith("r-1", {
        name: "Chuyển việc xong cho người duyệt",
        trigger: "ItemMovedToGroup",
        triggerGroupId: "g-done",
        conditions: [{ type: "status", optionId: "s-open" }],
        actions: [
          { type: "AssignPerson", userId: "u-lan" },
          { type: "ChangeStatus", optionId: "s-review" },
          {
            type: "NotifyMember",
            userId: "u-lan",
            message: "Có việc cần duyệt",
          },
        ],
      }),
    );
  });

  it(`${fui("wm.board-settings.workspace-rules:workspace-rules-delete", locale)}asks before deleting a rule and deletes it on confirmation (${locale})`, async () => {
    const onDelete = vi.fn().mockResolvedValue({ ok: true });
    renderRules(copy, { onDelete });
    fireEvent.click(
      screen.getByRole("button", {
        name: fillCopy(t.deleteName, { name: "Báo khi giao việc" }),
      }),
    );
    expect(onDelete).not.toHaveBeenCalled();
    const dialog = screen.getByRole("alertdialog", {
      name: t.confirmDelete.title,
    });
    fireEvent.click(
      within(dialog).getByRole("button", { name: t.confirmDelete.confirm }),
    );
    await waitFor(() => expect(onDelete).toHaveBeenCalledWith("r-2"));
  });

  it(`${fui("wm.board-settings.workspace-rules:workspace-rules-runs", locale)}opens the run history of a rule and shows each run with its result (${locale})`, () => {
    const onOpenRuns = vi.fn();
    renderRules(copy, {
      onOpenRuns,
      openRuleId: "r-1",
      runs: WORKSPACE_RULE_RUNS,
    });
    expect(screen.getAllByText(t.history.status.failed).length).toBeGreaterThan(
      0,
    );
    expect(screen.getByText(t.history.status.succeeded)).toBeTruthy();
    expect(
      screen.getByText(
        "Action 2 (AssignPerson): Target action business rejection.",
      ),
    ).toBeTruthy();
    expect(
      screen.getByText(
        fillCopy(t.history.step, {
          index: 2,
          type: t.form.actionTypes.AssignPerson,
        }),
      ),
    ).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: t.history.hide }));
    expect(onOpenRuns).toHaveBeenCalledWith(null);
  });

  it(`${fui("wm.board-settings.workspace-rules:workspace-rules-forbidden", locale)}tells a member who is not a workspace admin that only admins manage automations (${locale})`, () => {
    renderRules(copy, { status: "forbidden", rules: [] });
    expect(screen.getByText(t.forbidden.title, { exact: false })).toBeTruthy();
    expect(screen.queryByRole("button", { name: t.create })).toBeNull();
  });
});
