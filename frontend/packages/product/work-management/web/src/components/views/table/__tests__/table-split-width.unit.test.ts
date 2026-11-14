import { describe, expect, it } from "vitest";
import {
  SPLIT_PANE_DEFAULT,
  SPLIT_PANE_MAX,
  SPLIT_PANE_MIN,
  SPLIT_PANE_STORAGE_KEY,
  clampSplitWidth,
  readSplitWidth,
  splitWidthAt,
  writeSplitWidth,
} from "../table-split-width";

function memoryStorage(seed: Record<string, string> = {}) {
  const data = new Map(Object.entries(seed));
  return {
    getItem: (key: string) => data.get(key) ?? null,
    setItem: (key: string, value: string) => {
      data.set(key, value);
    },
    data,
  };
}

describe("table split pane width", () => {
  it("opens at the artboard's 46 % and keeps the table at 54 %", () => {
    expect(SPLIT_PANE_DEFAULT).toBe(0.46);
    expect(readSplitWidth(memoryStorage())).toBe(SPLIT_PANE_DEFAULT);
  });

  it("clamps a share to the limits and falls back to the default for NaN", () => {
    expect(clampSplitWidth(0.1)).toBe(SPLIT_PANE_MIN);
    expect(clampSplitWidth(0.95)).toBe(SPLIT_PANE_MAX);
    expect(clampSplitWidth(0.5)).toBe(0.5);
    expect(clampSplitWidth(Number.NaN)).toBe(SPLIT_PANE_DEFAULT);
  });

  it("turns a pointer position into the pane's share of the view (pane at the right edge)", () => {
    const view = { left: 100, width: 1000 };
    expect(splitWidthAt(600, view)).toBeCloseTo(0.5);
    expect(splitWidthAt(100, view)).toBe(SPLIT_PANE_MAX);
    expect(splitWidthAt(1100, view)).toBe(SPLIT_PANE_MIN);
    expect(splitWidthAt(600, { left: 0, width: 0 })).toBe(SPLIT_PANE_DEFAULT);
  });

  it("saves the width and reads it back on the next visit", () => {
    const storage = memoryStorage();
    writeSplitWidth(storage, 0.6);
    expect(storage.data.get(SPLIT_PANE_STORAGE_KEY)).toBe("0.6");
    expect(readSplitWidth(storage)).toBe(0.6);
  });

  it("ignores a stored value that is not a number and clamps one out of range", () => {
    expect(
      readSplitWidth(memoryStorage({ [SPLIT_PANE_STORAGE_KEY]: "wide" })),
    ).toBe(SPLIT_PANE_DEFAULT);
    expect(
      readSplitWidth(memoryStorage({ [SPLIT_PANE_STORAGE_KEY]: "0.99" })),
    ).toBe(SPLIT_PANE_MAX);
  });

  it("works without storage: a refused read or write keeps the default", () => {
    const refusing = {
      getItem: () => {
        throw new Error("blocked");
      },
      setItem: () => {
        throw new Error("full");
      },
    };
    expect(readSplitWidth(refusing)).toBe(SPLIT_PANE_DEFAULT);
    expect(() => writeSplitWidth(refusing, 0.5)).not.toThrow();
    expect(readSplitWidth(null)).toBe(SPLIT_PANE_DEFAULT);
    expect(() => writeSplitWidth(null, 0.5)).not.toThrow();
  });
});
