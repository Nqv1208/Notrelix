import { describe, expect, it, vi } from "vitest";
import {
  fireEvent,
  renderPureUi,
  screen,
  waitFor,
  within,
} from "@notrelix/testing";
import type { CardUpdate } from "@notrelix/work-management-core";
import { ItemPageSurface } from "../../item-page/item-page-surface";
import {
  en,
  vi as viCopy,
  type ItemPageCopy,
} from "../../item-page/item-page.copy";
import {
  en as sheetEn,
  vi as sheetVi,
  fillSheetCopy,
} from "../../card-detail/item-sheet.copy";
import {
  commenterSheetCapabilities,
  editorSheetCapabilities,
} from "../../../verification/item-sheet-fixtures";
import {
  erpPageBoard,
  erpPageDetailData,
  erpPageItem,
  erpPageNavigation,
  erpPageSpace,
  erpPageViewer,
  ERP_PAGE_NOW,
  ERP_PAGE_TIME_ZONE,
} from "../../../verification/item-page-fixtures";
import type { ItemPageSurfaceProps } from "../../item-page/item-page-types";
import { splitThreads } from "../thread-model";

/** A board Viewer: reads everything, comments on nothing. */
const viewerSheetCapabilities = {
  ...commenterSheetCapabilities,
  canComment: false,
};

const author = {
  id: "cm-linh",
  userId: "u-linh",
  name: "Lê Linh",
  initials: "LL",
  color: "#888",
};

function update(
  id: string,
  createdAt: string,
  extra: Partial<CardUpdate> = {},
): CardUpdate {
  return {
    id,
    cardId: "item-erp-142",
    author,
    body: `Body ${id}`,
    mentionUserIds: [],
    attachmentIds: [],
    createdAt,
    ...extra,
  };
}

/** One thread: a top comment with five replies, the top one carrying reactions. */
const thread: CardUpdate[] = [
  update("top", "2026-10-11T09:00:00+07:00", {
    reactions: [
      {
        emoji: "👍",
        count: 3,
        reactedByMe: true,
        users: [
          { userId: "u-1", name: "An" },
          { userId: "u-2", name: "Bình" },
          { userId: "u-me", name: "Tôi" },
        ],
      },
      {
        emoji: "🎉",
        count: 1,
        reactedByMe: false,
        users: [{ userId: "u-1", name: "An" }],
      },
    ],
  }),
  ...[1, 2, 3, 4, 5].map((n) =>
    update(`r${n}`, `2026-10-11T1${n}:00:00+07:00`, { parentId: "top" }),
  ),
];

function mount(
  copy: ItemPageCopy,
  capabilities: ItemPageSurfaceProps["capabilities"],
  updates: CardUpdate[] = thread,
) {
  const item = erpPageItem({ updates });
  const callbacks: ItemPageSurfaceProps["callbacks"] = {
    onClose: vi.fn(),
    onRenameTitle: vi.fn(),
    onToggleWatch: vi.fn(),
    onDuplicate: vi.fn(),
    onArchive: vi.fn(),
    onUpdateFieldValue: vi.fn(),
    onRefreshActivity: vi.fn(),
    onCreateUpdate: vi.fn((_input, options) => {
      options?.onSuccess?.();
      options?.onSettled?.();
    }),
    onToggleReaction: vi.fn(),
    onUpdateUpdate: vi.fn(),
    onDeleteUpdate: vi.fn(),
    onSelectTab: vi.fn(),
  };
  renderPureUi(
    <ItemPageSurface
      status="ready"
      board={erpPageBoard()}
      card={item}
      capabilities={capabilities}
      detailData={{ ...erpPageDetailData(item), updates }}
      callbacks={callbacks}
      navigation={erpPageNavigation({
        onBack: vi.fn(),
        onCollapse: vi.fn(),
        onOpenView: vi.fn(),
      })}
      copy={copy}
      sheetCopy={copy === viCopy ? sheetVi : sheetEn}
      now={ERP_PAGE_NOW}
      timeZone={ERP_PAGE_TIME_ZONE}
      space={erpPageSpace}
      viewer={erpPageViewer}
      creatorName="Nguyễn Thu Hà"
    />,
  );
  return callbacks;
}

describe("splitThreads", () => {
  it("nests replies oldest first under their top-level comment and keeps orphans visible", () => {
    const { top, repliesOf } = splitThreads([
      ...thread,
      update("orphan", "2026-10-12T09:00:00+07:00", { parentId: "gone" }),
    ]);
    expect(top.map((u) => u.id)).toEqual(["top", "orphan"]);
    expect(repliesOf("top").map((u) => u.id)).toEqual([
      "r1",
      "r2",
      "r3",
      "r4",
      "r5",
    ]);
  });
});

