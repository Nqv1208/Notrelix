import type { ThemePickerLabels } from "@notrelix/ui-web";

type Translate = (key: string) => string;

/** Maps the appearance catalog onto the ui-web ThemePicker label contract. */
export function themePickerLabels(t: Translate): ThemePickerLabels {
  const option = (key: string) => ({
    label: t(`${key}.label`),
    description: t(`${key}.description`),
  });
  return {
    mode: {
      legend: t("mode.legend"),
      options: {
        light: option("mode.light"),
        dark: option("mode.dark"),
        system: option("mode.system"),
        "high-contrast": option("mode.highContrast"),
      },
    },
    accent: {
      legend: t("accent.legend"),
      options: {
        blue: option("accent.blue"),
        coral: option("accent.coral"),
        ember: option("accent.ember"),
        sage: option("accent.sage"),
        amber: option("accent.amber"),
      },
    },
  };
}
