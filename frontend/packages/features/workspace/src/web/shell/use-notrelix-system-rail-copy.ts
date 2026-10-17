import { useLocale } from "@notrelix/i18n";
import {
  notrelixSystemRailCopy,
  type NotrelixSystemRailCopy,
} from "./notrelix-system-rail.copy";

/** System rail copy for the active locale (requires the host `I18nProvider`). */
export function useNotrelixSystemRailCopy(): NotrelixSystemRailCopy {
  return notrelixSystemRailCopy(useLocale().locale);
}
