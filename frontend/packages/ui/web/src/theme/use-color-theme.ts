import { useState, useEffect, useCallback } from "react";
import type { KeyValueStorage } from "./theme-provider";

export type ColorTheme =
  | "default"
  | "editorial"
  | "sage"
  | "ocean"
  | "sunset"
  | "midnight"
  | "rose"
  | "aurora";

export interface ColorThemeMeta {
  id: ColorTheme;
  name: string;
  description: string;
  /** Hex color for UI preview swatches */
  primaryColor: string;
  /** HSL hue value used by the CSS theme */
  accentHue: number;
}

const STORAGE_KEY = "notrelix-color-theme";

export const COLOR_THEMES: ColorThemeMeta[] = [
  {
    id: "default",
    name: "Notrelix",
    description: "Bảng màu mặc định",
    primaryColor: "#7c3aed",
    accentHue: 285,
  },
  {
    id: "ocean",
    name: "Ocean",
    description: "Xanh dương đại dương",
    primaryColor: "#2563eb",
    accentHue: 240,
  },
  {
    id: "sage",
    name: "Sage",
    description: "Xanh lá xô thơm",
    primaryColor: "#16a34a",
    accentHue: 140,
  },
  {
    id: "editorial",
    name: "Editorial",
    description: "Nâu ấm cổ điển",
    primaryColor: "#b45309",
    accentHue: 45,
  },
  {
    id: "sunset",
    name: "Sunset",
    description: "Hoàng hôn san hô",
    primaryColor: "#ea580c",
    accentHue: 35,
  },
  {
    id: "midnight",
    name: "Midnight",
    description: "Chàm đêm sâu thẳm",
    primaryColor: "#6366f1",
    accentHue: 278,
  },
  {
    id: "rose",
    name: "Rose",
    description: "Hồng phấn hiện đại",
    primaryColor: "#e11d48",
    accentHue: 350,
  },
  {
    id: "aurora",
    name: "Aurora",
    description: "Ngọc lam công nghệ",
    primaryColor: "#0d9488",
    accentHue: 190,
  },
];

function isValidTheme(value: string | null | undefined): value is ColorTheme {
  return !!value && COLOR_THEMES.some((t) => t.id === value);
}

function removeThemeClasses(el: HTMLElement) {
  const toRemove = Array.from(el.classList).filter((c) =>
    c.startsWith("theme-"),
  );
  toRemove.forEach((c) => el.classList.remove(c));
}

export function useColorTheme(storage?: KeyValueStorage) {
  const [colorTheme, setColorThemeState] = useState<ColorTheme>("default");

  useEffect(() => {
    try {
      const stored = storage?.getItem(STORAGE_KEY);
      if (isValidTheme(stored)) {
        setColorThemeState(stored);
      }
    } catch {
      // storage unavailable
    }
  }, [storage]);

  const setColorTheme = useCallback(
    (theme: ColorTheme) => {
      setColorThemeState(theme);

      try {
        if (typeof document !== "undefined") {
          const root = document.documentElement;
          removeThemeClasses(root);

          if (theme !== "default") {
            root.classList.add(`theme-${theme}`);
          }
        }

        storage?.setItem(STORAGE_KEY, theme);
      } catch {
        // storage / DOM unavailable
      }
    },
    [storage],
  );

  return { colorTheme, setColorTheme, themes: COLOR_THEMES } as const;
}
