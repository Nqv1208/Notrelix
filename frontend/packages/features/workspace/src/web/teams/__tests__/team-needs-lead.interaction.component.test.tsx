import { describe, expect, it, vi } from "vitest";
import { renderPureUi, screen, within } from "@notrelix/testing";

import {
  TEAMS,
  TEAMS_EXT,
  formatDayFixture,
  teamBackNavigationFixture,
  teamDefaultScenario,
  teamMembersFixture,
  teamUsageNavigationFixture,
  teamsCopyFixture,
  teamsDefaultScenario,
  teamsNavigationFixture,
} from "../../../verification/workspace-teams-fixtures";
import { buildTeamRows } from "../workspace-teams-models";
import { WorkspaceTeamSurface } from "../workspace-team-surface";
import { WorkspaceTeamsSurface } from "../workspace-teams-surface";

const copy = teamsCopyFixture("en");

/** The first Team's only lead left the workspace: its people are the same without the Lead. */
const leaderless = TEAMS_EXT.teams.map((team, index) =>
  index === 0
    ? {
        ...team,
        people: team.people.filter((person) => person.role !== "Lead"),
      }
    : team,
);

describe("A Team left without a lead (needs a lead)", () => {
  it("FUI[workspace.teams:needs-lead] the list says which Team needs a lead and counts them", () => {
    const base = teamsDefaultScenario("en");
    renderPureUi(
      <WorkspaceTeamsSurface
        {...base}
        copy={copy}
        teams={{
          status: "ready",
          data: buildTeamRows(copy, TEAMS, leaderless, formatDayFixture),
        }}
        navigation={teamsNavigationFixture()}
        onCreate={vi.fn()}
        onArchive={vi.fn()}
        onRestore={vi.fn()}
        onRetry={vi.fn()}
      />,
    );
    const table = screen.getByRole("table", { name: /Teams in/ });
    const rows = within(table).getAllByRole("row").slice(1);
    expect(within(rows[0]!).getByText(copy.page.needsLead)).toBeTruthy();
    // Only that Team is flagged; the others keep their lead's name.
    expect(screen.getAllByText(copy.page.needsLead)).toHaveLength(1);
    expect(screen.getByText(/1 without a Team lead/)).toBeTruthy();
  });

  it("FUI[workspace.teams:needs-lead] a Team whose extras are not read yet is not flagged", () => {
    const rows = buildTeamRows(copy, TEAMS, null, formatDayFixture);
    expect(rows.some((row) => row.needsLead)).toBe(false);
  });

  it("FUI[workspace.team:needs-lead] the Team page shows the banner when no active member is the lead, and not otherwise", () => {
    const base = teamDefaultScenario("en");
    const props = {
      ...base,
      onRename: vi.fn(),
      onDescribe: vi.fn(),
      onArchive: vi.fn(),
      onUnarchive: vi.fn(),
      onAddMember: vi.fn(),
      onChangeRole: vi.fn(),
      onRemove: vi.fn(),
      onRetry: vi.fn(),
      backNavigation: teamBackNavigationFixture(),
      usageNavigation: teamUsageNavigationFixture(),
    };
    const { unmount } = renderPureUi(<WorkspaceTeamSurface {...props} />);
    expect(screen.queryByText(copy.detail.needsLeadBanner)).toBeNull();
    unmount();

    renderPureUi(
      <WorkspaceTeamSurface
        {...props}
        members={{
          status: "ready",
          data: teamMembersFixture().filter((member) => member.role !== "Lead"),
        }}
      />,
    );
    expect(screen.getByText(copy.detail.needsLeadBanner)).toBeTruthy();
  });
});
