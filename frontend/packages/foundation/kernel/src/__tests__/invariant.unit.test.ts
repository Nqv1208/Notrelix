import { describe, it, expect } from "vitest";
import { invariant, assertNonNull } from "../assertions/invariant";

describe("invariant", () => {
  it("does not throw when condition is truthy", () => {
    expect(() => invariant(true, "error")).not.toThrow();
    expect(() => invariant(1, "error")).not.toThrow();
    expect(() => invariant("text", "error")).not.toThrow();
    expect(() => invariant({}, "error")).not.toThrow();
  });

  it("throws when condition is falsy", () => {
    expect(() => invariant(false, "test message")).toThrow(
      "Invariant violation: test message",
    );
    expect(() => invariant(0, "zero is falsy")).toThrow(
      "Invariant violation: zero is falsy",
    );
    expect(() => invariant(null, "null is falsy")).toThrow(
      "Invariant violation: null is falsy",
    );
    expect(() => invariant(undefined, "undefined is falsy")).toThrow(
      "Invariant violation: undefined is falsy",
    );
    expect(() => invariant("", "empty string")).toThrow(
      "Invariant violation: empty string",
    );
  });
});

describe("assertNonNull", () => {
  it("returns value when not null or undefined", () => {
    expect(assertNonNull("hello", "msg")).toBe("hello");
    expect(assertNonNull(42, "msg")).toBe(42);
    expect(assertNonNull(false, "msg")).toBe(false);
    expect(assertNonNull(0, "msg")).toBe(0);
  });

  it("throws when value is null", () => {
    expect(() => assertNonNull(null, "is null")).toThrow(
      "Assertion failed: is null",
    );
  });

  it("throws when value is undefined", () => {
    expect(() => assertNonNull(undefined, "is undefined")).toThrow(
      "Assertion failed: is undefined",
    );
  });
});
