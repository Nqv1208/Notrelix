import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";

import {
  workspacesCopyFixture,
  workspacesDefaultScenario,
  workspacesEdgeDataScenario,
  workspacesErrorScenario,
  workspacesLoadingScenario,
  workspacesNoticeScenario,
  workspacesSoleOwnerScenario,
  type WorkspacesScenario,
} from "../../../verification/account-workspaces-fixtures";
import { en } from "../account-workspaces.copy";
import {
  AccountWorkspacesSurface,
  type AccountWorkspacesSurfaceProps,
} from "../account-workspaces-surface";

const LOCALES = ["vi", "en"] as const;

function renderWorkspaces(scenario: WorkspacesScenario) {
  const props: AccountWorkspacesSurfaceProps = {
    ...scenario,
    onSetDefault: vi.fn(),
    onLeave: vi.fn(),
    onRetry: vi.fn(),
  };
  const view = renderPureUi(<AccountWorkspacesSurface {...props} />);
  return { props, view };
}

describe("AccountWorkspacesSurface", () => {
  it("FUI[account.workspaces:render] lists each workspace with its role and what the member sees", () => {
    for (const locale of LOCALES) {
      const copy = workspacesCopyFixture(locale);
      const { view } = renderWorkspaces(workspacesDefaultScenario(locale));
      expect(
        screen.getByRole("heading", { level: 2, name: copy.title }),
      ).toBeTruthy();
      expect(screen.getAllByRole("heading", { level: 3 })).toHaveLength(3);
      expect(screen.getByText(copy.defaultTag)).toBeTruthy();
      const body = document.body.textContent ?? "";
      expect(body).toContain(copy.roles.admin);
      expect(body).toContain(copy.roles.member);
      expect(body).toContain(copy.roles.guest);
      expect(body).toContain("86");
      expect(body).toContain("5");
      expect(
        screen.getByRole("link", { name: copy.create }).getAttribute("href"),
      ).toBe("/onboarding");
      view.unmount();
    }
  });

  it("FUI[account.workspaces:default] makes another workspace the default and never offers it on the default one", () => {
    const copy = workspacesCopyFixture("en");
    const { props, view } = renderWorkspaces(workspacesDefaultScenario("en"));
    expect(
      screen.queryByRole("button", {
        name: copy.makeDefaultLabel.replace("{{name}}", "Minh An Group"),
      }),
    ).toBeNull();
    fireEvent.click(
      screen.getByRole("button", {
        name: copy.makeDefaultLabel.replace("{{name}}", "Minh An Đà Nẵng"),
      }),
    );
    expect(props.onSetDefault).toHaveBeenCalledWith("workspace-danang");
    view.unmount();
  });

  it("FUI[account.workspaces:leave] asks first and leaves only after the confirmation", () => {
    for (const locale of LOCALES) {
      const copy = workspacesCopyFixture(locale);
      const { props, view } = renderWorkspaces(
        workspacesDefaultScenario(locale),
      );
      fireEvent.click(
        screen.getByRole("button", {
          name: copy.leaveLabel.replace("{{name}}", "Minh An Đà Nẵng"),
        }),
      );
      expect(props.onLeave).not.toHaveBeenCalled();
      const dialog = screen.getByRole("alertdialog");
      expect(dialog.textContent).toContain(
        copy.confirm.title.replace("{{name}}", "Minh An Đà Nẵng"),
      );
      fireEvent.click(
        within(dialog).getByRole("button", { name: copy.confirm.action }),
      );
      expect(props.onLeave).toHaveBeenCalledWith("workspace-danang");
      view.unmount();
    }
  });

  it("FUI[account.workspaces:sole-owner] blocks leaving the workspace the member alone owns", () => {
    const copy = workspacesCopyFixture("en");
    const { view } = renderWorkspaces(workspacesSoleOwnerScenario("en"));
    const leave = screen.getByRole("button", {
      name: copy.leaveLabel.replace("{{name}}", "Minh An Group"),
    }) as HTMLButtonElement;
    expect(leave.disabled).toBe(true);
    expect(
      screen.getByText(
        copy.noteSoleOwner.replace("{{workspace}}", "Minh An Group"),
      ),
    ).toBeTruthy();
    view.unmount();

    const refused = renderWorkspaces({
      ...workspacesSoleOwnerScenario("en"),
      actionError: "last-owner",
    });
    expect(screen.getByRole("alert").textContent).toContain(
      copy.lastOwnerError,
    );
    refused.view.unmount();
  });

  it("FUI[account.workspaces:guest] shows a guest only the role, with no people or department count and no invented notes", () => {
    const copy = workspacesCopyFixture("vi");
    const { view } = renderWorkspaces(workspacesDefaultScenario("vi"));
    const guest = screen
      .getByRole("heading", { level: 3, name: /Tân Lộc Logistics/ })
      .closest("section");
    expect(guest?.textContent).toContain(copy.roles.guest);
    expect(guest?.textContent).not.toContain("người");
    expect(guest?.textContent).not.toContain("phòng ban");
    // The producer sends no unread count, guest board or other owner.
    const body = document.body.textContent ?? "";
    expect(body).not.toContain("chưa đọc");
    expect(body).not.toContain("Trịnh Quốc Hưng");
    view.unmount();
  });

  it("FUI[account.workspaces:loading] shows a named loading state", () => {
    const copy = workspacesCopyFixture("en");
    const { view } = renderWorkspaces(workspacesLoadingScenario("en"));
    expect(screen.getByRole("status", { name: copy.loading })).toBeTruthy();
    view.unmount();
  });

  it("FUI[account.workspaces:error] offers a retry when the list cannot load", () => {
    const copy = workspacesCopyFixture("en");
    const { props, view } = renderWorkspaces(workspacesErrorScenario("en"));
    const alert = screen.getByRole("alert");
    expect(alert.textContent).toContain(copy.loadError);
    fireEvent.click(within(alert).getByRole("button", { name: copy.retry }));
    expect(props.onRetry).toHaveBeenCalledTimes(1);
    view.unmount();
  });

  it("FUI[account.workspaces:edge] keeps a very long name and a failed action readable", () => {
    const copy = workspacesCopyFixture("vi");
    const { view } = renderWorkspaces(workspacesEdgeDataScenario());
    expect(document.body.textContent).toContain("Minh An Việt Nam");
    expect(screen.getByRole("alert").textContent).toContain(copy.actionError);
    view.unmount();
  });

  it("FUI[account.workspaces:copy] shows the notice and leaves no English literal in the Vietnamese page", () => {
    const { view } = renderWorkspaces(workspacesNoticeScenario("vi"));
    expect(screen.getByRole("status").textContent).toContain(
      "sẽ là nơi mở mặc định",
    );
    const text = document.body.textContent ?? "";
    for (const literal of [
      en.title,
      en.create,
      en.defaultTag,
      en.open,
      en.makeDefault,
      en.leave,
    ])
      expect(text).not.toContain(literal);
    view.unmount();
  });
});
