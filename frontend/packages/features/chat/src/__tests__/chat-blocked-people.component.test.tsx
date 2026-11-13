import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, within } from "@notrelix/testing";
import {
  ChatBlockedPeopleDialog,
  type ChatBlockedPeopleDialogProps,
} from "../web/components/chat-blocked-people";
import { en } from "../web/components/chat.copy";

function props(
  overrides: Partial<ChatBlockedPeopleDialogProps> = {},
): ChatBlockedPeopleDialogProps {
  return {
    open: true,
    userIds: ["u-1", "u-2"],
    status: "ready",
    nameOf: (id) => (id === "u-1" ? "An Nguyen" : "Binh Tran"),
    busyId: null,
    copy: en,
    onOpenChange: vi.fn(),
    onUnblock: vi.fn(),
    ...overrides,
  };
}

describe("ChatBlockedPeopleDialog", () => {
  it("lists the blocked people by name with an Unblock button each, named for a screen reader", () => {
    const onUnblock = vi.fn();
    render(<ChatBlockedPeopleDialog {...props({ onUnblock })} />);
    const dialog = screen.getByRole("dialog");
    expect(within(dialog).getByText("An Nguyen")).not.toBeNull();
    fireEvent.click(
      within(dialog).getByRole("button", { name: "Unblock Binh Tran" }),
    );
    expect(onUnblock).toHaveBeenCalledWith("u-2");
  });

  it("says nobody is blocked when the list is empty and ready", () => {
    render(<ChatBlockedPeopleDialog {...props({ userIds: [] })} />);
    expect(screen.getByText("You have not blocked anyone.")).not.toBeNull();
  });

  it("disables the button of the person being unblocked and reports a failed read", () => {
    const { rerender } = render(
      <ChatBlockedPeopleDialog {...props({ busyId: "u-1" })} />,
    );
    expect(
      (
        screen.getByRole("button", {
          name: "Unblock An Nguyen",
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(true);
    rerender(<ChatBlockedPeopleDialog {...props({ status: "error" })} />);
    expect(screen.getByRole("alert").textContent).toContain("could not load");
  });
});
