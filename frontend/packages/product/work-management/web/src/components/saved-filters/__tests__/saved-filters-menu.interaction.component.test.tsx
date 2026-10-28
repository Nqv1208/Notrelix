import { describe, expect, it, vi } from "vitest";
import {
  fireEvent,
  renderPureUi,
  screen,
  waitFor,
  within,
} from "@notrelix/testing";
import {
  SAVED_FILTER_REVIEW,
  SAVED_FILTER_SHARED_MINE,
  SAVED_FILTER_SPRINT,
  SAVED_FILTER_URGENT,
  savedFiltersAppliedScenario,
  savedFiltersCopyFixture,
  savedFiltersDefaultScenario,
  savedFiltersEmptyScenario,
  savedFiltersErrorScenario,
  savedFiltersLoadingScenario,
  savedFiltersReadOnlyScenario,
  type SavedFiltersScenario,
} from "../../../verification/saved-filters-fixtures";
import { fillSavedFiltersCopy } from "../saved-filters.copy";
import {
  SavedFiltersMenu,
  type SavedFiltersMenuProps,
} from "../saved-filters-menu";

const LOCALES = ["vi", "en"] as const;

function renderMenu(
  scenario: SavedFiltersScenario,
  overrides: Partial<SavedFiltersMenuProps> = {},
) {
  const props: SavedFiltersMenuProps = {
    ...scenario,
    onApply: vi.fn(),
    onClearApplied: vi.fn(),
    onRetry: vi.fn(),
    onSave: vi.fn(async () => ({ ok: true }) as const),
    onRename: vi.fn(async () => ({ ok: true }) as const),
    onSetVisibility: vi.fn(),
    onRemove: vi.fn(),
    ...overrides,
  };
  const view = renderPureUi(<SavedFiltersMenu {...props} />);
  return { props, view };
}

function openList(locale: "vi" | "en" = "vi") {
  const copy = savedFiltersCopyFixture(locale);
  fireEvent.click(screen.getByRole("button", { name: copy.menu.trigger }));
}

const rowName = (message: string, name: string) =>
  fillSavedFiltersCopy(message, { name });

