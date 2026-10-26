import { beforeAll, describe, expect, it, vi } from "vitest";
import type { Editor } from "@tiptap/react";
import { TextSelection } from "@tiptap/pm/state";
import {
  act,
  fireEvent,
  renderPureUi,
  screen,
  within,
} from "@notrelix/testing";
import { docsCopy } from "../../verification/docs-ui-fixtures";
import { DocToolbarSurface } from "../doc-page-surfaces";
import {
  DocPageSetupDialogSurface,
  DocShortcutsDialogSurface,
  DocStatsDialogSurface,
  DEFAULT_PAGE_SETUP,
} from "../doc-tools-dialogs-surface";
import { docStats } from "../../editor/doc-stats";
import { docToDrafts } from "../../editor/block-mapping";
import { SHORTCUTS } from "../../editor/shortcut-catalog";
import {
  headlessEditor,
  paragraph,
  stubLayout,
} from "../../editor/__tests__/headless-editor";
import type { DocPageCallbacks } from "../doc-page-models";

beforeAll(stubLayout);

const VIEW = {
  panelOpen: true,
  panelTab: "links" as const,
  fullWidth: false,
  focusMode: false,
  toolbarShown: true,
  spellcheck: true,
};

function setup(
  options: {
    text?: string;
    callbacks?: DocPageCallbacks;
    canFormat?: boolean;
    view?: Partial<typeof VIEW>;
  } = {},
) {
  const editor = headlessEditor([
    paragraph("a", options.text ?? "Hello world"),
  ]);
  const onSpellcheck = vi.fn();
  const utils = renderPureUi(
    <DocToolbarSurface
      copy={docsCopy}
      fileMenuOpen={false}
      canFormat={options.canFormat ?? true}
      editor={editor as unknown as Editor}
      callbacks={options.callbacks}
      menu={{
        view: { ...VIEW, ...options.view },
        hosts: {},
        onSpellcheck,
      }}
    />,
  );
  return { editor, onSpellcheck, ...utils };
}

function select(editor: Editor, from: number, to = from) {
  act(() => {
    editor.view.dispatch(
      editor.state.tr.setSelection(
        TextSelection.create(editor.state.doc, from, to),
      ),
    );
  });
}

const trigger = (name: string) =>
  screen.getByRole("menuitem", { name, expanded: undefined });

function open(name: string) {
  fireEvent.click(trigger(name));
  return screen.getByRole("menu", { name });
}

const drafts = (editor: Editor) => docToDrafts(editor.getJSON());

