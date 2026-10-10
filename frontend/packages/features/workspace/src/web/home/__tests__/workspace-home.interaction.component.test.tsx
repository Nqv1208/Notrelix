import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";

import {
  homeCopyFixture,
  homeDefaultScenario,
  homeEdgeDataScenario,
  homeErrorScenario,
  homeLoadingScenario,
  homeOnboardingScenario,
  myWorkDefaultScenario,
  myWorkEmptyScenario,
  myWorkErrorScenario,
} from "../../../verification/workspace-home-fixtures";
import { shellNavigationFixture } from "../../../verification/workspace-shell-fixtures";
import { CreateResourceDialog } from "../create-resource-dialog";
import {
  WorkspaceHomeSurface,
  type WorkspaceHomeSurfaceProps,
} from "../workspace-home-surface";
import {
  WorkspaceMyWorkSurface,
  type WorkspaceMyWorkSurfaceProps,
} from "../workspace-my-work-surface";
import { WorkspaceOnboardingSurface } from "../workspace-onboarding-surface";

const copy = homeCopyFixture();

function renderHome(
  scenario: Omit<
    WorkspaceHomeSurfaceProps,
    | "onComplete"
    | "onCreateBoard"
    | "onCreateDoc"
    | "onInvite"
    | "onRetryContinue"
    | "onRetryMyWork"
    | "onRetryActivity"
    | "onRetryInvitations"
  > = homeDefaultScenario(),
  overrides: Partial<WorkspaceHomeSurfaceProps> = {},
) {
  const props: WorkspaceHomeSurfaceProps = {
    ...scenario,
    navigation: shellNavigationFixture(vi.fn()),
    onComplete: vi.fn(),
    onCreateBoard: vi.fn(),
    onCreateDoc: vi.fn(),
    onInvite: vi.fn(),
    onRetryContinue: vi.fn(),
    onRetryMyWork: vi.fn(),
    onRetryActivity: vi.fn(),
    onRetryInvitations: vi.fn(),
    ...overrides,
  };
  renderPureUi(<WorkspaceHomeSurface {...props} />);
  return props;
}

