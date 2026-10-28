import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, within } from "@testing-library/react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuTrigger,
} from "@notrelix/ui-web";
import { en, vi as viCopy } from "../item-actions.copy";
import { DuplicateItemDialog } from "../duplicate-item-dialog";
import { ItemActionsMenuItems } from "../item-actions-menu";
import { MoveBoardDialog, type MovePreviewView } from "../move-board-dialog";
import { RemindDialog } from "../remind-dialog";

const noop = () => undefined;

function renderMenu(
  props: Partial<Parameters<typeof ItemActionsMenuItems>[0]> = {},
  copy = en,
) {
  const on = {
    copyLink: vi.fn(),
    openFull: vi.fn(),
    complete: vi.fn(),
    reopen: vi.fn(),
    remind: vi.fn(),
    duplicate: vi.fn(),
    moveBoard: vi.fn(),
    archive: vi.fn(),
    unarchive: vi.fn(),
    remove: vi.fn(),
  };
  render(
    <DropdownMenu open>
      <DropdownMenuTrigger>open</DropdownMenuTrigger>
      <DropdownMenuContent>
        <ItemActionsMenuItems
          copy={copy}
          item={{
            itemId: "i1",
            itemKey: "ERP-142",
            archived: false,
            completed: false,
          }}
          can={{ edit: true, create: true, move: true, remove: true }}
          on={on}
          {...props}
        />
      </DropdownMenuContent>
    </DropdownMenu>,
  );
  return on;
}

describe("ItemActionsMenuItems", () => {
  it("FUI[wm.item-actions:menu] lists every action in one order and runs the one picked", () => {
    const on = renderMenu();
    const names = screen
      .getAllByRole("menuitem")
      .map((item) => item.textContent);
    expect(names.map((name) => name?.replace(/ERP-142/, ""))).toEqual([
      en.copyLink,
      en.openFull,
      en.complete,
      en.remind,
      en.duplicate,
      en.moveBoard,
      en.archive,
      en.delete,
    ]);
    fireEvent.click(screen.getByRole("menuitem", { name: en.duplicate }));
    expect(on.duplicate).toHaveBeenCalledWith("i1");
  });

  it("FUI[wm.item-actions:menu-viewer] keeps a locked entry visible, disabled, with its reason read through aria-describedby", () => {
    renderMenu({
      can: { edit: false, create: false, move: false, remove: false },
    });
    const duplicate = screen.getByRole("menuitem", {
      name: new RegExp(en.duplicate),
    });
    expect(duplicate.getAttribute("aria-disabled")).toBe("true");
    const reason = document.getElementById(
      duplicate.getAttribute("aria-describedby") ?? "",
    );
    expect(reason?.textContent).toBe(en.reasonCreate);
    // a viewer may still copy the link and remind
    expect(
      screen
        .getByRole("menuitem", { name: new RegExp(en.remind) })
        .getAttribute("aria-disabled"),
    ).toBeNull();
  });

  it("FUI[wm.item-actions:menu-states] offers Reopen for a completed item and Unarchive for an archived one, and says why the rest is locked", () => {
    renderMenu({
      item: { itemId: "i1", archived: true, completed: false },
      can: { edit: false, create: false, move: false, remove: false },
    });
    expect(screen.getByRole("menuitem", { name: en.unarchive })).toBeTruthy();
    expect(screen.queryByRole("menuitem", { name: en.archive })).toBeNull();
    expect(
      screen.getByRole("menuitem", { name: new RegExp(en.moveBoard) })
        .textContent,
    ).toContain(en.reasonUnarchiveFirst);
  });

  it("FUI[wm.item-actions:menu-vi] speaks Vietnamese", () => {
    renderMenu({}, viCopy);
    expect(
      screen.getByRole("menuitem", { name: viCopy.moveBoard }),
    ).toBeTruthy();
    expect(viCopy.moveBoard).toBe("Chuyển board…");
  });
});

const boards = [
  { id: "b1", title: "Kế hoạch Q4" },
  { id: "b2", title: "Hỗ trợ khách hàng" },
];

