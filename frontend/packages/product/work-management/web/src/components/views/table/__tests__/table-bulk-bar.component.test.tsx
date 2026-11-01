import { describe, expect, it, vi } from "vitest";
import { renderPureUi, screen } from "@notrelix/testing";
import { mainTableCopy } from "../main-table.copy";
import { TableBulkBar, type TableBulkBarProps } from "../table-bulk-bar";

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
