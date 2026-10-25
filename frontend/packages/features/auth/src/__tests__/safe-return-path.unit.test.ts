import { describe, expect, it } from "vitest";
import {
  RETURN_PATH_MAX_LENGTH,
  isSafeReturnPath,
  safeReturnPath,
} from "../core";

describe("safe return path", () => {
  it.each([
    "/home",
    "/workspaces/w1/boards/b1",
    "/boards/42?tab=list&sort=due#row-3",
    "/invite/token-abc",
    "/a//b",
    "/",
  ])("accepts the internal path %s", (path) => {
    expect(isSafeReturnPath(path)).toBe(true);
    expect(safeReturnPath(path)).toBe(path);
  });

  it.each([
    ["protocol-relative", "//evil.com"],
    ["protocol-relative with a path", "//evil.com/phish"],
    ["backslash after the slash", "/\\evil.com"],
    ["backslash anywhere", "/ok\\evil"],
    ["https url", "https://evil.com"],
    ["http url", "http://evil.com/x"],
    ["javascript url", "javascript:alert(1)"],
    ["data url", "data:text/html,<script>1</script>"],
    ["host without a slash", "evil.com"],
    ["empty", ""],
    ["encoded double slash", "/%2F%2Fevil.com"],
    ["encoded double slash, lower case", "/%2f/evil.com"],
    ["encoded slash then slash", "/%2F/evil.com"],
    ["encoded backslash", "/%5Cevil.com"],
    ["double-encoded double slash", "/%252F%252Fevil.com"],
    ["triple-encoded double slash", "/%25252F%25252Fevil.com"],
    ["carriage return and line feed", "/ok\r\nSet-Cookie: x=1"],
    ["encoded carriage return and line feed", "/ok%0d%0aSet-Cookie:x=1"],
    ["tab", "/ok\tevil"],
    ["null byte", "/ok\u0000evil"],
    ["malformed escape", "/%E0%A4%A"],
  ])("refuses %s", (_name, path) => {
    expect(isSafeReturnPath(path)).toBe(false);
    expect(safeReturnPath(path)).toBeNull();
  });

  it("refuses nothing, null and undefined", () => {
    expect(isSafeReturnPath(null)).toBe(false);
    expect(isSafeReturnPath(undefined)).toBe(false);
    expect(safeReturnPath(undefined)).toBeNull();
  });

  it("caps the length", () => {
    expect(isSafeReturnPath(`/${"a".repeat(RETURN_PATH_MAX_LENGTH - 1)}`)).toBe(
      true,
    );
    expect(isSafeReturnPath(`/${"a".repeat(RETURN_PATH_MAX_LENGTH)}`)).toBe(
      false,
    );
  });
});
