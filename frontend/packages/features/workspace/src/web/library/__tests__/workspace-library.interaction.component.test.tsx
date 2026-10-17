import { beforeAll, describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";

import {
  libraryCopyFixture,
  libraryDefaultScenario,
  libraryEmptyScenario,
  libraryErrorScenario,
  libraryHighDensityScenario,
  libraryReadOnlyScenario,
} from "../../../verification/workspace-library-fixtures";
import { shellNavigationFixture } from "../../../verification/workspace-shell-fixtures";
import {
  WorkspaceLibrarySurface,
  type WorkspaceLibrarySurfaceProps,
} from "../workspace-library-surface";

beforeAll(() => {
  // Radix Select measures and scrolls items; JSDOM has no layout.
  Element.prototype.scrollIntoView = () => undefined;
  Element.prototype.hasPointerCapture = () => false;
  Element.prototype.releasePointerCapture = () => undefined;
});

const copy = libraryCopyFixture();

function renderLibrary(
  scenario = libraryDefaultScenario(),
  overrides: Partial<WorkspaceLibrarySurfaceProps> = {},
) {
  const props: WorkspaceLibrarySurfaceProps = {
    ...scenario,
    navigation: shellNavigationFixture(vi.fn()),
    onCreateBoard: vi.fn(),
    onCreateDoc: vi.fn(),
    onSetPinned: vi.fn(),
    onRetry: vi.fn(),
    ...overrides,
  };
  const view = renderPureUi(<WorkspaceLibrarySurface {...props} />);
  return { props, view };
}

function bodyRowNames(): string[] {
  const table = screen.getByRole("table");
  return within(table)
    .getAllByRole("row")
    .slice(1)
    .map((row) => row.querySelector("[data-row-link]")?.textContent ?? "")
    .filter(Boolean);
}

describe("WorkspaceLibrarySurface", () => {
  it("FUI[workspace.library:render] lists Boards and Docs with type, location, updated and pin columns", () => {
    renderLibrary();
    const headers = screen
      .getAllByRole("columnheader")
      .map((header) => header.textContent);
    expect(headers).toEqual([
      "",
      copy.columns.name,
      copy.columns.type,
      copy.columns.location,
      copy.columns.updated,
      copy.columns.pinned,
    ]);
    const retro = screen.getByRole("link", { name: "Sprint 42 retro" });
    expect(retro.getAttribute("href")).toBe(
      "/workspaces/ws-product/docs/doc-retro",
    );
    const row = retro.closest("tr")!;
    expect(within(row).getByText("Team rituals")).toBeTruthy();
    expect(within(row).getByText("3 hours ago")).toBeTruthy();
    expect(
      within(row)
        .getByRole("button", { name: "Unpin Sprint 42 retro" })
        .getAttribute("aria-pressed"),
    ).toBe("true");
    // Default sort: updated, newest first; unknown times last.
    expect(bodyRowNames().slice(0, 2)).toEqual([
      "Sprint 42 retro",
      "Team rituals",
    ]);
  });

  it("FUI[workspace.library:filter] filters by type and by text, ignoring case and diacritics", () => {
    renderLibrary();
    fireEvent.click(screen.getByRole("radio", { name: copy.types.board }));
    expect(bodyRowNames().sort()).toEqual([
      "Bug Triage",
      "Product Roadmap",
      "Q4 Launch",
    ]);

    fireEvent.click(screen.getByRole("radio", { name: copy.types.all }));
    fireEvent.change(screen.getByLabelText(copy.search.label), {
      target: { value: "ONBOARDING" },
    });
    // Matches the name and the location of "Pricing research notes".
    expect(bodyRowNames().sort()).toEqual([
      "Onboarding guide",
      "Pricing research notes",
    ]);

    fireEvent.change(screen.getByLabelText(copy.search.label), {
      target: { value: "zzz" },
    });
    expect(screen.getByText(copy.noResults.title)).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: copy.noResults.clear }));
    expect(bodyRowNames()).toHaveLength(7);
  });

  it("FUI[workspace.library:sort] sorts by a column and flips direction, exposing aria-sort", () => {
    renderLibrary();
    const nameHeader = screen.getByRole("columnheader", { name: /Name/ });
    fireEvent.click(within(nameHeader).getByRole("button"));
    expect(nameHeader.getAttribute("aria-sort")).toBe("ascending");
    expect(bodyRowNames()[0]).toBe("Bug Triage");
    fireEvent.click(within(nameHeader).getByRole("button"));
    expect(nameHeader.getAttribute("aria-sort")).toBe("descending");
    expect(bodyRowNames()[0]).toBe("Team rituals");
  });

  it("FUI[workspace.library:pin] pins one row and bulk pins the selection", () => {
    const { props } = renderLibrary();
    fireEvent.click(screen.getByRole("button", { name: "Pin Q4 Launch" }));
    expect(props.onSetPinned).toHaveBeenCalledWith(
      [expect.objectContaining({ id: "board-launch" })],
      true,
    );

    fireEvent.click(
      screen.getByRole("checkbox", { name: "Select Bug Triage" }),
    );
    fireEvent.click(
      screen.getByRole("checkbox", { name: "Select Team rituals" }),
    );
    const toolbar = screen.getByRole("toolbar", { name: "2 selected" });
    fireEvent.click(
      within(toolbar).getByRole("button", { name: copy.selection.pin }),
    );
    expect(props.onSetPinned).toHaveBeenLastCalledWith(
      expect.arrayContaining([
        expect.objectContaining({ id: "board-triage" }),
        expect.objectContaining({ id: "doc-rituals" }),
      ]),
      true,
    );
    fireEvent.click(
      within(toolbar).getByRole("button", { name: copy.selection.clear }),
    );
    expect(screen.queryByRole("toolbar")).toBeNull();

    fireEvent.click(
      screen.getByRole("checkbox", { name: copy.selection.selectAll }),
    );
    expect(screen.getByRole("toolbar", { name: "7 selected" })).toBeTruthy();
  });

  it("FUI[workspace.library:keyboard] moves between rows with arrow keys and selects with Space", () => {
    renderLibrary();
    const links = () =>
      screen
        .getAllByRole("link")
        .filter((link) => link.hasAttribute("data-row-link"));
    expect(links().filter((link) => link.tabIndex === 0)).toHaveLength(1);
    const first = links()[0]!;
    first.focus();
    fireEvent.keyDown(first, { key: "ArrowDown" });
    expect(document.activeElement).toBe(links()[1]);
    fireEvent.keyDown(document.activeElement!, { key: " " });
    expect(screen.getByRole("toolbar", { name: "1 selected" })).toBeTruthy();
    fireEvent.keyDown(document.activeElement!, { key: "End" });
    expect(document.activeElement).toBe(links()[links().length - 1]);
    fireEvent.keyDown(document.activeElement!, { key: "Home" });
    expect(document.activeElement).toBe(links()[0]);
  });

  it("FUI[workspace.library:virtualize] windows 320 rows and keeps keyboard navigation working", () => {
    const started = performance.now();
    renderLibrary(libraryHighDensityScenario());
    const elapsed = performance.now() - started;
    const table = screen.getByRole("table");
    expect(table.getAttribute("aria-rowcount")).toBe("321");
    const rendered = table.querySelectorAll("tr[data-row-index]").length;
    expect(rendered).toBeGreaterThan(0);
    expect(rendered).toBeLessThan(60);
    // Rendering 320 entries stays well inside an interactive budget in JSDOM.
    expect(elapsed).toBeLessThan(1500);

    const first = table.querySelector<HTMLElement>(
      '[data-row-index="0"] [data-row-link]',
    )!;
    first.focus();
    fireEvent.keyDown(first, { key: "End" });
    const last = table.querySelector<HTMLElement>(
      '[data-row-index="319"] [data-row-link]',
    );
    expect(last).not.toBeNull();
    expect(document.activeElement).toBe(last);
    expect(screen.getByText(/320 results/)).toBeTruthy();
  });

  it("FUI[workspace.library:states] teaches the empty state, retries a failed load and hides create for guests", () => {
    const { props, view } = renderLibrary(libraryEmptyScenario());
    expect(screen.getByText(copy.empty.title)).toBeTruthy();
    fireEvent.click(screen.getAllByRole("button", { name: copy.newBoard })[1]!);
    expect(props.onCreateBoard).toHaveBeenCalledTimes(1);
    view.unmount();

    const failed = renderLibrary(libraryErrorScenario());
    fireEvent.click(screen.getByRole("button", { name: copy.error.retry }));
    expect(failed.props.onRetry).toHaveBeenCalledTimes(1);
    failed.view.unmount();

    renderLibrary(libraryReadOnlyScenario());
    expect(screen.queryByRole("button", { name: copy.newBoard })).toBeNull();
    expect(screen.getByText(copy.empty.readOnly)).toBeTruthy();
  });

  it("FUI[workspace.library:space-filter] filters by a Space, including the Spaces inside it", () => {
    renderLibrary();
    expect(bodyRowNames()).toHaveLength(7);
    fireEvent.keyDown(
      screen.getByRole("combobox", { name: copy.spaces.label }),
      {
        key: "Enter",
      },
    );
    fireEvent.click(screen.getByRole("option", { name: "IT" }));
    expect(bodyRowNames().sort()).toEqual(["Product Roadmap", "Q4 Launch"]);
    fireEvent.keyDown(
      screen.getByRole("combobox", { name: copy.spaces.label }),
      {
        key: "Enter",
      },
    );
    fireEvent.click(screen.getByRole("option", { name: copy.spaces.none }));
    expect(bodyRowNames()).not.toContain("Product Roadmap");
    expect(bodyRowNames()).toHaveLength(4);
  });

  it("FUI[workspace.library:move] offers moving a Board or a root Doc (never a nested Doc) to a Space", () => {
    const onMoveBoard = vi.fn();
    const onMoveDoc = vi.fn();
    renderLibrary(undefined, { onMoveBoard, onMoveDoc });
    fireEvent.click(
      screen.getByRole("button", { name: "Move Bug Triage to a Space" }),
    );
    expect(onMoveBoard).toHaveBeenCalledWith(
      expect.objectContaining({ id: "board-triage" }),
    );
    fireEvent.click(
      screen.getByRole("button", { name: "Move Team rituals to a Space" }),
    );
    expect(onMoveDoc).toHaveBeenCalledWith(
      expect.objectContaining({ id: "doc-rituals" }),
    );
    expect(onMoveBoard).toHaveBeenCalledTimes(1);
    // A nested Doc follows its root page (docs/product/workspaces.md §79).
    expect(
      screen.queryByRole("button", { name: "Move Sprint 42 retro to a Space" }),
    ).toBeNull();
  });
});
