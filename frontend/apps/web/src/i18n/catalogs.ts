import type { MessageCatalog } from "@notrelix/i18n";
import {
  notrelixSystemRailCatalog,
  workspaceHomeCatalog,
  workspaceLibraryCatalog,
  workspaceMembersCatalog,
  workspaceSettingsCatalog,
  workspaceShellCatalog,
  workspaceSpacesCatalog,
  workspaceTeamsCatalog,
} from "@notrelix/features-workspace/web";
import {
  accountNavCatalog,
  accountAppearanceCatalog,
  accountNotificationsCatalog,
  accountProfileCatalog,
  accountSecurityCatalog,
} from "@notrelix/features-account";
import { notificationBellCatalog } from "@notrelix/features-notifications";
import { docPageCatalog } from "@notrelix/docs-web";
import {
  boardHeaderCatalog,
  calendarCatalog,
  itemPageCatalog,
  itemSheetCatalog,
  kanbanCardCatalog,
  kanbanLaneCatalog,
  kanbanToolbarCatalog,
  mainTableCatalog,
  statusOptionFormCatalog,
} from "@notrelix/work-management-web";

/**
 * Catalogs composed into the web host's I18nProvider. Packages export their
 * own catalogs; the host lists them here (explicit composition, no registry).
 */
export const webCatalogs: readonly MessageCatalog[] = [
  workspaceShellCatalog,
  notrelixSystemRailCatalog,
  workspaceHomeCatalog,
  workspaceLibraryCatalog,
  workspaceMembersCatalog,
  workspaceSettingsCatalog,
  workspaceSpacesCatalog,
  workspaceTeamsCatalog,
  kanbanCardCatalog,
  kanbanLaneCatalog,
  kanbanToolbarCatalog,
  statusOptionFormCatalog,
  mainTableCatalog,
  calendarCatalog,
  itemSheetCatalog,
  itemPageCatalog,
  boardHeaderCatalog,
  notificationBellCatalog,
  docPageCatalog,
  accountNavCatalog,
  accountProfileCatalog,
  accountAppearanceCatalog,
  accountSecurityCatalog,
  accountNotificationsCatalog,
];
