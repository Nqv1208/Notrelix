import { describe, expect, it } from "vitest";
import {
  LEGACY_ACCENT_MIGRATION,
  THEME_ACCENTS,
  THEME_STORAGE_KEYS,
  readThemePreference,
  resolveAppearance,
  themeRootState,
  writeThemePreference,
  type KeyValueStorage,
} from "../theme/theme-model";

function memoryStorage(initial: Record<string, string> = {}) {
  const data = new Map(Object.entries(initial));
  const storage: KeyValueStorage = {
    getItem: (key) => data.get(key) ?? null,
    setItem: (key, value) => void data.set(key, value),
    removeItem: (key) => void data.delete(key),
  };
  return { storage, data };
}

describe("theme model", () => {
  it("defaults to light mode and the Notrelix Blue accent", () => {
    expect(readThemePreference(undefined)).toEqual({
      preference: { mode: "light", accent: "blue" },
      migrated: false,
    });
  });

  it("resolves system mode from the OS preference only", () => {
    expect(resolveAppearance("system", true)).toBe("dark");
    expect(resolveAppearance("system", false)).toBe("light");
    expect(resolveAppearance("light", true)).toBe("light");
    expect(resolveAppearance("dark", false)).toBe("dark");
    expect(resolveAppearance("high-contrast", true)).toBe("high-contrast");
  });

  it("reads current keys without migration", () => {
    const { storage } = memoryStorage({
      [THEME_STORAGE_KEYS.mode]: "high-contrast",
      [THEME_STORAGE_KEYS.accent]: "sage",
    });
    expect(readThemePreference(storage)).toEqual({
      preference: { mode: "high-contrast", accent: "sage" },
      migrated: false,
    });
  });

  it.each(Object.entries(LEGACY_ACCENT_MIGRATION))(
    "migrates the old accent theme %s to %s",
    (legacy, next) => {
      const { storage } = memoryStorage({ "notrelix-color-theme": legacy });
      const result = readThemePreference(storage);
      expect(result.preference.accent).toBe(next);
      expect(result.migrated).toBe(true);
    },
  );

  it("maps all seven old accent themes (plus default) onto new accents", () => {
    expect(Object.keys(LEGACY_ACCENT_MIGRATION).sort()).toEqual(
      [
        "aurora",
        "default",
        "editorial",
        "midnight",
        "ocean",
        "rose",
        "sage",
        "sunset",
      ].sort(),
    );
    for (const accent of Object.values(LEGACY_ACCENT_MIGRATION)) {
      expect(THEME_ACCENTS).toContain(accent);
    }
  });

  it("migrates the web host's old `theme` mode key", () => {
    const { storage } = memoryStorage({ theme: "dark" });
    expect(readThemePreference(storage)).toEqual({
      preference: { mode: "dark", accent: "blue" },
      migrated: true,
    });
  });

  it("ignores unknown stored values", () => {
    const { storage } = memoryStorage({
      [THEME_STORAGE_KEYS.mode]: "sepia",
      [THEME_STORAGE_KEYS.accent]: "violet",
      "notrelix-color-theme": "zinc",
    });
    expect(readThemePreference(storage).preference).toEqual({
      mode: "light",
      accent: "blue",
    });
  });

  it("degrades to defaults when storage throws (FE-UI-067)", () => {
    const throwing: KeyValueStorage = {
      getItem: () => {
        throw new Error("blocked");
      },
      setItem: () => {
        throw new Error("blocked");
      },
    };
    expect(readThemePreference(throwing).preference).toEqual({
      mode: "light",
      accent: "blue",
    });
    expect(() =>
      writeThemePreference(throwing, { mode: "dark", accent: "coral" }),
    ).not.toThrow();
  });

  it("writes current keys and removes legacy keys", () => {
    const { storage, data } = memoryStorage({
      theme: "dark",
      "notrelix-ui-theme": "light",
      "notrelix-color-theme": "rose",
    });
    writeThemePreference(storage, { mode: "dark", accent: "ember" });
    expect(Object.fromEntries(data)).toEqual({
      [THEME_STORAGE_KEYS.mode]: "dark",
      [THEME_STORAGE_KEYS.accent]: "ember",
    });
  });

  it("maps appearance to the root contract the token CSS reads", () => {
    expect(themeRootState("light", "blue")).toEqual({
      colorClass: "light",
      contrast: null,
      accent: "blue",
    });
    expect(themeRootState("dark", "coral")).toEqual({
      colorClass: "dark",
      contrast: null,
      accent: "coral",
    });
    expect(themeRootState("high-contrast", "amber")).toEqual({
      colorClass: "light",
      contrast: "high",
      accent: "amber",
    });
  });
});
