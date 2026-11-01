import { useLocale } from "@notrelix/i18n";
import {
  boardLifecycleCopy,
  type BoardLifecycleCopy,
} from "./board-lifecycle.copy";

/** Board lifecycle copy for the active locale (requires the host `I18nProvider`). */
export function useBoardLifecycleCopy(): BoardLifecycleCopy {
  return boardLifecycleCopy(useLocale().locale);
}
