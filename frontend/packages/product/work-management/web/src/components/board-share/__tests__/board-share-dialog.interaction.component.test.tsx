import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";
import {
  shareDefaultScenario,
  shareEmptyScenario,
  shareLevelScenario,
  sharePrivateScenario,
  shareProducedOnlyScenario,
  shareWorkspaceScenario,
  type BoardShareScenario,
} from "../../../verification/board-share-fixtures";
import { vi as copy } from "../board-share.copy";
import {
  BoardShareDialogSurface,
  type BoardShareDialogSurfaceProps,
} from "../board-share-dialog-surface";

function renderShare(
  scenario: BoardShareScenario = shareDefaultScenario(),
  overrides: Partial<BoardShareDialogSurfaceProps> = {},
) {
  const props: BoardShareDialogSurfaceProps = {
    ...scenario,
    onClose: vi.fn(),
    onCopyLink: vi.fn(),
    ...overrides,
  };
  const view = renderPureUi(<BoardShareDialogSurface {...props} />);
  return { props, view };
}

describe("BoardShareDialogSurface", () => {
  it("FUI[wm.board-share:render] names the Board and its department, who opens it through the department and who was added", () => {
    renderShare();
    const dialog = screen.getByRole("dialog", {
      name: "Chia sẻ Triển khai ERP Q4",
    });
    expect(
      within(dialog).getByText("Board trong Phòng Công nghệ"),
    ).toBeTruthy();
    const general = within(dialog).getByRole("radiogroup", {
      name: copy.general.group,
    });
    expect(within(general).getByText("Theo Phòng Công nghệ")).toBeTruthy();
    expect(
      within(general).getByText(/24 người trong phòng vào được/),
    ).toBeTruthy();
    expect(within(general).getByText("Quản trị → Quản lý")).toBeTruthy();
    expect(
      within(dialog).getByText("Quyền riêng trên board này · 5"),
    ).toBeTruthy();
    expect(within(dialog).getByText("Nguyễn Thu Hà")).toBeTruthy();
    expect(within(dialog).getByText("Ngoài công ty")).toBeTruthy();
    expect(within(dialog).queryByText(/hết quyền/)).toBeNull();
    expect(within(dialog).getByText(copy.link.publicOff)).toBeTruthy();
  });

  it("FUI[wm.board-share:inert] draws what has no write behind it aria-disabled with the reason", () => {
    const { props } = renderShare(shareProducedOnlyScenario());
    const invite = screen.getByRole("button", { name: copy.add.invite });
    expect(invite.getAttribute("aria-disabled")).toBe("true");
    expect(invite.getAttribute("title")).toBe(copy.add.unavailable);
    fireEvent.click(invite);
    expect(
      screen
        .getByRole("textbox", { name: copy.add.placeholder })
        .getAttribute("aria-disabled"),
    ).toBe("true");
    const change = screen.getByRole("button", { name: copy.general.change });
    expect(change.getAttribute("aria-disabled")).toBe("true");
    expect(change.getAttribute("title")).toBe(copy.general.changeUnavailable);
    const log = screen.getByRole("button", { name: copy.footer.log });
    expect(log.getAttribute("aria-disabled")).toBe("true");
    // The owners' level reads as text; no entry has a level the page can change.
    expect(screen.queryByRole("menu")).toBeNull();
    expect(props.onClose).not.toHaveBeenCalled();
  });

  it("FUI[wm.board-share:copy-close] copies the link and closes with Done, Close and Escape", () => {
    const { props } = renderShare();
    fireEvent.click(screen.getByRole("button", { name: copy.footer.copy }));
    expect(props.onCopyLink).toHaveBeenCalledTimes(1);
    fireEvent.click(screen.getByRole("button", { name: copy.footer.done }));
    fireEvent.click(screen.getByRole("button", { name: copy.close }));
    expect(props.onClose).toHaveBeenCalledTimes(2);
    fireEvent.keyDown(screen.getByRole("dialog"), { key: "Escape" });
    expect(props.onClose).toHaveBeenCalledTimes(3);
  });

  it("FUI[wm.board-share:level-menu] changes a permission from the menu and keeps a guest on view only", async () => {
    const onChangeLevel = vi.fn();
    const onRemove = vi.fn();
    renderShare(shareLevelScenario(), { onChangeLevel, onRemove });
    const menu = await screen.findByRole("menu", {
      name: "Quyền của Đặng Minh Tuấn",
    });
    const items = within(menu).getAllByRole("menuitemradio");
    expect(items.map((item) => item.getAttribute("aria-checked"))).toEqual([
      "false",
      "false",
      "true",
      "false",
    ]);
    // A guest is view-only: every level above view is disabled, with the reason.
    expect(items.map((item) => item.getAttribute("aria-disabled"))).toEqual([
      null,
      "true",
      "true",
      "true",
    ]);
    expect(within(menu).queryByText("31/01/2027")).toBeNull();
    expect(
      within(menu).getAllByText("Khách chỉ được xem phần được chia sẻ với họ"),
    ).toHaveLength(3);
    for (const blocked of [items[1]!, items[2]!, items[3]!]) {
      fireEvent.click(blocked);
    }
    expect(onChangeLevel).not.toHaveBeenCalled();
    fireEvent.click(items[0]!);
    expect(onChangeLevel).toHaveBeenCalledWith("u-tuan", "view");
    expect(screen.queryByRole("menu")).toBeNull();
  });

  it("FUI[wm.board-share:level-remove] removes an entry from the menu and Escape closes it with focus back", () => {
    const onRemove = vi.fn();
    renderShare(shareDefaultScenario(), { onChangeLevel: vi.fn(), onRemove });
    const trigger = screen.getByRole("button", {
      name: /Quyền của Đặng Minh Tuấn/,
    });
    fireEvent.click(trigger);
    fireEvent.keyDown(screen.getByRole("menu"), { key: "Escape" });
    expect(screen.queryByRole("menu")).toBeNull();
    expect(document.activeElement).toBe(trigger);
    fireEvent.click(trigger);
    fireEvent.click(screen.getByRole("menuitem", { name: /Gỡ quyền/ }));
    expect(onRemove).toHaveBeenCalledWith("u-tuan");
  });

  it("FUI[wm.board-share:private] shows who loses access when switching to Private and goes back", () => {
    const onChangeGeneral = vi.fn();
    renderShare(shareDefaultScenario(), {
      onChangeGeneral,
      impact: shareDefaultScenario().impact,
    });
    fireEvent.click(screen.getByRole("button", { name: copy.general.change }));
    expect(onChangeGeneral).toHaveBeenCalledWith("private");
    expect(
      screen.getByRole("dialog", { name: copy.privateConfirm.title }),
    ).toBeTruthy();
    expect(
      screen
        .getByRole("radio", { name: /Riêng tư/ })
        .getAttribute("aria-checked"),
    ).toBe("true");
    expect(
      screen
        .getByRole("button", { name: copy.privateConfirm.switchOnly })
        .getAttribute("aria-disabled"),
    ).toBe("true");
  });

  it("FUI[wm.board-share:private-confirm] adds the chosen people, then switches", () => {
    const onConfirmPrivate = vi.fn();
    const { props } = renderShare(shareDefaultScenario(), {
      onChangeGeneral: vi.fn(),
      onConfirmPrivate,
      impact: sharePrivateScenario().impact,
    });
    fireEvent.click(screen.getByRole("button", { name: copy.general.change }));
    const boxes = screen.getAllByRole("checkbox");
    expect(boxes).toHaveLength(3);
    fireEvent.click(boxes[2]!);
    fireEvent.click(
      screen.getByRole("button", { name: "Thêm 2 người rồi chuyển" }),
    );
    expect(onConfirmPrivate).toHaveBeenCalledWith(["u-quan", "u-anh"], false);
    expect(props.onClose).toHaveBeenCalled();
  });

  it("FUI[wm.board-share:log] reads the history and goes back", () => {
    renderShare(shareDefaultScenario(), {});
    const log = screen.getByRole("button", { name: copy.footer.log });
    expect(log.getAttribute("aria-disabled")).toBeNull();
    fireEvent.click(log);
    expect(screen.getByRole("heading", { name: copy.log.title })).toBeTruthy();
    expect(screen.getAllByRole("listitem")).toHaveLength(5);
    fireEvent.click(screen.getByRole("button", { name: copy.log.back }));
    expect(screen.getByText("Quyền riêng trên board này · 5")).toBeTruthy();
  });

  it("FUI[wm.board-share:more] shows five rows and the rest after Show more", () => {
    const scenario = shareDefaultScenario();
    const rows = Array.from({ length: 8 }, (_, index) => ({
      ...scenario.view.rows[0]!,
      id: `r${index}`,
      name: `Người ${index}`,
      fixed: false,
    }));
    renderShare({ ...scenario, view: { ...scenario.view, rows } });
    expect(screen.queryByText("Người 5")).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Hiện thêm 3" }));
    expect(screen.getByText("Người 7")).toBeTruthy();
  });

  it("FUI[wm.board-share:variants] words a workspace Board and a Private one without a department", () => {
    const workspace = renderShare(shareWorkspaceScenario());
    expect(screen.getByText("Theo workspace")).toBeTruthy();
    expect(screen.getByText("Board trong Minh An Group")).toBeTruthy();
    workspace.view.unmount();
    renderShare(shareEmptyScenario());
    expect(screen.getByText(copy.general.private)).toBeTruthy();
    expect(screen.getByText(copy.people.empty)).toBeTruthy();
    expect(screen.getByText(copy.link.publicOn)).toBeTruthy();
  });

  it("hides the permission list from a reader the producer discloses no entries to and says how many can open the Board", () => {
    const scenario = shareDefaultScenario();
    renderShare({
      ...scenario,
      view: {
        ...scenario.view,
        rows: [],
        canManage: false,
        hiddenCounts: { people: 24, guests: 2 },
      },
    });
    const dialog = screen.getByRole("dialog");
    expect(
      within(dialog).getByText(
        "24 người và 2 khách mở được board này. Chỉ thành viên và người quản lý board thấy là ai.",
      ),
    ).toBeTruthy();
    expect(within(dialog).queryByText(/Quyền riêng trên board này/)).toBeNull();
    expect(within(dialog).queryByText("Nguyễn Thu Hà")).toBeNull();
  });

  it("reads the sharing list: shows a loading state and no people while it is on its way", () => {
    renderShare(shareDefaultScenario(), { peopleState: "loading" });
    const dialog = screen.getByRole("dialog");
    expect(
      within(dialog).getByRole("status", { name: copy.people.loading }),
    ).toBeTruthy();
    expect(within(dialog).queryByText("Nguyễn Thu Hà")).toBeNull();
    expect(within(dialog).queryByText(/Quyền riêng trên board này/)).toBeNull();
  });

  it("says the sharing read failed and reads it again on Try again", () => {
    const onRetryPeople = vi.fn();
    renderShare(shareDefaultScenario(), {
      peopleState: "error",
      onRetryPeople,
    });
    const dialog = screen.getByRole("dialog");
    expect(within(dialog).getByRole("alert").textContent).toBe(
      copy.people.loadError,
    );
    expect(within(dialog).queryByText("Nguyễn Thu Hà")).toBeNull();
    fireEvent.click(
      within(dialog).getByRole("button", { name: copy.people.retry }),
    );
    expect(onRetryPeople).toHaveBeenCalledOnce();
  });

  it("says no one was added when the sharing read is empty", () => {
    renderShare(shareEmptyScenario());
    expect(
      within(screen.getByRole("dialog")).getByText(copy.people.empty),
    ).toBeTruthy();
  });
});
