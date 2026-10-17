import { useLocale } from "@notrelix/i18n";
import {
  boardSettingsCopy,
  type BoardSettingsCopy,
} from "./board-settings.copy";

/** Board settings copy for the active locale (requires the host `I18nProvider`). */
export function useBoardSettingsCopy(): BoardSettingsCopy {
  return boardSettingsCopy(useLocale().locale);
}
