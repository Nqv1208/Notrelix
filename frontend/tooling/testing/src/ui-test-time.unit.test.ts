import { describe, expect, it } from "vitest";
import {
  createUiTestReferenceDate,
  UI_TEST_REFERENCE_INSTANT_ISO,
} from "./ui-test-time";

describe("ui test time", () => {
  it("returns a fresh date for the fixed UI reference instant", () => {
    const first = createUiTestReferenceDate();
    const second = createUiTestReferenceDate();

    expect(first.toISOString()).toBe(UI_TEST_REFERENCE_INSTANT_ISO);
    expect(second.toISOString()).toBe(UI_TEST_REFERENCE_INSTANT_ISO);
    expect(first).not.toBe(second);
  });
});
