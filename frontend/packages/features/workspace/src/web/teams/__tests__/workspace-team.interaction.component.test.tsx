import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";

import {
  securityDefaultScenario,
  securityLinksOnScenario,
  securityReadOnlyScenario,
  teamArchivedScenario,
  teamBackNavigationFixture,
  teamDefaultScenario,
  teamHighDensityScenario,
  teamReadOnlyScenario,
  teamRoleMenuScenario,
  teamUsageNavigationFixture,
  type TeamScenario,
} from "../../../verification/workspace-teams-fixtures";
import {
  WorkspaceSecuritySurface,
  type WorkspaceSecuritySurfaceProps,
} from "../workspace-security-surface";
import {
  WorkspaceTeamSurface,
  type WorkspaceTeamSurfaceProps,
} from "../workspace-team-surface";

function renderTeam(
  scenario: TeamScenario = teamDefaultScenario(),
  overrides: Partial<WorkspaceTeamSurfaceProps> = {},
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
    ...overrides,
  };
  renderPureUi(<WorkspaceTeamSurface {...props} />);
  return props;
}

describe("WorkspaceTeamSurface", () => {
  it("FUI[workspace.team:render] shows the Team, its people with email, job and day joined, and where it has access", () => {
    renderTeam();
    expect(
      screen.getByRole("heading", { name: "Nhóm Kỹ sư ERP" }),
    ).toBeTruthy();
    expect(
      screen.getByText(
        "Kỹ sư triển khai và dữ liệu ERP · tạo 01/09 bởi Nguyễn Thu Hà",
      ),
    ).toBeTruthy();
    expect(
      screen.getByText("quan.tran@minhan.vn · Kỹ sư dữ liệu"),
    ).toBeTruthy();
    expect(screen.getByText("từ 15/09")).toBeTruthy();
    expect(screen.getByText("Phòng Công nghệ")).toBeTruthy();
    expect(screen.getByText("Vai trò Thành viên · thêm 01/09")).toBeTruthy();
    expect(screen.getByText("Trưởng nhóm là gì")).toBeTruthy();
  });

  it("FUI[workspace.team:rename] edits the name and description in a dialog", () => {
    const props = renderTeam();
    fireEvent.click(screen.getByRole("button", { name: "Sửa tên và mô tả" }));
    const dialog = screen.getByRole("dialog", { name: "Sửa tên và mô tả" });
    fireEvent.change(within(dialog).getByLabelText("Tên nhóm"), {
      target: { value: "Nhóm ERP" },
    });
    fireEvent.click(within(dialog).getByRole("button", { name: "Lưu" }));
    expect(props.onRename).toHaveBeenCalledWith("Nhóm ERP");
    expect(props.onDescribe).not.toHaveBeenCalled();
  });

  it("FUI[workspace.team:role] changes a person's role from the menu", () => {
    const props = renderTeam();
    fireEvent.click(
      screen.getByRole("button", {
        name: "Vai trò của Phan Ngọc Anh: Thành viên",
      }),
    );
    fireEvent.click(screen.getByRole("menuitemradio", { name: /Trưởng nhóm/ }));
    expect(props.onChangeRole).toHaveBeenCalledWith("u-anh", "Lead");
  });

  it("FUI[workspace.team:last-lead] says why the only lead cannot be lowered or removed", () => {
    const props = renderTeam(teamRoleMenuScenario());
    const menu = screen.getByRole("menu", {
      name: "Đổi vai trò của Trần Minh Quân trong nhóm",
    });
    expect(
      within(menu).getByText("Không hạ được: Quân là trưởng nhóm duy nhất"),
    ).toBeTruthy();
    expect(
      within(menu)
        .getByRole("menuitem", { name: /Gỡ khỏi nhóm/ })
        .getAttribute("aria-disabled"),
    ).toBe("true");
    fireEvent.click(
      within(menu).getByRole("menuitem", { name: /Gỡ khỏi nhóm/ }),
    );
    expect(props.onRemove).not.toHaveBeenCalled();
    expect(
      screen
        .getByRole("button", { name: "Gỡ Trần Minh Quân khỏi nhóm" })
        .hasAttribute("disabled"),
    ).toBe(true);
  });

  it("FUI[workspace.team:add] adds a person found by name or email as a member", () => {
    const props = renderTeam();
    const input = screen.getByRole("combobox");
    fireEvent.change(input, { target: { value: "tai.khuc" } });
    fireEvent.keyDown(input, { key: "Enter" });
    expect(props.onAddMember).toHaveBeenCalledWith({
      userId: "u-tai",
      role: "Member",
    });
  });

  it("FUI[workspace.team:remove] removes a person from the Team", () => {
    const props = renderTeam();
    fireEvent.click(
      screen.getByRole("button", { name: "Gỡ Lâm Gia Minh khỏi nhóm" }),
    );
    expect(props.onRemove).toHaveBeenCalledWith("u-minh");
  });

  it("FUI[workspace.team:archive] confirms before archiving the Team", () => {
    const props = renderTeam();
    fireEvent.click(screen.getByRole("button", { name: "Lưu trữ nhóm" }));
    const dialog = screen.getByRole("dialog", {
      name: "Lưu trữ Nhóm Kỹ sư ERP?",
    });
    fireEvent.click(
      within(dialog).getByRole("button", { name: "Lưu trữ nhóm" }),
    );
    expect(props.onArchive).toHaveBeenCalled();
  });

  it("FUI[workspace.team:archived-blocked] freezes an archived Team and offers Restore", () => {
    const props = renderTeam(teamArchivedScenario());
    expect(screen.getByText(/Nhóm này đã lưu trữ/)).toBeTruthy();
    expect(screen.queryByRole("combobox")).toBeNull();
    expect(screen.queryByRole("button", { name: "Thêm người" })).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Khôi phục nhóm" }));
    expect(props.onUnarchive).toHaveBeenCalled();
    expect(screen.getByText(/tạm ngừng khi nhóm lưu trữ/)).toBeTruthy();
  });

  it("FUI[workspace.team:read-only] shows the people but offers no change to a member", () => {
    renderTeam(teamReadOnlyScenario());
    expect(screen.getByText("Phan Ngọc Anh")).toBeTruthy();
    expect(screen.queryByRole("combobox")).toBeNull();
    expect(screen.queryByRole("button", { name: /^Gỡ / })).toBeNull();
    expect(screen.queryByRole("button", { name: "Lưu trữ nhóm" })).toBeNull();
  });

  it("FUI[workspace.team:high-density] lists forty people", () => {
    renderTeam(teamHighDensityScenario());
    expect(screen.getByText("Người 40")).toBeTruthy();
  });
});

