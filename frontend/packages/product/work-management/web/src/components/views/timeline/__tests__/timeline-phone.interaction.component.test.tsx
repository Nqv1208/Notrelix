import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";
import {
  timelineMockupGroups,
  timelineMockupLinks,
} from "../../../../verification/timeline-fixtures";
import { TimelineLinkSheet, TimelinePhoneList } from "../timeline-phone";
import { timelineCopy } from "../timeline.copy";

const copy = timelineCopy("vi");
const groups = timelineMockupGroups();
const items = groups.flatMap((group) => group.items);
const itemOf = (id: string) => items.find((item) => item.id === id)!;
const linkable = items.filter((item) => item.kind === "item");

describe("TimelinePhoneList (WM-TLCal-320)", () => {
  it("lists every item with its key, title and due day, and opens it from the row", () => {
    const onOpenItem = vi.fn();
    renderPureUi(
      <TimelinePhoneList
        copy={copy}
        groups={groups}
        onOpenItem={onOpenItem}
        onLink={vi.fn()}
      />,
    );
    const list = screen.getByRole("list", { name: "Việc trên dòng thời gian" });
    expect(within(list).getAllByRole("listitem")).toHaveLength(items.length);
    fireEvent.click(screen.getByText(itemOf("erp-142").title));
    expect(onOpenItem).toHaveBeenCalledWith("erp-142");
  });

  it("gives each board item a 44 px Liên kết với… button, and none to phases and milestones", () => {
    const onLink = vi.fn();
    renderPureUi(
      <TimelinePhoneList
        copy={copy}
        groups={groups}
        onOpenItem={vi.fn()}
        onLink={onLink}
      />,
    );
    const buttons = screen.getAllByRole("button", { name: /^Liên kết với… / });
    expect(buttons).toHaveLength(linkable.length);
    fireEvent.click(
      screen.getByRole("button", {
        name: `Liên kết với… ${itemOf("erp-142").itemKey}`,
      }),
    );
    expect(onLink).toHaveBeenCalledWith(itemOf("erp-142"));
  });

  it("offers no link button to a viewer", () => {
    renderPureUi(
      <TimelinePhoneList copy={copy} groups={groups} onOpenItem={vi.fn()} />,
    );
    expect(
      screen.queryByRole("button", { name: /^Liên kết với… / }),
    ).toBeNull();
  });
});

describe("TimelineLinkSheet (WM-TLCal-320)", () => {
  const renderSheet = (id: string) => {
    const onCreate = vi.fn();
    const onClose = vi.fn();
    renderPureUi(
      <TimelineLinkSheet
        copy={copy}
        item={itemOf(id)}
        items={linkable}
        links={timelineMockupLinks()}
        onCreate={onCreate}
        onRemove={vi.fn()}
        onClose={onClose}
      />,
    );
    return { onCreate, onClose };
  };

  it("is a dialog named for the item, with the two directions", () => {
    renderSheet("erp-146");
    const key = itemOf("erp-146").itemKey!;
    const sheet = screen.getByRole("dialog", { name: `Liên kết với… ${key}` });
    const radios = within(sheet).getAllByRole("radio");
    expect(radios.map((radio) => radio.textContent)).toEqual([
      `${key} chờ…`,
      "chặn…",
    ]);
    expect(radios[0]?.getAttribute("aria-checked")).toBe("true");
  });

  it("disables a row that would close a loop, says which waits on which, and names the chain when chosen", () => {
    // ERP-150 already waits on ERP-146 (through erp-146 -> erp-150): ERP-146 waiting on ERP-150 closes a loop.
    const { onCreate } = renderSheet("erp-146");
    const sheet = screen.getByRole("dialog");
    const k146 = itemOf("erp-146").itemKey!;
    const k150 = itemOf("erp-150").itemKey!;
    const looped = within(sheet)
      .getAllByRole("option")
      .find((option) => option.textContent?.includes(k150))!;
    expect(looped?.getAttribute("aria-disabled")).toBe("true");
    expect(looped.textContent).toContain(`Tạo vòng: ${k150} đã chờ ${k146}`);
    fireEvent.click(looped);
    const alert = within(sheet).getByRole("alert");
    expect(alert.textContent).toContain("Không thể nối: tạo vòng phụ thuộc.");
    expect(alert.textContent).toContain(`${k150} → ${k146} → ${k150}.`);
    const create = within(sheet).getByRole("button", { name: "Tạo liên kết" });
    expect(create?.getAttribute("aria-disabled")).toBe("true");
    fireEvent.click(create);
    expect(onCreate).not.toHaveBeenCalled();
  });

  it("creates the link from a free row and closes", () => {
    const { onCreate, onClose } = renderSheet("erp-151");
    const sheet = screen.getByRole("dialog");
    const free = within(sheet)
      .getAllByRole("option")
      .find((option) => !option.hasAttribute("aria-disabled"))!;
    expect(free.textContent).toMatch(/· \d{2}\/\d{2} đến \d{2}\/\d{2}$/);
    fireEvent.click(free);
    fireEvent.click(
      within(sheet).getByRole("button", { name: "Tạo liên kết" }),
    );
    expect(onCreate).toHaveBeenCalledWith(
      expect.objectContaining({ to: "erp-151" }),
    );
    expect(onClose).toHaveBeenCalled();
  });

  it("closes from the 44 px Đóng button", () => {
    const { onClose } = renderSheet("erp-151");
    fireEvent.click(screen.getByRole("button", { name: "Đóng" }));
    expect(onClose).toHaveBeenCalled();
  });
});