describe("SavedFiltersMenu", () => {
  it("FUI[wm.saved-filters.menu:save] saves the current filter under a name, personal by default or shared with the board", async () => {
    for (const locale of LOCALES) {
      const copy = savedFiltersCopyFixture(locale);
      const { props, view } = renderMenu(savedFiltersDefaultScenario(locale));

      fireEvent.click(screen.getByRole("button", { name: copy.save.trigger }));
      const form = await screen.findByRole("form", { name: copy.save.title });
      const name = within(form).getByRole("textbox", { name: copy.save.name });
      expect(
        (
          within(form).getByRole("radio", {
            name: new RegExp(copy.save.personal),
          }) as HTMLInputElement
        ).checked,
      ).toBe(true);

      fireEvent.change(name, { target: { value: "  Việc của tôi  " } });
      fireEvent.click(
        within(form).getByRole("radio", { name: new RegExp(copy.save.shared) }),
      );
      fireEvent.click(
        within(form).getByRole("button", { name: copy.save.submit }),
      );

      await waitFor(() =>
        expect(props.onSave).toHaveBeenCalledWith({
          name: "Việc của tôi",
          visibility: "board",
        }),
      );
      await waitFor(() =>
        expect(
          screen.queryByRole("form", { name: copy.save.title }),
        ).toBeNull(),
      );
      view.unmount();
    }
  });

  it("FUI[wm.saved-filters.menu:save-failed] keeps what was typed when the save fails and says why", async () => {
    const copy = savedFiltersCopyFixture("vi");
    const onSave = vi.fn(
      async () => ({ ok: false, reason: "failed" }) as const,
    );
    renderMenu(savedFiltersDefaultScenario(), { onSave });

    fireEvent.click(screen.getByRole("button", { name: copy.save.trigger }));
    const form = await screen.findByRole("form", { name: copy.save.title });
    const name = within(form).getByRole("textbox", { name: copy.save.name });
    fireEvent.change(name, { target: { value: "Bộ lọc mới" } });
    fireEvent.click(
      within(form).getByRole("button", { name: copy.save.submit }),
    );

    const alert = await screen.findByRole("alert");
    expect(alert.textContent).toBe(copy.save.failed);
    expect((name as HTMLInputElement).value).toBe("Bộ lọc mới");
    expect(name.getAttribute("aria-invalid")).toBe("true");
    // The same form retries.
    fireEvent.click(
      within(form).getByRole("button", { name: copy.save.retry }),
    );
    await waitFor(() => expect(onSave).toHaveBeenCalledTimes(2));
  });

  it("FUI[wm.saved-filters.menu:save-name] asks for a name before saving", async () => {
    const copy = savedFiltersCopyFixture("vi");
    const { props } = renderMenu(savedFiltersDefaultScenario());

    fireEvent.click(screen.getByRole("button", { name: copy.save.trigger }));
    const form = await screen.findByRole("form", { name: copy.save.title });
    fireEvent.click(
      within(form).getByRole("button", { name: copy.save.submit }),
    );

    expect((await screen.findByRole("alert")).textContent).toBe(
      copy.save.nameRequired,
    );
    expect(props.onSave).not.toHaveBeenCalled();
  });

  it("FUI[wm.saved-filters.menu:apply] lists personal filters before shared ones, applies one and clears the applied one", () => {
    for (const locale of LOCALES) {
      const copy = savedFiltersCopyFixture(locale);
      const { props, view } = renderMenu(savedFiltersAppliedScenario(locale));

      // The applied filter names the menu button.
      fireEvent.click(
        screen.getByRole("button", { name: SAVED_FILTER_URGENT.name }),
      );
      const mine = screen.getByRole("region", { name: copy.menu.mine });
      const shared = screen.getByRole("region", { name: copy.menu.shared });
      expect(
        mine.compareDocumentPosition(shared) & Node.DOCUMENT_POSITION_FOLLOWING,
      ).toBeTruthy();
      expect(within(mine).getAllByRole("listitem")).toHaveLength(2);
      expect(within(shared).getAllByRole("listitem")).toHaveLength(2);

      const applied = within(mine).getByRole("button", {
        name: rowName(copy.row.apply, SAVED_FILTER_URGENT.name),
      });
      expect(applied.getAttribute("aria-current")).toBe("true");

      fireEvent.click(
        within(shared).getByRole("button", {
          name: rowName(copy.row.apply, SAVED_FILTER_SPRINT.name),
        }),
      );
      expect(props.onApply).toHaveBeenCalledWith(SAVED_FILTER_SPRINT);

      fireEvent.click(screen.getByRole("button", { name: copy.menu.clear }));
      expect(props.onClearApplied).toHaveBeenCalledOnce();
      view.unmount();
    }
  });

  it("FUI[wm.saved-filters.menu:rename] renames a filter in place with Enter and drops the edit with Escape", async () => {
    const copy = savedFiltersCopyFixture("vi");
    const { props } = renderMenu(savedFiltersDefaultScenario());
    openList();

    fireEvent.click(
      screen.getByRole("button", {
        name: rowName(copy.row.rename, SAVED_FILTER_REVIEW.name),
      }),
    );
    const input = screen.getByRole("textbox", { name: copy.row.renameLabel });
    fireEvent.change(input, { target: { value: "Chờ duyệt gấp" } });
    fireEvent.keyDown(input, { key: "Enter" });
    await waitFor(() =>
      expect(props.onRename).toHaveBeenCalledWith(
        SAVED_FILTER_REVIEW,
        "Chờ duyệt gấp",
      ),
    );
    await waitFor(() =>
      expect(
        screen.queryByRole("textbox", { name: copy.row.renameLabel }),
      ).toBeNull(),
    );

    fireEvent.click(
      screen.getByRole("button", {
        name: rowName(copy.row.rename, SAVED_FILTER_URGENT.name),
      }),
    );
    fireEvent.keyDown(
      screen.getByRole("textbox", { name: copy.row.renameLabel }),
      { key: "Escape" },
    );
    expect(
      screen.queryByRole("textbox", { name: copy.row.renameLabel }),
    ).toBeNull();
    expect(props.onRename).toHaveBeenCalledOnce();
  });

  it("FUI[wm.saved-filters.menu:rename-failed] keeps the edit open and says so when the rename is refused", async () => {
    const copy = savedFiltersCopyFixture("vi");
    renderMenu(savedFiltersDefaultScenario(), {
      onRename: vi.fn(async () => ({ ok: false, reason: "failed" }) as const),
    });
    openList();

    fireEvent.click(
      screen.getByRole("button", {
        name: rowName(copy.row.rename, SAVED_FILTER_REVIEW.name),
      }),
    );
    const input = screen.getByRole("textbox", { name: copy.row.renameLabel });
    fireEvent.change(input, { target: { value: "Tên khác" } });
    fireEvent.keyDown(input, { key: "Enter" });

    expect((await screen.findByRole("alert")).textContent).toBe(
      rowName(copy.toast.failed, SAVED_FILTER_REVIEW.name),
    );
    expect(screen.getByRole("textbox", { name: copy.row.renameLabel })).toBe(
      input,
    );
  });

  it("FUI[wm.saved-filters.menu:share] shares a personal filter and takes back a shared one of its creator, never offered on someone else's", () => {
    const copy = savedFiltersCopyFixture("vi");
    const { props } = renderMenu(savedFiltersDefaultScenario());
    openList();

    fireEvent.click(
      screen.getByRole("button", {
        name: rowName(copy.row.share, SAVED_FILTER_REVIEW.name),
      }),
    );
    expect(props.onSetVisibility).toHaveBeenLastCalledWith(
      SAVED_FILTER_REVIEW,
      "board",
    );

    fireEvent.click(
      screen.getByRole("button", {
        name: rowName(copy.row.unshare, SAVED_FILTER_SHARED_MINE.name),
      }),
    );
    expect(props.onSetVisibility).toHaveBeenLastCalledWith(
      SAVED_FILTER_SHARED_MINE,
      "me",
    );

    // Someone else's shared filter: rename and delete for editors, no sharing switch.
    expect(
      screen.queryByRole("button", {
        name: rowName(copy.row.unshare, SAVED_FILTER_SPRINT.name),
      }),
    ).toBeNull();
    expect(
      screen.queryByRole("button", {
        name: rowName(copy.row.share, SAVED_FILTER_SPRINT.name),
      }),
    ).toBeNull();
    expect(
      screen.getByRole("button", {
        name: rowName(copy.row.rename, SAVED_FILTER_SPRINT.name),
      }),
    ).toBeTruthy();
  });

  it("FUI[wm.saved-filters.menu:remove] asks the host to delete the filter of the row", () => {
    const copy = savedFiltersCopyFixture("vi");
    const { props } = renderMenu(savedFiltersDefaultScenario());
    openList();

    fireEvent.click(
      screen.getByRole("button", {
        name: rowName(copy.row.remove, SAVED_FILTER_URGENT.name),
      }),
    );
    expect(props.onRemove).toHaveBeenCalledWith(SAVED_FILTER_URGENT);
  });

  it("FUI[wm.saved-filters.menu:read-only] gives a viewer the shared filters to apply but nothing to save, rename, share or delete", () => {
    const copy = savedFiltersCopyFixture("vi");
    const { props } = renderMenu(savedFiltersReadOnlyScenario());

    expect(
      screen.queryByRole("button", { name: copy.save.trigger }),
    ).toBeNull();
    openList();
    expect(screen.getByText(copy.menu.readOnly)).toBeTruthy();
    for (const message of [
      copy.row.rename,
      copy.row.share,
      copy.row.unshare,
      copy.row.remove,
    ]) {
      expect(
        screen.queryByRole("button", {
          name: rowName(message, SAVED_FILTER_SPRINT.name),
        }),
      ).toBeNull();
    }
    fireEvent.click(
      screen.getByRole("button", {
        name: rowName(copy.row.apply, SAVED_FILTER_SPRINT.name),
      }),
    );
    expect(props.onApply).toHaveBeenCalledWith(SAVED_FILTER_SPRINT);
  });

  it("FUI[wm.saved-filters.menu:states] shows the empty, loading and error states, and retries a failed read", () => {
    const copy = savedFiltersCopyFixture("vi");

    const empty = renderMenu(savedFiltersEmptyScenario());
    openList();
    expect(screen.getByText(copy.menu.empty)).toBeTruthy();
    empty.view.unmount();

    const loading = renderMenu(savedFiltersLoadingScenario());
    openList();
    expect(
      screen.getByRole("status", { name: copy.menu.loading }),
    ).toBeTruthy();
    loading.view.unmount();

    const failed = renderMenu(savedFiltersErrorScenario());
    openList();
    expect(screen.getByRole("alert").textContent).toContain(
      copy.menu.loadFailed,
    );
    fireEvent.click(screen.getByRole("button", { name: copy.menu.retry }));
    expect(failed.props.onRetry).toHaveBeenCalledOnce();
  });

  it("FUI[wm.saved-filters.menu:locale] speaks Vietnamese and English from the same surface", () => {
    for (const locale of LOCALES) {
      const copy = savedFiltersCopyFixture(locale);
      const { view } = renderMenu(savedFiltersDefaultScenario(locale));
      expect(
        screen.getByRole("button", { name: copy.save.trigger }),
      ).toBeTruthy();
      expect(
        screen.getByRole("button", { name: copy.menu.trigger }),
      ).toBeTruthy();
      view.unmount();
    }
    expect(savedFiltersCopyFixture("vi").save.trigger).toBe("Lưu bộ lọc");
  });
});
