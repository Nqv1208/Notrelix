import { beforeAll, describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";
import {
  ERP_TABLE_ITEM,
  erpTableProps,
  erpTableReadOnlyProps,
} from "../../../../verification/table-fixtures";
import {
  MainTableSurface,
  type MainTableSurfaceProps,
} from "../main-table-surface";
import type { TableLayout } from "../table-layout";

// The menu test opens four sheets one after the other; jsdom is slow under load.
vi.setConfig({ testTimeout: 20_000 });

beforeAll(() => {
  // Radix sheets and popovers measure and scroll; JSDOM has no layout.
  Element.prototype.scrollIntoView = () => undefined;
  Element.prototype.hasPointerCapture = () => false;
  Element.prototype.releasePointerCapture = () => undefined;
  Element.prototype.setPointerCapture = () => undefined;
});

const members = [
  { userId: "u-ha", name: "Nguyễn Thu Hà", initials: "HA" },
  { userId: "u-quan", name: "Trần Minh Quân", initials: "MQ" },
];

function render(
  layout: TableLayout,
  base: MainTableSurfaceProps = erpTableProps(),
) {
  const editors = {
    members,
    priorities: [
      { id: "p-urgent", label: "Khẩn" },
      { id: "p-high", label: "Cao" },
    ],
    onAssignee: vi.fn(),
    onDue: vi.fn(),
    onPriority: vi.fn(),
  };
  const props = {
    ...base,
    layout,
    editors,
    onOpenItem: vi.fn(),
    onStatusChange: vi.fn(),
    onGroupToggle: vi.fn(),
    rowActions: { onCopyLink: vi.fn() },
  };
  renderPureUi(<MainTableSurface {...props} />);
  return props;
}

const id = (key: string) => ERP_TABLE_ITEM(key);

describe("the table on a tablet", () => {
  it("pins the title with its key in it and drops the key column", () => {
    render("tablet");
    expect(screen.queryByRole("columnheader", { name: "Mã" })).toBeNull();
    const row = document.querySelector<HTMLElement>(
      `[data-row-id="${id("ERP-144")}"]`,
    )!;
    expect(within(row).getByText("ERP-144")).toBeTruthy();
    // The due cell says the date over "Hôm nay", as the drawing does.
    expect(within(row).getByText("12/10")).toBeTruthy();
    expect(within(row).getByText("Hôm nay")).toBeTruthy();
  });

  it("the due picker offers presets and a time, and a preset writes a day at that time", () => {
    const props = render("tablet");
    fireEvent.click(
      screen.getByRole("button", { name: "Đổi hạn của ERP-151" }),
    );
    for (const preset of ["Hôm nay", "Ngày mai", "Thứ Hai tới", "Không có hạn"])
      expect(
        within(screen.getByRole("dialog")).getByRole("button", {
          name: new RegExp(`^${preset}`),
        }),
      ).toBeTruthy();
    expect(screen.getByLabelText("Giờ đến hạn của công việc")).toBeTruthy();
    fireEvent.click(
      within(screen.getByRole("dialog")).getByRole("button", {
        name: /^Ngày mai/,
      }),
    );
    expect(props.editors.onDue).toHaveBeenCalledWith(
      id("ERP-151"),
      expect.stringMatching(/^\d{4}-\d{2}-\d{2}$/),
      "17:00",
    );
  });

  it("Không có hạn clears the date", () => {
    const props = render("tablet");
    fireEvent.click(
      screen.getByRole("button", { name: "Đổi hạn của ERP-151" }),
    );
    fireEvent.click(
      within(screen.getByRole("dialog")).getByRole("button", {
        name: /^Không có hạn/,
      }),
    );
    expect(props.editors.onDue).toHaveBeenCalledWith(
      id("ERP-151"),
      null,
      "17:00",
    );
  });
});

describe("the table on a phone", () => {
  const card = (key: string) =>
    document.querySelector<HTMLElement>(`[data-row-id="${id(key)}"]`)!;

  it("draws a card per item with a check circle and a row menu", () => {
    render("phone");
    const list = document.querySelector<HTMLElement>(
      "[data-slot=table-phone-list]",
    )!;
    expect(within(list).getAllByRole("heading", { level: 2 }).length).toBe(3);
    const row = within(card("ERP-144"));
    expect(
      row.getByRole("button", { name: "Đánh dấu xong ERP-144" }),
    ).toBeTruthy();
    expect(
      row.getByRole("button", { name: "Thao tác khác cho ERP-144" }),
    ).toBeTruthy();
    expect(row.getByText("Hôm nay")).toBeTruthy();
  });

  it("the check circle writes the finished status through the same callback", () => {
    const props = render("phone");
    fireEvent.click(
      screen.getByRole("button", { name: "Đánh dấu xong ERP-144" }),
    );
    expect(props.onStatusChange).toHaveBeenCalledWith(
      id("ERP-144"),
      expect.any(String),
    );
  });

  it("the row menu moves, assigns, sets the due date and the priority", () => {
    const props = render("phone");
    const open = (key: string) =>
      fireEvent.click(
        screen.getByRole("button", { name: `Thao tác khác cho ${key}` }),
      );
    open("ERP-151");
    fireEvent.click(screen.getByRole("button", { name: /^Đang làm/ }));
    expect(props.onStatusChange).toHaveBeenCalledWith(
      id("ERP-151"),
      expect.any(String),
    );

    open("ERP-151");
    fireEvent.click(screen.getByRole("button", { name: /^Giao cho…/ }));
    fireEvent.click(screen.getByRole("button", { name: /Trần Minh Quân/ }));
    expect(props.editors.onAssignee).toHaveBeenCalledWith(
      id("ERP-151"),
      "u-quan",
    );

    open("ERP-151");
    fireEvent.click(screen.getByRole("button", { name: /^Đổi hạn…/ }));
    fireEvent.click(screen.getByRole("button", { name: /^Không có hạn/ }));
    expect(props.editors.onDue).toHaveBeenCalledWith(
      id("ERP-151"),
      null,
      "17:00",
    );

    open("ERP-151");
    fireEvent.click(screen.getByRole("button", { name: /^Đặt ưu tiên…/ }));
    fireEvent.click(screen.getByRole("button", { name: "Khẩn" }));
    expect(props.editors.onPriority).toHaveBeenCalledWith(
      id("ERP-151"),
      "p-urgent",
    );
  });

  it("a read-only viewer can open the menu but sees no edit rows", () => {
    const props = render("phone", erpTableReadOnlyProps());
    expect(
      (
        screen.getByRole("button", {
          name: "Đánh dấu xong ERP-144",
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(true);
    fireEvent.click(
      screen.getByRole("button", { name: "Thao tác khác cho ERP-144" }),
    );
    expect(screen.getByText("Bạn chỉ xem được board này")).toBeTruthy();
    expect(screen.queryByRole("button", { name: /^Giao cho…/ })).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: /^Mở toàn trang/ }));
    expect(props.onOpenItem).toHaveBeenCalledWith(id("ERP-144"));
  });
});
