import type { MessageCatalog } from "@notrelix/i18n";
import { appearanceCatalog } from "./appearance.catalog";

/**
 * Catalogs composed into the web host's I18nProvider. Packages export their
 * own catalogs; the host lists them here (explicit composition, no registry).
 */
export const webCatalogs: readonly MessageCatalog[] = [appearanceCatalog];
