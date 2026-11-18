import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import { en, vi as viCopy } from "../board-settings.copy";
import { FieldTypePicker } from "../board-settings-field-picker";

const BOARDS = [
  { id: "b-1", title: "Chiến dịch Tết 2027", locked: false },
  { id: "b-2", title: "Hạ tầng cũ", locked: true },
];

function openBoardStep(boards?: typeof BOARDS) {
  const onPick = vi.fn();
  renderPureUi(<FieldTypePicker copy={en} boards={boards} onPick={onPick} />);
  fireEvent.click(screen.getByRole("button", { name: /^Relation/ }));
  return onPick;
}

describe("the Relation field type asks for its target board", () => {
  it("sends the chosen board in the field settings", () => {
    const onPick = openBoardStep(BOARDS);
    fireEvent.click(
      screen.getByRole("button", { name: "Chiến dịch Tết 2027" }),
    );
    expect(onPick).toHaveBeenCalledWith(
      "relation",
      JSON.stringify({ targetBoardId: "b-1" }),
    );
  });

  it("locks an archived board", () => {
    const onPick = openBoardStep(BOARDS);
    const archived = screen.getByRole("button", { name: /Hạ tầng cũ/ });
    expect((archived as HTMLButtonElement).disabled).toBe(true);
    fireEvent.click(archived);
    expect(onPick).not.toHaveBeenCalled();
  });

  it("shows loading while the boards are read, and none when there is none", () => {
    openBoardStep(undefined);
    expect(screen.getByText(en.picker.relationKind.loading)).toBeTruthy();
  });

  it("says there is no board to link to", () => {
    openBoardStep([]);
    expect(screen.getByText(en.picker.relationKind.none)).toBeTruthy();
  });

  it("has Vietnamese words for every line of the step", () => {
    expect(Object.keys(viCopy.picker.relationKind)).toEqual(
      Object.keys(en.picker.relationKind),
    );
  });
});
