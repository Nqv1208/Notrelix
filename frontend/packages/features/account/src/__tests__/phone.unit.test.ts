import { describe, expect, it } from "vitest";
import { checkPhone, phoneForSave } from "../core/phone";

describe("checkPhone", () => {
  it("keeps a plain E.164 number", () => {
    expect(checkPhone("+84912345678")).toEqual({
      kind: "valid",
      e164: "+84912345678",
    });
  });

  it("drops spaces, hyphens, dots and parentheses", () => {
    for (const typed of [
      "+84 912 345 678",
      " +84-912-345-678 ",
      "+84 (912) 345.678",
    ])
      expect(checkPhone(typed)).toEqual({
        kind: "valid",
        e164: "+84912345678",
      });
  });

  it("treats blank input as clearing the phone", () => {
    expect(checkPhone("")).toEqual({ kind: "empty" });
    expect(checkPhone("   ")).toEqual({ kind: "empty" });
    expect(checkPhone(" - ( ) ")).toEqual({ kind: "empty" });
  });

  it("asks for the country code instead of guessing a region", () => {
    expect(checkPhone("0912 345 678")).toEqual({
      kind: "invalid",
      reason: "country-code",
    });
    expect(checkPhone("84912345678")).toEqual({
      kind: "invalid",
      reason: "country-code",
    });
  });

  it("refuses letters, a misplaced plus and a wrong length", () => {
    for (const typed of [
      "call me",
      "+84 912 abc 678",
      "84+912345678",
      "+8491234",
      "+8491234567890123",
      "+0912345678",
      "+",
    ])
      expect(checkPhone(typed)).toEqual({ kind: "invalid", reason: "format" });
  });

  it("accepts the shortest and the longest number the producer accepts", () => {
    expect(checkPhone("+12345678").kind).toBe("valid");
    expect(checkPhone("+123456789012345").kind).toBe("valid");
  });
});

describe("phoneForSave", () => {
  it("gives the value to send", () => {
    expect(phoneForSave("+84 912 345 678")).toBe("+84912345678");
    expect(phoneForSave("")).toBe("");
    expect(phoneForSave("0912")).toBeNull();
  });
});
