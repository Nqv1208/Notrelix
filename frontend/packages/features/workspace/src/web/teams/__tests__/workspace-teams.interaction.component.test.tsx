import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";

import {
  createTeamDefaultScenario,
  createTeamErrorScenario,
  createTeamPendingScenario,
  teamsArchivedScenario,
  teamsCopyFixture,
  teamsDefaultScenario,
  teamsEmptyScenario,
  teamsErrorScenario,
  teamsLoadingScenario,
  teamsNavigationFixture,
  teamsReadOnlyScenario,
} from "../../../verification/workspace-teams-fixtures";
import {
  CreateTeamDialog,
  type CreateTeamDialogProps,
} from "../create-team-dialog";
import {
  WorkspaceTeamsSurface,
  type WorkspaceTeamsSurfaceProps,
} from "../workspace-teams-surface";

const copy = teamsCopyFixture();

function renderTeams(
  scenario = teamsDefaultScenario(),
  overrides: Partial<WorkspaceTeamsSurfaceProps> = {},
) {
  const onNavigate = vi.fn();
  const props: WorkspaceTeamsSurfaceProps = {
    ...scenario,
    navigation: teamsNavigationFixture(onNavigate),
    onFilterChange: vi.fn(),
    onCreate: vi.fn(),
    onRetry: vi.fn(),
    ...overrides,
  };
  const view = renderPureUi(<WorkspaceTeamsSurface {...props} />);
  return { props, onNavigate, view };
}

describe("WorkspaceTeamsSurface", () => {
  it("FUI[workspace.teams:render] lists active Teams as links with their member counts", () => {
    const { onNavigate } = renderTeams();
    expect(
      screen.getByRole("heading", { level: 1, name: copy.list.title }),
    ).toBeTruthy();
    const platform = screen.getByRole("link", { name: "Platform engineering" });
    expect(platform.getAttribute("href")).toBe(
      "/w/product/teams/team-platform",
    );
    expect(platform.className).toContain("text-link");
    expect(screen.getByText("3 members")).toBeTruthy();
    expect(screen.getByText("1 member")).toBeTruthy();
    // The archived Teams belong to the other filter.
    expect(screen.queryByText("Legacy billing")).toBeNull();

    // JSDOM cannot navigate; swallow the browser default after React ran.
    const swallow = (event: Event) => event.preventDefault();
    document.addEventListener("click", swallow);
    try {
      fireEvent.click(platform);
      expect(onNavigate).toHaveBeenCalledWith("team-platform");
      // A modified click is left to the browser.
      onNavigate.mockClear();
      fireEvent.click(platform, { ctrlKey: true });
      expect(onNavigate).not.toHaveBeenCalled();
    } finally {
      document.removeEventListener("click", swallow);
    }
  });

  it("FUI[workspace.teams:filter] switches between active and archived Teams through the host", () => {
    const { props } = renderTeams();
    expect(
      screen
        .getByRole("radio", { name: copy.list.filter.active })
        .getAttribute("aria-checked"),
    ).toBe("true");
    fireEvent.click(
      screen.getByRole("radio", { name: copy.list.filter.archived }),
    );
    expect(props.onFilterChange).toHaveBeenCalledWith("archived");
  });

  it("shows archived Teams with an Archived badge under the Archived filter", () => {
    renderTeams(teamsArchivedScenario());
    const legacy = screen
      .getByRole("link", { name: "Legacy billing" })
      .closest("li") as HTMLElement;
    expect(within(legacy).getByText(copy.list.archived)).toBeTruthy();
    expect(screen.queryByText("Platform engineering")).toBeNull();
  });

  it("FUI[workspace.teams:create] offers New Team to managers", () => {
    const { props } = renderTeams();
    fireEvent.click(screen.getByRole("button", { name: copy.list.new }));
    expect(props.onCreate).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.teams:read-only] offers no creation and states why", () => {
    renderTeams(teamsReadOnlyScenario());
    expect(screen.queryByRole("button", { name: copy.list.new })).toBeNull();
    expect(screen.getByText(copy.readOnly)).toBeTruthy();
    expect(
      screen.getByRole("link", { name: "Platform engineering" }),
    ).toBeTruthy();
  });

  it("FUI[workspace.teams:empty] explains an empty list and offers New Team once", () => {
    const { props } = renderTeams(teamsEmptyScenario());
    expect(screen.getByText(copy.list.empty.title)).toBeTruthy();
    const create = screen.getAllByRole("button", { name: copy.list.new });
    expect(create).toHaveLength(1);
    fireEvent.click(create[0]!);
    expect(props.onCreate).toHaveBeenCalledTimes(1);
  });

  it("explains an empty archive and keeps New Team in the header only", () => {
    renderTeams({ ...teamsEmptyScenario(), filter: "archived" });
    expect(screen.getByText(copy.list.empty.archivedTitle)).toBeTruthy();
    expect(screen.getAllByRole("button", { name: copy.list.new })).toHaveLength(
      1,
    );
  });

  it("offers nothing to create in an empty list to a viewer who cannot manage Teams", () => {
    renderTeams({ ...teamsEmptyScenario(), canManage: false });
    expect(screen.getByText(copy.list.empty.title)).toBeTruthy();
    expect(screen.queryByRole("button", { name: copy.list.new })).toBeNull();
  });

  it("shows a status while loading and retries a failed load", () => {
    const loading = renderTeams(teamsLoadingScenario());
    expect(
      screen.getByRole("status", { name: copy.list.loading }),
    ).toBeTruthy();
    loading.view.unmount();

    const { props } = renderTeams(teamsErrorScenario());
    expect(screen.getByRole("alert").textContent).toContain(copy.list.error);
    fireEvent.click(screen.getByRole("button", { name: copy.list.retry }));
    expect(props.onRetry).toHaveBeenCalledTimes(1);
  });
});

