'use client';

import { useState, useEffect, useCallback } from 'react';

// ── Types ───────────────────────────────────────────────────────────────────

export type ColorTheme =
  | 'default'
  | 'editorial'
  | 'sage'
  | 'ocean'
  | 'sunset'
  | 'midnight'
  | 'rose'
  | 'aurora';

export interface ColorThemeMeta {
  id: ColorTheme;
  name: string;
  description: string;
  /** Hex color for UI preview swatches */
  primaryColor: string;
  /** HSL hue value used by the CSS theme */
  accentHue: number;
}

// ── Constants ───────────────────────────────────────────────────────────────

const STORAGE_KEY = 'notrelix-color-theme';

export const COLOR_THEMES: ColorThemeMeta[] = [
  { id: 'default', name: 'Notrelix', description: 'Bảng màu mặc định', primaryColor: '#7c3aed', accentHue: 285 },
  { id: 'ocean', name: 'Ocean', description: 'Xanh dương đại dương', primaryColor: '#2563eb', accentHue: 240 },
  { id: 'sage', name: 'Sage', description: 'Xanh lá xô thơm', primaryColor: '#16a34a', accentHue: 140 },
  { id: 'editorial', name: 'Editorial', description: 'Nâu ấm cổ điển', primaryColor: '#b45309', accentHue: 45 },
  { id: 'sunset', name: 'Sunset', description: 'Hoàng hôn san hô', primaryColor: '#ea580c', accentHue: 35 },
  { id: 'midnight', name: 'Midnight', description: 'Chàm đêm sâu thẳm', primaryColor: '#6366f1', accentHue: 278 },
  { id: 'rose', name: 'Rose', description: 'Hồng phấn hiện đại', primaryColor: '#e11d48', accentHue: 350 },
  { id: 'aurora', name: 'Aurora', description: 'Ngọc lam công nghệ', primaryColor: '#0d9488', accentHue: 190 },
];

// ── Helpers ─────────────────────────────────────────────────────────────────

function isValidTheme(value: string | null): value is ColorTheme {
  return !!value && COLOR_THEMES.some((t) => t.id === value);
}

function removeThemeClasses(el: HTMLElement) {
  const toRemove = Array.from(el.classList).filter((c) => c.startsWith('theme-'));
  toRemove.forEach((c) => el.classList.remove(c));
}

// ── Hook ────────────────────────────────────────────────────────────────────

/**
 * Manages the active **color** theme (not light/dark mode — that's next-themes).
 *
 * On mount, reads from localStorage. When setting a new theme it:
 * 1. Removes all existing `theme-*` classes from `<html>`
 * 2. Adds the new class (unless 'default')
 * 3. Persists the choice to localStorage
 */
export function useColorTheme() {
  const [colorTheme, setColorThemeState] = useState<ColorTheme>('default');

  // Hydration-safe: read persisted value after mount
  useEffect(() => {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (isValidTheme(stored)) {
        setColorThemeState(stored);
      }
    } catch {
      // localStorage unavailable (SSR / privacy mode)
    }
  }, []);

  const setColorTheme = useCallback((theme: ColorTheme) => {
    setColorThemeState(theme);

    try {
      const root = document.documentElement;
      removeThemeClasses(root);

      if (theme !== 'default') {
        root.classList.add(`theme-${theme}`);
      }

      localStorage.setItem(STORAGE_KEY, theme);
    } catch {
      // localStorage / DOM unavailable
    }
  }, []);

  return { colorTheme, setColorTheme, themes: COLOR_THEMES } as const;
}
