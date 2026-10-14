import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";

import {
  homeAllDoneScenario,
  homeCopyFixture,
  homeDefaultScenario,
  homeEdgeDataScenario,
  homeErrorScenario,
  homeLoadingScenario,
  homeOnboardingScenario,
  homeProducedOnlyScenario,
} from "../../../verification/workspace-home-fixtures";
import { shellNavigationFixture } from "../../../verification/workspace-shell-fixtures";
import { CreateResourceDialog } from "../create-resource-dialog";
import {
  WorkspaceHomeSurface,
  type WorkspaceHomeSurfaceProps,
} from "../workspace-home-surface";
import { WorkspaceOnboardingSurface } from "../workspace-onboarding-surface";

const copy = homeCopyFixture();
const viCopy = homeCopyFixture("vi");

type HomeScenario = ReturnType<typeof homeDefaultScenario>;

function renderHome(
  scenario: HomeScenario = homeDefaultScenario(),
  overrides: Partial<WorkspaceHomeSurfaceProps> = {},
) {
  const props: WorkspaceHomeSurfaceProps = {
    ...scenario,
    navigation: shellNavigationFixture(vi.fn()),
    onComplete: vi.fn(),
    onAddItem: null,
    onCreateBoard: vi.fn(),
    onCreateDoc: vi.fn(),
    onInvite: vi.fn(),
    onRetryContinue: vi.fn(),
    onRetryMyWork: vi.fn(),
    ...overrides,
  };
  renderPureUi(<WorkspaceHomeSurface {...props} />);
  return props;
}

