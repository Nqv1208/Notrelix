import { useLocale } from "@notrelix/i18n";
import { onboardingCopy, type OnboardingCopy } from "./onboarding.copy";

/** Onboarding copy for the active locale (requires the host `I18nProvider`). */
export function useOnboardingCopy(): OnboardingCopy {
  return onboardingCopy(useLocale().locale);
}
