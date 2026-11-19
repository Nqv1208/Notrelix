import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";

import {
  teamsCopyFixture,
  teamsDefaultScenario,
  teamsNavigationFixture,
  teamsReadOnlyScenario,
} from "../../../verification/workspace-teams-fixtures";
import type { DeletedTeam } from "../../../core/model/teams";
import {
  WorkspaceTeamsSurface,
  type WorkspaceTeamsSurfaceProps,
} from "../workspace-teams-surface";

const copy = teamsCopyFixture();

const deletedTeams: DeletedTeam[] = [
  {
    id: "team-support",
    name: "Nhóm Hỗ trợ khách hàng",
    description: null,
    memberCount: 6,
    deletedAt: "2026-11-18T09:00:00Z",
    deletedByName: "Trịnh Quốc Hưng",
  },
  {
    id: "team-old",
    name: "Nhóm Dự án cũ",
    description: null,
    memberCount: 3,
    deletedAt: "2026-11-02T09:00:00Z",
    deletedByName: null,
  },
];

function renderTeams(
  overrides: Partial<WorkspaceTeamsSurfaceProps> = {},
  scenario = teamsDefaultScenario(),
) {
  const props: WorkspaceTeamsSurfaceProps = {
    ...scenario,
    navigation: teamsNavigationFixture(),
    onCreate: vi.fn(),
    onArchive: vi.fn(),
    onRestore: vi.fn(),
    onRetry: vi.fn(),
    deleted: { status: "ready", data: deletedTeams },
    onRestoreDeleted: vi.fn(),
    onRetryDeleted: vi.fn(),
    ...overrides,
  };
  renderPureUi(<WorkspaceTeamsSurface {...props} />);
  return props;
}

const openDeleted = () =>
  fireEvent.click(
    screen.getByRole("button", { name: new RegExp(`^${copy.deleted.tab}`) }),
  );

describe("Deleted Teams view (L09)", () => {
  it("FUI[workspace.teams:deleted] lists the deleted Teams with who deleted them, their people and Restore", () => {
    renderTeams();
    expect(
      screen.getByRole("button", { name: /Đã xoá/ }).textContent,
      "the tab says how many",
    ).toContain("2");

    openDeleted();

    const table = screen.getByRole("table", {
      name: "Nhóm đã xoá trong Minh An Group",
    });
    const rows = within(table).getAllByRole("row").slice(1);
    expect(rows).toHaveLength(2);
    expect(within(rows[0]!).getByText("Nhóm Hỗ trợ khách hàng")).toBeTruthy();
    expect(within(rows[0]!).getByText("Trịnh Quốc Hưng")).toBeTruthy();
    expect(within(rows[0]!).getByText("6 người")).toBeTruthy();
    expect(
      within(rows[1]!).getByText(copy.deleted.unknownPerson),
      "a deleter who is gone is not shown as a blank",
    ).toBeTruthy();
    expect(screen.getByText("2 nhóm đã xoá")).toBeTruthy();
    expect(
      screen.queryByRole("table", { name: "Nhóm trong Minh An Group" }),
      "the list of Teams in use is replaced",
    ).toBeNull();
  });

  it("FUI[workspace.teams:deleted-restore] Restore asks the host to restore that Team, and a Team being restored cannot be restored twice", () => {
    const props = renderTeams({ pendingIds: new Set(["team-old"]) });
    openDeleted();

    fireEvent.click(
      screen.getByRole("button", { name: "Khôi phục Nhóm Hỗ trợ khách hàng" }),
    );

    expect(props.onRestoreDeleted).toHaveBeenCalledWith("team-support");
    const pending = screen.getByRole("button", {
      name: "Khôi phục Nhóm Dự án cũ",
    }) as HTMLButtonElement;
    expect(pending.disabled).toBe(true);
    expect(pending.textContent).toBe(copy.deleted.restoring);
  });

  it("FUI[workspace.teams:deleted-empty] says no Team is deleted yet", () => {
    renderTeams({ deleted: { status: "ready", data: [] } });
    openDeleted();

    expect(
      screen.getByRole("heading", { name: copy.deleted.empty.title }),
    ).toBeTruthy();
    expect(screen.getByText(copy.deleted.empty.text)).toBeTruthy();
    expect(screen.queryByRole("table")).toBeNull();
  });

  it("FUI[workspace.teams:deleted-loading] shows a skeleton while the deleted Teams load, never an empty message", () => {
    renderTeams({ deleted: { status: "loading" } });
    openDeleted();

    expect(
      screen.getByRole("status", { name: copy.deleted.loading }),
    ).toBeTruthy();
    expect(screen.queryByText(copy.deleted.empty.title)).toBeNull();
  });

  it("FUI[workspace.teams:deleted-error] says the read failed and retries only the deleted list", () => {
    const props = renderTeams({ deleted: { status: "error" } });
    openDeleted();

    expect(screen.getByRole("alert").textContent).toContain(copy.deleted.error);
    fireEvent.click(screen.getByRole("button", { name: copy.deleted.retry }));

    expect(props.onRetryDeleted).toHaveBeenCalledTimes(1);
    expect(props.onRetry).not.toHaveBeenCalled();
    expect(screen.queryByText(copy.deleted.empty.title)).toBeNull();
  });

  it("FUI[workspace.teams:deleted-search] the search narrows the deleted Teams and says when none matches", () => {
    renderTeams();
    openDeleted();
    const search = screen.getByRole("searchbox");

    fireEvent.change(search, { target: { value: "dự án" } });
    const table = screen.getByRole("table", {
      name: "Nhóm đã xoá trong Minh An Group",
    });
    expect(within(table).getAllByRole("row")).toHaveLength(2);
    expect(within(table).getByText("Nhóm Dự án cũ")).toBeTruthy();

    fireEvent.change(search, { target: { value: "không có" } });
    expect(screen.getByText(copy.deleted.noMatches)).toBeTruthy();
  });

  it("FUI[workspace.teams:deleted-hidden] offers no deleted view without the list, and none to a member who cannot manage Teams", () => {
    const { unmount } = (() => {
      const view = renderPureUi(
        <WorkspaceTeamsSurface
          {...teamsDefaultScenario()}
          navigation={teamsNavigationFixture()}
          onCreate={vi.fn()}
          onArchive={vi.fn()}
          onRestore={vi.fn()}
          onRetry={vi.fn()}
        />,
      );
      return view;
    })();
    expect(
      screen.queryByRole("group", { name: copy.deleted.viewLabel }),
    ).toBeNull();
    unmount();

    renderTeams({}, teamsReadOnlyScenario());
    expect(
      screen.queryByRole("group", { name: copy.deleted.viewLabel }),
    ).toBeNull();
    expect(screen.queryByText(copy.deleted.tab)).toBeNull();
  });
});
