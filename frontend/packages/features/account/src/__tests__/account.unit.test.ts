import { describe, it, expect } from "vitest";
import { accountQueryKeys } from "../query/keys";

describe("accountQueryKeys", () => {
  it("should generate correct root key", () => {
    expect(accountQueryKeys.all("acc-1")).toEqual([
      "account",
      "acc-1",
      "account",
    ]);
  });

  it("should generate profile key", () => {
    expect(accountQueryKeys.profile("acc-1")).toEqual([
      "account",
      "acc-1",
      "account",
      "profile",
    ]);
  });

  it("should generate preferences key", () => {
    expect(accountQueryKeys.preferences("acc-1")).toEqual([
      "account",
      "acc-1",
      "account",
      "preferences",
    ]);
  });

  it("should generate security key", () => {
    expect(accountQueryKeys.security("acc-1")).toEqual([
      "account",
      "acc-1",
      "account",
      "security",
    ]);
  });
});
