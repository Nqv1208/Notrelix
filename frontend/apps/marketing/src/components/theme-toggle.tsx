"use client";

import * as React from "react";
import { Moon, Sun } from "lucide-react";
import { useTranslations } from "next-intl";
import { Button } from "@notrelix/ui-web/components/ui/button";

function applyTheme(theme: "light" | "dark", persist = true) {
  const root = document.documentElement;
  root.classList.remove("light", "dark");
  root.classList.add(theme);
  root.style.colorScheme = theme;
  if (persist) {
    localStorage.setItem("theme", theme);
  }
}

export function ThemeToggle() {
  const t = useTranslations("themeToggle");
  const [mounted, setMounted] = React.useState(false);
  const [theme, setThemeState] = React.useState<"light" | "dark">("light");

  React.useEffect(() => {
    const rootTheme = document.documentElement.classList.contains("dark")
      ? ("dark" as const)
      : ("light" as const);
    setThemeState(rootTheme);
    setMounted(true);
  }, []);

  const toggleTheme = () => {
    const next: "light" | "dark" = theme === "dark" ? "light" : "dark";
    const root = document.documentElement;
    root.setAttribute("data-theme-changing", "true");
    window.setTimeout(() => {
      root.removeAttribute("data-theme-changing");
    }, 320);
    applyTheme(next);
    setThemeState(next);
  };

  if (!mounted) {
    return (
      <Button
        variant="ghost"
        size="icon"
        className="rounded-full"
        aria-label={t("switch")}
      >
        <span className="w-4 h-4" />
      </Button>
    );
  }

  return (
    <Button
      variant="ghost"
      size="icon"
      onClick={toggleTheme}
      className="rounded-full"
      aria-label={theme === "dark" ? t("switchToLight") : t("switchToDark")}
      title={theme === "dark" ? t("light") : t("dark")}
    >
      {theme === "dark" ? (
        <Sun className="w-4 h-4" />
      ) : (
        <Moon className="w-4 h-4" />
      )}
    </Button>
  );
}
