import { describe, expect, it } from "vitest";
import { ASSIGNABLE_ROLES, INVITABLE_ROLES } from "../core/model/members";

describe("INVITABLE_ROLES (WSP-026)", () => {
  it("offers Member or Guest only; Admin is granted after joining", () => {
    expect(INVITABLE_ROLES).toEqual(["member", "guest"]);
    expect(INVITABLE_ROLES).not.toContain("admin");
  });

  it("keeps Admin assignable to an existing member", () => {
    expect(ASSIGNABLE_ROLES).toContain("admin");
  });
});
