import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";

import {
  createTeamDefaultScenario,
  teamsArchiveScenario,
  teamsCopyFixture,
  teamsDefaultScenario,
  teamsEmptyScenario,
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
  const props: WorkspaceTeamsSurfaceProps = {
    ...scenario,
    navigation: teamsNavigationFixture(),
    onCreate: vi.fn(),
    onArchive: vi.fn(),
    onRestore: vi.fn(),
    onRetry: vi.fn(),
    ...overrides,
  };
  const view = renderPureUi(<WorkspaceTeamsSurface {...props} />);
  return { props, view };
}

/** Opens the styled Select behind a label and picks one option by its text. */
function choose(trigger: HTMLElement, option: string | RegExp) {
  fireEvent.keyDown(trigger, { key: "Enter" });
  fireEvent.click(screen.getByRole("option", { name: option }));
}

describe("WorkspaceTeamsSurface", () => {
  it("FUI[workspace.teams:render] lists the Teams with people, lead and where each is used, the archived one apart", () => {
    renderTeams();
    expect(screen.getByRole("heading", { name: "Nhóm" })).toBeTruthy();
    expect(screen.getByText("4 nhóm đang dùng · 1 đã lưu trữ")).toBeTruthy();
    const table = screen.getByRole("table", {
      name: "Nhóm trong Minh An Group",
    });
    const rows = within(table).getAllByRole("row").slice(1);
    expect(rows).toHaveLength(4);
    expect(within(rows[0]!).getByText("Nhóm Kỹ sư ERP")).toBeTruthy();
    expect(within(rows[0]!).getByText("4 người")).toBeTruthy();
    expect(within(rows[0]!).getByText("Trần Minh Quân")).toBeTruthy();
    expect(
      within(rows[0]!).getByText("Phòng Công nghệ · Thành viên"),
    ).toBeTruthy();
    expect(
      within(rows[1]!).getByText("Phòng Kế toán · Người xem"),
    ).toBeTruthy();
    expect(
      within(rows[2]!).getByText("Board Triển khai ERP Q4 · Có thể xem"),
    ).toBeTruthy();
    expect(within(rows[3]!).getByText("Chưa dùng ở đâu")).toBeTruthy();
    expect(screen.getByText("Đã lưu trữ 1")).toBeTruthy();
    const archived = screen.getByRole("table", { name: "Nhóm đã lưu trữ" });
    expect(
      within(archived).getByText("Lưu trữ 20/09 · Nguyễn Thu Hà"),
    ).toBeTruthy();
    expect(
      within(archived).getByText("Hạ tầng & bảo mật · tạm ngừng"),
    ).toBeTruthy();
  });

  it("FUI[workspace.teams:filter] narrows by Team name or person, ignoring marks", () => {
    renderTeams();
    fireEvent.change(screen.getByRole("searchbox"), {
      target: { value: "vo thi hanh" },
    });
    const table = screen.getByRole("table", {
      name: "Nhóm trong Minh An Group",
    });
    expect(within(table).getAllByRole("row")).toHaveLength(2);
    expect(within(table).getByText("Nhóm Kế toán tổng hợp")).toBeTruthy();
    fireEvent.change(screen.getByRole("searchbox"), {
      target: { value: "khong co nhom nay" },
    });
    expect(screen.getByText(copy.page.noMatches)).toBeTruthy();
  });

  it("FUI[workspace.teams:create] opens the create dialog from the button", () => {
    renderTeams();
    fireEvent.click(screen.getByRole("button", { name: "Tạo nhóm" }));
    expect(screen.getByRole("dialog", { name: "Tạo nhóm" })).toBeTruthy();
  });

  it("FUI[workspace.teams:archive] confirms before archiving and says who loses access", () => {
    const { props } = renderTeams(teamsArchiveScenario());
    const dialog = screen.getByRole("dialog", {
      name: "Lưu trữ Nhóm Kỹ sư ERP?",
    });
    expect(
      within(dialog).getByText(/3 người sẽ mất quyền vào Phòng Công nghệ/),
    ).toBeTruthy();
    expect(within(dialog).getByText(/Trần Minh Quân vẫn còn/)).toBeTruthy();
    fireEvent.click(
      within(dialog).getByRole("button", { name: "Lưu trữ nhóm" }),
    );
    expect(props.onArchive).toHaveBeenCalledWith("t-erp");
  });

  it("FUI[workspace.teams:read-only] tells a member whom to ask instead of the list", () => {
    renderTeams(teamsReadOnlyScenario());
    expect(
      screen.getByRole("heading", {
        name: "Chỉ quản trị workspace quản lý nhóm.",
      }),
    ).toBeTruthy();
    expect(screen.getByText("Trịnh Quốc Hưng")).toBeTruthy();
    expect(
      screen.getByText(/Chủ sở hữu workspace · hung.tq@minhan.vn/),
    ).toBeTruthy();
    expect(screen.queryByRole("table")).toBeNull();
    expect(screen.getByRole("link", { name: "Về Trang chủ" })).toBeTruthy();
  });

  it("FUI[workspace.teams:empty] explains when there is no Team yet", () => {
    renderTeams(teamsEmptyScenario());
    expect(screen.getByText(copy.list.empty.title)).toBeTruthy();
  });

  it("shows a loading placeholder while the Teams read", () => {
    renderTeams(teamsLoadingScenario());
    expect(
      screen.getByRole("status", { name: copy.list.loading }),
    ).toBeTruthy();
  });

  it("restores an archived Team from its row", () => {
    const { props } = renderTeams();
    fireEvent.click(
      screen.getByRole("button", { name: "Khôi phục Nhóm Trực hệ thống" }),
    );
    expect(props.onRestore).toHaveBeenCalledWith("t-oncall");
  });
});

describe("CreateTeamDialog", () => {
  function renderCreate(overrides: Partial<CreateTeamDialogProps> = {}) {
    const props: CreateTeamDialogProps = {
      ...createTeamDefaultScenario(),
      onOpenChange: vi.fn(),
      onSubmit: vi.fn(),
      ...overrides,
    };
    renderPureUi(<CreateTeamDialog {...props} />);
    return props;
  }

  it("FUI[workspace.create-team:validate] asks for a name and a team lead", () => {
    const props = renderCreate();
    fireEvent.click(screen.getByRole("button", { name: "Tạo nhóm" }));
    expect(props.onSubmit).not.toHaveBeenCalled();
    expect(screen.getAllByRole("alert").length).toBeGreaterThanOrEqual(2);
  });

  it("FUI[workspace.create-team:submit] sends the name, the lead and the people added", () => {
    const props = renderCreate();
    fireEvent.change(screen.getByLabelText("Tên nhóm"), {
      target: { value: "Nhóm Triển khai kho" },
    });
    choose(screen.getByLabelText(/Trưởng nhóm/), /Đặng Quốc Bảo/);
    const input = screen.getByPlaceholderText("Gõ tên hoặc email");
    fireEvent.change(input, { target: { value: "minh.lam" } });
    fireEvent.keyDown(input, { key: "Enter" });
    expect(
      screen.getByText("2 người · Đặng Quốc Bảo là trưởng nhóm"),
    ).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Tạo nhóm" }));
    expect(props.onSubmit).toHaveBeenCalledWith({
      name: "Nhóm Triển khai kho",
      description: null,
      leadId: "u-bao",
      memberIds: ["u-minh"],
    });
  });
});
