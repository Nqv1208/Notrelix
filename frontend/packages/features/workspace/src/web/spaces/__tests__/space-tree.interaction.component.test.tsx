import * as React from "react";
import { describe, expect, it, vi } from "vitest";
import {
  act,
  fireEvent,
  renderPureUi,
  screen,
  within,
} from "@notrelix/testing";
import { SidebarProvider } from "@notrelix/ui-web";

import { shellNavigationFixture } from "../../../verification/workspace-shell-fixtures";
import {
  spaceTreeBoardsScenario,
  spaceTreeDefaultScenario,
  spaceTreeEmptyScenario,
  spaceTreeErrorScenario,
  spaceTreeHighDensityScenario,
  spaceTreeLoadingScenario,
  spaceTreeReadOnlyScenario,
  spacesCopyFixture,
} from "../../../verification/workspace-spaces-fixtures";
import { SpaceTreeNav, type SpaceTreeNavProps } from "../space-tree-nav";

const copy = spacesCopyFixture();

function Stateful(props: SpaceTreeNavProps) {
  const [expanded, setExpanded] = React.useState(props.expandedIds);
  return (
    <SpaceTreeNav
      {...props}
      expandedIds={expanded}
      onExpandedChange={(id, open) => {
        props.onExpandedChange(id, open);
        setExpanded((current) => {
          const next = new Set(current);
          if (open) next.add(id);
          else next.delete(id);
          return next;
        });
      }}
    />
  );
}

function renderTree(
  scenario = spaceTreeDefaultScenario(),
  overrides: Partial<SpaceTreeNavProps> = {},
) {
  const props: SpaceTreeNavProps = {
    ...scenario,
    navigation: shellNavigationFixture(vi.fn()),
    onExpandedChange: vi.fn(),
    onCreateSpace: vi.fn(),
    onRetry: vi.fn(),
    ...overrides,
  };
  renderPureUi(
    <SidebarProvider>
      <Stateful {...props} />
    </SidebarProvider>,
  );
  return props;
}

const item = (name: RegExp | string) => screen.getByRole("treeitem", { name });

