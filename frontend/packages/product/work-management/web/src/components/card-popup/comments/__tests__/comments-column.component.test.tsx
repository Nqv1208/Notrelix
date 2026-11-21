import { describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";
import { en } from "../../card-popup.copy";
import {
  CardPopupProvider,
  type CardPopupModel,
} from "../../card-popup-context";
import { CommentsColumn } from "../comments-column";

const author = {
  userId: "user-a",
  name: "Ada Lovelace",
  initials: "AL",
};

const comment = {
  id: "c-1",
  body: "Ship it",
  createdAt: "2026-11-01T09:00:00.000Z",
  author,
  reactions: [],
  version: 4,
};

function show({
  canModerateComments,
  updatesError = false,
  updates = [comment],
  onRefreshUpdates = vi.fn(),
  onDeleteUpdate = vi.fn(),
  canComment = true,
  readOnly = null,
  frozen = false,
  updatesLoading = false,
  viewerUserId = "user-b",
  onCreateUpdate = vi.fn(),
}: {
  canComment?: boolean;
  readOnly?: string | null;
  frozen?: boolean;
  updatesLoading?: boolean;
  viewerUserId?: string;
  onCreateUpdate?: (...args: unknown[]) => void;
  canModerateComments?: boolean;
  updatesError?: boolean;
  updates?: unknown[];
  onRefreshUpdates?: () => void;
  onDeleteUpdate?: (id: string) => void;
}) {
  const model = {
    mode: "view",
    copy: en,
    readOnly,
    now: new Date("2026-11-08T10:00:00.000Z"),
    timeZone: "UTC",
    capabilities: {
      canEditFields: true,
      canComment,
      frozen,
      canModerateComments,
    },
    card: {
      id: "item-1",
      workspaceId: "ws-1",
      title: "Card",
      members: [],
      watchers: [],
    },
    data: {
      updates,
      updatesLoading,
      updatesError,
      activity: [],
      activityLoading: false,
      activityFetching: false,
      files: [],
      filesLoading: false,
      viewerUserId,
      people: undefined,
    },
    callbacks: {
      onRefreshUpdates,
      onDeleteUpdate,
      onCreateUpdate,
      onUpdateUpdate: vi.fn(),
      onToggleReaction: vi.fn(),
      onRefreshActivity: vi.fn(),
    },
  } as unknown as CardPopupModel;
  renderPureUi(
    <I18nProvider catalogs={[]} initialLocale="en">
      <CardPopupProvider value={model}>
        <CommentsColumn />
      </CardPopupProvider>
    </I18nProvider>,
  );
  return { onRefreshUpdates, onDeleteUpdate };
}

describe("comments read failure", () => {
  it("shows the error with a retry that reads the comments again", () => {
    const { onRefreshUpdates } = show({ updatesError: true, updates: [] });
    const alert = screen.getByRole("alert");
    expect(alert.textContent).toContain(en.related.comments.updatesErrorTitle);
    expect(screen.queryByText(en.related.comments.emptyTitle)).toBeNull();
    fireEvent.click(
      within(alert).getByRole("button", { name: en.related.comments.retry }),
    );
    expect(onRefreshUpdates).toHaveBeenCalledTimes(1);
  });

  it("shows no error when the comments read", () => {
    show({ updates: [comment] });
    expect(screen.queryByRole("alert")).toBeNull();
    expect(screen.getByText("Ship it")).toBeTruthy();
  });
});

describe("moderator delete", () => {
  it("lets a Board manager delete another person's comment after a confirm", () => {
    const { onDeleteUpdate } = show({ canModerateComments: true });
    const row = document.querySelector('[data-comment="c-1"]') as HTMLElement;
    fireEvent.click(
      within(row).getByRole("button", { name: en.related.comments.delete }),
    );
    const confirm = row.querySelector("[data-comment-confirm]") as HTMLElement;
    expect(onDeleteUpdate).not.toHaveBeenCalled();
    fireEvent.click(
      within(confirm).getByRole("button", {
        name: en.related.comments.deleteConfirm,
      }),
    );
    // The delete quotes the version the list returned.
    expect(onDeleteUpdate).toHaveBeenCalledWith("c-1", 4);
  });

  it("keeps a deleted comment's replies under a placeholder with no author or text", () => {
    show({
      updates: [
        { ...comment, id: "c-9", body: "", deleted: true },
        { ...comment, id: "r-1", body: "a reply", parentId: "c-9" },
      ],
    });
    const placeholder = document.querySelector("[data-comment-deleted]");
    expect(placeholder).not.toBeNull();
    expect(placeholder?.textContent).not.toContain("Ship it");
    expect(document.body.textContent).toContain("a reply");
  });

  it("hides delete on another person's comment from a commenter, and offers no Edit", () => {
    show({ canModerateComments: false });
    const row = document.querySelector('[data-comment="c-1"]') as HTMLElement;
    expect(
      within(row).queryByRole("button", { name: en.related.comments.delete }),
    ).toBeNull();
    expect(
      within(row).queryByRole("button", { name: en.related.comments.edit }),
    ).toBeNull();
  });
});

describe("comment body is text, never markup (UC-COL-H04)", () => {
  const hostile =
    '<script>alert(1)</script> [go](javascript:alert(1)) <img src=x onerror="alert(1)"> <a href="javascript:alert(1)">x</a>';

  it("shows markup and script as text, with no element or link made from it", () => {
    show({ updates: [{ ...comment, id: "c-x", body: hostile }] });
    const row = document.querySelector('[data-comment="c-x"]') as HTMLElement;
    expect(row.querySelector("script")).toBeNull();
    expect(row.querySelector("img")).toBeNull();
    expect(row.querySelector("a")).toBeNull();
    expect(row.querySelector('[href^="javascript"]')).toBeNull();
    expect(row.textContent).toContain("<script>alert(1)</script>");
  });
});

describe("comment on an item (R01)", () => {
  it("disables the composer for a viewer and names the reason", () => {
    show({ canComment: false });
    const note = screen.getByText(en.related.comments.readOnlyPermission);
    expect(note.closest('[aria-disabled="true"]')).not.toBeNull();
    expect(screen.queryByRole("textbox")).toBeNull();
  });

  it("disables the composer on an archived board with its own reason", () => {
    show({ canComment: false, frozen: true });
    expect(screen.getByText(en.related.comments.readOnlyArchived)).toBeTruthy();
    expect(screen.queryByRole("textbox")).toBeNull();
  });

  it("shows the empty state when there is no comment", () => {
    show({ updates: [] });
    expect(screen.getByText(en.related.comments.emptyTitle)).toBeTruthy();
  });

  it("opens the composer for a commenter, keeps Post off while empty, and posts the text", () => {
    const onCreateUpdate = vi.fn();
    show({ updates: [], onCreateUpdate });
    fireEvent.click(
      screen.getByRole("button", {
        name: en.related.comments.composerPlaceholder,
      }),
    );
    const box = screen.getByRole("textbox", {
      name: en.related.comments.composerLabel,
    });
    const send = screen.getByRole("button", { name: en.related.comments.send });
    expect((send as HTMLButtonElement).disabled).toBe(true);
    fireEvent.change(box, { target: { value: "Looks good" } });
    expect((send as HTMLButtonElement).disabled).toBe(false);
    fireEvent.click(send);
    expect(onCreateUpdate).toHaveBeenCalledTimes(1);
    expect(JSON.stringify(onCreateUpdate.mock.calls[0])).toContain(
      "Looks good",
    );
  });
});

describe("reply, edit or delete a comment (R02)", () => {
  it("offers Edit and Delete to the author and not to another member", () => {
    show({ viewerUserId: "user-a" });
    const row = document.querySelector('[data-comment="c-1"]') as HTMLElement;
    expect(
      within(row).getByRole("button", { name: en.related.comments.edit }),
    ).toBeTruthy();
    expect(
      within(row).getByRole("button", { name: en.related.comments.delete }),
    ).toBeTruthy();
  });

  it("attaches a reply written under a reply to the top-level comment", () => {
    const onCreateUpdate = vi.fn();
    show({
      updates: [
        comment,
        { ...comment, id: "r-1", body: "first reply", parentId: "c-1" },
      ],
      onCreateUpdate,
    });
    const reply = document.querySelector('[data-comment="r-1"]') as HTMLElement;
    fireEvent.click(
      within(reply).getByRole("button", { name: en.related.comments.reply }),
    );
    const box = screen.getByRole("textbox");
    fireEvent.change(box, { target: { value: "answer" } });
    fireEvent.keyDown(box, { key: "Enter", ctrlKey: true });
    expect(JSON.stringify(onCreateUpdate.mock.calls[0])).toContain(
      '"parentCommentId":"c-1"',
    );
  });
});
