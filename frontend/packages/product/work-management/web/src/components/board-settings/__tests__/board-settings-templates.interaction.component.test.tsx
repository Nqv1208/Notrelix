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
  BoardTemplatesPanelProps,
} from "../board-settings-types";
import {
  ERP_SETTINGS_TIME_ZONE,
  erpBoardSettings,
  noopSettingsActions,
} from "../../../verification/board-settings-fixtures";

function panel(
  overrides: Partial<BoardTemplatesPanelProps> = {},
): BoardTemplatesPanelProps {
  return {
    status: "ready",
    itemCount: 24,
    templates: [
      {
        id: "t-1",
        name: "Sprint",
        description: "Two weeks",
        status: "published",
      },
      { id: "t-2", name: "UAT", description: null, status: "draft" },
      { id: "t-3", name: "Hiring", description: null, status: "archived" },
    ],
    onReload: vi.fn(),
    onSave: vi.fn().mockResolvedValue({ ok: true }),
    onPublish: vi.fn().mockResolvedValue({ ok: true }),
    onArchive: vi.fn().mockResolvedValue({ ok: true }),
    onDelete: vi.fn().mockResolvedValue({ ok: true }),
    ...overrides,
  };
}

function renderTemplates(
  templates: BoardTemplatesPanelProps | undefined,
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
      section="templates"
      onSelectSection={vi.fn()}
      fieldId={null}
      onSelectField={vi.fn()}
      viewId={null}
      onSelectView={vi.fn()}
      actions={noopSettingsActions()}
      onReload={vi.fn()}
      onBack={vi.fn()}
      templates={templates}
      {...overrides}
    />,
  );
}

describe.each([
  ["en", en],
  ["vi", viCopy],
] as const)("board settings templates (%s)", (_locale, copy) => {
  const t = copy.templates;

  it("lists the templates with their status and the actions each status allows", () => {
    renderTemplates(panel(), copy);
    const list = screen.getByRole("list", { name: t.list });
    expect(within(list).getAllByRole("listitem")).toHaveLength(3);
    // A draft is published, a published template is archived, an archived one only deleted.
    expect(
      screen.getByRole("button", {
        name: fillCopy(t.publish, { name: "UAT" }),
      }),
    ).toBeTruthy();
    expect(
      screen.queryByRole("button", {
        name: fillCopy(t.publish, { name: "Sprint" }),
      }),
    ).toBeNull();
    expect(
      screen.getByRole("button", {
        name: fillCopy(t.archive, { name: "Sprint" }),
      }),
    ).toBeTruthy();
    expect(
      screen.queryByRole("button", {
        name: fillCopy(t.archive, { name: "Hiring" }),
      }),
    ).toBeNull();
    expect(
      screen.getByRole("button", {
        name: fillCopy(t.delete, { name: "Hiring" }),
      }),
    ).toBeTruthy();
  });

  it("is a section only when the page is given the templates", () => {
    renderTemplates(undefined, copy);
    expect(
      screen.queryByRole("button", { name: copy.menu.templates }),
    ).toBeNull();
  });

  it("saving grows the form in place, sends the name, description and the items choice, then folds it into a saved note", async () => {
    const templates = panel();
    renderTemplates(templates, copy);
    fireEvent.click(screen.getByRole("button", { name: t.save }));

    const form = screen.getByRole("form");
    const name = within(form).getByLabelText(t.nameLabel);
    // The board's own title is the starting name.
    expect((name as HTMLInputElement).value).toBe(
      erpBoardSettings().general.title,
    );
    fireEvent.change(name, { target: { value: "  ERP rollout  " } });
    fireEvent.change(within(form).getByLabelText(t.descriptionLabel), {
      target: { value: "For new branches" },
    });
    fireEvent.click(
      within(form).getByRole("checkbox", {
        name: fillCopy(t.includeItemsCount_other, { count: 24 }),
      }),
    );
    fireEvent.click(within(form).getByRole("button", { name: t.saveConfirm }));

    await waitFor(() =>
      expect(templates.onSave).toHaveBeenCalledWith({
        name: "ERP rollout",
        description: "For new branches",
        includeItems: true,
      }),
    );
    await waitFor(() => expect(screen.queryByRole("form")).toBeNull());
    expect(screen.getByRole("status").textContent).toContain("ERP rollout");
  });

  it("saves only the structure by default and keeps the typed name when the write fails", async () => {
    const templates = panel({
      onSave: vi.fn().mockResolvedValue({ ok: false, reason: "failed" }),
    });
    renderTemplates(templates, copy);
    fireEvent.click(screen.getByRole("button", { name: t.save }));
    const form = screen.getByRole("form");
    fireEvent.change(within(form).getByLabelText(t.nameLabel), {
      target: { value: "Kept name" },
    });
    fireEvent.click(within(form).getByRole("button", { name: t.saveConfirm }));

    await waitFor(() =>
      expect(templates.onSave).toHaveBeenCalledWith({
        name: "Kept name",
        includeItems: false,
      }),
    );
    expect(await screen.findByRole("alert")).toBeTruthy();
    expect(
      (
        within(screen.getByRole("form")).getByLabelText(
          t.nameLabel,
        ) as HTMLInputElement
      ).value,
    ).toBe("Kept name");
  });

  it("publishes a draft and archives a published template", async () => {
    const templates = panel();
    renderTemplates(templates, copy);
    fireEvent.click(
      screen.getByRole("button", {
        name: fillCopy(t.publish, { name: "UAT" }),
      }),
    );
    await waitFor(() =>
      expect(templates.onPublish).toHaveBeenCalledWith("t-2"),
    );
    fireEvent.click(
      screen.getByRole("button", {
        name: fillCopy(t.archive, { name: "Sprint" }),
      }),
    );
    await waitFor(() =>
      expect(templates.onArchive).toHaveBeenCalledWith("t-1"),
    );
  });

  it("asks before deleting and deletes only on confirm", async () => {
    const templates = panel();
    renderTemplates(templates, copy);
    fireEvent.click(
      screen.getByRole("button", {
        name: fillCopy(t.delete, { name: "Hiring" }),
      }),
    );
    expect(templates.onDelete).not.toHaveBeenCalled();
    const confirm = screen.getByRole("group", {
      name: fillCopy(t.deleteQuestion, { name: "Hiring" }),
    });
    fireEvent.click(within(confirm).getByRole("button", { name: t.keep }));
    expect(templates.onDelete).not.toHaveBeenCalled();

    fireEvent.click(
      screen.getByRole("button", {
        name: fillCopy(t.delete, { name: "Hiring" }),
      }),
    );
    fireEvent.click(
      within(
        screen.getByRole("group", {
          name: fillCopy(t.deleteQuestion, { name: "Hiring" }),
        }),
      ).getByRole("button", { name: t.deleteConfirm }),
    );
    await waitFor(() => expect(templates.onDelete).toHaveBeenCalledWith("t-3"));
  });

  it("shows the empty and the error states", () => {
    renderTemplates(panel({ templates: [] }), copy);
    expect(screen.getByText(t.empty)).toBeTruthy();
  });
});
