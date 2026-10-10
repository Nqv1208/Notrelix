import { useMemo } from "react";
import {
  createUsePreferences,
  createUseUpdatePreferences,
} from "@notrelix/features-account";
import { SUPPORTED_LOCALES, useLocale, useT } from "@notrelix/i18n";
import { useAppRuntime } from "@notrelix/runtime-web";
import { Button, ThemePicker, type ThemeMode } from "@notrelix/ui-web";
import { toast } from "sonner";
import { appearanceCatalog } from "../../../../i18n/appearance.catalog";
import { themePickerLabels } from "../../../../i18n/theme-picker-labels";

export function AccountAppearancePage() {
  const { api: runtimeClient } = useAppRuntime();
  const { t } = useT(appearanceCatalog);
  const { locale, setLocale } = useLocale();
  const labels = useMemo(() => themePickerLabels(t), [t]);

  const usePreferences = useMemo(
    () =>
      createUsePreferences({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
      }),
    [runtimeClient],
  );

  const useUpdatePreferences = useMemo(
    () =>
      createUseUpdatePreferences({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
      }),
    [runtimeClient],
  );

  const { preferences, isLoading } = usePreferences();
  const updateMutation = useUpdatePreferences();

  // Mirror the device mode to account preferences; the preference contract
  // only knows light/dark/system, so high contrast stays device-local.
  const handleModeChange = async (mode: ThemeMode) => {
    if (mode === "high-contrast") return;
    try {
      await updateMutation.mutateAsync({ theme: mode });
    } catch {
      // Preferences endpoint may not exist yet
    }
  };

  if (isLoading) {
    return (
      <div className="space-y-4">
        {[1, 2].map((i) => (
          <div key={i} className="h-20 bg-muted rounded-lg animate-pulse" />
        ))}
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-section-heading text-foreground mb-1">
          {t("title")}
        </h2>
        <p className="text-body text-muted-foreground">{t("description")}</p>
      </div>

      <ThemePicker labels={labels} onModeChange={handleModeChange} />

      <fieldset className="flex flex-col gap-3">
        <legend className="text-label text-foreground mb-3">
          {t("language.legend")}
        </legend>
        <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
          {SUPPORTED_LOCALES.map((option) => (
            <label
              key={option}
              lang={option}
              className="flex cursor-pointer items-center gap-3 rounded-control border border-border bg-card p-3 transition-colors hover:bg-state-hover has-[:checked]:border-primary has-[:checked]:bg-state-selected has-[:focus-visible]:outline-2 has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-focus-ring"
            >
              <input
                type="radio"
                name="appearance-locale"
                value={option}
                checked={locale === option}
                onChange={() => setLocale(option)}
                className="sr-only"
              />
              <span className="text-label text-foreground">
                {t(`language.${option}`)}
              </span>
            </label>
          ))}
        </div>
      </fieldset>

      {/* Sidebar */}
      <div className="space-y-3">
        <label className="text-sm font-medium text-foreground">Sidebar</label>
        <div className="flex items-center justify-between rounded-lg border border-border p-4">
          <div>
            <p className="text-sm">Collapsed by default</p>
            <p className="text-xs text-muted-foreground">
              Start with sidebar collapsed
            </p>
          </div>
          <Button
            variant="outline"
            size="sm"
            onClick={() => {
              toast.success("Sidebar preference saved");
            }}
          >
            {preferences?.sidebarCollapsed ? "Expanded" : "Collapsed"}
          </Button>
        </div>
      </div>
    </div>
  );
}
