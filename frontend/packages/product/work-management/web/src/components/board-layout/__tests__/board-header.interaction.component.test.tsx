import { beforeAll, describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";
import { en, vi as viCopy, type BoardHeaderCopy } from "../board-header.copy";
import {
  BoardHeaderSurface,
  boardAccessSentence,
  type BoardHeaderAccess,
  type BoardHeaderData,
  type BoardHeaderSurfaceProps,
} from "../board-header-surface";

beforeAll(() => {
  // Radix menus measure and scroll items; JSDOM has no layout.
  Element.prototype.scrollIntoView = () => undefined;
  Element.prototype.hasPointerCapture = () => false;
  Element.prototype.releasePointerCapture = () => undefined;
});

const LOCALES = [
  ["vi", viCopy],
  ["en", en],
] as const satisfies readonly (readonly [string, BoardHeaderCopy])[];

/** Hà: workspace admin, effective manager. */
const managerAccess: BoardHeaderAccess = {
  myLevel: "manager",
  mySource: { kind: "workspace-admin" },
  peopleCount: 32,
  guestCount: 1,
  sources: [
    { kind: "space", id: "space-cn", name: "Phòng Công nghệ", count: 24 },
    { kind: "team", id: "team-kt", name: "Nhóm Kế toán tổng hợp", count: 6 },
    { kind: "user", id: "user-linh", name: "Đỗ Khánh Linh", count: 1 },
    { kind: "guest", count: 1 },
  ],
};

/** Linh: explicit commenter entry granted by Phúc. */
const commenterAccess: BoardHeaderAccess = {
  ...managerAccess,
  myLevel: "commenter",
  mySource: {
    kind: "explicit",
    grantedBy: { userId: "user-phuc", name: "Lê Hoàng Phúc" },
  },
};

const erpHeader: BoardHeaderData = {
  space: {
    name: "Phòng Công nghệ",
    initials: "CN",
    markTone: "blue",
    href: "/workspaces/ws-1/spaces/space-cn",
  },
  visibility: "space",
  access: managerAccess,
  people: [
    { id: "user-ha", name: "Nguyễn Thu Hà", initials: "HA", tone: "sage" },
    { id: "user-quan", name: "Trần Minh Quân", initials: "MQ", tone: "blue" },
    { id: "user-phuc", name: "Lê Hoàng Phúc", initials: "HP", tone: "ember" },
    { id: "user-anh", name: "Phan Ngọc Anh", initials: "NA", tone: "coral" },
  ],
};

function renderHeader(
  copy: BoardHeaderCopy,
  overrides: Partial<BoardHeaderSurfaceProps> = {},
) {
  const props: BoardHeaderSurfaceProps = {
    title: "Triển khai ERP Q4",
    copy,
    ...erpHeader,
    ...overrides,
  };
  return renderPureUi(<BoardHeaderSurface {...props} />);
}

function fill(template: string, values: Record<string, string | number>) {
  return template.replace(/\{\{(\w+)\}\}/g, (_, key: string) =>
    String(values[key]),
  );
}

describe.each(LOCALES)("BoardHeaderSurface (%s)", (locale, copy) => {
  it("FUI[wm.board-layout.header:title] shows the department mark, the Space crumb and the board title as the page heading", () => {
    const onOpenSpace = vi.fn();
    renderHeader(copy, { onOpenSpace });
    expect(
      screen.getByRole("heading", { level: 1, name: "Triển khai ERP Q4" }),
    ).toBeTruthy();
    const mark = screen.getByText("CN");
    expect(mark.getAttribute("aria-hidden")).toBe("true");
    expect(mark.className).toContain("bg-mark-blue");
    expect(mark.className).toContain("text-mark-blue-foreground");
    const crumb = screen.getByRole("link", { name: "Phòng Công nghệ" });
    expect(crumb.getAttribute("href")).toBe("/workspaces/ws-1/spaces/space-cn");
    fireEvent.click(crumb);
    expect(onOpenSpace).toHaveBeenCalledOnce();
  });

  it("FUI[wm.board-layout.header:crumb-modified-click] leaves a modified click on the Space crumb to the browser", () => {
    const onOpenSpace = vi.fn();
    renderHeader(copy, { onOpenSpace });
    const crumb = screen.getByRole("link", { name: "Phòng Công nghệ" });
    // Record whether the surface prevented the default, then stop JSDOM from
    // attempting the navigation (it implements none).
    let preventedBySurface: boolean | undefined;
    const record = (event: Event) => {
      preventedBySurface = event.defaultPrevented;
      event.preventDefault();
    };
    window.addEventListener("click", record);
    try {
      fireEvent.click(crumb, { metaKey: true });
    } finally {
      window.removeEventListener("click", record);
    }
    expect(onOpenSpace).not.toHaveBeenCalled();
    expect(preventedBySurface).toBe(false);
  });

  it("FUI[wm.board-layout.header:access-chip] words the access chip by visibility with the guest count", () => {
    const { unmount } = renderHeader(copy);
    const spaceChip =
      locale === "vi"
        ? "Phòng Công nghệ · có 1 khách"
        : "Phòng Công nghệ · 1 guest";
    expect(screen.getByRole("button", { name: spaceChip })).toBeTruthy();
    unmount();

    const noGuests = { ...managerAccess, guestCount: 0 };
    const second = renderHeader(copy, {
      visibility: "workspace",
      access: noGuests,
    });
    expect(
      screen.getByRole("button", { name: copy.header.scope.workspace }),
    ).toBeTruthy();
    second.unmount();

    renderHeader(copy, {
      visibility: "private",
      access: { ...managerAccess, guestCount: 2 },
    });
    expect(
      screen.getByRole("button", {
        name: fill(copy.header.guests_other, {
          scope: copy.header.scope.private,
          count: 2,
        }),
      }),
    ).toBeTruthy();
  });

  it("FUI[wm.board-layout.header:avatar-stack] names everyone who can open the board in one sentence and shows the overflow count", () => {
    renderHeader(copy);
    const sentence =
      locale === "vi"
        ? "32 người vào được board: 24 người Phòng Công nghệ, 6 người Nhóm Kế toán tổng hợp, Đỗ Khánh Linh và 1 khách"
        : "32 people can open this board: 24 people in Phòng Công nghệ, 6 people in Nhóm Kế toán tổng hợp, Đỗ Khánh Linh, and 1 guest";
    const stack = screen.getByRole("button", { name: sentence });
    const faces = within(stack)
      .getAllByText(/.+/)
      .map((node) => node.textContent);
    expect(faces).toEqual(["HA", "MQ", "HP", "+29"]);
  });

  it("FUI[wm.board-layout.header:share-unavailable] keeps the chip, the stack and Share focusable but disabled with the stated reason", () => {
    renderHeader(copy);
    const share = screen.getByRole("button", { name: copy.header.share });
    const chip = screen.getByRole("button", {
      name:
        locale === "vi"
          ? "Phòng Công nghệ · có 1 khách"
          : "Phòng Công nghệ · 1 guest",
    });
    for (const control of [share, chip]) {
      expect(control.getAttribute("aria-disabled")).toBe("true");
      const reasonId = control.getAttribute("aria-describedby");
      expect(reasonId).toBeTruthy();
      expect(document.getElementById(reasonId ?? "")?.textContent).toBe(
        copy.header.shareUnavailable,
      );
      expect(() => fireEvent.click(control)).not.toThrow();
    }
  });

  it("FUI[wm.board-layout.header:share] opens sharing from the chip, the stack and Share when the route provides it", () => {
    const onOpenShare = vi.fn();
    renderHeader(copy, { onOpenShare });
    const share = screen.getByRole("button", { name: copy.header.share });
    expect(share.getAttribute("aria-disabled")).toBeNull();
    fireEvent.click(share);
    const stack = screen.getByRole("button", { name: /32/ });
    fireEvent.click(stack);
    expect(onOpenShare).toHaveBeenCalledTimes(2);
  });

  it("FUI[wm.board-layout.header:settings] opens settings when available and states why when not", () => {
    const onOpenSettings = vi.fn();
    const { unmount } = renderHeader(copy, { onOpenSettings });
    fireEvent.click(screen.getByRole("button", { name: copy.header.settings }));
    expect(onOpenSettings).toHaveBeenCalledOnce();
    unmount();

    renderHeader(copy);
    const settings = screen.getByRole("button", {
      name: copy.header.settings,
    });
    expect(settings.getAttribute("aria-disabled")).toBe("true");
    expect(
      document.getElementById(settings.getAttribute("aria-describedby") ?? "")
        ?.textContent,
    ).toBe(copy.header.settingsUnavailable);
  });

  it("FUI[wm.board-layout.header:more-menu] opens More options and copies the board link", () => {
    const onCopyLink = vi.fn();
    renderHeader(copy, { onCopyLink });
    fireEvent.keyDown(
      screen.getByRole("button", { name: copy.header.moreOptions }),
      { key: "Enter" },
    );
    fireEvent.click(
      within(screen.getByRole("menu")).getByRole("menuitem", {
        name: copy.header.copyLink,
      }),
    );
    expect(onCopyLink).toHaveBeenCalledOnce();
    expect(screen.queryByRole("menu")).toBeNull();
  });

  it("FUI[wm.board-layout.header:commenter-banner] tells a commenter their level and who added them, and hides Settings and Share", () => {
    renderHeader(copy, { access: commenterAccess, onOpenSettings: vi.fn() });
    const banner = screen.getByRole("note");
    expect(banner.textContent).toBe(
      locale === "vi"
        ? "Bạn có quyền bình luận trên board này · được Lê Hoàng Phúc thêm riêng"
        : "You can comment on this board · added directly by Lê Hoàng Phúc",
    );
    expect(
      screen.queryByRole("button", { name: copy.header.settings }),
    ).toBeNull();
    expect(
      screen.queryByRole("button", { name: copy.header.share }),
    ).toBeNull();
    // The access summary stays visible to a commenter.
    expect(screen.getByRole("button", { name: /32/ })).toBeTruthy();
  });

  it("FUI[wm.board-layout.header:manager-no-banner] shows no banner to someone who can edit", () => {
    renderHeader(copy, { access: { ...managerAccess, myLevel: "editor" } });
    expect(screen.queryByRole("note")).toBeNull();
  });

  it("FUI[wm.board-layout.header:access-pending] shows only the title while the access summary is not loaded", () => {
    renderHeader(copy, { access: null, onOpenSettings: vi.fn() });
    expect(screen.getByRole("heading", { level: 1 })).toBeTruthy();
    expect(screen.queryByRole("note")).toBeNull();
    expect(screen.queryByRole("button", { name: /32/ })).toBeNull();
    expect(
      screen.queryByRole("button", { name: copy.header.settings }),
    ).toBeNull();
    expect(
      screen.queryByRole("button", { name: copy.header.share }),
    ).toBeNull();
  });

  it("FUI[wm.board-layout.header:no-space] omits the mark and the crumb for a board outside any Space", () => {
    renderHeader(copy, { space: null, visibility: "workspace" });
    expect(screen.queryByRole("link")).toBeNull();
    expect(screen.queryByText("CN")).toBeNull();
    expect(
      screen.getByRole("button", {
        name: fill(copy.header.guests_one, {
          scope: copy.header.scope.workspace,
          count: 1,
        }),
      }),
    ).toBeTruthy();
  });
});

describe("boardAccessSentence", () => {
  it("FUI[wm.board-layout.header:sentence-counts] keeps counts only for sources whose names the caller cannot see", () => {
    const access: BoardHeaderAccess = {
      myLevel: "viewer",
      mySource: { kind: "space" },
      peopleCount: 9,
      guestCount: 0,
      sources: [
        { kind: "space", id: "space-cn", name: "Phòng Công nghệ", count: 6 },
        { kind: "team", count: 2 },
        { kind: "user", count: 1 },
      ],
    };
    expect(boardAccessSentence(access, viCopy)).toBe(
      "9 người vào được board: 6 người Phòng Công nghệ, 2 người khác và 1 người được thêm riêng",
    );
    expect(boardAccessSentence(access, en)).toBe(
      "9 people can open this board: 6 people in Phòng Công nghệ, 2 other people, and 1 person added directly",
    );
  });

  it("FUI[wm.board-layout.header:sentence-empty] states only the total when no source is listed", () => {
    const access: BoardHeaderAccess = {
      myLevel: "manager",
      mySource: { kind: "owner" },
      peopleCount: 1,
      guestCount: 0,
      sources: [],
    };
    expect(boardAccessSentence(access, viCopy)).toBe("1 người vào được board");
    expect(boardAccessSentence(access, en)).toBe(
      "1 person can open this board",
    );
  });
});
