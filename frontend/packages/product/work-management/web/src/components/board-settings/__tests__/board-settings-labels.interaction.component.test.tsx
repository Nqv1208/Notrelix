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
  BoardLabelsPanelProps,
  BoardSettingsSurfaceProps,
} from "../board-settings-types";
import {
  ERP_SETTINGS_TIME_ZONE,
  erpBoardSettings,
  noopSettingsActions,
} from "../../../verification/board-settings-fixtures";
import { ITEM_LABEL_COLORS, itemLabelTone } from "../../shared/item-display";

function panel(
  overrides: Partial<BoardLabelsPanelProps> = {},
): BoardLabelsPanelProps {
  return {
    status: "ready",
    labels: [
      {
        id: "l-bug",
        name: "Bug",
        color: ITEM_LABEL_COLORS.security,
        itemCount: 3,
      },
      { id: "l-doc", name: "Docs", color: ITEM_LABEL_COLORS.doc, itemCount: 0 },
    ],
    onReload: vi.fn(),
    onCreate: vi.fn().mockResolvedValue({ ok: true }),
    onUpdate: vi.fn().mockResolvedValue({ ok: true }),
    onDelete: vi.fn().mockResolvedValue({ ok: true }),
    ...overrides,
  };
}

function renderLabels(
  labels: BoardLabelsPanelProps | undefined,
  copy: BoardSettingsCopy = en,
  overrides: Partial<BoardSettingsSurfaceProps> = {},
) {
  renderPureUi(
    <BoardSettingsSurface
      status="ready"
      settings={erpBoardSettings()}
      copy={copy}
      boardTitle="ERP"
      timeZone={ERP_SETTINGS_TIME_ZONE}
      section="labels"
      onSelectSection={vi.fn()}
      fieldId={null}
      onSelectField={vi.fn()}
      viewId={null}
      onSelectView={vi.fn()}
      actions={noopSettingsActions()}
      onReload={vi.fn()}
      onBack={vi.fn()}
      labels={labels}
      {...overrides}
    />,
  );
}

describe("the label colours", () => {
  it("each choice paints its own chip tone", () => {
    for (const [tone, color] of Object.entries(ITEM_LABEL_COLORS))
      expect(itemLabelTone({ name: "", color })).toBe(tone);
  });
});

