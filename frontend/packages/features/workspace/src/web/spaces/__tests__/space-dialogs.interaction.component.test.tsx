import { beforeAll, describe, expect, it, vi } from "vitest";
import {
  act,
  fireEvent,
  renderPureUi,
  screen,
  within,
} from "@notrelix/testing";

import {
  createSpaceChildScenario,
  createSpaceDefaultScenario,
  createSpaceErrorScenario,
  moveBoardScenario,
  moveHighDensityScenario,
  movePageScenario,
  moveSpaceScenario,
  spacesCopyFixture,
} from "../../../verification/workspace-spaces-fixtures";
import {
  CreateSpaceDialog,
  type CreateSpaceDialogProps,
} from "../create-space-dialog";
import {
  MoveToSpaceDialog,
  type MoveToSpaceDialogProps,
} from "../move-to-space-dialog";

beforeAll(() => {
  Element.prototype.scrollIntoView = () => undefined;
  Element.prototype.hasPointerCapture = () => false;
  Element.prototype.releasePointerCapture = () => undefined;
});

const copy = spacesCopyFixture();

function renderCreate(
  scenario = createSpaceDefaultScenario(),
  overrides: Partial<CreateSpaceDialogProps> = {},
) {
  const props: CreateSpaceDialogProps = {
    ...scenario,
    onOpenChange: vi.fn(),
    onSubmit: vi.fn(),
    ...overrides,
  };
  renderPureUi(<CreateSpaceDialog {...props} />);
  return props;
}

function renderMove(
  scenario = moveSpaceScenario(),
  overrides: Partial<MoveToSpaceDialogProps> = {},
) {
  const props: MoveToSpaceDialogProps = {
    ...scenario,
    onOpenChange: vi.fn(),
    onSubmit: vi.fn(),
    ...overrides,
  };
  renderPureUi(<MoveToSpaceDialog {...props} />);
  return props;
}

describe("CreateSpaceDialog", () => {
  it("FUI[workspace.create-space:submit] creates a Department Private by default (WSP-023)", () => {
    const props = renderCreate();
    const dialog = screen.getByRole("dialog", { name: copy.create.title });
    expect(
      (
        within(dialog).getByRole("radio", {
          name: /Private/,
        }) as HTMLButtonElement
      ).getAttribute("aria-checked"),
    ).toBe("true");
    fireEvent.change(within(dialog).getByLabelText(copy.create.nameLabel), {
      target: { value: "  Accounting " },
    });
    fireEvent.click(
      within(dialog).getByRole("button", { name: copy.create.submit }),
    );
    expect(props.onSubmit).toHaveBeenCalledWith({
      name: "Accounting",
      type: "Department",
      visibility: "Private",
    });
  });

  it("FUI[workspace.create-space:defaults] follows the type's default visibility until the member picks one", () => {
    const props = renderCreate(createSpaceChildScenario());
    const dialog = screen.getByRole("dialog", { name: "New Space in IT" });
    fireEvent.keyDown(
      within(dialog).getByRole("combobox", { name: copy.create.typeLabel }),
      { key: "Enter" },
    );
    fireEvent.click(screen.getByRole("option", { name: copy.types.Folder }));
    expect(
      within(dialog)
        .getByRole("radio", { name: /Open/ })
        .getAttribute("aria-checked"),
    ).toBe("true");
    fireEvent.click(within(dialog).getByRole("radio", { name: /Private/ }));
    fireEvent.change(within(dialog).getByLabelText(copy.create.nameLabel), {
      target: { value: "Platform" },
    });
    fireEvent.click(
      within(dialog).getByRole("button", { name: copy.create.submit }),
    );
    expect(props.onSubmit).toHaveBeenCalledWith({
      name: "Platform",
      type: "Folder",
      visibility: "Private",
    });
  });

  it("FUI[workspace.create-space:validation] requires a name and shows the producer's message", () => {
    const props = renderCreate(createSpaceErrorScenario());
    const dialog = screen.getByRole("dialog");
    expect(
      within(dialog).getByText(
        "A space with this name already exists under the same parent.",
      ),
    ).toBeTruthy();
    fireEvent.click(
      within(dialog).getByRole("button", { name: copy.create.submit }),
    );
    expect(within(dialog).getByText(copy.create.nameRequired)).toBeTruthy();
    expect(props.onSubmit).not.toHaveBeenCalled();
  });
});

