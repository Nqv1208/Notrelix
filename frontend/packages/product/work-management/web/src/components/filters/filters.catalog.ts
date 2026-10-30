import { defineCatalog } from "@notrelix/i18n";
import { en, vi } from "./filters.copy";

/** The `view-state` catalog: filter, sort, group and swimlane copy (completeness checked by `pnpm check:i18n`). */
export const viewStateCatalog = defineCatalog("view-state", { vi, en });
