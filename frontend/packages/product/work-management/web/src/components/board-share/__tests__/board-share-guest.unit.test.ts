import { describe, expect, it } from "vitest";
import { vi as copy } from "../board-share.copy";
import {
  formatInviteDate,
  guestInviteConflict,
  guestOffer,
  type PendingGuestInvitation,
} from "../board-share-guest";

const PENDING: PendingGuestInvitation[] = [
  {
    id: "inv-2",
    email: "Ketoan@doitac-erp.vn",
    issuedByYou: false,
    canManage: false,
    invitedByName: "Lê Hoàng Phúc",
    expiresAt: "2026-11-24T12:00:00Z",
  },
  {
    id: "inv-3",
    email: "an@doitac-erp.vn",
    issuedByYou: false,
    canManage: false,
    invitedByName: "",
    expiresAt: "2026-11-24T12:00:00Z",
  },
];

describe("guestOffer", () => {
  it("UC-WM-A19 offers a well-formed email no member matches, and calls an email-looking text invalid", () => {
    expect(guestOffer(" khach@doitac-erp.vn ", 0)).toEqual({
      kind: "email",
      email: "khach@doitac-erp.vn",
    });
    expect(guestOffer("khach@doitac", 0)).toEqual({ kind: "invalid" });
    expect(guestOffer("khach@", 0)).toEqual({ kind: "invalid" });
  });

  it("UC-WM-A19 leaves a member search and a matched name alone", () => {
    expect(guestOffer("tuan", 0)).toBeNull();
    expect(guestOffer("", 0)).toBeNull();
    expect(guestOffer("khach@doitac-erp.vn", 1)).toBeNull();
  });
});

describe("guestInviteConflict", () => {
  it("UC-WM-A19 names the person who already invited the email, whatever the case of the address", () => {
    expect(guestInviteConflict("ketoan@doitac-erp.vn", PENDING, copy)).toEqual({
      title: "Chưa gửi được lời mời",
      body: "Lê Hoàng Phúc đã mời Ketoan@doitac-erp.vn rồi. Danh sách lời mời đã được cập nhật.",
    });
  });

  it("UC-WM-A19 says someone else when the pending list gives no name, and nothing when no invitation matches", () => {
    expect(guestInviteConflict("an@doitac-erp.vn", PENDING, copy)?.body).toBe(
      "Một người khác đã mời an@doitac-erp.vn rồi. Danh sách lời mời đã được cập nhật.",
    );
    expect(guestInviteConflict("khac@doitac-erp.vn", PENDING, copy)).toBeNull();
  });
});

describe("formatInviteDate", () => {
  it("writes day/month/year and nothing for a bad instant", () => {
    expect(formatInviteDate("2026-11-26T12:00:00Z")).toBe("26/11/2026");
    expect(formatInviteDate("not a date")).toBe("");
  });
});