describe("WorkspaceHomeSurface", () => {
  it("FUI[workspace.home:render] renders resume-work sections with real destinations and no metric cards", () => {
    renderHome();

    expect(
      screen.getByRole("heading", { level: 1, name: copy.page.title }),
    ).toBeTruthy();
    const continueSection = screen.getByRole("region", {
      name: copy.continue.title,
    });
    const roadmap = within(continueSection).getByRole("link", {
      name: /Product Roadmap/,
    });
    expect(roadmap.getAttribute("href")).toBe(
      "/workspaces/ws-product/boards/board-roadmap",
    );
    expect(roadmap.textContent).toContain("25 minutes ago");
    expect(
      within(continueSection).getByRole("link", { name: /Sprint 42 retro/ })
        .textContent,
    ).toContain("Team rituals");

    const myWork = screen.getByRole("region", { name: copy.myWork.title });
    expect(
      within(myWork).getByRole("heading", { name: /Overdue/ }),
    ).toBeTruthy();
    expect(within(myWork).getByRole("heading", { name: /Today/ })).toBeTruthy();
    expect(
      within(myWork).getByRole("link", { name: "Open Review vendor contract" }),
    ).toBeTruthy();

    const side = screen.getByRole("complementary", {
      name: copy.page.sideLabel,
    });
    expect(within(side).getByText(/Ada Lovelace edited/)).toBeTruthy();
    expect(within(side).getByText("linh.nguyen@example.com")).toBeTruthy();
    expect(
      within(side)
        .getByRole("link", { name: /Manage invitations/ })
        .getAttribute("href"),
    ).toBe("/workspaces/ws-product/members");
  });

  it("FUI[workspace.home:complete] completes an item inline and blocks a second request while one is in flight", () => {
    const props = renderHome();

    fireEvent.click(
      screen.getByRole("button", {
        name: "Mark Review vendor contract complete",
      }),
    );
    expect(props.onComplete).toHaveBeenCalledWith(
      expect.objectContaining({ id: "item-contract" }),
    );

    const inFlight = screen.getByRole("button", {
      name: "Completing Sign off QA checklist…",
    }) as HTMLButtonElement;
    expect(inFlight.disabled).toBe(true);
  });

  it("FUI[workspace.home:navigate] hands item and section links to the host router", () => {
    const props = renderHome();
    fireEvent.click(
      screen.getByRole("link", { name: "Open Final pass on launch copy" }),
      { button: 0 },
    );
    expect(props.navigation.onNavigate).toHaveBeenCalledWith({
      section: "board",
      boardId: "board-launch",
      item: "item-copy",
    });
    fireEvent.click(
      screen.getAllByRole("link", { name: /View all my work/ })[0]!,
      { button: 0 },
    );
    expect(props.navigation.onNavigate).toHaveBeenCalledWith({
      section: "my-work",
    });
  });

  it("FUI[workspace.home:loading] shows stable skeletons per section while loading", () => {
    renderHome(homeLoadingScenario());
    expect(screen.getAllByRole("status").length).toBeGreaterThanOrEqual(3);
    expect(screen.queryByRole("link", { name: /Product Roadmap/ })).toBeNull();
    expect(screen.getByText(copy.page.greetingLoading)).toBeTruthy();
  });

  it("FUI[workspace.home:states] offers a retry per failed section", () => {
    const props = renderHome(homeErrorScenario());
    const retries = screen.getAllByRole("button", {
      name: copy.sections.retry,
    });
    expect(retries).toHaveLength(4);
    retries.forEach((button) => fireEvent.click(button));
    expect(props.onRetryContinue).toHaveBeenCalledTimes(1);
    expect(props.onRetryMyWork).toHaveBeenCalledTimes(1);
    expect(props.onRetryActivity).toHaveBeenCalledTimes(1);
    expect(props.onRetryInvitations).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.home:partial] keeps loaded sections usable when another section fails", () => {
    renderHome(homeEdgeDataScenario());
    expect(
      screen.getByRole("link", { name: /Lộ trình tích hợp hệ thống/ }),
    ).toBeTruthy();
    expect(screen.getAllByRole("alert")).toHaveLength(1);
    // The Board of this item is not in the authorized list: no Board name.
    expect(screen.queryByText(/^trong /)).toBeNull();
  });

  it("FUI[workspace.home:permission] hides pending invitations from members who are not admins", () => {
    renderHome({ ...homeDefaultScenario(), invitations: null });
    expect(
      screen.queryByRole("region", { name: copy.invitations.title }),
    ).toBeNull();
  });

  it("FUI[workspace.home:onboarding] replaces Continue with the onboarding steps in a new workspace", () => {
    const props = renderHome(homeOnboardingScenario());
    expect(
      screen.queryByRole("region", { name: copy.continue.title }),
    ).toBeNull();
    fireEvent.click(
      screen.getByRole("button", { name: copy.onboarding.boardAction }),
    );
    expect(props.onCreateBoard).toHaveBeenCalledTimes(1);
  });
});

describe("WorkspaceOnboardingSurface", () => {
  it("FUI[workspace.onboarding:actions] offers create Board, create Doc and invite as real actions", () => {
    const onCreateBoard = vi.fn();
    const onCreateDoc = vi.fn();
    const onInvite = vi.fn();
    renderPureUi(
      <WorkspaceOnboardingSurface
        copy={copy}
        workspaceName="Product"
        canCreate
        canInvite
        onCreateBoard={onCreateBoard}
        onCreateDoc={onCreateDoc}
        onInvite={onInvite}
      />,
    );
    expect(
      screen.getByRole("heading", { name: "Set up Product" }),
    ).toBeTruthy();
    fireEvent.click(
      screen.getByRole("button", { name: copy.onboarding.boardAction }),
    );
    fireEvent.click(
      screen.getByRole("button", { name: copy.onboarding.docAction }),
    );
    fireEvent.click(
      screen.getByRole("button", { name: copy.onboarding.inviteAction }),
    );
    expect(onCreateBoard).toHaveBeenCalledTimes(1);
    expect(onCreateDoc).toHaveBeenCalledTimes(1);
    expect(onInvite).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.onboarding:permission] explains steps the member cannot take instead of showing them", () => {
    renderPureUi(
      <WorkspaceOnboardingSurface
        copy={copy}
        workspaceName="Product"
        canCreate={false}
        canInvite={false}
        onCreateBoard={vi.fn()}
        onCreateDoc={vi.fn()}
        onInvite={vi.fn()}
      />,
    );
    expect(screen.queryByRole("button")).toBeNull();
    expect(screen.getByText(copy.onboarding.createUnavailable)).toBeTruthy();
  });
});

