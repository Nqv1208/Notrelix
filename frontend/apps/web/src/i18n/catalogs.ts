import type { MessageCatalog } from "@notrelix/i18n";
import {
  notrelixSystemRailCatalog,
  workspaceCommandCatalog,
  workspaceHomeCatalog,
  workspaceInboxCatalog,
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
  boardSettingsCatalog,
  boardShareCatalog,
  calendarCatalog,
  itemPageCatalog,
  itemSheetCatalog,
  kanbanCardCatalog,
  kanbanLaneCatalog,
  kanbanToolbarCatalog,
  mainTableCatalog,
  statusOptionFormCatalog,
  timelineCatalog,
} from "@notrelix/work-management-web";
import { routerStatesCatalog } from "../router/router-states.catalog";

/**
 * Catalogs composed into the web host's I18nProvider. Packages export their
 * own catalogs; the host lists them here (explicit composition, no registry).
 */
export const webCatalogs: readonly MessageCatalog[] = [
  workspaceShellCatalog,
  notrelixSystemRailCatalog,
  workspaceCommandCatalog,
  workspaceHomeCatalog,
  workspaceInboxCatalog,
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
  timelineCatalog,
  itemSheetCatalog,
  itemPageCatalog,
  boardHeaderCatalog,
  boardSettingsCatalog,
  boardShareCatalog,
  notificationBellCatalog,
  docPageCatalog,
  accountNavCatalog,
  accountProfileCatalog,
  accountAppearanceCatalog,
  accountSecurityCatalog,
  accountNotificationsCatalog,
  routerStatesCatalog,
];