describe("SpaceTreeNav", () => {
  it("FUI[workspace.space-tree:render] renders the org tree as a navigation tree with levels, links and Private markers", () => {
    renderTree();
    const tree = screen.getByRole("tree", { name: copy.tree.label });
    const items = within(tree).getAllByRole("treeitem");
    // Accounting is collapsed: Payroll is not rendered (lazy).
    expect(items.map((node) => node.getAttribute("data-space-id"))).toEqual([
      "sp-it",
      "sp-it-platform",
      "sp-it-infra",
      "sp-it-security",
      "sp-accounting",
      "sp-marketing",
      "sp-archive",
    ]);
    const infra = item(/Infrastructure/);
    expect(infra.getAttribute("href")).toBe(
      "/workspaces/ws-product/spaces/sp-it-infra",
    );
    expect(infra.getAttribute("aria-level")).toBe("3");
    expect(infra.getAttribute("aria-current")).toBe("page");
    expect(item(/^IT/).getAttribute("aria-expanded")).toBe("true");
    expect(item(/Accounting/).getAttribute("aria-expanded")).toBe("false");
    expect(infra.getAttribute("aria-expanded")).toBeNull();
    expect(item(/^IT/).textContent).toContain(copy.tree.private);
    expect(item(/Marketing/).textContent).not.toContain(copy.tree.private);
    expect(item(/2025 campaigns/).textContent).toContain(copy.tree.archived);
    // One tab stop: the current Space.
    expect(
      items.filter((node) => node.getAttribute("tabindex") === "0"),
    ).toEqual([infra]);
  });

  it("FUI[workspace.space-tree:keyboard] follows the WAI-ARIA tree keys", () => {
    const props = renderTree();
    const infra = item(/Infrastructure/);
    act(() => infra.focus());
    fireEvent.keyDown(infra, { key: "ArrowUp" });
    expect(document.activeElement).toBe(item(/Platform/));
    // ← on an open Space closes it.
    fireEvent.keyDown(item(/Platform/), { key: "ArrowLeft" });
    expect(props.onExpandedChange).toHaveBeenLastCalledWith(
      "sp-it-platform",
      false,
    );
    expect(
      screen.queryByRole("treeitem", { name: /Infrastructure/ }),
    ).toBeNull();
    // ← on a closed Space goes to its parent.
    fireEvent.keyDown(item(/Platform/), { key: "ArrowLeft" });
    expect(document.activeElement).toBe(item(/^IT/));
    fireEvent.keyDown(item(/^IT/), { key: "End" });
    expect(document.activeElement).toBe(item(/2025 campaigns/));
    fireEvent.keyDown(item(/2025 campaigns/), { key: "ArrowUp" });
    fireEvent.keyDown(item(/Marketing/), { key: "ArrowUp" });
    // → on a closed Space opens it, a second → enters it.
    const accounting = item(/Accounting/);
    expect(document.activeElement).toBe(accounting);
    fireEvent.keyDown(accounting, { key: "ArrowRight" });
    expect(props.onExpandedChange).toHaveBeenLastCalledWith(
      "sp-accounting",
      true,
    );
    fireEvent.keyDown(item(/Accounting/), { key: "ArrowRight" });
    expect(document.activeElement).toBe(item(/Payroll/));
    fireEvent.keyDown(item(/Payroll/), { key: "Home" });
    expect(document.activeElement).toBe(item(/^IT/));
    // Type-ahead.
    fireEvent.keyDown(item(/^IT/), { key: "m" });
    expect(document.activeElement).toBe(item(/Marketing/));
  });

  it("FUI[workspace.space-tree:navigate] hands plain clicks to the host and toggles from the chevron", () => {
    const onNavigated = vi.fn();
    const props = renderTree(undefined, { onNavigated });
    fireEvent.click(item(/Marketing/));
    expect(props.navigation.onNavigate).toHaveBeenCalledWith({
      section: "space",
      spaceId: "sp-marketing",
    });
    expect(onNavigated).toHaveBeenCalledTimes(1);
    const toggle = item(/Accounting/).querySelector("[data-tree-toggle]")!;
    fireEvent.click(toggle);
    expect(props.onExpandedChange).toHaveBeenCalledWith("sp-accounting", true);
    expect(props.navigation.onNavigate).toHaveBeenCalledTimes(1);
    expect(item(/Payroll/)).toBeTruthy();
  });

  it("FUI[workspace.space-tree:states] shows loading, error with retry and a teaching empty state", () => {
    const { unmount } = renderPureUi(
      <SidebarProvider>
        <SpaceTreeNav
          {...spaceTreeLoadingScenario()}
          onExpandedChange={vi.fn()}
          onCreateSpace={vi.fn()}
          onRetry={vi.fn()}
        />
      </SidebarProvider>,
    );
    expect(
      screen.getByRole("status", { name: copy.tree.loading }),
    ).toBeTruthy();
    unmount();

    const onRetry = vi.fn();
    const error = renderPureUi(
      <SidebarProvider>
        <SpaceTreeNav
          {...spaceTreeErrorScenario()}
          onExpandedChange={vi.fn()}
          onCreateSpace={vi.fn()}
          onRetry={onRetry}
        />
      </SidebarProvider>,
    );
    fireEvent.click(screen.getByRole("button", { name: copy.tree.retry }));
    expect(onRetry).toHaveBeenCalledTimes(1);
    error.unmount();

    renderTree(spaceTreeEmptyScenario());
    expect(screen.getByText(copy.tree.emptyManager)).toBeTruthy();
    expect(screen.queryByRole("tree")).toBeNull();
  });

  it("FUI[workspace.space-tree:create] offers a new top-level Space only to workspace managers", () => {
    const props = renderTree();
    fireEvent.click(screen.getByRole("button", { name: copy.tree.create }));
    expect(props.onCreateSpace).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.space-tree:read-only] hides creation for members who cannot create top-level Spaces", () => {
    renderTree(spaceTreeReadOnlyScenario());
    expect(screen.queryByRole("button", { name: copy.tree.create })).toBeNull();
    expect(item(/Payroll/)).toBeTruthy();
  });

  it("FUI[workspace.space-tree:windowing] windows 525 visible Spaces and keeps End reachable", () => {
    const started = performance.now();
    renderTree(spaceTreeHighDensityScenario());
    const elapsed = performance.now() - started;
    const tree = screen.getByRole("tree");
    const rendered = within(tree).getAllByRole("treeitem");
    expect(rendered.length).toBeLessThan(80);
    expect(rendered[0]!.getAttribute("aria-setsize")).toBe("25");
    act(() => rendered[0]!.focus());
    fireEvent.keyDown(rendered[0]!, { key: "End" });
    const last = item(/Team 25\.20/);
    expect(document.activeElement).toBe(last);
    expect(last.getAttribute("aria-posinset")).toBe("20");
    expect(last.getAttribute("aria-level")).toBe("2");
    expect(elapsed).toBeLessThan(1500);
  });

  it("FUI[workspace.space-tree:boards] lists boards under an expanded Space, before its sub-Spaces, and moves through them with the keys", () => {
    const props = renderTree(spaceTreeBoardsScenario());
    const tree = screen.getByRole("tree", { name: copy.tree.label });
    const names = within(tree)
      .getAllByRole("treeitem")
      .map((node) => node.textContent?.trim());
    expect(names.slice(0, 6)).toEqual([
      "IIT, Private",
      "ERP rollout",
      "IT budget 2027, Private",
      "PPlatform",
      "SSecurity, Private",
      "AAccounting, Private",
    ]);
    const erp = item(/ERP rollout/);
    expect(erp.getAttribute("aria-level")).toBe("2");
    expect(erp.getAttribute("aria-posinset")).toBe("1");
    expect(erp.getAttribute("aria-setsize")).toBe("4");
    expect(item(/Platform/).getAttribute("aria-posinset")).toBe("3");
    expect(item(/IT budget 2027/).querySelector("svg")).toBeTruthy();

    act(() => item(/^IT(?! budget)/).focus());
    fireEvent.keyDown(item(/^IT(?! budget)/), { key: "ArrowDown" });
    expect(document.activeElement).toBe(erp);
    fireEvent.keyDown(erp, { key: "ArrowDown" });
    expect(document.activeElement).toBe(item(/IT budget 2027/));
    // ← on a board goes to its Space.
    fireEvent.keyDown(item(/IT budget 2027/), { key: "ArrowLeft" });
    expect(document.activeElement).toBe(item(/^IT(?! budget)/));

    fireEvent.click(erp, { button: 0 });
    expect(props.navigation.onNavigate).toHaveBeenCalledWith({
      section: "board",
      boardId: "board-it-erp",
    });

    // A Space with only boards can still be opened.
    const marketing = item(/Marketing/);
    expect(marketing.getAttribute("aria-expanded")).toBe("false");
    fireEvent.keyDown(marketing, { key: "ArrowRight" });
    expect(props.onExpandedChange).toHaveBeenLastCalledWith(
      "sp-marketing",
      true,
    );
    expect(item(/Campaign calendar/)).toBeTruthy();
  });
});