describe("document menu bar", () => {
  it("is a menubar with File, Edit, View, Insert, Format and Tools", () => {
    setup();
    const bar = screen.getByRole("menubar", { name: docsCopy.menus.label });
    const names = within(bar)
      .getAllByRole("menuitem")
      .map((item) => item.textContent);
    expect(names).toEqual([
      docsCopy.menus.file,
      docsCopy.menus.edit,
      docsCopy.menus.view,
      docsCopy.menus.insert,
      docsCopy.menus.format,
      docsCopy.menus.tools,
    ]);
    // One tab stop for the whole bar.
    expect(
      within(bar)
        .getAllByRole("menuitem")
        .filter((item) => item.tabIndex === 0),
    ).toHaveLength(1);
  });

  it("opens a menu with the keyboard, moves with arrows and closes with Escape", () => {
    setup();
    const edit = trigger(docsCopy.menus.edit);
    act(() => edit.focus());
    fireEvent.keyDown(edit, { key: "ArrowDown" });
    const menu = screen.getByRole("menu", { name: docsCopy.menus.edit });
    const items = within(menu).getAllByRole("menuitem");
    // The first enabled entry has the focus.
    const enabled = items.filter(
      (item) => item.getAttribute("aria-disabled") !== "true",
    );
    expect(document.activeElement).toBe(enabled[0]);
    fireEvent.keyDown(menu, { key: "ArrowDown" });
    expect(document.activeElement).toBe(items[items.indexOf(enabled[0]!) + 1]);
    fireEvent.keyDown(menu, { key: "ArrowRight" });
    // The View menu took over.
    expect(
      screen.getByRole("menu", { name: docsCopy.menus.view }),
    ).toBeTruthy();
    expect(
      screen.queryByRole("menu", { name: docsCopy.menus.edit }),
    ).toBeNull();
    fireEvent.keyDown(screen.getByRole("menu", { name: docsCopy.menus.view }), {
      key: "Escape",
    });
    expect(screen.queryByRole("menu")).toBeNull();
    expect(document.activeElement).toBe(trigger(docsCopy.menus.view));
  });

  it("closes on a press outside the menu", () => {
    setup();
    open(docsCopy.menus.format);
    fireEvent.pointerDown(document.body);
    expect(screen.queryByRole("menu")).toBeNull();
  });

  it("Format: toggles bold on the selection, states it and shows its shortcut", () => {
    const { editor } = setup();
    select(editor, 1, 6);
    const menu = open(docsCopy.menus.format);
    fireEvent.click(
      within(menu).getByRole("menuitem", {
        name: docsCopy.menubar.format.text,
      }),
    );
    const text = screen.getByRole("menu", {
      name: docsCopy.menubar.format.text,
    });
    const bold = within(text).getByRole("menuitemcheckbox", {
      name: new RegExp(docsCopy.menubar.format.bold),
    });
    expect(bold.getAttribute("aria-checked")).toBe("false");
    expect(bold.getAttribute("aria-keyshortcuts")).toMatch(/\+B$/);
    fireEvent.click(bold);
    expect(drafts(editor)[0]?.content.runs[0]).toEqual({
      text: "Hello",
      marks: [{ type: "bold" }],
    });
    expect(screen.queryByRole("menu")).toBeNull();
  });

  it("Format: superscript, colour, alignment, indent and line spacing run their commands", () => {
    const { editor } = setup();
    select(editor, 1, 6);
    const pick = (path: string[], leaf: RegExp | string) => {
      let menu = open(docsCopy.menus.format);
      for (const step of path) {
        fireEvent.click(within(menu).getByRole("menuitem", { name: step }));
        menu = screen.getByRole("menu", { name: step });
      }
      fireEvent.click(
        within(menu).getByRole("menuitemcheckbox", { name: leaf }),
      );
    };
    pick(
      [docsCopy.menubar.format.text],
      new RegExp(docsCopy.menubar.format.superscript),
    );
    expect(drafts(editor)[0]?.content.runs[0]?.marks).toEqual([
      { type: "superscript" },
    ]);
    pick([docsCopy.menubar.format.color], docsCopy.toolbar.colorNames.danger);
    expect(drafts(editor)[0]?.content.runs[0]?.marks).toContainEqual({
      type: "color",
      token: "danger",
    });
    pick(
      [docsCopy.menubar.format.align],
      new RegExp(docsCopy.toolbar.alignCenter),
    );
    expect(drafts(editor)[0]?.properties.align).toBe("center");
    pick(
      [docsCopy.menubar.format.lineSpacing],
      docsCopy.toolbar.lineSpacingNames.loose,
    );
    expect(drafts(editor)[0]?.properties.lineSpacing).toBe("loose");
    const menu = open(docsCopy.menus.format);
    fireEvent.click(
      within(menu).getByRole("menuitem", {
        name: new RegExp(docsCopy.menubar.format.indent),
      }),
    );
    expect(drafts(editor)[0]?.properties.indent).toBe(1);
  });

  it("Format: table entries are off outside a table and say why", () => {
    setup();
    const menu = open(docsCopy.menus.format);
    const table = within(menu).getByRole("menuitem", {
      name: docsCopy.menubar.format.table,
    });
    expect(table.getAttribute("aria-disabled")).toBe("true");
    expect(table.getAttribute("title")).toBe(docsCopy.menubar.table.outside);
  });

  it("Insert: a table, a divider and a callout land after the current block", () => {
    const { editor } = setup();
    select(editor, 3);
    let menu = open(docsCopy.menus.insert);
    fireEvent.click(
      within(menu).getByRole("menuitem", {
        name: docsCopy.menubar.insert.callout,
      }),
    );
    expect(drafts(editor).map((draft) => draft.type)).toEqual([
      "paragraph",
      "callout",
    ]);
    menu = open(docsCopy.menus.insert);
    fireEvent.click(
      within(menu).getByRole("menuitem", {
        name: docsCopy.menubar.insert.table,
      }),
    );
    const sizes = screen.getByRole("menu", {
      name: docsCopy.menubar.insert.table,
    });
    fireEvent.click(within(sizes).getAllByRole("menuitem")[0]!);
    expect(drafts(editor).some((draft) => draft.type === "table")).toBe(true);
  });

  it("Insert: today's date and special characters go in as text", () => {
    const { editor } = setup({ text: "Due " });
    select(editor, 5);
    let menu = open(docsCopy.menus.insert);
    fireEvent.click(
      within(menu).getByRole("menuitem", {
        name: docsCopy.menubar.insert.date,
      }),
    );
    expect(drafts(editor)[0]?.content.runs[0]?.text).toMatch(/^Due .*\d/);
    menu = open(docsCopy.menus.insert);
    fireEvent.click(
      within(menu).getByRole("menuitem", {
        name: docsCopy.menubar.insert.specialChars,
      }),
    );
    const grid = screen.getByRole("menu", {
      name: docsCopy.menubar.insert.specialChars,
    });
    fireEvent.click(within(grid).getByRole("menuitem", { name: "→" }));
    expect(drafts(editor)[0]?.content.runs[0]?.text).toContain("→");
  });

  it("Edit: undo, select all, find and delete block", () => {
    const { editor } = setup();
    let menu = open(docsCopy.menus.edit);
    const undo = within(menu).getByRole("menuitem", {
      name: new RegExp(`^${docsCopy.menubar.edit.undo}`),
    });
    expect(undo.getAttribute("aria-disabled")).toBe("true");
    fireEvent.pointerDown(document.body);
    act(() => {
      editor.commands.insertContentAt(1, "X");
    });
    menu = open(docsCopy.menus.edit);
    fireEvent.click(
      within(menu).getByRole("menuitem", {
        name: new RegExp(`^${docsCopy.menubar.edit.undo}`),
      }),
    );
    expect(drafts(editor)[0]?.content.runs[0]?.text).toBe("Hello world");
    menu = open(docsCopy.menus.edit);
    fireEvent.click(
      within(menu).getByRole("menuitem", {
        name: new RegExp(`^${docsCopy.menubar.edit.selectAll}`),
      }),
    );
    expect(editor.state.selection.empty).toBe(false);
    menu = open(docsCopy.menus.edit);
    fireEvent.click(
      within(menu).getByRole("menuitem", {
        name: new RegExp(`^${docsCopy.menubar.edit.find}`),
      }),
    );
    expect(document.querySelector("[data-find-panel]") ?? true).toBeTruthy();
  });

  it("View: toggles say their state and call the page", () => {
    const onToggleFullWidth = vi.fn();
    const onToggleFocusMode = vi.fn();
    const onToggleToolbar = vi.fn();
    const onPanelOpenChange = vi.fn();
    const onPanelTab = vi.fn();
    const onShowWordCount = vi.fn();
    setup({
      callbacks: {
        onToggleFullWidth,
        onToggleFocusMode,
        onToggleToolbar,
        onPanelOpenChange,
        onPanelTab,
        onShowWordCount,
      },
      view: { fullWidth: true },
    });
    const click = (name: RegExp | string) => {
      const menu = open(docsCopy.menus.view);
      fireEvent.click(within(menu).getByText(name).closest("button")!);
    };
    const menu = open(docsCopy.menus.view);
    const full = within(menu).getByRole("menuitemcheckbox", {
      name: new RegExp(docsCopy.menubar.view.fullWidth),
    });
    expect(full.getAttribute("aria-checked")).toBe("true");
    expect(
      within(menu)
        .getByRole("menuitemcheckbox", {
          name: new RegExp(docsCopy.menubar.view.sidePanel),
        })
        .getAttribute("aria-checked"),
    ).toBe("true");
    fireEvent.click(full);
    expect(onToggleFullWidth).toHaveBeenCalledTimes(1);
    click(new RegExp(docsCopy.menubar.view.focusMode));
    expect(onToggleFocusMode).toHaveBeenCalledTimes(1);
    click(new RegExp(docsCopy.menubar.view.toolbar));
    expect(onToggleToolbar).toHaveBeenCalledTimes(1);
    click(new RegExp(`^${docsCopy.menubar.view.sidePanel}`));
    expect(onPanelOpenChange).toHaveBeenCalledWith(false);
    click(docsCopy.menubar.view.outline);
    expect(onPanelTab).toHaveBeenCalledWith("toc");
    click(new RegExp(docsCopy.menubar.view.wordCount));
    expect(onShowWordCount).toHaveBeenCalledTimes(1);
  });

  it("Tools: opens the shortcut sheet and toggles spell check", () => {
    const onShowShortcuts = vi.fn();
    const { onSpellcheck } = setup({ callbacks: { onShowShortcuts } });
    let menu = open(docsCopy.menus.tools);
    fireEvent.click(
      within(menu).getByRole("menuitem", {
        name: new RegExp(docsCopy.menubar.tools.shortcuts),
      }),
    );
    expect(onShowShortcuts).toHaveBeenCalledTimes(1);
    menu = open(docsCopy.menus.tools);
    fireEvent.click(
      within(menu).getByRole("menuitemcheckbox", {
        name: docsCopy.menubar.tools.spellcheck,
      }),
    );
    expect(onSpellcheck).toHaveBeenCalledWith(false);
  });

  it("read-only: edit entries are off with the reason, copying and finding stay", () => {
    const { editor } = setup({ canFormat: false });
    editor.setEditable(false);
    const menu = open(docsCopy.menus.format);
    const style = within(menu).getByRole("menuitem", {
      name: docsCopy.menubar.format.textStyle,
    });
    expect(style.getAttribute("aria-disabled")).toBe("true");
    expect(style.getAttribute("title")).toBe(docsCopy.menubar.needsEditor);
    fireEvent.pointerDown(document.body);
    const edit = open(docsCopy.menus.edit);
    expect(
      within(edit)
        .getByRole("menuitem", {
          name: new RegExp(`^${docsCopy.menubar.edit.find}`),
        })
        .getAttribute("aria-disabled"),
    ).toBeNull();
  });
});

