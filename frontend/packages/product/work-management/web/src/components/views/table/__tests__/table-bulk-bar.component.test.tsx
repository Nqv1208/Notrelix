import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import { mainTableCopy } from "../main-table.copy";
import {
  TableBulkBar,
  TableBulkFailure,
  type TableBulkBarProps,
} from "../table-bulk-bar";

const copy = mainTableCopy("en");

const button = (name: string | RegExp) =>
  screen.getByRole("button", { name }) as HTMLButtonElement;

function bar(overrides: Partial<TableBulkBarProps>) {
  const props: TableBulkBarProps = {
    copy,
    count: 3,
    statusOptions: [],
    canEdit: true,
    canArchive: true,
    onStatus: vi.fn(),
    onDue: vi.fn(),
    onArchive: vi.fn(),
    onClear: vi.fn(),
    ...overrides,
  };
  renderPureUi(<TableBulkBar {...props} />);
  return props;
}

describe("TableBulkBar selection limit", () => {
  it("a pick over the most one change covers says so and locks every action", () => {
    bar({ count: 201, limit: { max: 200, available: 260 } });

    expect(screen.getByText("201 selected, at most 200")).toBeTruthy();
    for (const name of [/Status/, /Set due date/, /Archive/]) {
      expect(button(name).matches(":disabled")).toBe(true);
    }
    // The way out stays open (the fieldset locks the actions, not the close button).
    expect(button(copy.bulk.clear).matches(":disabled")).toBe(false);
  });

  it("a pick at the limit with rows left tells how many were not picked and keeps the actions open", () => {
    bar({ count: 200, limit: { max: 200, available: 260 } });

    expect(screen.getByText("200 selected")).toBeTruthy();
    expect(
      screen.getByText("At most 200 at a time. 60 more are not selected."),
    ).toBeTruthy();
    expect(button(/Archive/).matches(":disabled")).toBe(false);
  });

  it("a pick under the limit shows no limit message", () => {
    bar({ count: 5, limit: { max: 200, available: 260 } });

    expect(screen.getByText("5 selected")).toBeTruthy();
    expect(screen.queryByRole("status")).toBeNull();
  });
});

describe("TableBulkBar without edit rights", () => {
  it("Status and Due date stay reachable, are not operable and say why", () => {
    const props = bar({ canEdit: false });
    const reason = copy.bulk.readOnly;

    for (const name of [copy.bulk.status, copy.bulk.due]) {
      const control = button(`${name}: ${reason}`);
      expect(control.getAttribute("aria-disabled")).toBe("true");
      expect(control.title).toBe(reason);
      fireEvent.click(control);
    }
    // Neither opens a menu or a popover, and nothing is written.
    expect(screen.queryByRole("menu")).toBeNull();
    expect(screen.queryByLabelText(copy.bulk.dueLabel)).toBeNull();
    expect(props.onStatus).not.toHaveBeenCalled();
    expect(props.onDue).not.toHaveBeenCalled();
  });

  it("Assign to and Move to board look disabled and the reason is written on the bar", () => {
    bar({ canEdit: false });
    const reason = copy.bulk.readOnly;

    // A touch screen shows no title or label: the reason is text on the bar.
    expect(screen.getByText(reason)).toBeTruthy();
    for (const name of [copy.bulk.assign, copy.bulk.move]) {
      const control = button(`${name}: ${reason}`);
      expect(control.getAttribute("aria-disabled")).toBe("true");
      expect(control.className).toContain("opacity-50");
    }
  });
});

describe("TableBulkFailure", () => {
  it("names what happened and retries on Try again", () => {
    const onRetry = vi.fn();
    renderPureUi(
      <TableBulkFailure
        copy={copy}
        count={3}
        detail="Nothing was changed."
        onRetry={onRetry}
      />,
    );

    expect(screen.getByRole("alert").textContent).toContain(
      "Couldn't apply to all 3 items",
    );
    expect(screen.getByText("Nothing was changed.")).toBeTruthy();
    fireEvent.click(button(copy.bulk.retry));
    expect(onRetry).toHaveBeenCalledOnce();
  });
});
