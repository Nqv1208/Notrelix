import { beforeAll, describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, waitFor } from "@notrelix/testing";
import { vi as copy } from "../board-header.copy";
import { ViewTabMenu, type ViewTabActions } from "../view-tab-menu";

beforeAll(() => {
  // Radix menus measure and scroll items; JSDOM has no layout.
  Element.prototype.scrollIntoView = () => undefined;
  Element.prototype.hasPointerCapture = () => false;
  Element.prototype.releasePointerCapture = () => undefined;
});

const kanban = {
  id: "v1",
  name: "Kanban",
  viewMode: "kanban",
  isDefault: true,
};
const table = { id: "v2", name: "Bảng", viewMode: "table" };
const mine = {
  id: "v3",
  name: "Của tôi",
  viewMode: "table",
  visibility: "me" as const,
};

function actions(over: Partial<ViewTabActions> = {}): ViewTabActions {
  return {
    archivedViews: [],
    onSetDefault: vi.fn(async () => "ok" as const),
    onArchive: vi.fn(async () => "ok" as const),
    onDelete: vi.fn(async () => "ok" as const),
    onRestore: vi.fn(async () => "ok" as const),
    ...over,
  };
}

function open(view: typeof kanban | typeof table, a: ViewTabActions) {
  renderPureUi(
    <ViewTabMenu
      view={view}
      savedViews={[kanban, table, mine]}
      actions={a}
      copy={copy}
    />,
  );
  fireEvent.keyDown(screen.getByRole("button", { name: /^Tuỳ chọn của/ }), {
    key: "Enter",
  });
}

describe("ViewTabMenu", () => {
  it("deleting the default view names its successor among the shared views, never a private one", async () => {
    const a = actions();
    open(kanban, a);
    fireEvent.click(
      await screen.findByRole("menuitem", { name: /^Xoá chế độ xem/ }),
    );
    const picker = await screen.findByRole("combobox", {
      name: "Chế độ xem mặc định kế tiếp",
    });
    expect(picker.textContent).toContain("Bảng");
    fireEvent.click(screen.getByRole("button", { name: "Xoá chế độ xem" }));
    await waitFor(() => expect(a.onDelete).toHaveBeenCalledWith("v1", "v2"));
  });

  it("a stale version changes nothing and the confirm says so", async () => {
    const a = actions({ onArchive: vi.fn(async () => "conflict" as const) });
    open(table, a);
    fireEvent.click(
      await screen.findByRole("menuitem", { name: /^Lưu trữ chế độ xem/ }),
    );
    fireEvent.click(
      await screen.findByRole("button", { name: "Lưu trữ chế độ xem" }),
    );
    expect((await screen.findByRole("alert")).textContent).toContain(
      "Có người vừa sửa chế độ xem này",
    );
    expect(a.onArchive).toHaveBeenCalledWith("v2", undefined);
  });

  it("the default view is not offered as the default again", async () => {
    open(kanban, actions());
    const item = await screen.findByRole("menuitem", {
      name: "Đặt làm chế độ xem mặc định",
    });
    expect(item.getAttribute("aria-disabled")).toBe("true");
  });
});
