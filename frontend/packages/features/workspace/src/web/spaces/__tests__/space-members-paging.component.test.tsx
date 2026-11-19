import { beforeAll, describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";

import {
  spacePageMembersScenario,
  spacePageOverviewScenario,
  type SpacePageScenario,
} from "../../../verification/workspace-space-page-fixtures";
import { spacesCopyFixture } from "../../../verification/workspace-spaces-fixtures";
import { shellNavigationFixture } from "../../../verification/workspace-shell-fixtures";
import {
  WorkspaceSpaceSurface,
  type WorkspaceSpaceSurfaceProps,
} from "../workspace-space-surface";
import {
  DEFAULT_MEMBERS_PAGING,
  pagingOf,
} from "./space-members-paging-fixtures";

beforeAll(() => {
  Element.prototype.scrollIntoView = () => undefined;
  Element.prototype.hasPointerCapture = () => false;
  Element.prototype.releasePointerCapture = () => undefined;
});

const copy = spacesCopyFixture("vi");

function render(
  scenario: SpacePageScenario,
  overrides: Partial<WorkspaceSpaceSurfaceProps> = {},
) {
  const props: WorkspaceSpaceSurfaceProps = {
    ...scenario,
    navigation: shellNavigationFixture(() => undefined),
    onCreateChild: vi.fn(),
    onCreateBoard: vi.fn(),
    onCreateDoc: vi.fn(),
    onMoveSpace: vi.fn(),
    onUnarchive: vi.fn(),
    onSave: vi.fn(),
    onArchive: vi.fn(),
    onDelete: null,
    onAddMembers: vi.fn(),
    onExpandTeam: vi.fn(),
    onChangeMemberRole: vi.fn(),
    onRemoveMember: vi.fn(),
    onRetry: vi.fn(),
    onRetryContent: vi.fn(),
    onRetryMembers: vi.fn(),
    ...overrides,
  };
  return { props, view: renderPureUi(<WorkspaceSpaceSurface {...props} />) };
}

function renderPaged(
  paging: ReturnType<typeof pagingOf>,
  overrides: Partial<WorkspaceSpaceSurfaceProps> = {},
) {
  // The tab lists what the paged read returned; `members` stays the whole roster for the dialogs.
  const scenario = spacePageMembersScenario();
  return render(scenario, {
    membersTab: scenario.members,
    membersPaging: paging,
    membersPageSize: 100,
    ...overrides,
  });
}

describe("Space members tab on a paged read", () => {
  it("asks the producer for the role and kind the person picks, instead of filtering the loaded rows", () => {
    const paging = pagingOf();
    renderPaged(paging);
    expect(paging.onFilterChange).toHaveBeenLastCalledWith({
      role: null,
      kind: null,
    });

    fireEvent.click(screen.getByRole("button", { name: /Vai trò/ }));
    fireEvent.click(screen.getByRole("menuitemradio", { name: "Quản trị" }));
    expect(paging.onFilterChange).toHaveBeenLastCalledWith({
      role: "Admin",
      kind: null,
    });

    fireEvent.click(screen.getByRole("button", { name: /Vào qua/ }));
    fireEvent.click(screen.getByRole("menuitemradio", { name: "Cả nhóm" }));
    expect(paging.onFilterChange).toHaveBeenLastCalledWith({
      role: "Admin",
      kind: "Team",
    });

    fireEvent.click(screen.getByRole("button", { name: /Vào qua/ }));
    fireEvent.click(screen.getByRole("menuitemradio", { name: "Trực tiếp" }));
    expect(paging.onFilterChange).toHaveBeenLastCalledWith({
      role: "Admin",
      kind: "User",
    });
  });

  it("keeps the rows the producer returned: they are not hidden again on the client", () => {
    // The producer sent these rows for the filter it was asked; the tab shows them all.
    renderPaged(pagingOf());
    fireEvent.click(screen.getByRole("button", { name: /Vai trò/ }));
    fireEvent.click(screen.getByRole("menuitemradio", { name: "Quản trị" }));
    expect(screen.getByText("Lê Hoàng Phúc", { selector: "b" })).toBeTruthy();
    expect(screen.getByText("Vũ Thanh", { selector: "b" })).toBeTruthy();
  });

  it("offers Load more only while the producer has another page, and asks for it once", () => {
    const paging = pagingOf({ hasMore: true });
    renderPaged(paging);
    const more = screen.getByRole("button", { name: copy.members.loadMore });
    fireEvent.click(more);
    expect(paging.onLoadMore).toHaveBeenCalledTimes(1);
  });

  it("shows no Load more on the last page", () => {
    renderPaged(pagingOf({ hasMore: false }));
    expect(
      screen.queryByRole("button", { name: copy.members.loadMore }),
    ).toBeNull();
  });

  it("disables Load more and says so while the next page loads", () => {
    renderPaged(pagingOf({ hasMore: true, loadingMore: true }));
    const busy = screen.getByRole("button", {
      name: copy.members.loadingMore,
    });
    expect((busy as HTMLButtonElement).disabled).toBe(true);
  });

  it("says when more people could not be loaded and keeps the loaded rows and the button", () => {
    renderPaged(pagingOf({ hasMore: true, loadMoreFailed: true }));
    expect(screen.getByRole("alert").textContent).toBe(
      copy.members.loadMoreFailed,
    );
    expect(screen.getByText("Lê Hoàng Phúc", { selector: "b" })).toBeTruthy();
    expect(
      screen.getByRole("button", { name: copy.members.loadMore }),
    ).toBeTruthy();
  });

  it("says a name search covers only the loaded people, and offers to load more, when nothing loaded matches", () => {
    const paging = pagingOf({ hasMore: true });
    renderPaged(paging);
    fireEvent.change(screen.getByRole("searchbox"), {
      target: { value: "zzz" },
    });
    const empty = screen.getByText(/Tìm kiếm chỉ chạy trên những người đã tải/);
    expect(empty.textContent).toMatch(/Không ai trong \d+ người đã tải khớp/);
    const list = screen.getByRole("table");
    fireEvent.click(
      within(list).getByRole("button", { name: copy.members.loadMore }),
    );
    expect(paging.onLoadMore).toHaveBeenCalledTimes(1);
  });

  it("says plainly that no one matches when every person is loaded", () => {
    renderPaged(pagingOf({ hasMore: false }));
    fireEvent.change(screen.getByRole("searchbox"), {
      target: { value: "zzz" },
    });
    expect(screen.getByText(copy.members.noMatches)).toBeTruthy();
    expect(screen.queryByText(/đã tải/)).toBeNull();
  });

  it("says no one matches, not that the department is empty, when a filter returns nobody", () => {
    renderPaged(pagingOf(), {
      membersTab: { status: "ready", data: [] },
      invites: [],
    });
    expect(screen.getByText(copy.members.empty)).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: /Vai trò/ }));
    fireEvent.click(screen.getByRole("menuitemradio", { name: "Người xem" }));
    expect(screen.getByText(copy.members.noMatches)).toBeTruthy();
    expect(screen.queryByText(copy.members.empty)).toBeNull();
  });

  it("shows the loading and error states of the paged read", () => {
    const loading = renderPaged(pagingOf(), {
      membersTab: { status: "loading" },
    });
    expect(
      screen.getByRole("status", { name: copy.members.loading }),
    ).toBeTruthy();
    loading.view.unmount();
    const onRetryMembers = vi.fn();
    renderPaged(pagingOf(), {
      membersTab: { status: "error" },
      onRetryMembers,
    });
    fireEvent.click(screen.getByRole("button", { name: copy.members.retry }));
    expect(onRetryMembers).toHaveBeenCalled();
  });

  it("takes the head counts from the summary, and shows none when it has not loaded, never counting the loaded page", () => {
    const withSummary = renderPaged(pagingOf(), {
      membersSummary: { admins: 3, members: 400, viewers: 97 },
    });
    expect(
      screen.getByText(/3 quản trị · 400 thành viên · 97 người xem/),
    ).toBeTruthy();
    withSummary.view.unmount();
    const { view } = renderPaged(pagingOf(), { membersSummary: null });
    expect(view.container.querySelector('[data-f="sum"]')).toBeNull();
  });

  it("keeps the whole roster for the dialogs while the tab shows a page", () => {
    const scenario = spacePageMembersScenario();
    renderPaged(DEFAULT_MEMBERS_PAGING, {
      membersTab: { status: "ready", data: [] },
      members: scenario.members,
    });
    expect(screen.getByRole("button", { name: /^Thêm/ })).toBeTruthy();
  });
});

