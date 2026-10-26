import { describe, expect, it } from "vitest";
import { parseInviteList } from "../core/model/members";

describe("parseInviteList", () => {
  it("splits on commas, semicolons, spaces and new lines and keeps each address once", () => {
    expect(
      parseInviteList("a@x.com, b@x.com;c@x.com\nA@X.com  d@x.com").valid,
    ).toEqual(["a@x.com", "b@x.com", "c@x.com", "d@x.com"]);
  });

  it("keeps what is not an address, in the order typed", () => {
    expect(parseInviteList("a@x.com nope also@bad").invalid).toEqual([
      "nope",
      "also@bad",
    ]);
  });

  it("reads an empty field as nothing", () => {
    expect(parseInviteList("  \n ")).toEqual({ valid: [], invalid: [] });
  });
});
