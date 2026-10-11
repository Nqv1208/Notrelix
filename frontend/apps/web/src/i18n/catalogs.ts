import type { MessageCatalog } from "@notrelix/i18n";
import {
  workspaceHomeCatalog,
  workspaceLibraryCatalog,
  workspaceMembersCatalog,
  workspaceSettingsCatalog,
  workspaceShellCatalog,
  workspaceSpacesCatalog,
  workspaceTeamsCatalog,
} from "@notrelix/features-workspace/web";
import {
  boardHeaderCatalog,
  kanbanCardCatalog,
} from "@notrelix/work-management-web";
import { appearanceCatalog } from "./appearance.catalog";

/**
 * Catalogs composed into the web host's I18nProvider. Packages export their
 * own catalogs; the host lists them here (explicit composition, no registry).
 */
export const webCatalogs: readonly MessageCatalog[] = [
  appearanceCatalog,
  workspaceShellCatalog,
  workspaceHomeCatalog,
  workspaceLibraryCatalog,
  workspaceMembersCatalog,
  workspaceSettingsCatalog,
  workspaceSpacesCatalog,
  workspaceTeamsCatalog,
  kanbanCardCatalog,
  boardHeaderCatalog,
];