function renderSecurity(
  scenario = securityDefaultScenario(),
  overrides: Partial<WorkspaceSecuritySurfaceProps> = {},
) {
  const props: WorkspaceSecuritySurfaceProps = {
    ...scenario,
    onToggleLinks: vi.fn(),
    onRetry: vi.fn(),
    ...overrides,
  };
  renderPureUi(<WorkspaceSecuritySurface {...props} />);
  return props;
}

describe("WorkspaceSecuritySurface", () => {
  it("FUI[workspace.security:render] states the rules: links off since a day, guests, default end, domain", () => {
    renderSecurity();
    expect(
      screen.getByRole("heading", { name: "Chia sẻ và bảo mật" }),
    ).toBeTruthy();
    expect(
      screen.getByText(
        "Đang tắt từ 15/09 · Không ai tạo được liên kết chia sẻ mới",
      ),
    ).toBeTruthy();
    expect(screen.getByText("2 khách đang có")).toBeTruthy();
    expect(
      (
        screen.getByLabelText(
          "Hạn quyền mặc định cho khách",
        ) as HTMLSelectElement
      ).value,
    ).toBe("d90");
    expect(
      (screen.getByLabelText("Tên miền công ty") as HTMLInputElement).value,
    ).toBe("minhan.vn");
  });

  it("FUI[workspace.security:toggle] turns the sharing links on", () => {
    const props = renderSecurity();
    const toggle = screen.getByRole("switch", { name: "Liên kết chia sẻ" });
    expect(toggle.getAttribute("aria-checked")).toBe("false");
    fireEvent.click(toggle);
    expect(props.onToggleLinks).toHaveBeenCalledWith(true);
  });

  it("FUI[workspace.security:read-only] leaves the switch inert for a member", () => {
    const props = renderSecurity(securityReadOnlyScenario());
    const toggle = screen.getByRole("switch", { name: "Liên kết chia sẻ" });
    expect(toggle.getAttribute("aria-disabled")).toBe("true");
    fireEvent.click(toggle);
    expect(props.onToggleLinks).not.toHaveBeenCalled();
  });

  it("says the links are on when they are", () => {
    renderSecurity(securityLinksOnScenario());
    expect(
      screen
        .getByRole("switch", { name: "Liên kết chia sẻ" })
        .getAttribute("aria-checked"),
    ).toBe("true");
    expect(screen.getByText("Liên kết chia sẻ tạo và dùng được.")).toBeTruthy();
  });
});
