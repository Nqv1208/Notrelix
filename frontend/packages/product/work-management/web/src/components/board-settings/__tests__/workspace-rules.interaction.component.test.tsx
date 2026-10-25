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

  it(`${fui("wm.board-settings.workspace-rules:workspace-rules", locale)}lists the rules in a sentence with the groups named, and the system's reason for a rule turned off (${locale})`, () => {
    renderRules(copy);
    expect(
      screen.getByRole("heading", { level: 2, name: t.title }),
    ).toBeTruthy();
    expect(
      screen.getByText(
        fillCopy(t.line, {
          trigger: fillCopy(t.triggerMovedTo, { group: "“Cần làm”" }),
          action: fillCopy(t.actions.move, { group: "“Hoàn thành”" }),
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
    const running = screen.getByRole("switch", {
      name: fillCopy(t.toggle, {
        name: "Cất việc chốt phạm vi khi có việc mới",
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
        name: fillCopy(t.toggle, { name: "Gọi webhook khi giao việc" }),
      }),
    );
    expect(onSetEnabled).toHaveBeenCalledWith("r-2", true);
    await waitFor(() =>
      expect(screen.getByRole("alert").textContent).toContain(t.feedback.limit),
    );
  });

  it(`${fui("wm.board-settings.workspace-rules:workspace-rules-create", locale)}creates a rule from a name, a trigger, the group it watches and a group of the board (${locale})`, async () => {
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
    fireEvent.change(within(form).getByLabelText(t.form.group), {
      target: { value: "g-done" },
    });
    fireEvent.click(submit);
    await waitFor(() =>
      expect(onCreate).toHaveBeenCalledWith({
        name: "Cất việc",
        trigger: "ItemMovedToGroup",
        boardId: "b-1",
        triggerGroupId: "g-todo",
        targetGroupId: "g-done",
      }),
    );
  });

  it(`${fui("wm.board-settings.workspace-rules:workspace-rules-runs", locale)}opens the run history of a rule and shows each run with its result (${locale})`, () => {
    const onOpenRuns = vi.fn();
    renderRules(copy, {
      onOpenRuns,
      openRuleId: "r-1",
      runs: WORKSPACE_RULE_RUNS,
    });
    expect(screen.getByText(t.history.status.failed)).toBeTruthy();
    expect(screen.getByText(t.history.status.succeeded)).toBeTruthy();
    expect(screen.getByText("The target group was deleted.")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: t.history.hide }));
    expect(onOpenRuns).toHaveBeenCalledWith(null);
  });

  it(`${fui("wm.board-settings.workspace-rules:workspace-rules-forbidden", locale)}tells a member who is not a workspace admin that only admins manage automations (${locale})`, () => {
    renderRules(copy, { status: "forbidden", rules: [] });
    expect(screen.getByText(t.forbidden.title, { exact: false })).toBeTruthy();
    expect(screen.queryByRole("button", { name: t.create })).toBeNull();
  });
});
