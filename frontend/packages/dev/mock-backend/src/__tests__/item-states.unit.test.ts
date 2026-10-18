import { describe, expect, it } from "vitest";
import { defaultConfig } from "../config/mock-config";
import { mockIds } from "../state/mock-ids";
import { erpItemId } from "../state/mock-erp-board";
import { peopleDirectory } from "../state/mock-people-directory";
import { MockStore } from "../state/mock-store";

const store = () => new MockStore({ ...defaultConfig });

describe("items of the States mockup (CTR-GAP-WM-ITEM-STATES)", () => {
  it("opens by address and stays out of every board read", () => {
    const s = store();
    for (const key of ["ERP-154", "ERP-118", "ERP-160"])
      expect(s.getCard(erpItemId(key))?.itemKey).toBe(key);
    const listed = s
      .getLists(mockIds.boards.erp)
      .flatMap((list) => s.getCards(list.id))
      .filter((card) => !card.parentItemId);
    expect(listed).toHaveLength(16);
    expect(listed.map((card) => card.itemKey)).not.toContain("ERP-160");
  });

  it("ERP-118 is archived by Nguyễn Thu Hà and restore brings it back", () => {
    const s = store();
    const id = erpItemId("ERP-118");
    const card = s.getCard(id)!;
    expect(card.isArchived).toBe(true);
    expect(s.getUser(card.archivedByUserId!)?.name).toBe("Nguyễn Thu Hà");
    expect(s.restoreCard(id)).toBe(true);
    expect(s.getCard(id)?.isArchived).toBe(false);
    expect(s.restoreCard(id)).toBe(false);
  });

  it("ERP-160 has 48 child items, 12 done", () => {
    const s = store();
    const children = s.getChildCards(erpItemId("ERP-160"));
    expect(children).toHaveLength(48);
    expect(children.filter((child) => child.completedAt)).toHaveLength(12);
    expect(children[0]?.title).toBe("Chi nhánh Hà Nội");
  });

  it("the directory splits people who see the board from those who do not", () => {
    const s = store();
    const directory = peopleDirectory(s, mockIds.boards.erp, true)!;
    const phuc = directory.people.find((p) => p.name === "Lê Hoàng Phúc");
    expect(phuc?.onBoard).toBe(true);
    expect(phuc?.department).toBe("Công nghệ");
    expect(directory.people.some((p) => !p.onBoard)).toBe(true);
    expect(directory.canInvite).toBe(true);
    const outsider = directory.people.find((p) => !p.onBoard)!;
    expect(s.addBoardMember(mockIds.boards.erp, outsider.userId)).toBe(true);
    expect(s.addBoardMember(mockIds.boards.erp, outsider.userId)).toBe(false);
    expect(
      peopleDirectory(s, mockIds.boards.erp, true)!.people.find(
        (p) => p.userId === outsider.userId,
      )?.onBoard,
    ).toBe(true);
  });
});
