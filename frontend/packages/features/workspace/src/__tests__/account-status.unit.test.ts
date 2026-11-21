import { describe, expect, it } from "vitest";
import {
  describeWorkspaceFailure,
  isAccountReadOnly,
  markAccountSuspended,
  parseWorkspaceSummary,
} from "../core";

describe("account status of a workspace (D06)", () => {
  it("reads Suspended and Closed through the workspace read, and an unknown or missing value as Active", () => {
    expect(
      parseWorkspaceSummary({ id: "w", accountStatus: "Suspended" })
        .accountStatus,
    ).toBe("Suspended");
    expect(
      parseWorkspaceSummary({ id: "w", accountStatus: "Closed" }).accountStatus,
    ).toBe("Closed");
    expect(
      parseWorkspaceSummary({ id: "w", accountStatus: "Active" }).accountStatus,
    ).toBe("Active");
    expect("accountStatus" in parseWorkspaceSummary({ id: "w" })).toBe(false);
    expect(
      "accountStatus" in
        parseWorkspaceSummary({ id: "w", accountStatus: "Frozen" }),
    ).toBe(false);
  });

  it("isAccountReadOnly is true for Suspended and Closed only", () => {
    expect(isAccountReadOnly({ accountStatus: "Suspended" })).toBe(true);
    expect(isAccountReadOnly({ accountStatus: "Closed" })).toBe(true);
    expect(isAccountReadOnly({ accountStatus: "Active" })).toBe(false);
    expect(isAccountReadOnly({})).toBe(false);
    expect(isAccountReadOnly(null)).toBe(false);
    expect(isAccountReadOnly(undefined)).toBe(false);
  });

  it("markAccountSuspended flips an Active workspace and keeps Closed as it is", () => {
    const bare: { id: string; accountStatus?: "Active" | "Closed" } = {
      id: "w",
    };
    expect(markAccountSuspended(bare).accountStatus).toBe("Suspended");
    expect(
      markAccountSuspended({ id: "w", accountStatus: "Active" }).accountStatus,
    ).toBe("Suspended");
    expect(
      markAccountSuspended({ id: "w", accountStatus: "Closed" }).accountStatus,
    ).toBe("Closed");
  });

  it("names a refused write as account-suspended without changing its kind", () => {
    const failure = describeWorkspaceFailure({
      kind: "forbidden",
      status: 403,
      details: { errorCode: "account.suspended" },
    });
    expect(failure).toMatchObject({
      kind: "forbidden",
      reason: "account-suspended",
    });
  });
});
