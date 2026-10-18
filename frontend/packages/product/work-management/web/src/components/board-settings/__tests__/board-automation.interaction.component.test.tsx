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
import type { BoardSettingsSurfaceProps } from "../board-settings-types";
import type { AutomationPanelProps } from "../automation-types";
import {
  ERP_SETTINGS_TIME_ZONE,
  erpBoardSettings,
  erpSettingsSpace,
  noopSettingsActions,
} from "../../../verification/board-settings-fixtures";
import {
  automationPanel,
  automationRuns,
  blockedRule,
  draftRule,
  eventRule,
  noopAutomationActions,
  scheduleRule,
} from "../../../verification/board-automation-fixtures";

/** The interaction-case marker, once per case: on the Vietnamese run only. */
const fui = (id: string, locale: string) =>
  locale === "vi" ? `FUI[wm.board-settings:${id}] ` : "";

function renderAutomation(
  copy: BoardSettingsCopy,
  panel: Partial<AutomationPanelProps>,
  settings: BoardSettingsSurfaceProps["settings"] = erpBoardSettings(),
) {
  const pageProps: BoardSettingsSurfaceProps = {
    status: "ready",
    settings,
    copy,
    boardTitle: "Triển khai ERP Q4",
    space: erpSettingsSpace,
    timeZone: ERP_SETTINGS_TIME_ZONE,
    section: "automations",
    onSelectSection: vi.fn(),
    fieldId: null,
    onSelectField: vi.fn(),
    viewId: null,
    onSelectView: vi.fn(),
    actions: noopSettingsActions(),
    automation: automationPanel(panel),
    onReload: vi.fn(),
    onBack: vi.fn(),
    boardHref: "/workspaces/w/boards/b",
    onOpenAccess: vi.fn(),
  };
  return {
    ...renderPureUi(<BoardSettingsSurface {...pageProps} />),
    pageProps,
  };
}

const ready = (rule: typeof draftRule): Partial<AutomationPanelProps> => ({
  ruleId: rule.id,
  rule: { status: "ready", data: rule },
});

