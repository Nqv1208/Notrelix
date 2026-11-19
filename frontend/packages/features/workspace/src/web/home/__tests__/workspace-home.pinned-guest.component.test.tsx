import { afterEach, describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";

import {
  NAVIGATION_RESOURCE_KIND,
  resolveNavigationEntries,
} from "../../../core/model/navigation";
import {
  continueEntriesFixture,
  continueEntry,
  homeCopyFixture,
  homeDefaultScenario,
  homeOnboardingScenario,
} from "../../../verification/workspace-home-fixtures";
import { shellNavigationFixture } from "../../../verification/workspace-shell-fixtures";
import {
  WorkspaceHomeSurface,
  type WorkspaceHomeSurfaceProps,
} from "../workspace-home-surface";

const viCopy = homeCopyFixture("vi");

function renderHome(overrides: Partial<WorkspaceHomeSurfaceProps> = {}) {
  const props: WorkspaceHomeSurfaceProps = {
    ...homeDefaultScenario(),
    navigation: shellNavigationFixture(vi.fn()),
    onComplete: vi.fn(),
    onAddItem: null,
    onCreateBoard: vi.fn(),
    onCreateDoc: vi.fn(),
    onInvite: vi.fn(),
    onRetryContinue: vi.fn(),
    onRetryMyWork: vi.fn(),
    ...overrides,
  };
  renderPureUi(<WorkspaceHomeSurface {...props} />);
  return props;
}

const pinned = [
  continueEntry({
    resourceId: "board-erp",
    name: "Lộ trình ERP",
    location: "Công nghệ",
    pinned: true,
  }),
  continueEntry({
    kind: NAVIGATION_RESOURCE_KIND.doc,
    resourceId: "doc-audit",
    name: "Kiểm toán Q4",
    location: "Kế toán",
    pinned: true,
  }),
];

describe("WorkspaceHomeSurface pinned list", () => {
  it("FUI[workspace.home:pinned] draws the pinned Boards and Docs with where they live and opens them", () => {
    renderHome({ pinnedState: { status: "ready", data: pinned } });

    const card = screen.getByRole("region", { name: /Đã ghim/ });
    expect(card.textContent).toContain("2");
    const board = within(card).getByRole("link", { name: /Lộ trình ERP/ });
    expect(board.getAttribute("href")).toBe(
      "/workspaces/ws-product/boards/board-erp",
    );
    expect(board.textContent).toContain("Công nghệ");
    // A pin says where the resource lives, never when it was last opened.
    expect(board.textContent).not.toMatch(/trước|giây|phút|giờ/);
    expect(
      within(card).getByRole("link", { name: /Kiểm toán Q4/ }).textContent,
    ).toContain("Kế toán");
  });

  it("FUI[workspace.home:pinned-empty] draws no pinned card when nothing is pinned or no pin read exists, and keeps Continue", () => {
    renderHome({ pinnedState: { status: "ready", data: [] } });
    expect(screen.queryByRole("region", { name: /Đã ghim/ })).toBeNull();
    expect(
      screen.getByRole("region", { name: viCopy.home.continue.title }),
    ).toBeTruthy();
  });

  it("FUI[workspace.home:pinned-error] says the pins failed to load next to a retry, without hiding the other sections", () => {
    const props = renderHome({ pinnedState: { status: "error" } });
    expect(screen.queryByRole("region", { name: /Đã ghim/ })).toBeNull();
    fireEvent.click(
      screen.getAllByRole("button", { name: viCopy.home.retry })[0]!,
    );
    expect(props.onRetryContinue).toHaveBeenCalledTimes(1);
    expect(screen.getByRole("region", { name: /Quá hạn/ })).toBeTruthy();
  });
});

describe("WorkspaceHomeSurface for a Guest", () => {
  it("FUI[workspace.home:guest-sees-less] a Guest in a new workspace is offered no create or invite action and sees no workspace-wide figure", () => {
    renderHome({
      ...homeOnboardingScenario({ canCreate: false, canInvite: false }),
      pinnedState: { status: "ready", data: [] },
    });
    expect(
      screen.queryByRole("button", { name: viCopy.onboarding.boardAction }),
    ).toBeNull();
    expect(
      screen.queryByRole("button", { name: viCopy.onboarding.docAction }),
    ).toBeNull();
    expect(
      screen.queryByRole("button", { name: viCopy.onboarding.inviteAction }),
    ).toBeNull();
    expect(screen.getByText(viCopy.onboarding.createUnavailable)).toBeTruthy();
    // Home draws only the member's own counts: no member, seat or invitation total.
    expect(document.body.textContent).not.toMatch(
      /\d+\s*(thành viên|chỗ còn trống|lời mời|người)/i,
    );
  });

  it("FUI[workspace.home:guest-unreadable-pins] a pin, or a recent, of a Board the Guest cannot read has no catalog match and is never drawn", () => {
    const guestCatalog = {
      boards: [{ id: "board-shared", name: "Board được chia sẻ" }],
      docs: [],
    };
    const references = [
      {
        kind: NAVIGATION_RESOURCE_KIND.board,
        resourceId: "board-private",
        at: "2026-10-12T08:00:00.000Z",
      },
      {
        kind: NAVIGATION_RESOURCE_KIND.board,
        resourceId: "board-shared",
        at: "2026-10-12T07:00:00.000Z",
      },
    ];

    const pins = resolveNavigationEntries(references, guestCatalog, references);
    const recents = resolveNavigationEntries(references, guestCatalog);

    expect(pins.map((entry) => entry.resourceId)).toEqual(["board-shared"]);
    expect(recents.map((entry) => entry.resourceId)).toEqual(["board-shared"]);
    expect(JSON.stringify(pins)).not.toContain("board-private");
  });
});

describe("WorkspaceHomeSurface on a phone", () => {
  const originalMatchMedia = window.matchMedia;
  const originalWidth = window.innerWidth;

  afterEach(() => {
    window.matchMedia = originalMatchMedia;
    Object.defineProperty(window, "innerWidth", {
      configurable: true,
      value: originalWidth,
    });
  });

  it("FUI[workspace.home:phone] keeps every section reachable at 390 px: quick filters, due windows, pinned, continue, and no fixed width beyond the screen", () => {
    Object.defineProperty(window, "innerWidth", {
      configurable: true,
      value: 390,
    });
    window.matchMedia = ((query: string) => ({
      matches: /max-width:\s*(7\d\d|[1-3]\d\d)px/.test(query),
      media: query,
      onchange: null,
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      addListener: vi.fn(),
      removeListener: vi.fn(),
      dispatchEvent: vi.fn(),
    })) as typeof window.matchMedia;

    const { container } = (() => {
      renderHome({
        pinnedState: { status: "ready", data: pinned },
        continueState: { status: "ready", data: continueEntriesFixture() },
      });
      return { container: document.body };
    })();

    expect(
      screen.getByRole("group", { name: viCopy.home.chipsLabel }),
    ).toBeTruthy();
    expect(screen.getByRole("region", { name: /Quá hạn/ })).toBeTruthy();
    expect(screen.getByRole("region", { name: /Đã ghim/ })).toBeTruthy();
    expect(
      screen.getByRole("region", { name: viCopy.home.continue.title }),
    ).toBeTruthy();
    // Every control has a name a screen reader can read.
    container.querySelectorAll("button, a[href]").forEach((el) => {
      expect(
        (el.textContent ?? "").trim() ||
          el.getAttribute("aria-label") ||
          el.getAttribute("title"),
      ).toBeTruthy();
    });
    // No inline pixel width wider than the 390 px screen.
    container.querySelectorAll<HTMLElement>("[style]").forEach((el) => {
      const width = parseFloat(el.style.width);
      if (el.style.width.endsWith("px")) expect(width).toBeLessThanOrEqual(390);
    });
  });
});