const losing: MovePreviewView = {
  boardTitle: "Kế hoạch Q4",
  group: { title: "Đang chạy", matched: true },
  status: { name: "Đang làm", matched: true },
  losses: ["property Budget", "label Data", "assignee Nam"],
};

function renderMove(
  props: Partial<Parameters<typeof MoveBoardDialog>[0]> = {},
) {
  const onConfirm = vi.fn();
  render(
    <MoveBoardDialog
      copy={en}
      title="Fix login"
      itemKey="ERP-142"
      boards={boards}
      open
      preview={null}
      status="idle"
      onPick={noop}
      onCancel={noop}
      onConfirm={onConfirm}
      {...props}
    />,
  );
  return onConfirm;
}

describe("MoveBoardDialog", () => {
  it("FUI[wm.item-actions:move] asks for a board first and says what carries over before it moves", () => {
    const onPick = vi.fn();
    const onConfirm = renderMove({ onPick, preview: losing });
    const move = screen.getByRole("button", { name: /Move and drop 3/ });
    expect((move as HTMLButtonElement).disabled).toBe(true);
    fireEvent.click(screen.getByRole("option", { name: "Kế hoạch Q4" }));
    expect(onPick).toHaveBeenCalledWith("b1");
    expect(screen.getByText("Loses 3 items")).toBeTruthy();
    expect(screen.getByText(/Gets the other board's next key/)).toBeTruthy();
    expect(onConfirm).not.toHaveBeenCalled();
  });

  it("FUI[wm.item-actions:move-ack] needs the tick that names the losses before the button works", () => {
    const onConfirm = renderMove({ preview: losing });
    fireEvent.click(screen.getByRole("option", { name: "Kế hoạch Q4" }));
    const move = screen.getByRole("button", { name: /Move and drop 3/ });
    expect((move as HTMLButtonElement).disabled).toBe(true);
    fireEvent.click(
      screen.getByRole("checkbox", {
        name: /property Budget, label Data, assignee Nam/,
      }),
    );
    expect((move as HTMLButtonElement).disabled).toBe(false);
    fireEvent.click(move);
    expect(onConfirm).toHaveBeenCalledWith("b1");
  });

  it("FUI[wm.item-actions:move-clean] moves without a tick when nothing is lost", () => {
    const onConfirm = renderMove({ preview: { ...losing, losses: [] } });
    fireEvent.click(screen.getByRole("option", { name: "Kế hoạch Q4" }));
    fireEvent.click(screen.getByRole("button", { name: en.moveConfirm }));
    expect(onConfirm).toHaveBeenCalledWith("b1");
  });

  it("FUI[wm.item-actions:move-refused] shows a refusal with its way out and no confirm for an archived item", () => {
    const onUnarchive = vi.fn();
    renderMove({ status: "archived", onUnarchive });
    expect(screen.getByRole("alert").textContent).toContain(
      en.moveRefusedArchived,
    );
    fireEvent.click(screen.getByRole("button", { name: en.unarchive }));
    expect(onUnarchive).toHaveBeenCalled();
    expect(screen.queryByRole("button", { name: en.moveConfirm })).toBeNull();
  });

  it("FUI[wm.item-actions:move-dependencies] names the dependency refusal and offers to open the blocking item", () => {
    const onOpenBlocking = vi.fn();
    renderMove({ status: "deps", blockingKey: "ERP-140", onOpenBlocking });
    expect(screen.getByRole("alert").textContent).toContain(en.moveRefusedDeps);
    fireEvent.click(screen.getByRole("button", { name: "Open ERP-140" }));
    expect(onOpenBlocking).toHaveBeenCalled();
  });

  it("FUI[wm.item-actions:move-busy] offers no Cancel once the request is sent", () => {
    renderMove({ status: "busy", preview: losing });
    expect(screen.queryByRole("button", { name: en.cancel })).toBeNull();
    expect(
      (screen.getByRole("button", { name: en.moveBusy }) as HTMLButtonElement)
        .disabled,
    ).toBe(true);
  });

  it("FUI[wm.item-actions:move-empty] says so when no board accepts items", () => {
    renderMove({ boards: [] });
    expect(screen.getByText(en.moveNoBoards)).toBeTruthy();
  });
});

describe("DuplicateItemDialog", () => {
  const renderDup = (
    props: Partial<Parameters<typeof DuplicateItemDialog>[0]> = {},
  ) => {
    const onConfirm = vi.fn();
    render(
      <DuplicateItemDialog
        copy={en}
        defaultName="Fix login (copy)"
        open
        busy={false}
        failed={false}
        onCancel={noop}
        onConfirm={onConfirm}
        {...props}
      />,
    );
    return onConfirm;
  };

  it("FUI[wm.item-actions:duplicate] ticks every copyable part by default and sends the choices", () => {
    const onConfirm = renderDup();
    fireEvent.click(screen.getByRole("checkbox", { name: en.dupLabels }));
    fireEvent.click(screen.getByRole("button", { name: en.dupConfirm }));
    expect(onConfirm).toHaveBeenCalledWith("Fix login (copy)", {
      includeFields: true,
      includeChecklists: true,
      includeSubItems: true,
      includeLabels: false,
      includeAssignees: true,
    });
  });

  it("FUI[wm.item-actions:duplicate-locked] draws files, comments and history as locked rows with the reason", () => {
    renderDup();
    for (const [label, reason] of [
      [en.dupFiles, en.dupFilesLocked],
      [en.dupComments, en.dupNeverCopied],
      [en.dupHistory, en.dupNeverCopied],
    ]) {
      const row = screen.getByRole("checkbox", { name: `${label}. ${reason}` });
      expect(row.getAttribute("aria-disabled")).toBe("true");
      expect(within(row).getByText(reason as string)).toBeTruthy();
    }
  });

  it("FUI[wm.item-actions:duplicate-long] counts the name and blocks one over 200 characters", () => {
    renderDup({ defaultName: "x".repeat(212) });
    expect(screen.getByText("212/200")).toBeTruthy();
    expect(screen.getByRole("alert").textContent).toBe(en.dupNameTooLong);
    expect(
      (screen.getByRole("button", { name: en.dupConfirm }) as HTMLButtonElement)
        .disabled,
    ).toBe(true);
  });

  it("FUI[wm.item-actions:duplicate-failed] says the original is unchanged and offers Try again", () => {
    renderDup({ failed: true });
    expect(screen.getByRole("alert").textContent).toContain(en.dupFailed);
    expect(screen.getByRole("button", { name: en.retry })).toBeTruthy();
  });
});

describe("RemindDialog", () => {
  const renderRemind = (
    props: Partial<Parameters<typeof RemindDialog>[0]> = {},
  ) => {
    const onConfirm = vi.fn();
    render(
      <RemindDialog
        copy={en}
        itemKey="ERP-142"
        recipients={["Ha", "Minh"]}
        state="confirm"
        nextRemindAt={null}
        open
        onCancel={noop}
        onConfirm={onConfirm}
        {...props}
      />,
    );
    return onConfirm;
  };

  it("FUI[wm.item-actions:remind] names who will get the reminder and sends on confirm", () => {
    const onConfirm = renderRemind();
    expect(screen.getByText(/Ha, Minh will get a notice/)).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: en.remindSend }));
    expect(onConfirm).toHaveBeenCalledTimes(1);
  });

  it("FUI[wm.item-actions:remind-limit] says when the next reminder is possible when the server refuses", () => {
    renderRemind({ state: "tooSoon", nextRemindAt: "13/10 14:30" });
    expect(screen.getByRole("alert").textContent).toContain("13/10 14:30");
    expect(screen.queryByRole("button", { name: en.remindSend })).toBeNull();
  });

  it("FUI[wm.item-actions:remind-nobody] offers to assign someone when nobody else is in charge", () => {
    const onAssign = vi.fn();
    renderRemind({ recipients: [], state: "nobody", onAssign });
    fireEvent.click(screen.getByRole("button", { name: en.remindAssign }));
    expect(onAssign).toHaveBeenCalled();
  });

  it("FUI[wm.item-actions:remind-error] keeps the dialog open with Try again after a failure", () => {
    renderRemind({ state: "error" });
    expect(screen.getByRole("alert").textContent).toContain(en.remindFailed);
    expect(screen.getByRole("button", { name: en.retry })).toBeTruthy();
  });
});
