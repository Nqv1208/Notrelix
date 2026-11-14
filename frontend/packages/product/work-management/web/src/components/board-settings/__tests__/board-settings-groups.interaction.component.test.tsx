import { describe, expect, it, vi } from "vitest";
import {
  act,
  fireEvent,
  renderPureUi,
  screen,
  waitFor,
  within,
} from "@notrelix/testing";
import type { BoardSettingsDto } from "@notrelix/work-management-core";
import { BoardSettingsSurface } from "../board-settings-surface";
import { vi as copy } from "../board-settings.copy";
import { fillCopy } from "../board-settings-format";
import type {
  BoardSettingsActions,
  BoardSettingsSurfaceProps,
  BoardSettingsWriteResult,
} from "../board-settings-types";
import {
  ERP_SETTINGS_TIME_ZONE,
  erpBoardSettings,
  erpSettingsSpace,
  noopSettingsActions,
} from "../../../verification/board-settings-fixtures";

/**
 * The groups section of board settings (UC-WM-E01, E02, E03, F02): the add panel and its pending and failed
 * states, a rename to the same name sending nothing, the pending rename, archive and unarchive, the group
 * removed by someone else while its panel is open, and the order a move sends.
 */
type Spied = { [K in keyof BoardSettingsActions]: ReturnType<typeof vi.fn> };

function spyActions(): Spied {
  return Object.fromEntries(
    Object.entries(noopSettingsActions()).map(([key, fn]) => [key, vi.fn(fn)]),
  ) as never;
}

function props(
  actions: Spied,
  settings: BoardSettingsDto = erpBoardSettings(),
): BoardSettingsSurfaceProps {
  return {
    status: "ready",
    settings,
    copy,
    boardTitle: "Triển khai ERP Q4",
    space: erpSettingsSpace,
    askName: "Lê Hoàng Phúc",
    timeZone: ERP_SETTINGS_TIME_ZONE,
    section: "groups",
    onSelectSection: vi.fn(),
    fieldId: null,
    onSelectField: vi.fn(),
    viewId: null,
    onSelectView: vi.fn(),
    actions: actions as unknown as BoardSettingsActions,
    onReload: vi.fn(),
    onBack: vi.fn(),
    boardHref: "/workspaces/w/boards/b",
    onOpenAccess: vi.fn(),
    onLeave: vi.fn(),
  };
}

function renderGroups(actions = spyActions(), settings?: BoardSettingsDto) {
  const view = renderPureUi(
    <BoardSettingsSurface {...props(actions, settings)} />,
  );
  const rerender = (next: BoardSettingsDto) =>
    view.rerender(<BoardSettingsSurface {...props(actions, next)} />);
  return { actions, rerender };
}

/** A write held open until the test answers it. */
function held() {
  let answer!: (result: BoardSettingsWriteResult) => void;
  const promise = new Promise<BoardSettingsWriteResult>((resolve) => {
    answer = resolve;
  });
  return { promise, answer };
}

const groupsList = () => screen.getAllByRole("list")[0]!;

async function openMenuItem(groupName: string, item: string) {
  fireEvent.keyDown(
    screen.getByRole("button", {
      name: fillCopy(copy.groups.more, { name: groupName }),
    }),
    { key: "Enter" },
  );
  fireEvent.click(await screen.findByRole("menuitem", { name: item }));
}

