import { describe, expect, it, vi } from "vitest";
import {
  fireEvent,
  renderPureUi,
  screen,
  waitFor,
  within,
} from "@notrelix/testing";
import { BoardSettingsSurface } from "../board-settings-surface";
import { en } from "../board-settings.copy";
import { fillCopy } from "../board-settings-format";
import type {
  BoardSettingsActions,
  BoardSettingsSurfaceProps,
} from "../board-settings-types";
import {
  ERP_SETTINGS_TIME_ZONE,
  erpBoardSettings,
  noopSettingsActions,
} from "../../../verification/board-settings-fixtures";

const copy = en;

function spyActions() {
  return Object.fromEntries(
    Object.entries(noopSettingsActions()).map(([key, fn]) => [key, vi.fn(fn)]),
  ) as unknown as {
    [K in keyof BoardSettingsActions]: ReturnType<typeof vi.fn>;
  };
}

function renderPage(
  actions: ReturnType<typeof spyActions>,
  overrides: Partial<BoardSettingsSurfaceProps> = {},
) {
  const onReload = vi.fn();
  renderPureUi(
    <BoardSettingsSurface
      status="ready"
      settings={erpBoardSettings()}
      copy={copy}
      boardTitle="ERP"
      timeZone={ERP_SETTINGS_TIME_ZONE}
      section="fields"
      onSelectSection={vi.fn()}
      fieldId={null}
      onSelectField={vi.fn()}
      viewId={null}
      onSelectView={vi.fn()}
      actions={actions as unknown as BoardSettingsActions}
      onReload={onReload}
      onBack={vi.fn()}
      {...overrides}
    />,
  );
  return { onReload };
}

const panelOf = () =>
  screen.getByRole("complementary", {
    name: fillCopy(copy.panel.label, { name: "Mức ưu tiên" }),
  });

describe("a failed settings write", () => {
  it("keeps the typed field name, says so under it and retries", async () => {
    const actions = spyActions();
    actions.updateField!.mockResolvedValueOnce({ ok: false, reason: "failed" });
    renderPage(actions, { fieldId: "sf-priority" });
    const panel = panelOf();
    const name = within(panel).getByLabelText(copy.panel.name);
    fireEvent.change(name, { target: { value: "Độ ưu tiên" } });
    fireEvent.click(
      within(panel).getByRole("button", { name: copy.common.save }),
    );

    const alert = await within(panel).findByRole("alert");
    expect(alert.textContent).toContain(copy.feedback.failedKept);
    expect((name as HTMLInputElement).value).toBe("Độ ưu tiên");
    expect(name.getAttribute("aria-describedby")).toBe(alert.id);
    expect(name.getAttribute("aria-invalid")).toBe("true");
    // The failure is under the control, not also in the page callout.
    expect(screen.getAllByRole("alert")).toHaveLength(1);

    fireEvent.click(
      within(alert).getByRole("button", { name: copy.feedback.retry }),
    );
    await waitFor(() => expect(actions.updateField).toHaveBeenCalledTimes(2));
    expect(actions.updateField).toHaveBeenLastCalledWith("sf-priority", {
      name: "Độ ưu tiên",
    });
    await waitFor(() => expect(screen.queryByRole("alert")).toBeNull());
  });

  it("keeps a typed option name after a failed rename and offers Retry", async () => {
    const actions = spyActions();
    actions.updateOption!.mockResolvedValueOnce({
      ok: false,
      reason: "failed",
    });
    renderPage(actions, { fieldId: "sf-priority" });
    const panel = panelOf();
    const rename = within(panel).getAllByLabelText(copy.panel.optionName)[3]!;
    fireEvent.change(rename, { target: { value: "Rất thấp" } });
    fireEvent.blur(rename);

    const alert = await within(panel).findByRole("alert");
    expect((rename as HTMLInputElement).value).toBe("Rất thấp");
    expect(rename.getAttribute("aria-describedby")).toBe(alert.id);
    fireEvent.click(
      within(alert).getByRole("button", { name: copy.feedback.retry }),
    );
    await waitFor(() => expect(actions.updateOption).toHaveBeenCalledTimes(2));
  });

  it("keeps a group rename open with the typed name after a failure", async () => {
    const actions = spyActions();
    actions.updateGroup!.mockResolvedValueOnce({ ok: false, reason: "failed" });
    renderPage(actions, { section: "groups" });
    fireEvent.click(
      screen.getByRole("button", {
        name: fillCopy(copy.groups.rename, { name: "Kiểm thử" }),
      }),
    );
    const input = screen.getByLabelText(copy.groups.renameField);
    fireEvent.change(input, { target: { value: "QA" } });
    fireEvent.blur(input);

    const alert = await screen.findByRole("alert");
    expect(alert.textContent).toContain(copy.feedback.failedKept);
    const kept = screen.getByLabelText(copy.groups.renameField);
    expect((kept as HTMLInputElement).value).toBe("QA");
    fireEvent.click(
      within(alert).getByRole("button", { name: copy.feedback.retry }),
    );
    await waitFor(() => expect(actions.updateGroup).toHaveBeenCalledTimes(2));
  });

  it("reloads the latest settings on a version conflict and says so", async () => {
    const actions = spyActions();
    actions.updateField!.mockResolvedValueOnce({
      ok: false,
      reason: "conflict",
    });
    const { onReload } = renderPage(actions, { fieldId: "sf-priority" });
    const panel = panelOf();
    fireEvent.change(within(panel).getByLabelText(copy.panel.name), {
      target: { value: "Độ ưu tiên" },
    });
    fireEvent.click(
      within(panel).getByRole("button", { name: copy.common.save }),
    );
    // A stale save of a field is settled in the panel: the latest is loaded, who changed it is named,
    // and the person picks to keep theirs or take the new version.
    const alert = await within(panel).findByRole("alert");
    expect(alert.textContent).toContain(
      fillCopy(copy.conflict.titleField, { by: copy.conflict.someone }),
    );
    // Nothing is overwritten or dropped until the person chooses: the typed value stays in the form.
    expect(onReload).not.toHaveBeenCalled();
    fireEvent.click(
      within(alert).getByRole("button", { name: copy.conflict.takeTheirs }),
    );
    await waitFor(() => expect(onReload).toHaveBeenCalledTimes(1));
  });
});
