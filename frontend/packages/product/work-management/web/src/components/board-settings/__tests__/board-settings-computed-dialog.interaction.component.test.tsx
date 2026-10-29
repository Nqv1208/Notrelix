import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, waitFor } from "@notrelix/testing";
import { BoardSettingsSurface } from "../board-settings-surface";
import { en, vi as viCopy } from "../board-settings.copy";
import {
  formulaNodeCount,
  formulaSettings,
  rollupSettings,
} from "../board-settings-computed-dialog";
import type {
  BoardSettingsActions,
  BoardSettingsSurfaceProps,
} from "../board-settings-types";
import {
  ERP_SETTINGS_TIME_ZONE,
  erpBoardSettings,
  noopSettingsActions,
} from "../../../verification/board-settings-fixtures";

function spyActions() {
  return Object.fromEntries(
    Object.entries(noopSettingsActions()).map(([key, fn]) => [key, vi.fn(fn)]),
  ) as unknown as {
    [K in keyof BoardSettingsActions]: ReturnType<typeof vi.fn>;
  };
}

function renderPage(
  actions: ReturnType<typeof spyActions>,
  copy = en,
  settings = erpBoardSettings(),
) {
  renderPureUi(
    <BoardSettingsSurface
      status="ready"
      settings={{
        ...settings,
        unavailable: {
          ...settings.unavailable,
          creatableFieldTypes: undefined,
        },
      }}
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
      onReload={vi.fn()}
      onBack={vi.fn()}
      {...({} as Partial<BoardSettingsSurfaceProps>)}
    />,
  );
}

async function openDialog(copy: typeof en, typeName: string) {
  fireEvent.click(screen.getByRole("button", { name: copy.fields.add }));
  fireEvent.click(
    await screen.findByRole("button", { name: new RegExp(`^${typeName}`) }),
  );
  return screen.findByRole("dialog");
}

describe("definition helpers", () => {
  it("counts the elements of a formula", () => {
    expect(formulaNodeCount("[Effort]*2")).toBe(3);
    expect(formulaNodeCount('IF([A] > 1, "x", "y")')).toBe(6);
    expect(formulaNodeCount("")).toBe(0);
    expect(formulaNodeCount("SUM([A], MAX([B], 2))")).toBe(5);
  });

  it("counts a very long run of letters in linear time", () => {
    const started = performance.now();
    expect(formulaNodeCount("a".repeat(50_000))).toBe(0);
    expect(performance.now() - started).toBeLessThan(500);
  });

  it("writes the settings JSON the server reads", () => {
    expect(JSON.parse(formulaSettings("  [Effort]*2 "))).toEqual({
      expression: "[Effort]*2",
    });
    expect(JSON.parse(rollupSettings("sum", "f1"))).toEqual({
      function: "sum",
      fieldId: "f1",
    });
    expect(JSON.parse(rollupSettings("count", "f1"))).toEqual({
      function: "count",
    });
  });
});

describe("the Formula dialog", () => {
  it("creates a formula field with its expression", async () => {
    const actions = spyActions();
    renderPage(actions);
    await openDialog(en, en.picker.names.formula);
    fireEvent.change(screen.getByLabelText(en.computedDialog.name), {
      target: { value: "Double" },
    });
    fireEvent.change(screen.getByLabelText(en.computedDialog.expression), {
      target: { value: "[Effort] * 2" },
    });
    fireEvent.click(
      screen.getByRole("button", { name: en.computedDialog.save }),
    );
    await waitFor(() => expect(actions.createField).toHaveBeenCalled());
    expect(actions.createField).toHaveBeenCalledWith("formula", {
      name: "Double",
      settings: formulaSettings("[Effort] * 2"),
    });
  });

  it("keeps what was typed and names the rule when the server refuses a cycle", async () => {
    const actions = spyActions();
    actions.createField!.mockResolvedValueOnce({
      ok: false,
      reason: "invalid",
      code: "WorkManagement_Field_ComputedFieldCycle",
    });
    renderPage(actions, viCopy as typeof en);
    await openDialog(viCopy as typeof en, viCopy.picker.names.formula);
    fireEvent.change(screen.getByLabelText(viCopy.computedDialog.name), {
      target: { value: "Vòng" },
    });
    fireEvent.change(screen.getByLabelText(viCopy.computedDialog.expression), {
      target: { value: "[Vòng] + 1" },
    });
    fireEvent.click(
      screen.getByRole("button", { name: viCopy.computedDialog.save }),
    );
    expect((await screen.findByRole("alert")).textContent).toContain(
      viCopy.computedDialog.errors.WorkManagement_Field_ComputedFieldCycle,
    );
    expect(
      (
        screen.getByLabelText(
          viCopy.computedDialog.expression,
        ) as HTMLTextAreaElement
      ).value,
    ).toBe("[Vòng] + 1");
  });

  it("refuses to save over 200 elements and an empty formula", () => {
    const actions = spyActions();
    renderPage(actions);
    return openDialog(en, en.picker.names.formula).then(() => {
      const save = screen.getByRole("button", { name: en.computedDialog.save });
      fireEvent.change(screen.getByLabelText(en.computedDialog.name), {
        target: { value: "Big" },
      });
      expect((save as HTMLButtonElement).disabled).toBe(true);
      fireEvent.change(screen.getByLabelText(en.computedDialog.expression), {
        target: { value: Array.from({ length: 101 }, () => "1").join("+") },
      });
      expect((save as HTMLButtonElement).disabled).toBe(true);
    });
  });
});

describe("the Rollup dialog", () => {
  it("creates a sum over a number field of the sub-items", async () => {
    const actions = spyActions();
    renderPage(actions);
    await openDialog(en, en.picker.names.rollup);
    fireEvent.change(screen.getByLabelText(en.computedDialog.name), {
      target: { value: "Sub effort" },
    });
    fireEvent.click(
      screen.getByRole("radio", { name: en.computedDialog.functionNames.sum }),
    );
    const source = screen.getByLabelText(
      en.computedDialog.sourceField,
    ) as HTMLSelectElement;
    expect(source.disabled).toBe(false);
    fireEvent.click(
      screen.getByRole("button", { name: en.computedDialog.save }),
    );
    await waitFor(() => expect(actions.createField).toHaveBeenCalled());
    const [kind, input] = actions.createField!.mock.calls[0] as [
      string,
      { settings: string },
    ];
    expect(kind).toBe("rollup");
    expect(JSON.parse(input.settings).function).toBe("sum");
  });

  it("counts without asking for a field", async () => {
    const actions = spyActions();
    renderPage(actions);
    await openDialog(en, en.picker.names.rollup);
    fireEvent.change(screen.getByLabelText(en.computedDialog.name), {
      target: { value: "How many" },
    });
    expect(
      (
        screen.getByLabelText(
          en.computedDialog.sourceField,
        ) as HTMLSelectElement
      ).disabled,
    ).toBe(true);
    fireEvent.click(
      screen.getByRole("button", { name: en.computedDialog.save }),
    );
    await waitFor(() => expect(actions.createField).toHaveBeenCalled());
    expect(
      JSON.parse(
        (actions.createField!.mock.calls[0]![1] as { settings: string })
          .settings,
      ),
    ).toEqual({
      function: "count",
    });
  });
});