describe("MoveToSpaceDialog", () => {
  it("FUI[workspace.move-to-space:targets] never offers a Space inside itself or its current place", () => {
    renderMove();
    const dialog = screen.getByRole("dialog", { name: "Move Platform" });
    const radio = (name: RegExp) =>
      within(dialog).getByRole("radio", { name }) as HTMLButtonElement;
    // Its own subtree and the current parent are disabled.
    expect(radio(/^Platform/).disabled).toBe(true);
    expect(radio(/^Infrastructure/).disabled).toBe(true);
    expect(radio(/^IT/).disabled).toBe(true);
    // A Space the member only edits (Accounting: Editor) cannot take a Space.
    expect(radio(/^Accounting/).disabled).toBe(true);
    expect(radio(/^Security/).disabled).toBe(false);
    expect(radio(/Top level of Product/).disabled).toBe(false);
    // Archived Spaces are not offered.
    expect(
      within(dialog).queryByRole("radio", { name: /2025 campaigns/ }),
    ).toBeNull();
    expect(
      (
        within(dialog).getByRole("button", {
          name: copy.move.submit,
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(true);
  });

  it("FUI[workspace.move-to-space:submit] submits the picked destination", () => {
    const props = renderMove();
    const dialog = screen.getByRole("dialog");
    fireEvent.click(within(dialog).getByRole("radio", { name: /^Security/ }));
    fireEvent.click(
      within(dialog).getByRole("button", { name: copy.move.submit }),
    );
    expect(props.onSubmit).toHaveBeenCalledWith("sp-it-security");
  });

  it("FUI[workspace.move-to-space:board] moves a Board into any Space the member can add Boards to, or out", () => {
    const props = renderMove(moveBoardScenario());
    const dialog = screen.getByRole("dialog", {
      name: "Move IT roadmap to a Space",
    });
    expect(
      (
        within(dialog).getByRole("radio", {
          name: /^Accounting/,
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(false);
    expect(
      (
        within(dialog).getByRole("radio", {
          name: /^Marketing/,
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(true);
    fireEvent.click(
      within(dialog).getByRole("radio", { name: copy.move.rootBoard }),
    );
    fireEvent.click(
      within(dialog).getByRole("button", { name: copy.move.submit }),
    );
    expect(props.onSubmit).toHaveBeenCalledWith(null);
  });

  it("FUI[workspace.move-to-space:page] moves a root Doc into any Space the member can add Docs to, or out", () => {
    const props = renderMove(movePageScenario());
    const dialog = screen.getByRole("dialog", {
      name: "Move Onboarding checklist to a Space",
    });
    expect(within(dialog).getByText(copy.move.pageDescription)).toBeTruthy();
    const radio = (name: RegExp | string) =>
      within(dialog).getByRole("radio", { name }) as HTMLButtonElement;
    expect(radio(/^IT/).disabled).toBe(true);
    expect(radio(/^Accounting/).disabled).toBe(false);
    expect(radio(/^Marketing/).disabled).toBe(true);
    expect(radio(copy.move.rootBoard).disabled).toBe(false);
    fireEvent.click(radio(/^Accounting/));
    fireEvent.click(
      within(dialog).getByRole("button", { name: copy.move.submit }),
    );
    expect(props.onSubmit).toHaveBeenCalledWith("sp-accounting");
  });

  it("FUI[workspace.move-to-space:search] filters the tree by name, keeping ancestors", () => {
    renderMove();
    const dialog = screen.getByRole("dialog");
    fireEvent.change(within(dialog).getByLabelText(copy.move.searchLabel), {
      target: { value: "payr" },
    });
    expect(
      within(dialog).getByRole("radio", { name: /^Payroll/ }),
    ).toBeTruthy();
    expect(
      within(dialog).getByRole("radio", { name: /^Accounting/ }),
    ).toBeTruthy();
    expect(
      within(dialog).queryByRole("radio", { name: /^Security/ }),
    ).toBeNull();
  });

  // Heavy interaction sequence: many role queries in jsdom exceed the 5 s default on a loaded runner.
  it("FUI[workspace.move-to-space:windowing] windows 525 Spaces, keeps the selection and reaches the last one", () => {
    const props = renderMove(moveHighDensityScenario(), {
      windowThreshold: 150,
    });
    const dialog = screen.getByRole("dialog");
    const region = within(dialog).getByTestId("move-to-space-window");
    expect(within(dialog).getAllByRole("radio").length).toBeLessThan(80);
    expect(
      Array.from(region.querySelectorAll("[aria-hidden='true']")).some(
        (spacer) => spacer.getAttribute("role") === "none",
      ),
    ).toBe(true);

    // End reaches the last Space even though it is not rendered yet.
    const first = within(dialog).getByRole("radio", {
      name: /^Department 01/,
    });
    act(() => first.focus());
    fireEvent.keyDown(first, { key: "End" });
    const last = within(dialog).getByRole("radio", { name: /^Team 25\.20/ });
    expect(document.activeElement).toBe(last);
    expect(last.getAttribute("aria-checked")).toBe("true");
    expect(within(dialog).getAllByRole("radio").length).toBeLessThan(80);

    // The selection survives scrolling away from it.
    region.scrollTop = 0;
    fireEvent.scroll(region);
    expect(
      within(dialog).getByRole("radio", { name: /^Team 25\.20/ }),
    ).toBeTruthy();
    expect(
      (
        within(dialog).getByRole("radio", {
          name: /^Team 25\.20/,
        }) as HTMLButtonElement
      ).getAttribute("aria-checked"),
    ).toBe("true");
    expect(within(dialog).getAllByRole("radio").length).toBeLessThan(80);

    fireEvent.click(
      within(dialog).getByRole("button", { name: copy.move.submit }),
    );
    expect(props.onSubmit).toHaveBeenCalledWith("sp-dept-24-team-19");
  }, 20_000);

  it("windowed picker: arrow keys at the window edge scroll the next row into view", () => {
    renderMove(moveHighDensityScenario(), { windowThreshold: 150 });
    const dialog = screen.getByRole("dialog");
    const region = within(dialog).getByTestId("move-to-space-window");
    const rendered = within(dialog).getAllByRole("radio");
    const edge = rendered[rendered.length - 1]!;
    act(() => edge.focus());
    const before = within(dialog).queryAllByRole("radio").length;
    fireEvent.keyDown(edge, { key: "ArrowDown" });
    expect(region.scrollTop).toBeGreaterThan(0);
    expect(document.activeElement).not.toBe(edge);
    expect(document.activeElement?.getAttribute("role")).toBe("radio");
    expect(within(dialog).getAllByRole("radio").length).toBeLessThan(
      before + 10,
    );
  });

  it("windowed picker: a search narrows to a plain, non-windowed list", () => {
    renderMove(moveHighDensityScenario(), { windowThreshold: 150 });
    const dialog = screen.getByRole("dialog");
    fireEvent.change(within(dialog).getByLabelText(copy.move.searchLabel), {
      target: { value: "Team 07.0" },
    });
    expect(within(dialog).queryByTestId("move-to-space-window")).toBeNull();
    expect(
      within(dialog).getByRole("radio", { name: /^Team 07\.01/ }),
    ).toBeTruthy();
    expect(
      within(dialog).queryByRole("radio", { name: /^Team 25\.20/ }),
    ).toBeNull();
  });
});
