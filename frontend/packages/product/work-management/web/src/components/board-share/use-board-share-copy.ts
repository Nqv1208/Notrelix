import { useLocale } from "@notrelix/i18n";
import { boardShareCopy, type BoardShareCopy } from "./board-share.copy";

/** Board share copy for the active locale (requires the host `I18nProvider`). */
export function useBoardShareCopy(): BoardShareCopy {
  return boardShareCopy(useLocale().locale);
}
