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
};

function show({
  canModerateComments,
  updatesError = false,
  updates = [comment],
  onRefreshUpdates = vi.fn(),
  onDeleteUpdate = vi.fn(),
}: {
  canModerateComments?: boolean;
  updatesError?: boolean;
  updates?: unknown[];
  onRefreshUpdates?: () => void;
  onDeleteUpdate?: (id: string) => void;
}) {
  const model = {
    mode: "view",
    copy: en,
    readOnly: null,
    now: new Date("2026-11-08T10:00:00.000Z"),
    timeZone: "UTC",
    capabilities: {
      canEditFields: true,
      canComment: true,
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
      updatesLoading: false,
      updatesError,
      activity: [],
      activityLoading: false,
      activityFetching: false,
      files: [],
      filesLoading: false,
      viewerUserId: "user-b",
      people: undefined,
    },
    callbacks: {
      onRefreshUpdates,
      onDeleteUpdate,
      onCreateUpdate: vi.fn(),
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
    expect(onDeleteUpdate).toHaveBeenCalledWith("c-1");
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
