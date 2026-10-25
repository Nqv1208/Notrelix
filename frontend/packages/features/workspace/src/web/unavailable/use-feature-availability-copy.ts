import { useLocale } from "@notrelix/i18n";
import {
  featureAvailabilityCopy,
  type FeatureAvailabilityCopy,
} from "./feature-availability.copy";

/** Feature availability copy for the active locale (requires the host `I18nProvider`). */
export function useFeatureAvailabilityCopy(): FeatureAvailabilityCopy {
  return featureAvailabilityCopy(useLocale().locale);
}