function renderCreate(
  scenario = createTeamDefaultScenario(),
  overrides: Partial<CreateTeamDialogProps> = {},
) {
  const props: CreateTeamDialogProps = {
    ...scenario,
    onOpenChange: vi.fn(),
    onSubmit: vi.fn(),
    ...overrides,
  };
  const view = renderPureUi(<CreateTeamDialog {...props} />);
  return { props, view };
}

describe("CreateTeamDialog", () => {
  it("FUI[workspace.create-team:validate] requires a name and enforces the 160 and 1024 character limits", () => {
    const { props } = renderCreate();
    const submit = screen.getByRole("button", { name: copy.create.submit });

    fireEvent.click(submit);
    expect(screen.getByText(copy.create.nameRequired)).toBeTruthy();
    expect(props.onSubmit).not.toHaveBeenCalled();

    const name = screen.getByLabelText(copy.create.nameLabel);
    expect(name.getAttribute("maxlength")).toBe("160");
    fireEvent.change(name, { target: { value: "   " } });
    fireEvent.click(submit);
    expect(screen.getByText(copy.create.nameRequired)).toBeTruthy();

    fireEvent.change(name, { target: { value: "x".repeat(161) } });
    fireEvent.click(submit);
    expect(screen.getByText(copy.create.nameTooLong)).toBeTruthy();
    expect(props.onSubmit).not.toHaveBeenCalled();

    fireEvent.change(name, { target: { value: "x".repeat(160) } });
    fireEvent.change(screen.getByLabelText(copy.create.descriptionLabel), {
      target: { value: "d".repeat(1025) },
    });
    fireEvent.click(submit);
    expect(screen.getByText(copy.create.descriptionTooLong)).toBeTruthy();
    expect(props.onSubmit).not.toHaveBeenCalled();
  });

  it("FUI[workspace.create-team:submit] submits the trimmed name and description, and null for none", () => {
    const { props } = renderCreate();
    fireEvent.change(screen.getByLabelText(copy.create.nameLabel), {
      target: { value: "  Platform engineering  " },
    });
    fireEvent.click(screen.getByRole("button", { name: copy.create.submit }));
    expect(props.onSubmit).toHaveBeenLastCalledWith({
      name: "Platform engineering",
      description: null,
    });

    fireEvent.change(screen.getByLabelText(copy.create.descriptionLabel), {
      target: { value: "  Shared runtime  " },
    });
    fireEvent.click(screen.getByRole("button", { name: copy.create.submit }));
    expect(props.onSubmit).toHaveBeenLastCalledWith({
      name: "Platform engineering",
      description: "Shared runtime",
    });
  });

  it("keeps the form while pending, shows the producer error and cancels", () => {
    const pending = renderCreate(createTeamPendingScenario());
    const busy = screen.getByRole("button", {
      name: copy.create.submitting,
    }) as HTMLButtonElement;
    expect(busy.disabled).toBe(true);
    pending.view.unmount();

    const { props } = renderCreate(createTeamErrorScenario());
    const message = "A Team with this name already exists in the workspace.";
    expect(screen.getByText(message)).toBeTruthy();
    expect(
      screen.getByLabelText(copy.create.nameLabel).getAttribute("aria-invalid"),
    ).toBe("true");
    fireEvent.click(screen.getByRole("button", { name: copy.create.cancel }));
    expect(props.onOpenChange).toHaveBeenCalledWith(false);
  });
});