describe.each([
  ["en", en],
  ["vi", viCopy],
] as const)("board settings labels (%s)", (_locale, copy) => {
  const t = copy.labels;

  it("lists the labels with their item counts", () => {
    renderLabels(panel(), copy);
    const list = screen.getByRole("list", { name: t.list });
    expect(within(list).getByText("Bug")).toBeTruthy();
    expect(
      within(list).getByText(fillCopy(t.onItems_other, { count: 3 })),
    ).toBeTruthy();
    expect(
      within(list).getByText(fillCopy(t.onItems_other, { count: 0 })),
    ).toBeTruthy();
  });

  it("creates a label with a name and a chosen colour, growing the form in place", async () => {
    const labels = panel();
    renderLabels(labels, copy);
    fireEvent.click(screen.getByRole("button", { name: t.add }));
    const name = screen.getByRole("textbox", { name: t.nameLabel });
    expect(document.activeElement).toBe(name);
    fireEvent.change(name, { target: { value: "Urgent" } });
    fireEvent.click(screen.getByRole("radio", { name: t.colors.test }));
    fireEvent.click(screen.getByRole("button", { name: t.addConfirm }));
    await waitFor(() =>
      expect(labels.onCreate).toHaveBeenCalledWith(
        "Urgent",
        ITEM_LABEL_COLORS.test,
      ),
    );
    await waitFor(() =>
      expect(screen.queryByRole("textbox", { name: t.nameLabel })).toBeNull(),
    );
  });

  it("refuses a name another label already has", () => {
    const labels = panel();
    renderLabels(labels, copy);
    fireEvent.click(screen.getByRole("button", { name: t.add }));
    const name = screen.getByRole("textbox", { name: t.nameLabel });
    fireEvent.change(name, { target: { value: "bug" } });
    expect(
      (screen.getByRole("button", { name: t.addConfirm }) as HTMLButtonElement)
        .disabled,
    ).toBe(true);
    expect(name.getAttribute("aria-describedby")).toBeTruthy();
    expect(screen.getByText(t.nameTaken)).toBeTruthy();
  });

  it("keeps the typed name and offers Retry when creating fails", async () => {
    const labels = panel({
      onCreate: vi
        .fn()
        .mockResolvedValueOnce({ ok: false, reason: "failed" })
        .mockResolvedValueOnce({ ok: true }),
    });
    renderLabels(labels, copy);
    fireEvent.click(screen.getByRole("button", { name: t.add }));
    const name = screen.getByRole("textbox", { name: t.nameLabel });
    fireEvent.change(name, { target: { value: "Urgent" } });
    fireEvent.click(screen.getByRole("button", { name: t.addConfirm }));
    const alert = await screen.findByRole("alert");
    expect((name as HTMLInputElement).value).toBe("Urgent");
    expect(name.getAttribute("aria-describedby")).toBe(alert.id);
    fireEvent.click(
      within(alert).getByRole("button", { name: copy.feedback.retry }),
    );
    await waitFor(() => expect(labels.onCreate).toHaveBeenCalledTimes(2));
  });

  it("renames a label on blur", async () => {
    const labels = panel();
    renderLabels(labels, copy);
    fireEvent.click(
      screen.getByRole("button", { name: fillCopy(t.rename, { name: "Bug" }) }),
    );
    const input = screen.getByRole("textbox", { name: t.nameLabel });
    fireEvent.change(input, { target: { value: "Defect" } });
    fireEvent.blur(input);
    await waitFor(() =>
      expect(labels.onUpdate).toHaveBeenCalledWith("l-bug", { name: "Defect" }),
    );
  });

  it("recolours a label from the colour popover", async () => {
    const labels = panel();
    renderLabels(labels, copy);
    fireEvent.click(
      screen.getByRole("button", {
        name: fillCopy(t.recolor, { name: "Bug" }),
      }),
    );
    fireEvent.click(
      await screen.findByRole("button", { name: t.colors.training }),
    );
    await waitFor(() =>
      expect(labels.onUpdate).toHaveBeenCalledWith("l-bug", {
        color: ITEM_LABEL_COLORS.training,
      }),
    );
  });

  it("confirms a delete and says how many items lose the label", async () => {
    const labels = panel();
    renderLabels(labels, copy);
    fireEvent.click(
      screen.getByRole("button", { name: fillCopy(t.delete, { name: "Bug" }) }),
    );
    const confirm = screen.getByRole("group", {
      name: fillCopy(t.deleteQuestion, { name: "Bug" }),
    });
    expect(
      within(confirm).getByText(fillCopy(t.deleteBody_other, { count: 3 }), {
        exact: false,
      }),
    ).toBeTruthy();
    expect(labels.onDelete).not.toHaveBeenCalled();
    fireEvent.click(
      within(confirm).getByRole("button", { name: t.deleteConfirm }),
    );
    await waitFor(() => expect(labels.onDelete).toHaveBeenCalledWith("l-bug"));
  });

  it("says no item has the label when the count is zero, and nothing about a count when unknown", () => {
    renderLabels(
      panel({
        labels: [
          {
            id: "l-doc",
            name: "Docs",
            color: ITEM_LABEL_COLORS.doc,
            itemCount: 0,
          },
          { id: "l-x", name: "Spare", color: ITEM_LABEL_COLORS.data },
        ],
      }),
      copy,
    );
    fireEvent.click(
      screen.getByRole("button", {
        name: fillCopy(t.delete, { name: "Docs" }),
      }),
    );
    expect(screen.getByText(t.deleteBodyNone, { exact: false })).toBeTruthy();
    fireEvent.click(
      screen.getByRole("button", {
        name: fillCopy(t.delete, { name: "Spare" }),
      }),
    );
    expect(
      screen.getByText(t.deleteBodyUnknown, { exact: false }),
    ).toBeTruthy();
  });

  it("has no Labels section when the host gives no labels", () => {
    renderLabels(undefined, copy, { section: "labels" });
    expect(screen.queryByRole("heading", { name: t.title })).toBeNull();
    expect(
      screen.queryByRole("button", {
        name: new RegExp(`^${copy.menu.labels}`),
      }),
    ).toBeNull();
  });
});
