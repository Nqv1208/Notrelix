import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { afterEach, describe, expect, it } from "vitest";
import { themeInitScript } from "@notrelix/ui-web/theme-init-script";

/**
 * The web host inlines the pre-paint theme script in index.html (formatted
 * by Prettier). This proves the inlined copy behaves exactly like the
 * generated `themeInitScript` for every stored state, so the no-flash path
 * cannot drift from the runtime's keys, migration and defaults.
 */

const indexHtml = readFileSync(resolve(__dirname, "../../index.html"), "utf8");

function inlineThemeScript(): string {
  const scripts = [...indexHtml.matchAll(/<script>([\s\S]*?)<\/script>/g)].map(
    (match) => match[1]!,
  );
  const theme = scripts.filter((body) => body.includes("data-accent"));
  expect(theme, "exactly one inline theme script in <head>").toHaveLength(1);
  expect(indexHtml.indexOf(theme[0]!)).toBeLessThan(
    indexHtml.indexOf("</head>"),
  );
  return theme[0]!;
}

function snapshot(script: string, prefersDark: boolean) {
  const root = document.documentElement;
  root.className = "";
  root.removeAttribute("data-contrast");
  root.removeAttribute("data-accent");
  root.removeAttribute("style");
  const original = window.matchMedia;
  window.matchMedia = ((query: string) => ({
    matches: prefersDark && query.includes("dark"),
  })) as unknown as typeof window.matchMedia;
  try {
    new Function(script)();
  } finally {
    window.matchMedia = original;
  }
  return {
    className: root.className,
    contrast: root.getAttribute("data-contrast"),
    accent: root.getAttribute("data-accent"),
    colorScheme: root.style.colorScheme,
  };
}

const STATES: Array<Record<string, string>> = [
  {},
  { "notrelix-theme-mode": "dark", "notrelix-theme-accent": "coral" },
  { "notrelix-theme-mode": "system" },
  { "notrelix-theme-mode": "high-contrast", "notrelix-theme-accent": "amber" },
  { theme: "dark", "notrelix-color-theme": "rose" },
  { "notrelix-ui-theme": "system", "notrelix-color-theme": "editorial" },
  { theme: "light", "notrelix-color-theme": "aurora" },
  { "notrelix-theme-mode": "bogus", "notrelix-theme-accent": "violet" },
];

describe("index.html pre-paint theme script", () => {
  afterEach(() => window.localStorage.clear());

  it.each(
    STATES.flatMap((state) => [
      { state, prefersDark: false },
      { state, prefersDark: true },
    ]),
  )("matches themeInitScript for %j", ({ state, prefersDark }) => {
    const inline = inlineThemeScript();
    window.localStorage.clear();
    for (const [key, value] of Object.entries(state)) {
      window.localStorage.setItem(key, value);
    }
    const expected = snapshot(themeInitScript, prefersDark);
    const actual = snapshot(inline, prefersDark);
    expect(actual).toEqual(expected);
  });

  it("loads no third-party fonts (Be Vietnam Pro and IBM Plex Mono are self-hosted; Poppins removed)", () => {
    expect(indexHtml).not.toMatch(/fonts\.googleapis|fonts\.gstatic/);
    expect(indexHtml).not.toMatch(/Poppins/);
  });
});
