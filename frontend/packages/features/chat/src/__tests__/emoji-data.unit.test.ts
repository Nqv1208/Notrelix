import { describe, expect, it } from "vitest";
import {
  EMOJI_CATEGORIES,
  EMOJI_ENTRIES,
  CHAT_REACTION_PALETTE,
} from "../core";

describe("emoji dataset", () => {
  it("has about 300 emoji", () => {
    expect(EMOJI_ENTRIES.length).toBeGreaterThanOrEqual(300);
  });

  it("has unique shortcodes in the shortcode alphabet and unique glyphs", () => {
    const ids = EMOJI_ENTRIES.map((entry) => entry.id);
    expect(new Set(ids).size).toBe(ids.length);
    for (const id of ids) expect(id).toMatch(/^[a-z0-9_+-]{2,32}$/u);
    const chars = EMOJI_ENTRIES.map((entry) => entry.char);
    expect(new Set(chars).size).toBe(chars.length);
  });

  it("gives every emoji English and Vietnamese search words", () => {
    for (const entry of EMOJI_ENTRIES) {
      expect(entry.en.trim(), entry.id).not.toBe("");
      expect(entry.vi.trim(), entry.id).not.toBe("");
    }
  });

  it("fills every category and uses no category outside the list", () => {
    for (const category of EMOJI_CATEGORIES) {
      expect(
        EMOJI_ENTRIES.filter((entry) => entry.category === category).length,
        category,
      ).toBeGreaterThanOrEqual(10);
    }
    for (const entry of EMOJI_ENTRIES) {
      expect(EMOJI_CATEGORIES).toContain(entry.category);
    }
  });

  it("contains the six reaction palette emoji, so the full picker can react with them", () => {
    const chars = EMOJI_ENTRIES.map((entry) => entry.char);
    for (const emoji of CHAT_REACTION_PALETTE) expect(chars).toContain(emoji);
  });
});
