import { describe, it, expect } from "vitest";
import { generateCorrelationId } from "../ids/correlation-id";

describe("generateCorrelationId", () => {
  it("returns a string", () => {
    const id = generateCorrelationId();
    expect(typeof id).toBe("string");
  });

  it("returns a valid UUID format", () => {
    const id = generateCorrelationId();
    const uuidRegex =
      /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
    expect(id).toMatch(uuidRegex);
  });

  it("generates unique IDs", () => {
    const ids = new Set<string>();
    for (let i = 0; i < 100; i++) {
      ids.add(generateCorrelationId());
    }
    expect(ids.size).toBe(100);
  });
});
