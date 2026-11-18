import { describe, expect, it, vi } from "vitest";
import {
  act,
  fireEvent,
  renderPureUi,
  screen,
  waitFor,
  within,
} from "@notrelix/testing";
import {
  shareDefaultScenario,
  type BoardShareScenario,
} from "../../../verification/board-share-fixtures";
import { vi as copy } from "../board-share.copy";
import type { BoardShareDialogSurfaceProps } from "../board-share-dialog-surface";
import type { PendingGuestInvitation } from "../board-share-guest";
import { BoardShareSheet } from "../board-share-sheet";

const OWN: PendingGuestInvitation = {
  id: "inv-1",
  email: "khach@doitac-erp.vn",
  issuedByYou: true,
  canManage: true,
  invitedByName: "Nguyễn Thu Hà",
  expiresAt: "2026-11-26T12:00:00Z",
};
const OTHER: PendingGuestInvitation = {
  id: "inv-2",
  email: "ketoan@doitac-erp.vn",
  issuedByYou: false,
  canManage: false,
  invitedByName: "Lê Hoàng Phúc",
  expiresAt: "2026-11-24T12:00:00Z",
};

function renderSheet(
  overrides: Partial<BoardShareDialogSurfaceProps> = {},
  scenario: BoardShareScenario = shareDefaultScenario(),
) {
  const props: BoardShareDialogSurfaceProps = {
    ...scenario,
    onClose: vi.fn(),
    onCopyLink: vi.fn(),
    candidates: [],
    onInvite: vi.fn(),
    onInviteGuest: vi.fn(async () => true),
    ...overrides,
  };
  return { props, view: renderPureUi(<BoardShareSheet {...props} />) };
}

const field = () => screen.getByRole("combobox");
const guestLevelButton = () =>
  screen.getByRole("button", {
    name: new RegExp(copy.sheet.addGuestLevelLabel),
  });
const banner = () =>
  document.querySelector<HTMLElement>('[data-f="bn"]') as HTMLElement;
const guestOption = (email: string) =>
  screen.getByRole("button", {
    name: new RegExp(copy.add.guestOption.replace("{{email}}", email)),
  });

