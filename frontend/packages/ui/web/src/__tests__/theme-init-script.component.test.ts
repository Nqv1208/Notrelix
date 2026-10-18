import { afterEach, beforeEach, describe, expect, it } from "vitest";
import { themeInitScript } from "../theme/color-theme-script";
import {
  LEGACY_ACCENT_MIGRATION,
  readThemePreference,
  resolveAppearance,
  themeRootState,
  type KeyValueStorage,
} from "../theme/theme-model";

/** Runs the pre-paint script against jsdom's <html> and localStorage. */
function runScript(prefersDark = false) {
  const original = window.matchMedia;
  window.matchMedia = ((query: string) => ({
    matches: prefersDark && query.includes("dark"),
    media: query,
  })) as unknown as typeof window.matchMedia;
  try {
    new Function(themeInitScript)();
  } finally {
    window.matchMedia = original;
  }
  const root = document.documentElement;
  return {
    colorClass: root.classList.contains("dark")
      ? "dark"
      : root.classList.contains("light")
        ? "light"
        : null,
    contrast: root.getAttribute("data-contrast"),
    accent: root.getAttribute("data-accent"),
    colorScheme: root.style.colorScheme,
  };
}

const browserStorage: KeyValueStorage = {
  getItem: (key) => window.localStorage.getItem(key),
  setItem: (key, value) => window.localStorage.setItem(key, value),
};

function resetRoot() {
  const root = document.documentElement;
  root.className = "";
  root.removeAttribute("data-contrast");
  root.removeAttribute("data-accent");
  root.removeAttribute("style");
}

describe("pre-paint theme script (no flash)", () => {
  beforeEach(() => {
    window.localStorage.clear();
    resetRoot();
  });
  afterEach(() => {
    window.localStorage.clear();
    resetRoot();
  });

  it("applies the light/blue default before any stored preference exists", () => {
    expect(runScript()).toEqual({
      colorClass: "light",
      contrast: null,
      accent: "blue",
      colorScheme: "light",
    });
  });

  it("applies a stored dark mode and accent", () => {
    window.localStorage.setItem("notrelix-theme-mode", "dark");
    window.localStorage.setItem("notrelix-theme-accent", "coral");
    expect(runScript()).toMatchObject({
      colorClass: "dark",
      accent: "coral",
      colorScheme: "dark",
    });
  });

  it("resolves system mode from prefers-color-scheme", () => {
    window.localStorage.setItem("notrelix-theme-mode", "system");
    expect(runScript(true).colorClass).toBe("dark");
    resetRoot();
    expect(runScript(false).colorClass).toBe("light");
  });

  it("applies high contrast as light + data-contrast", () => {
    window.localStorage.setItem("notrelix-theme-mode", "high-contrast");
    expect(runScript(true)).toMatchObject({
      colorClass: "light",
      contrast: "high",
    });
  });

  it.each(Object.keys(LEGACY_ACCENT_MIGRATION))(
    "agrees with the runtime on the legacy accent %s",
    (legacy) => {
      window.localStorage.setItem("notrelix-color-theme", legacy);
      window.localStorage.setItem("theme", "dark");
      const { preference } = readThemePreference(browserStorage);
      const expected = themeRootState(
        resolveAppearance(preference.mode, false),
        preference.accent,
      );
      const applied = runScript();
      expect(applied.colorClass).toBe(expected.colorClass);
      expect(applied.contrast).toBe(expected.contrast);
      expect(applied.accent).toBe(expected.accent);
    },
  );

  it("falls back to defaults when storage access throws", () => {
    const descriptor = Object.getOwnPropertyDescriptor(window, "localStorage");
    Object.defineProperty(window, "localStorage", {
      configurable: true,
      get() {
        throw new Error("blocked");
      },
    });
    try {
      expect(runScript()).toMatchObject({
        colorClass: "light",
        accent: "blue",
      });
    } finally {
      if (descriptor) Object.defineProperty(window, "localStorage", descriptor);
    }
  });
});
