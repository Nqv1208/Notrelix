import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";

import type { WorkspaceActivityEntry } from "../../../core/api/activity.service";
import {
  homeCopyFixture,
  homeDefaultScenario,
} from "../../../verification/workspace-home-fixtures";
import { shellNavigationFixture } from "../../../verification/workspace-shell-fixtures";
import type { HomeActivityView } from "../workspace-activity";
import {
  WorkspaceHomeSurface,
  type WorkspaceHomeSurfaceProps,
} from "../workspace-home-surface";

const en = homeCopyFixture("en");
const vi_ = homeCopyFixture("vi");

const entry = (
  over: Partial<WorkspaceActivityEntry> = {},
): WorkspaceActivityEntry => ({
  id: "a1",
  type: "work.board-created",
  title: 'Board "Roadmap" was created',
  actorName: "Ada Lovelace",
  actorAvatarUrl: null,
  subjectName: "Roadmap",
  occurredAt: new Date(Date.now() - 5 * 60_000).toISOString(),
  ...over,
});

function view(over: Partial<HomeActivityView> = {}): HomeActivityView {
  return {
    state: { status: "ready", data: [entry()] },
    hasMore: false,
    loadingMore: false,
    loadMoreFailed: false,
    onLoadMore: vi.fn(),
    onRetry: vi.fn(),
    ...over,
  };
}

function renderHome(
  activity: HomeActivityView | undefined,
  copy = homeDefaultScenario().copy,
) {
  const props: WorkspaceHomeSurfaceProps = {
    ...homeDefaultScenario(),
    copy,
    navigation: shellNavigationFixture(vi.fn()),
    onComplete: vi.fn(),
    onAddItem: null,
    onCreateBoard: vi.fn(),
    onCreateDoc: vi.fn(),
    onInvite: vi.fn(),
    onRetryContinue: vi.fn(),
    onRetryMyWork: vi.fn(),
    activity,
  };
  renderPureUi(<WorkspaceHomeSurface {...props} />);
}

describe("workspace activity on Home", () => {
  it("FUI[workspace.home:activity-success] lists who did what, newest first, in the member's language", () => {
    renderHome(
      view({
        state: {
          status: "ready",
          data: [
            entry(),
            entry({
              id: "a2",
              type: "docs.page-renamed",
              actorName: null,
              subjectName: "Handbook",
            }),
          ],
        },
      }),
      vi_,
    );
    const list = screen.getByRole("list", {
      name: vi_.home.activity.label,
    });
    const rows = within(list).getAllByRole("listitem");
    expect(rows).toHaveLength(2);
    expect(rows[0]!.textContent).toContain("Ada Lovelace");
    expect(rows[0]!.textContent).toContain("đã tạo Bảng Roadmap");
    // A person who is gone is "Một người", never an empty name.
    expect(rows[1]!.textContent).toContain(
      `${vi_.home.activity.someone} đã đổi tên Tài liệu Handbook`,
    );
  });

  it("FUI[workspace.home:activity-unknown-type] shows the producer's phrase for a type it has no words for", () => {
    renderHome(
      view({
        state: {
          status: "ready",
          data: [
            entry({ type: "future.thing-happened", title: "did a thing" }),
          ],
        },
      }),
      en,
    );
    expect(screen.getByRole("listitem").textContent).toContain(
      "Ada Lovelace did a thing",
    );
  });

  it("FUI[workspace.home:activity-loading] holds the place while the feed loads", () => {
    renderHome(view({ state: { status: "loading" } }), en);
    expect(
      screen.getByRole("status", { name: en.home.activity.loading }),
    ).toBeTruthy();
  });

  it("FUI[workspace.home:activity-error] says the feed did not load and retries", () => {
    const activity = view({ state: { status: "error" } });
    renderHome(activity, en);
    expect(screen.getByRole("alert").textContent).toContain(
      en.home.activity.error,
    );
    fireEvent.click(
      screen.getByRole("button", { name: en.home.activity.retry }),
    );
    expect(activity.onRetry).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.home:activity-no-access] says a Guest does not see the activity when the read is refused", () => {
    renderHome(view({ state: { status: "error", reason: "forbidden" } }), en);
    expect(screen.getByRole("note").textContent).toBe(
      en.home.activity.forbidden,
    );
    expect(
      screen.queryByRole("button", { name: en.home.activity.retry }),
    ).toBeNull();
  });

  it("FUI[workspace.home:activity-empty] says nothing has happened yet", () => {
    renderHome(view({ state: { status: "ready", data: [] } }), en);
    expect(screen.getByText(en.home.activity.empty)).toBeTruthy();
    expect(screen.queryByRole("list", { name: en.home.activity.label })).toBe(
      null,
    );
  });

  it("FUI[workspace.home:activity-more] asks for the next page, shows it is loading, and keeps the rows when it fails", () => {
    const more = view({ hasMore: true });
    renderHome(more, en);
    fireEvent.click(
      screen.getByRole("button", { name: en.home.activity.loadMore }),
    );
    expect(more.onLoadMore).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.home:activity-more-loading] disables the button while the next page loads", () => {
    renderHome(view({ hasMore: true, loadingMore: true }), en);
    const button = screen.getByRole("button", {
      name: en.home.activity.loadingMore,
    }) as HTMLButtonElement;
    expect(button.disabled).toBe(true);
  });

  it("FUI[workspace.home:activity-more-error] keeps the shown rows, says more did not load and offers a retry", () => {
    const more = view({ hasMore: true, loadMoreFailed: true });
    renderHome(more, en);
    expect(screen.getAllByRole("listitem")).toHaveLength(1);
    expect(screen.getByRole("alert").textContent).toContain(
      en.home.activity.loadMoreError,
    );
    fireEvent.click(
      screen.getByRole("button", { name: en.home.activity.retry }),
    );
    expect(more.onLoadMore).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.home:activity-guest] draws no activity section when the host gives none", () => {
    renderHome(undefined, en);
    expect(
      screen.queryByRole("region", { name: en.home.activity.title }),
    ).toBeNull();
  });
});
