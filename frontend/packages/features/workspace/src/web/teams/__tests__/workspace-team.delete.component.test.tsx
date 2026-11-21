import { describe, expect, it, vi } from "vitest";
import {
  fireEvent,
  renderPureUi,
  screen,
  waitFor,
  within,
} from "@notrelix/testing";

import {
  teamArchivedScenario,
  teamBackNavigationFixture,
  teamDefaultScenario,
  teamReadOnlyScenario,
  teamUsageNavigationFixture,
  type TeamScenario,
} from "../../../verification/workspace-teams-fixtures";
import type { TeamDeleteOutcome } from "../delete-team-dialog";
import {
  WorkspaceTeamSurface,
  type WorkspaceTeamSurfaceProps,
} from "../workspace-team-surface";
import { workspaceTeamsCopy } from "../workspace-teams.copy";

const copy = workspaceTeamsCopy("vi");
const d = copy.deleteDialog;

function renderTeam(
  onDelete: WorkspaceTeamSurfaceProps["onDelete"],
  scenario: TeamScenario = teamDefaultScenario(),
) {
  const props: WorkspaceTeamSurfaceProps = {
    ...scenario,
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
    onDelete,
  };
  renderPureUi(<WorkspaceTeamSurface {...props} />);
  return props;
}
const openDelete = () =>
  fireEvent.click(screen.getByRole("button", { name: d.action }));

describe("Delete a Team (L09)", () => {
  it("FUI[workspace.team:delete-confirm] confirms, shows pending, sends once and closes when deleted", async () => {
    let finish: (outcome: TeamDeleteOutcome) => void = () => {};
    const onDelete = vi.fn(
      () => new Promise<TeamDeleteOutcome>((resolve) => (finish = resolve)),
    );
    renderTeam(onDelete);
    openDelete();
    const dialog = screen.getByRole("dialog");
    expect(onDelete).not.toHaveBeenCalled();
    fireEvent.click(within(dialog).getByRole("button", { name: d.confirm }));
    const running = await screen.findByRole("button", { name: d.confirming });
    expect((running as HTMLButtonElement).disabled).toBe(true);
    fireEvent.click(running);
    expect(onDelete).toHaveBeenCalledTimes(1);
    finish({ kind: "deleted" });
    await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
  });

  it("FUI[workspace.team:delete-refused] a Team that Spaces still list shows those Spaces and no number for the hidden ones", async () => {
    const onDelete = vi.fn(async (): Promise<TeamDeleteOutcome> => ({
      kind: "refused",
      refusal: {
        references: [
          {
            kind: "space",
            id: "s1",
            name: "Phòng Công nghệ",
            spaceType: "Department",
          },
          {
            kind: "space",
            id: "s2",
            name: "Phòng Kế toán",
            spaceType: "Department",
          },
        ],
        moreHidden: true,
      },
    }));
    renderTeam(onDelete);
    openDelete();
    fireEvent.click(
      within(screen.getByRole("dialog")).getByRole("button", {
        name: d.confirm,
      }),
    );
    const dialog = await screen.findByRole("dialog", {
      name: /Chưa xoá được/,
    });
    const text = dialog.textContent ?? "";
    expect(text).toContain("Phòng Công nghệ");
    expect(text).toContain("Phòng Kế toán");
    expect(text).toContain(d.others);
    // "and others you cannot see" carries no count.
    expect(text.replace(/\d{1,2}\/\d{1,2}/g, "")).not.toMatch(
      /\b\d+\b.*(không có quyền xem)/,
    );
    expect(screen.queryByRole("button", { name: d.confirm })).toBeNull();
    fireEvent.click(within(dialog).getByRole("button", { name: d.understood }));
    expect(screen.queryByRole("dialog")).toBeNull();
  });

  it("FUI[workspace.team:delete-refused-hidden-only] when every referencing Space is hidden the list is empty and still says there are others", async () => {
    const onDelete = vi.fn(async (): Promise<TeamDeleteOutcome> => ({
      kind: "refused",
      refusal: { references: [], moreHidden: true },
    }));
    renderTeam(onDelete);
    openDelete();
    fireEvent.click(
      within(screen.getByRole("dialog")).getByRole("button", {
        name: d.confirm,
      }),
    );
    const dialog = await screen.findByRole("dialog", { name: /Chưa xoá được/ });
    expect(within(dialog).queryAllByRole("listitem")).toHaveLength(0);
    expect(dialog.textContent).toContain(d.others);
  });

  it("FUI[workspace.team:delete-conflict] a stale version says the Team changed and Reload reads it again", async () => {
    const onDelete = vi.fn(async (): Promise<TeamDeleteOutcome> => ({
      kind: "conflict",
    }));
    const props = renderTeam(onDelete);
    openDelete();
    fireEvent.click(
      within(screen.getByRole("dialog")).getByRole("button", {
        name: d.confirm,
      }),
    );
    const alert = await screen.findByRole("alert");
    expect(alert.textContent).toContain(d.conflict);
    fireEvent.click(within(alert).getByRole("button", { name: d.reload }));
    expect(props.onRetry).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.team:delete-denied-failed] a refusal and a plain failure each say what happened", async () => {
    const answers: TeamDeleteOutcome[] = [
      { kind: "denied" },
      { kind: "failed" },
    ];
    const onDelete = vi.fn(async () => answers.shift()!);
    renderTeam(onDelete);
    openDelete();
    const confirm = () =>
      fireEvent.click(
        within(screen.getByRole("dialog")).getByRole("button", {
          name: d.confirm,
        }),
      );
    confirm();
    expect((await screen.findByRole("alert")).textContent).toBe(d.denied);
    confirm();
    await waitFor(() =>
      expect(screen.getByRole("alert").textContent).toBe(d.failed),
    );
  });

  it("FUI[workspace.team:delete-who] the action is offered to those who manage Teams, also for an archived Team, and is absent for a reader or without the host callback", () => {
    renderTeam(vi.fn(), teamArchivedScenario());
    expect(screen.getByRole("button", { name: d.action })).toBeTruthy();
  });

  it("FUI[workspace.team:delete-reader] a reader gets no delete action", () => {
    renderTeam(vi.fn(), teamReadOnlyScenario());
    expect(screen.queryByRole("button", { name: d.action })).toBeNull();
  });

  it("FUI[workspace.team:delete-hidden] no action without the host callback", () => {
    renderTeam(undefined);
    expect(screen.queryByRole("button", { name: d.action })).toBeNull();
  });
});