describe("toolbar controls", () => {
  it("indent and outdent buttons move the block and reflect what is possible", () => {
    const { editor } = setup();
    select(editor, 3);
    const indent = screen.getByRole("button", {
      name: docsCopy.toolbar.indent,
    });
    const outdent = screen.getByRole("button", {
      name: docsCopy.toolbar.outdent,
    });
    expect(outdent.getAttribute("aria-disabled")).toBe("true");
    fireEvent.click(indent);
    expect(drafts(editor)[0]?.properties.indent).toBe(1);
    expect(outdent.getAttribute("aria-disabled")).toBeNull();
    fireEvent.click(outdent);
    expect(drafts(editor)[0]?.properties.indent).toBeUndefined();
  });

  it("line spacing is chosen from a popover and shows the current step", () => {
    const { editor } = setup();
    select(editor, 3);
    fireEvent.click(
      screen.getByRole("button", { name: docsCopy.toolbar.lineSpacing }),
    );
    const group = screen.getByRole("group", {
      name: docsCopy.toolbar.lineSpacing,
    });
    fireEvent.click(
      within(group).getByRole("button", {
        name: docsCopy.toolbar.lineSpacingNames.relaxed,
      }),
    );
    expect(drafts(editor)[0]?.properties.lineSpacing).toBe("relaxed");
  });

  it("the toolbar can be hidden from the View menu state", () => {
    setup({ view: { toolbarShown: false } });
    expect(screen.queryByRole("toolbar")).toBeNull();
    expect(screen.getByRole("menubar")).toBeTruthy();
  });

  it("groups that do not fit go to a More tools popover, in order", () => {
    const client = vi
      .spyOn(HTMLElement.prototype, "clientWidth", "get")
      .mockReturnValue(420);
    const offset = vi
      .spyOn(HTMLElement.prototype, "offsetWidth", "get")
      .mockReturnValue(120);
    class Resize {
      constructor(private readonly callback: (entries: unknown[]) => void) {}
      observe() {
        this.callback([]);
      }
      disconnect() {}
      unobserve() {}
    }
    vi.stubGlobal("ResizeObserver", Resize);
    try {
      setup();
      const more = screen.getByRole("button", { name: docsCopy.toolbar.more });
      expect(
        screen.queryByRole("button", { name: docsCopy.toolbar.clearFormat }),
      ).toBeNull();
      fireEvent.click(more);
      expect(
        screen.getByRole("button", { name: docsCopy.toolbar.clearFormat }),
      ).toBeTruthy();
      // The first groups stay on the bar.
      expect(
        screen.getByRole("button", { name: docsCopy.toolbar.bold }),
      ).toBeTruthy();
    } finally {
      client.mockRestore();
      offset.mockRestore();
      vi.unstubAllGlobals();
    }
  });
});

