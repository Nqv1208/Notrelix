import { useLocale } from "@notrelix/i18n";
import { billingCopy, type BillingCopy } from "./billing.copy";

/** Billing copy for the active locale (requires the host `I18nProvider`). */
export function useBillingCopy(): BillingCopy {
  return billingCopy(useLocale().locale);
}
