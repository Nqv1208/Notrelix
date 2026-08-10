import * as React from "react";

export interface ThemeStorage {
  getItem(key: string): string | null;
  setItem(key: string, value: string): void;
  removeItem?(key: string): void;
  clear?(): void;
}

export type KeyValueStorage = ThemeStorage;

type Theme = "dark" | "light" | "system";

interface ThemeProviderProps {
  children: React.ReactNode;
  defaultTheme?: Theme;
  storageKey?: string;
  storage?: ThemeStorage;
}

interface ThemeProviderState {
  theme: Theme;
  setTheme: (theme: Theme) => void;
}

const initialState: ThemeProviderState = {
  theme: "system",
  setTheme: () => null,
};

const ThemeProviderContext =
  React.createContext<ThemeProviderState>(initialState);

export function ThemeProvider({
  children,
  defaultTheme = "system",
  storageKey = "notrelix-ui-theme",
  storage,
  ...props
}: ThemeProviderProps) {
  const [theme, setThemeState] = React.useState<Theme>(
    () =>
      (storage ? (storage.getItem(storageKey) as Theme) : undefined) ||
      defaultTheme,
  );

  React.useEffect(() => {
    if (typeof window === "undefined") return;

    const root = window.document.documentElement;
    const applyTheme = (nextTheme: Theme) => {
      root.classList.remove("light", "dark");

      if (nextTheme === "system") {
        const systemTheme = window.matchMedia("(prefers-color-scheme: dark)")
          .matches
          ? "dark"
          : "light";
        root.classList.add(systemTheme);
        return;
      }

      root.classList.add(nextTheme);
    };

    applyTheme(theme);

    if (theme !== "system") return;

    const media = window.matchMedia("(prefers-color-scheme: dark)");
    const onSystemThemeChange = () => applyTheme("system");
    media.addEventListener("change", onSystemThemeChange);

    return () => {
      media.removeEventListener("change", onSystemThemeChange);
    };
  }, [theme, storageKey]);

  const value = React.useMemo(
    () => ({
      theme,
      setTheme: (newTheme: Theme) => {
        try {
          storage?.setItem(storageKey, newTheme);
        } catch {
          // storage unavailable
        }
        setThemeState(newTheme);
      },
    }),
    [theme, storageKey, storage],
  );

  return (
    <ThemeProviderContext.Provider {...props} value={value}>
      <div {...props}>{children}</div>
    </ThemeProviderContext.Provider>
  );
}

export const useTheme = () => {
  const context = React.useContext(ThemeProviderContext);
  if (context === undefined) {
    return { theme: "system" as Theme, setTheme: () => null };
  }
  return context;
};