describe.each([
  ["vi", viCopy],
  ["en", en],
] as const)("board automations (%s)", (locale, copy) => {
  const t = copy.automations;
  const e = t.editor;

  it(`${fui("automation-list", locale)}creates a draft, opens a rule and the run log from the list (${locale})`, async () => {
    const base = erpBoardSettings();
    const onCreateRule = vi.fn(() => Promise.resolve(true));
    const onOpenRule = vi.fn();
    const onOpenLog = vi.fn();
    renderAutomation(
      copy,
      { onCreateRule, onOpenRule, onOpenLog },
      {
        ...base,
        automations: [
          ...base.automations,
          {
            id: "sa-draft",
            name: "Báo kế toán khi việc Kế toán hoàn thành",
            enabled: false,
            draft: true,
            when: "Trạng thái chuyển sang Hoàn thành",
            then: ["Gửi thông báo cho Nhóm Kế toán tổng hợp"],
            stats: "Nháp",
            runAs: { name: "Nguyễn Thu Hà", initials: "HA" },
          },
        ],
      },
    );
    expect(screen.getAllByText(e.list.drafts).length).toBeGreaterThan(0);
    fireEvent.click(screen.getByRole("button", { name: t.create }));
    expect(onCreateRule).toHaveBeenCalledTimes(1);
    const draft = screen.getByRole("article", {
      name: "Báo kế toán khi việc Kế toán hoàn thành",
    });
    // A draft has no switch: it cannot be turned on from the list.
    expect(within(draft).queryByRole("switch")).toBeNull();
    fireEvent.click(within(draft).getByRole("button", { name: t.edit }));
    expect(onOpenRule).toHaveBeenCalledWith("sa-draft");
    fireEvent.click(screen.getByRole("button", { name: t.log }));
    expect(onOpenLog).toHaveBeenCalledTimes(1);
  });

  it(`${fui("automation-editor", locale)}shows the flow, the checks and saves a draft or turns it on (${locale})`, async () => {
    const save = vi.fn(() => Promise.resolve({ ok: true as const }));
    renderAutomation(copy, {
      ...ready(draftRule),
      actions: noopAutomationActions({ save }),
    });
    expect(screen.getByRole("textbox", { name: e.nameLabel })).toHaveProperty(
      "value",
      draftRule.definition.name,
    );
    expect(screen.getByText(e.blocks.when)).toBeTruthy();
    expect(screen.getByText(e.blocks.if)).toBeTruthy();
    expect(screen.getByText(e.blocks.then)).toBeTruthy();
    expect(
      screen.getByText(
        fillCopy(e.checks["trigger-valid"], {
          event: "Trạng thái thay đổi sang Hoàn thành",
        }),
      ),
    ).toBeTruthy();
    fireEvent.change(screen.getByRole("textbox", { name: e.nameLabel }), {
      target: { value: "Báo kế toán" },
    });
    fireEvent.click(screen.getByRole("button", { name: e.footer.saveDraft }));
    await waitFor(() => expect(save).toHaveBeenCalledTimes(1));
    expect(save).toHaveBeenLastCalledWith(
      expect.objectContaining({ name: "Báo kế toán" }),
      "draft",
    );
    fireEvent.click(screen.getByRole("button", { name: e.footer.enable }));
    await waitFor(() => expect(save).toHaveBeenCalledTimes(2));
    expect(save).toHaveBeenLastCalledWith(expect.anything(), "enable");
  });

  it(`${fui("automation-pickers", locale)}picks another event and adds, moves and removes a step (${locale})`, async () => {
    const save = vi.fn(() => Promise.resolve({ ok: true as const }));
    renderAutomation(copy, {
      ...ready(draftRule),
      actions: noopAutomationActions({ save }),
    });
    fireEvent.click(
      screen.getByRole("button", {
        name: e.pickers.events["status-changed"].label,
      }),
    );
    const eventDialog = await screen.findByRole("dialog", {
      name: e.pickers.eventAria,
    });
    fireEvent.click(
      within(eventDialog).getByRole("button", {
        name: new RegExp(e.pickers.events["comment-added"].label),
      }),
    );
    fireEvent.click(screen.getByRole("button", { name: e.addAction }));
    const actionDialog = await screen.findByRole("dialog", {
      name: e.pickers.actionAria,
    });
    fireEvent.click(
      within(actionDialog).getByRole("button", {
        name: new RegExp(e.pickers.actions["archive-item"].label),
      }),
    );
    expect(screen.getByRole("list", { name: e.stepsAria })).toBeTruthy();
    expect(
      screen.getAllByRole("button", {
        name: new RegExp(fillCopy(e.stepMenu, { n: "\\d" })),
      }),
    ).toHaveLength(2);
    fireEvent.click(
      screen.getByRole("button", { name: fillCopy(e.stepMenu, { n: 2 }) }),
    );
    fireEvent.click(
      await screen.findByRole("menuitem", { name: e.stepRemove }),
    );
    expect(
      screen.queryByRole("button", { name: fillCopy(e.stepMenu, { n: 2 }) }),
    ).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: e.footer.saveDraft }));
    await waitFor(() => expect(save).toHaveBeenCalledTimes(1));
    const [definition] = save.mock.calls[0] as unknown as [
      { trigger: { kind: string }; steps: unknown[] },
    ];
    expect(definition.trigger.kind).toBe("comment-added");
    expect(definition.steps).toHaveLength(1);
  });

  it(`${fui("automation-running", locale)}saves a running rule as a new version only after a change (${locale})`, async () => {
    const save = vi.fn(() => Promise.resolve({ ok: true as const }));
    const setEnabled = vi.fn(() => Promise.resolve({ ok: true as const }));
    renderAutomation(copy, {
      ...ready(eventRule),
      actions: noopAutomationActions({ save, setEnabled }),
    });
    const button = screen.getByRole("button", {
      name: fillCopy(e.footer.saveVersion, { version: 4 }),
    });
    expect(button).toHaveProperty("disabled", true);
    fireEvent.change(screen.getByRole("textbox", { name: e.nameLabel }), {
      target: { value: "Gửi duyệt" },
    });
    expect(button).toHaveProperty("disabled", false);
    fireEvent.click(button);
    await waitFor(() => expect(save).toHaveBeenCalledTimes(1));
    expect(save).toHaveBeenLastCalledWith(
      expect.objectContaining({ name: "Gửi duyệt" }),
      "version",
    );
    fireEvent.click(
      screen.getByRole("switch", {
        name: fillCopy(e.toggle, { name: "Gửi duyệt" }),
      }),
    );
    await waitFor(() =>
      expect(setEnabled).toHaveBeenCalledWith("sa-review", false),
    );
    // Versions of a rule that waits for an event.
    expect(
      screen.getByRole("button", {
        name: fillCopy(e.aside.viewAria, { n: 2 }),
      }),
    ).toBeTruthy();
  });

  it(`${fui("automation-schedule", locale)}edits the schedule and lists the recent runs (${locale})`, async () => {
    const save = vi.fn(() => Promise.resolve({ ok: true as const }));
    const onOpenLog = vi.fn();
    renderAutomation(copy, {
      ...ready(scheduleRule),
      onOpenLog,
      actions: noopAutomationActions({ save }),
    });
    const group = screen.getByRole("group", { name: e.schedule.group });
    fireEvent.change(within(group).getByLabelText(e.schedule.time), {
      target: { value: "08:30" },
    });
    fireEvent.click(
      within(group).getByRole("button", { name: e.schedule.dayLong.sat }),
    );
    fireEvent.click(
      within(group).getByRole("radio", { name: new RegExp(e.schedule.skip) }),
    );
    fireEvent.click(
      screen.getByRole("button", {
        name: fillCopy(e.footer.saveVersion, { version: 3 }),
      }),
    );
    await waitFor(() => expect(save).toHaveBeenCalledTimes(1));
    const [definition] = save.mock.calls[0] as unknown as [
      {
        trigger: {
          schedule: { time: string; days: number[]; missed: string };
        };
      },
    ];
    expect(definition.trigger.schedule.time).toBe("08:30");
    expect(definition.trigger.schedule.days).not.toContain(6);
    expect(definition.trigger.schedule.missed).toBe("skip");
    expect(screen.getByText(e.aside.recent)).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: e.aside.openLog }));
    expect(onOpenLog).toHaveBeenCalledTimes(1);
  });

  it(`${fui("automation-blocked", locale)}cannot be turned on while a step writes into an archived field (${locale})`, async () => {
    const restoreField = vi.fn(() => Promise.resolve({ ok: true as const }));
    renderAutomation(copy, {
      ...ready(blockedRule),
      actions: noopAutomationActions({ restoreField }),
    });
    expect(screen.getByRole("alert").textContent).toContain("Ngày bàn giao");
    expect(
      screen.getByRole("button", { name: e.footer.enable }),
    ).toHaveProperty("disabled", true);
    expect(
      screen.getByRole("switch", {
        name: fillCopy(e.toggle, { name: blockedRule.definition.name }),
      }),
    ).toHaveProperty("disabled", true);
    // A draft can still be saved.
    expect(
      screen.getByRole("button", { name: e.footer.saveDraft }),
    ).toHaveProperty("disabled", false);
    const restore = screen.getAllByRole("button", {
      name: e.checks.restoreField,
    });
    fireEvent.click(restore[restore.length - 1]!);
    await waitFor(() =>
      expect(restoreField).toHaveBeenCalledWith("sf-handover"),
    );
    // Choosing another field opens the list of fields of that step.
    const choose = screen.getAllByRole("button", {
      name: e.checks.chooseField,
    });
    fireEvent.click(choose[choose.length - 1]!);
    expect(await screen.findByRole("listbox")).toBeTruthy();
  });

  it(`${fui("automation-state", locale)}says when the rule is loading, missing or failed to load (${locale})`, () => {
    const onReloadRule = vi.fn();
    const first = renderAutomation(copy, {
      ruleId: "sa-draft",
      rule: { status: "loading", data: null },
    });
    expect(screen.getByRole("status", { name: e.loading })).toBeTruthy();
    first.unmount();
    const second = renderAutomation(copy, {
      ruleId: "sa-draft",
      rule: { status: "error", data: null },
      onReloadRule,
    });
    fireEvent.click(screen.getByRole("button", { name: e.retry }));
    expect(onReloadRule).toHaveBeenCalledTimes(1);
    second.unmount();
    renderAutomation(copy, {
      ruleId: "sa-draft",
      rule: { status: "not-found", data: null },
    });
    expect(screen.getByText(e.notFound)).toBeTruthy();
  });

  it(`${fui("automation-log", locale)}filters the run log and opens a failed run with how to fix it (${locale})`, async () => {
    const onOpenRun = vi.fn();
    const { rerender, pageProps } = renderAutomation(copy, {
      log: true,
      runs: { status: "ready", data: automationRuns },
      onOpenRun,
    });
    const table = screen.getByRole("table", { name: e.log.table });
    // Header row plus the five runs of the fixture.
    expect(within(table).getAllByRole("row")).toHaveLength(6);
    fireEvent.click(screen.getByRole("tab", { name: new RegExp(e.log.error) }));
    expect(within(table).getAllByRole("row")).toHaveLength(2);
    fireEvent.click(
      screen.getByRole("button", {
        name: fillCopy(e.log.stepsAria, {
          rule: "Khẩn → báo Trưởng phòng",
          time: "18:05",
        }),
      }),
    );
    expect(onOpenRun).toHaveBeenCalledWith("run-4");
    rerender(
      <BoardSettingsSurface
        {...pageProps}
        automation={automationPanel({
          log: true,
          runId: "run-4",
          runs: { status: "ready", data: automationRuns },
          onOpenRun,
        })}
      />,
    );
    const sheet = await screen.findByRole("dialog", {
      name: "Khẩn → báo Trưởng phòng",
    });
    expect(
      within(sheet).getByText(
        fillCopy(e.run.step, {
          n: 2,
          title: 'Đăng tin Microsoft Teams vào kênh "ERP – Điều hành"',
        }),
      ),
    ).toBeTruthy();
    expect(
      within(sheet).getByRole("button", { name: e.run.rerun }),
    ).toHaveProperty("disabled", true);
    fireEvent.click(within(sheet).getByRole("button", { name: e.run.close }));
    expect(onOpenRun).toHaveBeenLastCalledWith(null);
  });
});