describe("tool dialogs", () => {
  it("the shortcut sheet lists every bound shortcut under its group", () => {
    renderPureUi(<DocShortcutsDialogSurface copy={docsCopy} />);
    const dialog = screen.getByRole("dialog", {
      name: docsCopy.shortcutSheet.title,
    });
    for (const entry of SHORTCUTS) {
      expect(
        within(dialog).getByText(docsCopy.shortcutSheet.items[entry.id]),
      ).toBeTruthy();
    }
    expect(within(dialog).getAllByRole("region")).toHaveLength(4);
  });

  it("the word count shows page and selection numbers", () => {
    const editor = headlessEditor([
      paragraph("a", "one two three"),
      paragraph("b", "four"),
    ]);
    act(() => {
      editor.view.dispatch(
        editor.state.tr.setSelection(
          TextSelection.create(editor.state.doc, 1, 8),
        ),
      );
    });
    const stats = docStats(editor);
    expect(stats.page.words).toBe(4);
    expect(stats.page.blocks).toBe(2);
    expect(stats.selection?.words).toBe(2);
    renderPureUi(<DocStatsDialogSurface copy={docsCopy} stats={stats} />);
    const dialog = screen.getByRole("dialog", {
      name: docsCopy.statsDialog.title,
    });
    const page = within(dialog).getByRole("region", {
      name: docsCopy.statsDialog.page,
    });
    expect(
      within(page).getByText(docsCopy.statsDialog.words).nextSibling
        ?.textContent,
    ).toBe("4");
    expect(
      within(dialog).getByRole("region", {
        name: docsCopy.statsDialog.selection,
      }),
    ).toBeTruthy();
  });

  it("page setup changes paper, orientation and margins", () => {
    const onChange = vi.fn();
    renderPureUi(
      <DocPageSetupDialogSurface
        copy={docsCopy}
        setup={DEFAULT_PAGE_SETUP}
        onChange={onChange}
      />,
    );
    fireEvent.click(
      screen.getByRole("radio", { name: docsCopy.pageSetup.papers.letter }),
    );
    expect(onChange).toHaveBeenLastCalledWith({
      ...DEFAULT_PAGE_SETUP,
      paper: "letter",
    });
    fireEvent.click(
      screen.getByRole("radio", {
        name: docsCopy.pageSetup.orientations.landscape,
      }),
    );
    expect(onChange).toHaveBeenLastCalledWith({
      ...DEFAULT_PAGE_SETUP,
      orientation: "landscape",
    });
    fireEvent.click(
      screen.getByRole("button", { name: docsCopy.pageSetup.reset }),
    );
    expect(onChange).toHaveBeenLastCalledWith(DEFAULT_PAGE_SETUP);
  });
});