describe.each([
  ["vi", viCopy],
  ["en", en],
] as const)(
  "comment replies and reactions on the item page (%s)",
  (_l, copy) => {
    const t = copy.thread;

    it("lists three replies under the comment and expands the rest in place", () => {
      mount(copy, commenterSheetCapabilities);
      const group = screen.getByRole("group", {
        name: fillSheetCopy(t.replies, { name: author.name }),
      });
      expect(within(group).queryByText("Body r3")).toBeTruthy();
      expect(within(group).queryByText("Body r4")).toBeNull();

      fireEvent.click(
        within(group).getByRole("button", {
          name: fillSheetCopy(t.showMore_other, { count: 2 }),
        }),
      );
      expect(within(group).queryByText("Body r4")).toBeTruthy();
      expect(within(group).queryByText("Body r5")).toBeTruthy();
      expect(
        within(group).queryByRole("button", { name: /^(Show|Hiện)/ }),
      ).toBeNull();
    });

    it("sends a reply from a reply to the top-level comment", () => {
      const callbacks = mount(copy, commenterSheetCapabilities);
      const group = screen.getByRole("group", {
        name: fillSheetCopy(t.replies, { name: author.name }),
      });
      // The second reply's own Reply button.
      fireEvent.click(
        within(group).getAllByRole("button", { name: t.reply })[1]!,
      );
      const box = screen.getByRole("textbox", {
        name: fillSheetCopy(t.replyTo, { name: author.name }),
      });
      fireEvent.change(box, { target: { value: "Cảm ơn" } });
      fireEvent.click(screen.getByRole("button", { name: t.replySend }));
      expect(callbacks.onCreateUpdate).toHaveBeenCalledWith(
        expect.objectContaining({ body: "Cảm ơn", parentCommentId: "top" }),
        expect.anything(),
      );
    });

    it("shows each chip as a toggle with its count, and toggles the viewer's own", () => {
      const callbacks = mount(copy, commenterSheetCapabilities);
      const mine = screen.getByRole("button", {
        name: fillSheetCopy(t.chipMine, { emoji: "👍", count: 3 }),
      });
      expect(mine.getAttribute("aria-pressed")).toBe("true");
      const other = screen.getByRole("button", {
        name: fillSheetCopy(t.chip, { emoji: "🎉", count: 1 }),
      });
      expect(other.getAttribute("aria-pressed")).toBe("false");

      fireEvent.click(mine);
      expect(callbacks.onToggleReaction).toHaveBeenLastCalledWith(
        "top",
        "👍",
        false,
      );
      fireEvent.click(other);
      expect(callbacks.onToggleReaction).toHaveBeenLastCalledWith(
        "top",
        "🎉",
        true,
      );
    });

    it("offers the six-emoji palette from the add-reaction button", async () => {
      const callbacks = mount(copy, commenterSheetCapabilities);
      fireEvent.click(
        screen.getAllByRole("button", { name: t.addReaction })[0]!,
      );
      await waitFor(() =>
        expect(
          screen.getAllByRole("button", {
            name: fillSheetCopy(t.pick, { emoji: "🚀" }),
          }),
        ).toHaveLength(1),
      );
      for (const emoji of ["👍", "❤️", "😄", "🎉", "👀", "🚀"]) {
        expect(
          screen.getByRole("button", {
            name: fillSheetCopy(t.pick, { emoji }),
          }),
        ).toBeTruthy();
      }
      fireEvent.click(
        screen.getByRole("button", {
          name: fillSheetCopy(t.pick, { emoji: "🚀" }),
        }),
      );
      expect(callbacks.onToggleReaction).toHaveBeenCalledWith(
        "top",
        "🚀",
        true,
      );
    });

    it("lets a viewer see the chips but not react or reply", () => {
      const callbacks = mount(copy, viewerSheetCapabilities);
      const chip = screen.getByRole("button", {
        name: fillSheetCopy(t.chipMine, { emoji: "👍", count: 3 }),
      });
      expect(chip.getAttribute("aria-disabled")).toBe("true");
      fireEvent.click(chip);
      expect(callbacks.onToggleReaction).not.toHaveBeenCalled();
      expect(screen.queryByRole("button", { name: t.addReaction })).toBeNull();
      expect(screen.queryByRole("button", { name: t.reply })).toBeNull();
    });
  },
);

describe.each([
  ["vi", viCopy],
  ["en", en],
] as const)("who may edit or delete a comment (%s)", (_l, copy) => {
  const mine = update("mine", "2026-10-11T09:00:00+07:00", {
    author: { ...author, id: erpPageViewer.id, userId: erpPageViewer.userId },
  });
  const theirs = update("theirs", "2026-10-11T10:00:00+07:00");

  it("offers Edit and Delete on the viewer's own comment only, even to an editor", () => {
    const callbacks = mount(copy, editorSheetCapabilities, [mine, theirs]);
    const menus = screen.getAllByRole("button", {
      name: copy.activity.actions,
    });
    // The editor's menu appears on their own comment alone.
    expect(menus).toHaveLength(1);
    fireEvent.pointerDown(menus[0]!, { button: 0, ctrlKey: false });
    fireEvent.keyDown(menus[0]!, { key: "Enter" });
    expect(
      screen.getByRole("menuitem", { name: copy.activity.edit }),
    ).toBeTruthy();
    fireEvent.click(
      screen.getByRole("menuitem", { name: copy.activity.delete }),
    );
    expect(callbacks.onDeleteUpdate).toHaveBeenCalledWith("mine");
  });

  it("offers nothing on someone else's comment", () => {
    mount(copy, editorSheetCapabilities, [theirs]);
    expect(
      screen.queryByRole("button", { name: copy.activity.actions }),
    ).toBeNull();
  });
});
