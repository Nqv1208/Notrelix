import { useLocale } from "@notrelix/i18n";
import {
  itemLifecycleCopy,
  type ItemLifecycleCopy,
} from "./item-lifecycle.copy";

/** Item lifecycle copy for the active locale (requires the host `I18nProvider`). */
export function useItemLifecycleCopy(): ItemLifecycleCopy {
  const { locale } = useLocale();
  return itemLifecycleCopy(locale);
}
