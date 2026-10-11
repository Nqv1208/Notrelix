/**
 * Deterministic fixtures for the account appearance page. The theme values
 * are the device preference; only the sidebar preference is loaded data.
 */
import {
  accountAppearanceCopy,
  type AccountAppearanceCopy,
} from "../web/appearance/account-appearance.copy";
import type { AccountAppearanceSurfaceProps } from "../web/appearance/account-appearance-surface";

export function appearanceCopyFixture(
  locale: "en" | "vi" = "en",
): AccountAppearanceCopy {
  return accountAppearanceCopy(locale);
}

export type AppearanceScenario = Omit<
  AccountAppearanceSurfaceProps,
  | "onModeChange"
  | "onAccentChange"
  | "onLocaleChange"
  | "onSidebarCollapsedChange"
  | "onRetryPreferences"
>;

export function appearanceDefaultScenario(
  locale: "en" | "vi" = "en",
): AppearanceScenario {
  return {
    copy: appearanceCopyFixture(locale),
    mode: "light",
    accent: "blue",
    locale,
    sidebar: { status: "ready", data: { collapsed: false } },
    sidebarSave: { state: "idle" },
  };
}

export function appearanceLoadingScenario(
  locale: "en" | "vi" = "en",
): AppearanceScenario {
  return {
    ...appearanceDefaultScenario(locale),
    sidebar: { status: "loading" },
  };
}

export function appearanceErrorScenario(
  locale: "en" | "vi" = "en",
): AppearanceScenario {
  return {
    ...appearanceDefaultScenario(locale),
    sidebar: { status: "error" },
  };
}

export function appearanceSavingScenario(
  locale: "en" | "vi" = "en",
): AppearanceScenario {
  return {
    ...appearanceDefaultScenario(locale),
    sidebarSave: { state: "saving" },
  };
}

export function appearanceSavedScenario(
  locale: "en" | "vi" = "en",
): AppearanceScenario {
  return {
    ...appearanceDefaultScenario(locale),
    sidebar: { status: "ready", data: { collapsed: true } },
    sidebarSave: { state: "saved" },
  };
}

export function appearanceSaveErrorScenario(
  locale: "en" | "vi" = "en",
): AppearanceScenario {
  return {
    ...appearanceDefaultScenario(locale),
    sidebarSave: { state: "error" },
  };
}

/** Vietnamese with the high-contrast mode and the amber accent selected. */
export function appearanceEdgeDataScenario(): AppearanceScenario {
  return {
    ...appearanceDefaultScenario("vi"),
    mode: "high-contrast",
    accent: "amber",
    sidebar: { status: "ready", data: { collapsed: true } },
    sidebarSave: { state: "error" },
  };
}