describe("WorkspaceHomeSurface", () => {
  it("FUI[workspace.home:render] renders the greeting, the quick filters, the due windows and the side cards", () => {
    renderHome();

    expect(
      screen.getByRole("heading", { level: 1, name: "Chào buổi sáng, Hà" }),
    ).toBeTruthy();
    expect(
      screen.getByText(
        "Thứ Hai, 12 tháng 10 · 3 cuộc họp hôm nay, 7 việc đang mở",
      ),
    ).toBeTruthy();

    const chips = screen.getByRole("group", { name: viCopy.home.chipsLabel });
    expect(within(chips).getByText("quá hạn").textContent).toBeTruthy();
    expect(chips.textContent).toContain("1 quá hạn");
    expect(chips.textContent).toContain("2 đến hạn hôm nay");
    expect(chips.textContent).toContain("1 chờ bạn duyệt");
    expect(chips.textContent).toContain("1 nhắc tên bạn");

    const overdue = screen.getByRole("region", { name: /Quá hạn/ });
    expect(
      within(overdue).getByRole("link", {
        name: "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP",
      }),
    ).toBeTruthy();
    expect(overdue.textContent).toContain("Quá hạn 2 ngày");
    expect(overdue.textContent).toContain("ERP-142");
    expect(overdue.textContent).toContain("bạn đang theo dõi");

    const today = screen.getByRole("region", { name: /Hôm nay/ });
    expect(today.textContent).toContain("17:00");
    const week = screen.getByRole("region", { name: /Tuần này/ });
    expect(week.textContent).toContain("Thứ Tư 14/10");
    expect(week.textContent).toContain("Thứ Sáu 16/10");
    expect(week.textContent).toContain("Sau đó · 2 việc đến hạn từ tuần sau");

    expect(
      screen.getByRole("region", { name: /Cần bạn phản hồi/ }),
    ).toBeTruthy();
    expect(
      screen.getByRole("region", { name: /Lịch hôm nay/ }).textContent,
    ).toContain("10:40 bây giờ");
    const cont = screen.getByRole("region", {
      name: viCopy.home.continue.title,
    });
    expect(
      within(cont)
        .getByRole("link", { name: /Triển khai ERP Q4/ })
        .getAttribute("href"),
    ).toBe("/workspaces/ws-product/boards/board-erp");
  });

  it("FUI[workspace.home:complete] completes an item inline and blocks a second request while one is in flight", () => {
    const props = renderHome(homeDefaultScenario(), {
      completingIds: new Set(["item-erp-144"]),
    });

    fireEvent.click(
      screen.getByRole("button", {
        name: "Đánh dấu hoàn thành Đối chiếu sao kê ngân hàng tháng 9",
      }),
    );
    expect(props.onComplete).toHaveBeenCalledWith(
      expect.objectContaining({ id: "item-kt-88" }),
    );

    const inFlight = screen.getByRole("button", {
      name: "Đánh dấu hoàn thành Kiểm thử phân hệ công nợ với số liệu tháng 9",
    });
    expect(inFlight.getAttribute("aria-disabled")).toBe("true");
    fireEvent.click(inFlight);
    expect(props.onComplete).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.home:navigate] hands item, section and quick-filter links to the host router", () => {
    const props = renderHome();
    fireEvent.click(
      screen.getByRole("link", {
        name: "Góp ý nội dung trang giới thiệu sản phẩm mới",
      }),
      { button: 0 },
    );
    expect(props.navigation.onNavigate).toHaveBeenCalledWith({
      section: "board",
      boardId: "board-mk",
      item: "item-mk-21",
    });
    fireEvent.click(
      screen.getByRole("link", { name: viCopy.home.sections.viewAll }),
      { button: 0 },
    );
    expect(props.navigation.onNavigate).toHaveBeenCalledWith({
      section: "my-work",
    });
    fireEvent.click(screen.getByRole("link", { name: /1 quá hạn/ }), {
      button: 0,
    });
    expect(props.navigation.onNavigate).toHaveBeenLastCalledWith({
      section: "my-work",
    });
    fireEvent.click(screen.getByRole("link", { name: /chờ bạn duyệt/ }), {
      button: 0,
    });
    expect(props.navigation.onNavigate).toHaveBeenLastCalledWith({
      section: "inbox",
    });
  });

  it("FUI[workspace.home:loading] shows stable skeletons for the sections while loading", () => {
    renderHome(homeLoadingScenario());
    expect(screen.getAllByRole("region", { busy: true }).length).toBe(5);
    expect(
      screen.queryByRole("link", { name: /Triển khai ERP Q4/ }),
    ).toBeNull();
    expect(
      screen.getByText("Thứ Hai, 12 tháng 10 · đang tải việc của bạn…"),
    ).toBeTruthy();
  });

  it("FUI[workspace.home:states] offers a retry per failed section", () => {
    const props = renderHome(homeErrorScenario());
    const retries = screen.getAllByRole("button", { name: viCopy.home.retry });
    expect(retries).toHaveLength(2);
    retries.forEach((button) => fireEvent.click(button));
    expect(props.onRetryMyWork).toHaveBeenCalledTimes(1);
    expect(props.onRetryContinue).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.home:partial] keeps loaded sections usable and names no Board the member cannot open", () => {
    renderHome(homeEdgeDataScenario());
    expect(
      screen.getByRole("link", { name: /Chuẩn bị tài liệu bàn giao/ }),
    ).toBeTruthy();
    expect(
      screen.getByRole("link", { name: /Lộ trình tích hợp hệ thống/ }),
    ).toBeTruthy();
    expect(screen.queryByText(/board-unknown/)).toBeNull();
    // The cards without a producer are not drawn.
    expect(
      screen.queryByRole("region", { name: /Cần bạn phản hồi/ }),
    ).toBeNull();
    expect(screen.queryByRole("region", { name: /Lịch hôm nay/ })).toBeNull();
  });

  it("FUI[workspace.home:all-done] says nothing else is due today and when the next item is", () => {
    renderHome(homeAllDoneScenario());
    const today = screen.getByRole("region", { name: /Hôm nay/ });
    expect(today.textContent).toContain(
      "Xong hết việc đến hạn hôm nay · còn 1 việc quá hạn",
    );
    expect(today.textContent).toContain("Việc tiếp theo đến hạn thứ Tư 14/10.");
    expect(screen.queryByText(/đến hạn hôm nay$/)).toBeNull();
  });

  it("FUI[workspace.home:produced-only] draws only what the host fills in: no key, Status, Priority or reply cards", () => {
    renderHome(homeProducedOnlyScenario());
    expect(screen.queryByText("ERP-142")).toBeNull();
    expect(screen.queryByText("Đang làm")).toBeNull();
    expect(screen.queryByRole("img", { name: /Ưu tiên/ })).toBeNull();
    expect(
      screen.queryByRole("region", { name: /Cần bạn phản hồi/ }),
    ).toBeNull();
    expect(
      screen.getByRole("link", {
        name: "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP",
      }),
    ).toBeTruthy();
  });

  it("FUI[workspace.home:inert] draws Add item and My calendar inert with the reason when no host action exists", () => {
    renderHome(homeDefaultScenario(), { calendarHref: null });
    const add = screen.getByRole("button", { name: viCopy.home.addItem });
    expect(add.getAttribute("aria-disabled")).toBe("true");
    expect(add.getAttribute("title")).toBe(viCopy.home.addItemUnavailable);
    const calendar = screen.getByRole("button", { name: viCopy.home.calendar });
    expect(calendar.getAttribute("aria-disabled")).toBe("true");
    expect(calendar.getAttribute("title")).toBe(
      viCopy.home.calendarUnavailable,
    );
  });

  it("FUI[workspace.home:onboarding] replaces the work sections with the onboarding steps in a new workspace", () => {
    const props = renderHome(homeOnboardingScenario());
    expect(screen.queryByRole("region", { name: /Hôm nay/ })).toBeNull();
    fireEvent.click(
      screen.getByRole("button", { name: viCopy.onboarding.boardAction }),
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