describe("CreateResourceDialog", () => {
  it("FUI[workspace.create-resource:submit] submits the trimmed name for the requested kind", () => {
    const onSubmit = vi.fn();
    renderPureUi(
      <CreateResourceDialog
        copy={copy}
        kind="board"
        pending={false}
        error={null}
        onOpenChange={vi.fn()}
        onSubmit={onSubmit}
      />,
    );
    const input = screen.getByLabelText(copy.create.boardLabel);
    fireEvent.change(input, { target: { value: "  Roadmap  " } });
    fireEvent.click(
      screen.getByRole("button", { name: copy.create.submitBoard }),
    );
    expect(onSubmit).toHaveBeenCalledWith("board", "Roadmap");
  });

  it("FUI[workspace.create-resource:validation] asks for a name and shows a failure next to the field", () => {
    const onSubmit = vi.fn();
    const { rerender } = renderPureUi(
      <CreateResourceDialog
        copy={copy}
        kind="doc"
        pending={false}
        error={null}
        onOpenChange={vi.fn()}
        onSubmit={onSubmit}
      />,
    );
    fireEvent.click(
      screen.getByRole("button", { name: copy.create.submitDoc }),
    );
    expect(onSubmit).not.toHaveBeenCalled();
    const input = screen.getByLabelText(copy.create.docLabel);
    expect(input.getAttribute("aria-invalid")).toBe("true");
    expect(screen.getByText(copy.create.required)).toBeTruthy();

    rerender(
      <CreateResourceDialog
        copy={copy}
        kind="doc"
        pending
        error={null}
        onOpenChange={vi.fn()}
        onSubmit={onSubmit}
      />,
    );
    expect(
      (
        screen.getByRole("button", {
          name: copy.create.submitting,
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(true);
  });
});

function renderMyWork(
  scenario = myWorkDefaultScenario(),
  overrides: Partial<WorkspaceMyWorkSurfaceProps> = {},
) {
  const props: WorkspaceMyWorkSurfaceProps = {
    ...scenario,
    onComplete: vi.fn(),
    onRetry: vi.fn(),
    onLoadMore: vi.fn(),
    ...overrides,
  };
  renderPureUi(<WorkspaceMyWorkSurface {...props} />);
  return props;
}

describe("WorkspaceMyWorkSurface", () => {
  it("FUI[workspace.my-work:render] groups every assigned item into due windows in order", () => {
    renderMyWork();
    const headings = screen
      .getAllByRole("heading", { level: 2 })
      .map((heading) => heading.textContent?.replace(/\d+$/, ""));
    expect(headings).toEqual([
      "Overdue",
      "Today",
      "This week",
      "Later",
      "No due date",
    ]);
    expect(screen.getByText("Archive stale automations")).toBeTruthy();
  });

  it("FUI[workspace.my-work:load-more] loads the next page and completes inline", () => {
    const props = renderMyWork();
    fireEvent.click(screen.getByRole("button", { name: copy.myWork.loadMore }));
    expect(props.onLoadMore).toHaveBeenCalledTimes(1);
    fireEvent.click(
      screen.getByRole("button", { name: "Mark Draft Q1 objectives complete" }),
    );
    expect(props.onComplete).toHaveBeenCalledWith(
      expect.objectContaining({ id: "item-okrs" }),
    );
  });

  it("FUI[workspace.my-work:empty] teaches the empty state", () => {
    renderMyWork(myWorkEmptyScenario());
    expect(screen.getByText(copy.myWork.empty)).toBeTruthy();
    expect(screen.getByText(copy.myWork.emptyHint)).toBeTruthy();
  });

  it("FUI[workspace.my-work:states] retries a failed load", () => {
    const props = renderMyWork(myWorkErrorScenario());
    fireEvent.click(screen.getByRole("button", { name: copy.sections.retry }));
    expect(props.onRetry).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.my-work:read-only] hides inline completion when the host offers none", () => {
    renderMyWork(myWorkDefaultScenario(), { onComplete: null });
    expect(screen.queryByRole("button", { name: /^Mark / })).toBeNull();
    expect(
      screen.getByRole("link", { name: "Open Review vendor contract" }),
    ).toBeTruthy();
  });
});