describe("BoardShareSheet guest invitation (phone)", () => {
  it("UC-WM-A19 offers an unknown email as a guest invitation at Viewer, sends it and clears the field", async () => {
    const { props } = renderSheet();
    fireEvent.change(field(), { target: { value: "khach@doitac-erp.vn" } });
    // The level row names the guest and is locked to View.
    expect(screen.getByText(copy.sheet.addGuestLevelLabel)).toBeTruthy();
    const level = guestLevelButton();
    expect(level.textContent).toContain(copy.sheet.levelShort.view);
    expect(screen.getByText(copy.add.guestHint)).toBeTruthy();
    fireEvent.click(guestOption("khach@doitac-erp.vn"));
    expect(props.onInviteGuest).toHaveBeenCalledWith("khach@doitac-erp.vn");
    await waitFor(() => expect((field() as HTMLInputElement).value).toBe(""));
    // A member's name is still added with the level of the row, never as a guest.
    expect(props.onInvite).not.toHaveBeenCalled();
  });

  it("UC-WM-A19 sends the guest invitation on Enter too", async () => {
    const { props } = renderSheet();
    fireEvent.change(field(), { target: { value: "khach@doitac-erp.vn" } });
    fireEvent.keyDown(field(), { key: "Enter" });
    expect(props.onInviteGuest).toHaveBeenCalledWith("khach@doitac-erp.vn");
  });

  it("UC-WM-A19 keeps a guest at Viewer: the level sheet checks Can view and disables the other levels with the reason", () => {
    renderSheet();
    fireEvent.change(field(), { target: { value: "khach@doitac-erp.vn" } });
    fireEvent.click(guestLevelButton());
    const sheet = screen.getByRole("dialog", {
      name: copy.sheet.addGuestLevelLabel,
    });
    expect(
      within(sheet).getByText(
        copy.sheet.guestOf.replace("{{email}}", "khach@doitac-erp.vn"),
      ),
    ).toBeTruthy();
    const levels = within(sheet).getAllByRole("radio");
    expect(levels.map((item) => item.getAttribute("aria-checked"))).toEqual([
      "true",
      "false",
      "false",
      "false",
    ]);
    expect(levels.map((item) => item.getAttribute("aria-disabled"))).toEqual([
      null,
      "true",
      "true",
      "true",
    ]);
    expect(within(sheet).getByText(copy.levelHints.guestNoManage)).toBeTruthy();
    for (const blocked of levels.slice(1)) fireEvent.click(blocked);
    // Nothing above View can be chosen: the sheet stays on the guest's own level.
    expect(levels[0]!.getAttribute("aria-checked")).toBe("true");
  });

  it("UC-WM-A19 says an email that is not an email is wrong, inline, and sends nothing", () => {
    const { props } = renderSheet();
    fireEvent.change(field(), { target: { value: "khach@doitac" } });
    // Typing does not nag; leaving the field does.
    expect(screen.queryByRole("alert")).toBeNull();
    fireEvent.blur(field());
    expect(screen.getByRole("alert").textContent).toBe(copy.sheet.emailInvalid);
    expect(field().getAttribute("aria-invalid")).toBe("true");
    expect(screen.queryByRole("button", { name: /làm khách/ })).toBeNull();
    fireEvent.keyDown(field(), { key: "Enter" });
    expect(props.onInviteGuest).not.toHaveBeenCalled();
    // A correction clears the error and offers the invitation.
    fireEvent.change(field(), { target: { value: "khach@doitac-erp.vn" } });
    expect(screen.queryByRole("alert")).toBeNull();
    expect(guestOption("khach@doitac-erp.vn")).toBeTruthy();
  });

  it("UC-WM-A19 keeps the email in the field when the invitation is refused and shows the notice that names who sent it", async () => {
    const onInviteGuest = vi.fn(async () => false);
    const { view } = renderSheet({ onInviteGuest });
    fireEvent.change(field(), { target: { value: "ketoan@doitac-erp.vn" } });
    fireEvent.click(guestOption("ketoan@doitac-erp.vn"));
    await waitFor(() => expect(onInviteGuest).toHaveBeenCalledOnce());
    expect((field() as HTMLInputElement).value).toBe("ketoan@doitac-erp.vn");
    // The host turns the refusal into the notice (see guestInviteConflict); the sheet shows it as an alert.
    view.rerender(
      <BoardShareSheet
        {...shareDefaultScenario()}
        onClose={vi.fn()}
        onCopyLink={vi.fn()}
        candidates={[]}
        onInvite={vi.fn()}
        onInviteGuest={onInviteGuest}
        pendingInvitations={[OTHER]}
        notice={{
          kind: "refused",
          title: copy.add.inviteFailed,
          body: copy.add.inviteConflict
            .replace("{{name}}", "Lê Hoàng Phúc")
            .replace("{{email}}", "ketoan@doitac-erp.vn"),
        }}
      />,
    );
    const alert = screen.getByRole("alert");
    expect(within(alert).getByText(copy.add.inviteFailed)).toBeTruthy();
    expect(
      within(alert).getByText(
        "Lê Hoàng Phúc đã mời ketoan@doitac-erp.vn rồi. Danh sách lời mời đã được cập nhật.",
      ),
    ).toBeTruthy();
  });

  it("UC-WM-A19 lists the pending invitations: the issuer resends and revokes, another person's row says why it cannot", async () => {
    const onResendInvitation = vi.fn(async () => true);
    const onRevokeInvitation = vi.fn(async () => true);
    renderSheet({
      pendingInvitations: [OWN, OTHER],
      onResendInvitation,
      onRevokeInvitation,
    });
    const section = screen.getByRole("region", {
      name: "Lời mời đang chờ · 2",
    });
    expect(
      within(section).getByText(
        /Người mời Nguyễn Thu Hà · hết hạn 26\/11\/2026/,
      ),
    ).toBeTruthy();
    const resends = within(section).getAllByRole("button", {
      name: copy.pending.resend,
    });
    const revokes = within(section).getAllByRole("button", {
      name: copy.pending.revoke,
    });
    expect(resends[0]!.getAttribute("aria-disabled")).toBeNull();
    expect(resends[1]!.getAttribute("aria-disabled")).toBe("true");
    expect(revokes[1]!.getAttribute("aria-disabled")).toBe("true");
    expect(resends[1]!.getAttribute("aria-describedby")).toBeTruthy();
    expect(within(section).getByText(copy.pending.notIssuer)).toBeTruthy();
    fireEvent.click(resends[1]!);
    fireEvent.click(revokes[1]!);
    expect(onResendInvitation).not.toHaveBeenCalled();
    expect(onRevokeInvitation).not.toHaveBeenCalled();
    fireEvent.click(resends[0]!);
    await waitFor(() =>
      expect(onResendInvitation).toHaveBeenCalledWith("inv-1"),
    );
    await waitFor(() =>
      expect(revokes[0]!.getAttribute("aria-disabled")).toBeNull(),
    );
    fireEvent.click(revokes[0]!);
    expect(onRevokeInvitation).toHaveBeenCalledWith("inv-1");
  });

  it("UC-WM-A20 lets a workspace Owner or Admin resend and revoke another person's invitation on the phone sheet", async () => {
    const onResendInvitation = vi.fn(async () => true);
    const onRevokeInvitation = vi.fn(async () => true);
    renderSheet({
      pendingInvitations: [{ ...OTHER, canManage: true }],
      onResendInvitation,
      onRevokeInvitation,
    });
    const section = screen.getByRole("region", {
      name: "Lời mời đang chờ · 1",
    });
    const resend = within(section).getByRole("button", {
      name: copy.pending.resend,
    });
    const revoke = within(section).getByRole("button", {
      name: copy.pending.revoke,
    });
    expect(resend.getAttribute("aria-disabled")).toBeNull();
    expect(revoke.getAttribute("aria-disabled")).toBeNull();
    expect(within(section).queryByText(copy.pending.notIssuer)).toBeNull();
    fireEvent.click(resend);
    await waitFor(() =>
      expect(onResendInvitation).toHaveBeenCalledWith("inv-2"),
    );
    await waitFor(() =>
      expect(revoke.getAttribute("aria-disabled")).toBeNull(),
    );
    fireEvent.click(revoke);
    expect(onRevokeInvitation).toHaveBeenCalledWith("inv-2");
  });

  it("UC-WM-A20 says who may resend or revoke when the reader is neither the issuer nor a workspace Owner or Admin", () => {
    renderSheet({ pendingInvitations: [OTHER] });
    expect(copy.pending.notIssuer).toContain("Quản trị workspace");
    expect(screen.getByText(copy.pending.notIssuer)).toBeTruthy();
  });

  it("UC-WM-A19 disables the add field for a reader who may not manage the board and says why", () => {
    const scenario = shareDefaultScenario();
    const { props } = renderSheet(
      { pendingInvitations: [OTHER] },
      { ...scenario, view: { ...scenario.view, canManage: false } },
    );
    const input = field() as HTMLInputElement;
    expect(input.disabled).toBe(true);
    expect(screen.getByText(copy.sheet.deniedReason)).toBeTruthy();
    expect(input.getAttribute("aria-describedby")).toBe("board-share-add-why");
    // No level row to open, and the permissions read as text.
    expect(screen.queryByText(copy.sheet.addLevelLabel)).toBeNull();
    expect(screen.queryByRole("button", { name: /Quyền của/ })).toBeNull();
    expect(props.onInviteGuest).not.toHaveBeenCalled();
  });

  it("UC-WM-A19 reads an archived board: a notice, a disabled field and level, no write offered", () => {
    const scenario = shareDefaultScenario();
    renderSheet(
      {
        pendingInvitations: [OWN],
        onResendInvitation: undefined,
        onRevokeInvitation: undefined,
      },
      { ...scenario, view: { ...scenario.view, archived: true } },
    );
    const notice = banner();
    expect(within(notice).getByText(copy.sheet.archivedTitle)).toBeTruthy();
    expect(within(notice).getByText(copy.sheet.archivedBody)).toBeTruthy();
    expect((field() as HTMLInputElement).disabled).toBe(true);
    expect(
      screen
        .getByRole("button", { name: new RegExp(copy.sheet.addLevelLabel) })
        .getAttribute("aria-disabled"),
    ).toBe("true");
    expect(screen.queryByRole("button", { name: /Quyền của/ })).toBeNull();
    expect(
      screen
        .getByRole("button", { name: copy.pending.revoke })
        .getAttribute("aria-disabled"),
    ).toBe("true");
  });

  it("UC-WM-A19 goes offline: a notice and a disabled field that keeps what was typed, then recovers", () => {
    const { props } = renderSheet();
    fireEvent.change(field(), { target: { value: "khach@doitac-erp.vn" } });
    act(() => {
      window.dispatchEvent(new Event("offline"));
    });
    const notice = banner();
    expect(within(notice).getByText(copy.sheet.offlineTitle)).toBeTruthy();
    expect(within(notice).getByText(copy.sheet.offlineBody)).toBeTruthy();
    const input = field() as HTMLInputElement;
    expect(input.disabled).toBe(true);
    expect(input.value).toBe("khach@doitac-erp.vn");
    expect(screen.queryByRole("button", { name: /làm khách/ })).toBeNull();
    expect(props.onInviteGuest).not.toHaveBeenCalled();
    act(() => {
      window.dispatchEvent(new Event("online"));
    });
    expect(screen.queryByText(copy.sheet.offlineTitle)).toBeNull();
    expect((field() as HTMLInputElement).disabled).toBe(false);
  });
});