describe("Department page at 390 px (no horizontal overflow)", () => {
  /** jsdom has no layout; what it can show is every width rule the page carries. */
  const FIXED_PX = /(^|\s)(min-)?w-\[(\d+)px\]/;

  function renderOverview() {
    return render(spacePageOverviewScenario()).view.container;
  }

  it("stacks the Board rows below md: two columns, no header row, no minimum width", () => {
    const root = renderOverview();
    const list = root.querySelector('[data-f="list"]') as HTMLElement;
    const header = list.querySelector('[data-f="lh"]') as HTMLElement;
    expect(header.className).toContain("max-md:hidden");
    const row = list.querySelector('[data-f="br"]') as HTMLElement;
    expect(row.className).toContain("grid-cols-[minmax(0,1fr)_auto]");
    expect(row.className).toContain("md:grid-cols-[var(--board-cols)]");
    // The wide table's columns come back only from md, through a variable.
    expect(row.getAttribute("style")).toContain("--board-cols");
    expect(row.getAttribute("style")).not.toContain("grid-template-columns");
  });

  it("scrolls the Board list sideways only from md up, never on a phone", () => {
    const list = renderOverview().querySelector(
      '[data-f="list"]',
    ) as HTMLElement;
    const classes = list.className.split(/\s+/);
    expect(classes).not.toContain("max-xl:overflow-x-auto");
    expect(classes.some((c) => /^(max-xl|max-lg):\[&>\*\]:min-w/.test(c))).toBe(
      false,
    );
    expect(classes).toContain("md:max-xl:overflow-x-auto");
    expect(classes).toContain("md:max-xl:[&>*]:min-w-[760px]");
  });

  it("hides the progress and people columns of a Board row on a phone and keeps the attention line", () => {
    const row = renderOverview().querySelector('[data-f="br"]') as HTMLElement;
    const progress = row.querySelector('[data-f="pg"]') as HTMLElement;
    expect(progress.parentElement?.className).toContain("max-md:hidden");
    const attention = row.querySelector('[data-f="att"]') as HTMLElement;
    expect(attention.className).toContain("max-md:col-span-2");
    expect(attention.className).not.toContain("max-md:hidden");
  });

  it.each([
    ["overview", () => renderOverview()],
    ["members tab", () => renderPaged(pagingOf()).view.container],
  ])(
    "carries no fixed pixel width on the %s that a 358 px column cannot hold",
    (_name, mount) => {
      const root = mount();
      const offenders = [...root.querySelectorAll<HTMLElement>("*")]
        .filter((el) => {
          const cls = (el.getAttribute("class") ?? "").split(/\s+/);
          // A fixed width that a `max-md:w-full` rule undoes is a desktop width, not a phone one.
          if (cls.includes("max-md:w-full")) return false;
          return cls.some((c) => {
            // Only unconditional rules count: a `max-md:` or `md:` rule is a decision for another width.
            if (c.includes(":")) return false;
            const m = FIXED_PX.exec(` ${c}`);
            return m !== null && Number(m[3]) > 330;
          });
        })
        .map((el) => el.getAttribute("class"));
      expect(offenders).toEqual([]);
    },
  );

  it("collapses the members list below md to the person, the role and the menu", () => {
    const { view } = renderPaged(pagingOf());
    const list = view.container.querySelector('[data-f="list"]') as HTMLElement;
    const row = list.querySelector('[data-f="mr"]') as HTMLElement;
    expect(row.className).toContain(
      "max-md:grid-cols-[minmax(0,1fr)_auto_36px]",
    );
    expect(
      (row.querySelector('[data-f="src"]') as HTMLElement).className,
    ).toContain("max-md:hidden");
    expect(
      (row.querySelector('[data-f="act"]') as HTMLElement).className,
    ).toContain("max-md:hidden");
  });
});

describe("Department overview loading states", () => {
  it("shows the page skeleton while the department loads, and the content skeleton while its Boards and Docs load", () => {
    const page = render(spacePageOverviewScenario(), {
      space: { status: "loading" },
    });
    expect(
      screen.getByRole("status", { name: copy.page.loading }),
    ).toBeTruthy();
    page.view.unmount();

    render(spacePageOverviewScenario(), { content: { status: "loading" } });
    expect(
      screen.getByRole("status", { name: copy.overview.loading }),
    ).toBeTruthy();
  });
});
