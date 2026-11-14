import { describe, expect, it } from "vitest";
import {
  EMOJI_RECENT_MAX,
  SHORTCODE_MAX,
  activeShortcode,
  insertAtSelection,
  insertShortcode,
  pushRecent,
  searchEmoji,
  shortcodeSuggestions,
} from "../core";

describe("searchEmoji", () => {
  it("finds by English and by Vietnamese words, with or without accents", () => {
    expect(searchEmoji("fire").map((e) => e.char)).toContain("🔥");
    expect(searchEmoji("lửa").map((e) => e.char)).toContain("🔥");
    expect(searchEmoji("lua").map((e) => e.char)).toContain("🔥");
    expect(searchEmoji("cảm ơn").map((e) => e.char)).toContain("🙏");
  });

  it("ranks a name that starts with the text before a word that merely contains it", () => {
    const found = searchEmoji("smi");
    expect(found[0]?.id.startsWith("smi")).toBe(true);
  });

  it("returns nothing for gibberish and everything for blank text", () => {
    expect(searchEmoji("zzzzqx")).toEqual([]);
    expect(searchEmoji("  ").length).toBeGreaterThan(250);
  });
});

describe("activeShortcode", () => {
  it("needs a colon after a space or the start and two characters", () => {
    expect(activeShortcode(":smi", 4)).toEqual({ start: 0, query: "smi" });
    expect(activeShortcode("hi :ro", 6)).toEqual({ start: 3, query: "ro" });
    expect(activeShortcode(":s", 2)).toBeNull();
    expect(activeShortcode("12:30", 5)).toBeNull();
    expect(activeShortcode("a:smile", 7)).toBeNull();
    expect(activeShortcode(":smi ", 5)).toBeNull();
  });
});

describe("shortcodeSuggestions", () => {
  it("lists names starting with the text first and caps the list", () => {
    const found = shortcodeSuggestions("smi");
    expect(found[0]?.id).toBe("smile");
    expect(found.length).toBeLessThanOrEqual(SHORTCODE_MAX);
    expect(shortcodeSuggestions("s")).toEqual([]);
  });
});

describe("inserting", () => {
  it("replaces the selection and puts the caret after the emoji", () => {
    expect(insertAtSelection("hello world", 5, 5, "😄")).toEqual({
      text: "hello😄 world",
      caret: 7,
    });
    expect(insertAtSelection("hello world", 6, 11, "🌍").text).toBe("hello 🌍");
  });

  it("replaces the :shortcode", () => {
    const trigger = activeShortcode("hi :smi", 7)!;
    expect(insertShortcode("hi :smi", trigger, 7, "😄")).toEqual({
      text: "hi 😄",
      caret: 5,
    });
  });
});

describe("pushRecent", () => {
  it("moves a repeat to the front and caps the list", () => {
    expect(pushRecent(["a", "b", "c"], "b")).toEqual(["b", "a", "c"]);
    let list: readonly string[] = [];
    for (let i = 0; i < 30; i += 1) list = pushRecent(list, String(i));
    expect(list).toHaveLength(EMOJI_RECENT_MAX);
    expect(list[0]).toBe("29");
  });
});