describe("board settings groups: create (UC-WM-E01)", () => {
  it("opens the add panel at the end of the list, shows it pending, then closes it", async () => {
    const actions = spyActions();
    const write = held();
    actions.createGroup!.mockReturnValueOnce(write.promise);
    renderGroups(actions);
    fireEvent.click(screen.getByRole("button", { name: copy.groups.add }));

    const panel = screen.getByRole("listitem", { name: copy.groups.addTitle });
    const items = within(groupsList()).getAllByRole("listitem");
    // Where the new group lands: after the last group.
    expect(items[items.length - 1]).toBe(panel);
    const input = within(panel).getByRole("textbox", {
      name: copy.groups.addPlaceholder,
    });
    const submit = within(panel).getByRole("button", {
      name: copy.groups.addConfirm,
    });
    expect((submit as HTMLButtonElement).disabled).toBe(true);
    fireEvent.change(input, { target: { value: "  Go-live  " } });
    fireEvent.click(submit);

    await waitFor(() => expect(panel.getAttribute("aria-busy")).toBe("true"));
    const pending = within(panel).getByRole("button", {
      name: copy.groups.adding,
    });
    expect((pending as HTMLButtonElement).disabled).toBe(true);
    expect((input as HTMLInputElement).readOnly).toBe(true);
    expect(
      (
        within(panel).getByRole("button", {
          name: copy.common.cancel,
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(true);
    // A second submit while the first runs sends nothing more.
    fireEvent.submit(panel);
    expect(actions.createGroup).toHaveBeenCalledTimes(1);
    expect(actions.createGroup).toHaveBeenCalledWith("Go-live");

    await act(async () => write.answer({ ok: true }));
    await waitFor(() =>
      expect(
        screen.queryByRole("listitem", { name: copy.groups.addTitle }),
      ).toBeNull(),
    );
  });

  it("keeps the typed name and offers Retry when the add fails", async () => {
    const actions = spyActions();
    actions.createGroup!.mockResolvedValueOnce({ ok: false, reason: "failed" });
    renderGroups(actions);
    fireEvent.click(screen.getByRole("button", { name: copy.groups.add }));
    const panel = screen.getByRole("listitem", { name: copy.groups.addTitle });
    const input = within(panel).getByRole("textbox");
    fireEvent.change(input, { target: { value: "Go-live" } });
    fireEvent.click(
      within(panel).getByRole("button", { name: copy.groups.addConfirm }),
    );

    const alert = await within(panel).findByRole("alert");
    expect(alert.textContent).toContain(copy.feedback.failedKept);
    expect((input as HTMLInputElement).value).toBe("Go-live");
    expect(input.getAttribute("aria-describedby")).toBe(alert.id);
    fireEvent.click(
      within(alert).getByRole("button", { name: copy.feedback.retry }),
    );
    await waitFor(() => expect(actions.createGroup).toHaveBeenCalledTimes(2));
  });
});

describe("board settings groups: rename (UC-WM-E02)", () => {
  const openRename = (name: string) =>
    fireEvent.click(
      screen.getByRole("button", {
        name: fillCopy(copy.groups.rename, { name }),
      }),
    );

  it("sends nothing when the name is saved unchanged, or with only spaces around it", () => {
    const { actions } = renderGroups();
    openRename("Kiểm thử");
    let input = screen.getByRole("textbox", { name: copy.groups.renameField });
    fireEvent.keyDown(input, { key: "Enter" });
    fireEvent.blur(input);
    expect(
      screen.queryByRole("textbox", { name: copy.groups.renameField }),
    ).toBeNull();

    openRename("Kiểm thử");
    input = screen.getByRole("textbox", { name: copy.groups.renameField });
    fireEvent.change(input, { target: { value: "  Kiểm thử " } });
    fireEvent.blur(input);
    expect(
      screen.queryByRole("textbox", { name: copy.groups.renameField }),
    ).toBeNull();
    expect(screen.getByText("Kiểm thử")).toBeTruthy();
    expect(actions.updateGroup).not.toHaveBeenCalled();
  });

  it("shows the rename pending, sends it once, then closes it", async () => {
    const actions = spyActions();
    const write = held();
    actions.updateGroup!.mockReturnValueOnce(write.promise);
    renderGroups(actions);
    openRename("Kiểm thử");
    const input = screen.getByRole("textbox", {
      name: copy.groups.renameField,
    });
    fireEvent.change(input, { target: { value: "QA" } });
    fireEvent.blur(input);

    const status = await screen.findByText(copy.groups.renaming);
    expect(status.getAttribute("role")).toBe("status");
    expect((input as HTMLInputElement).readOnly).toBe(true);
    expect(input.getAttribute("aria-busy")).toBe("true");
    fireEvent.blur(input);
    fireEvent.keyDown(input, { key: "Escape" });
    expect(actions.updateGroup).toHaveBeenCalledTimes(1);
    expect(actions.updateGroup).toHaveBeenCalledWith("g-test", { name: "QA" });
    // Escape while it is saved keeps the field: the write is already on its way.
    expect(
      screen.getByRole("textbox", { name: copy.groups.renameField }),
    ).toBeTruthy();

    await act(async () => write.answer({ ok: true }));
    await waitFor(() =>
      expect(
        screen.queryByRole("textbox", { name: copy.groups.renameField }),
      ).toBeNull(),
    );
    expect(screen.queryByText(copy.groups.renaming)).toBeNull();
  });
});

describe("board settings groups: archive and delete (UC-WM-E03)", () => {
  it("archives a group after the confirm that names its items, and unarchives one", async () => {
    const settings = erpBoardSettings();
    const { actions } = renderGroups(spyActions(), {
      ...settings,
      archivedGroups: [
        { id: "g-old", name: "Giai đoạn cũ", color: "#7A8391", itemCount: 4 },
      ],
    });
    await openMenuItem("Kiểm thử", copy.groups.archive);
    const confirm = screen.getByRole("listitem", {
      name: fillCopy(copy.groups.archiveQuestion, { name: "Kiểm thử" }),
    });
    expect(confirm.textContent).toContain(
      fillCopy(copy.groups.archiveBody, { count: 2 }),
    );
    fireEvent.click(
      within(confirm).getByRole("button", { name: copy.groups.archiveConfirm }),
    );
    await waitFor(() =>
      expect(actions.archiveGroup).toHaveBeenCalledWith("g-test"),
    );

    fireEvent.click(
      screen.getByRole("button", {
        name: fillCopy(copy.groups.unarchiveLabel, { name: "Giai đoạn cũ" }),
      }),
    );
    await waitFor(() =>
      expect(actions.unarchiveGroup).toHaveBeenCalledWith("g-old"),
    );
  });

  it("says so when someone else removes the group while its delete panel is open, and sends nothing", async () => {
    const settings = erpBoardSettings();
    const { actions, rerender } = renderGroups(spyActions(), settings);
    await openMenuItem("Kiểm thử", copy.groups.delete);
    expect(
      screen.getByRole("listitem", {
        name: fillCopy(copy.groups.deleteQuestion, { name: "Kiểm thử" }),
      }),
    ).toBeTruthy();

    // The settings are read again without the group (deleted or archived elsewhere).
    rerender({
      ...settings,
      groups: settings.groups.filter((group) => group.id !== "g-test"),
    });
    const notice = screen
      .getAllByRole("status")
      .find((element) => element.textContent?.includes(copy.groups.goneBody))!;
    expect(notice).toBeTruthy();
    expect(notice.textContent).toContain(
      fillCopy(copy.groups.goneTitle, { name: "Kiểm thử" }),
    );
    expect(notice.textContent).toContain(copy.groups.goneBody);
    expect(
      screen.queryByRole("listitem", {
        name: fillCopy(copy.groups.deleteQuestion, { name: "Kiểm thử" }),
      }),
    ).toBeNull();
    fireEvent.click(
      within(notice).getByRole("button", { name: copy.common.close }),
    );
    expect(screen.queryByText(copy.groups.goneBody)).toBeNull();
    expect(actions.deleteGroup).not.toHaveBeenCalled();
  });

  it("does not call the person's own delete news", async () => {
    const settings = erpBoardSettings();
    const actions = spyActions();
    const write = held();
    actions.deleteGroup!.mockReturnValueOnce(write.promise);
    const { rerender } = renderGroups(actions, settings);
    await openMenuItem("Kiểm thử", copy.groups.delete);
    fireEvent.click(
      screen.getByRole("button", { name: copy.groups.deleteConfirm }),
    );
    // The refetch lands before the write answers, as the container's write does.
    rerender({
      ...settings,
      groups: settings.groups.filter((group) => group.id !== "g-test"),
    });
    expect(screen.queryByText(copy.groups.goneBody)).toBeNull();
    await act(async () => write.answer({ ok: true }));
    expect(screen.queryByText(copy.groups.goneBody)).toBeNull();
    expect(actions.deleteGroup).toHaveBeenCalledWith("g-test", "g-training");
  });

  it("asks for another target when the chosen one is removed while the panel is open", async () => {
    const settings = erpBoardSettings();
    const { actions, rerender } = renderGroups(spyActions(), settings);
    await openMenuItem("Kiểm thử", copy.groups.delete);
    const question = fillCopy(copy.groups.deleteQuestion, { name: "Kiểm thử" });
    let confirm = screen.getByRole("listitem", { name: question });
    expect(within(confirm).getByRole("combobox").textContent).toBe(
      "Đào tạo và vận hành",
    );

    rerender({
      ...settings,
      groups: settings.groups.filter((group) => group.id !== "g-training"),
    });
    confirm = screen.getByRole("listitem", { name: question });
    expect(within(confirm).getByRole("combobox").textContent).toBe(
      copy.groups.choose,
    );
    const submit = within(confirm).getByRole("button", {
      name: copy.groups.deleteConfirm,
    }) as HTMLButtonElement;
    expect(submit.disabled).toBe(true);
    fireEvent.click(submit);
    expect(actions.deleteGroup).not.toHaveBeenCalled();
  });
});

describe("board settings groups: reorder (UC-WM-F02)", () => {
  it("moves a group one place with Alt+Arrow and sends the whole new order once", async () => {
    const { actions } = renderGroups();
    fireEvent.keyDown(
      screen.getByRole("button", {
        name: `${copy.common.dragHandle}: Kiểm thử`,
      }),
      { key: "ArrowUp", altKey: true },
    );
    await waitFor(() => expect(actions.reorderGroups).toHaveBeenCalledTimes(1));
    expect(actions.reorderGroups).toHaveBeenCalledWith([
      "g-prep",
      "g-test",
      "g-data",
      "g-training",
    ]);
    const names = within(groupsList())
      .getAllByRole("listitem")
      .map((row) => row.textContent ?? "")
      .filter(Boolean);
    expect(names[0]).toContain("Chuẩn bị");
    expect(names[1]).toContain("Kiểm thử");
    expect(names[2]).toContain("Dữ liệu");
  });

  it("sends nothing for a move past either end", () => {
    const { actions } = renderGroups();
    fireEvent.keyDown(
      screen.getByRole("button", {
        name: `${copy.common.dragHandle}: Chuẩn bị`,
      }),
      { key: "ArrowUp", altKey: true },
    );
    fireEvent.keyDown(
      screen.getByRole("button", {
        name: `${copy.common.dragHandle}: Đào tạo và vận hành`,
      }),
      { key: "ArrowDown", altKey: true },
    );
    expect(actions.reorderGroups).not.toHaveBeenCalled();
  });
});
