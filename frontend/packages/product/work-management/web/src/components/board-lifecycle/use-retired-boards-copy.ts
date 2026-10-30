import { useLocale } from "@notrelix/i18n";
import {
  retiredBoardsCopy,
  type RetiredBoardsCopy,
} from "./retired-boards.copy";

/** Archived and deleted boards copy for the active locale (requires the host `I18nProvider`). */
export function useRetiredBoardsCopy(): RetiredBoardsCopy {
  return retiredBoardsCopy(useLocale().locale);
}
