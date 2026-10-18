import { describe, expect, it } from "vitest";
import {
  DEFAULT_LOCALE,
  LOCALE_STORAGE_KEY,
  detectLocale,
  matchLocale,
  readStoredLocale,
  writeStoredLocale,
  type LocaleStorage,
} from "../locale";

describe("locale detection (stored → browser → vi)", () => {
  it("falls back to Vietnamese", () => {
    expect(DEFAULT_LOCALE).toBe("vi");
    expect(detectLocale({})).toBe("vi");
    expect(detectLocale({ browserLocales: ["fr-FR", "de"] })).toBe("vi");
  });

  it("prefers a stored preference over the browser", () => {
    expect(detectLocale({ stored: "en", browserLocales: ["vi-VN"] })).toBe(
      "en",
    );
  });

  it("ignores an unsupported stored value", () => {
    expect(detectLocale({ stored: "fr", browserLocales: ["en-GB"] })).toBe(
      "en",
    );
  });

  it("uses the first supported browser language", () => {
    expect(detectLocale({ browserLocales: ["fr-CA", "en-US", "vi"] })).toBe(
      "en",
    );
  });

  it("matches BCP 47 tags by language", () => {
    expect(matchLocale("en-US")).toBe("en");
    expect(matchLocale("VI_vn")).toBe("vi");
    expect(matchLocale("ja")).toBeUndefined();
    expect(matchLocale("")).toBeUndefined();
  });

  it("reads and writes the preference through the storage port, tolerating failures", () => {
    const data = new Map<string, string>();
    const storage: LocaleStorage = {
      getItem: (key) => data.get(key) ?? null,
      setItem: (key, value) => void data.set(key, value),
    };
    writeStoredLocale(storage, "en");
    expect(data.get(LOCALE_STORAGE_KEY)).toBe("en");
    expect(readStoredLocale(storage)).toBe("en");

    const broken: LocaleStorage = {
      getItem: () => {
        throw new Error("blocked");
      },
      setItem: () => {
        throw new Error("blocked");
      },
    };
    expect(readStoredLocale(broken)).toBeNull();
    expect(() => writeStoredLocale(broken, "vi")).not.toThrow();
  });
});
