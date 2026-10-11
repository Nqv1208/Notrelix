import { useLocale } from "@notrelix/i18n";
import { boardHeaderCopy, type BoardHeaderCopy } from "./board-header.copy";

/** Board header copy for the active locale (requires the host `I18nProvider`). */
export function useBoardHeaderCopy(): BoardHeaderCopy {
  return boardHeaderCopy(useLocale().locale);
}
