import { describe, it, expect } from "vitest";
import { accountQueryKeys } from "../query/keys";

describe("accountQueryKeys", () => {
  it("should generate correct root key", () => {
    expect(accountQueryKeys.all).toEqual(["account", "account"]);
  });

  it("should generate profile key", () => {
    expect(accountQueryKeys.profile).toEqual(["account", "account", "profile"]);
  });

  it("should generate preferences key", () => {
    expect(accountQueryKeys.preferences).toEqual([
      "account",
      "account",
      "preferences",
    ]);
  });

  it("should generate security key", () => {
    expect(accountQueryKeys.security).toEqual([
      "account",
      "account",
      "security",
    ]);
  });
});
