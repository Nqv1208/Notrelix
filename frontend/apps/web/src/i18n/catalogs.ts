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
  workspaceOnboardingCatalog,
  workspaceReportsCatalog,
  workspaceBillingCatalog,
  workspaceIntegrationsCatalog,
  workspaceFeatureAvailabilityCatalog,
} from "@notrelix/features-workspace/web";
import {
  accountNavCatalog,
  accountAppearanceCatalog,
  accountNotificationsCatalog,
  accountWorkspacesCatalog,
  accountProfileCatalog,
  accountSecurityCatalog,
} from "@notrelix/features-account";
import { authCatalog } from "@notrelix/features-auth";
import { notificationBellCatalog } from "@notrelix/features-notifications";
import { docPageCatalog } from "@notrelix/docs-web";
import {
  boardHeaderCatalog,
  itemCreateCatalog,
  boardSettingsCatalog,
  boardShareCatalog,
  calendarCatalog,
  itemPageCatalog,
  itemSheetCatalog,
  itemChecklistsCatalog,
  kanbanCardCatalog,
  kanbanLaneCatalog,
  kanbanToolbarCatalog,
  mainTableCatalog,
  savedFiltersCatalog,
  statusOptionFormCatalog,
  timelineCatalog,
} from "@notrelix/work-management-web";
import { routerStatesCatalog } from "../router/router-states.catalog";
import { emailConfirmationCatalog } from "../shell/workspace/email-confirmation.catalog";

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
  workspaceOnboardingCatalog,
  workspaceReportsCatalog,
  workspaceBillingCatalog,
  workspaceIntegrationsCatalog,
  workspaceFeatureAvailabilityCatalog,
  kanbanCardCatalog,
  kanbanLaneCatalog,
  kanbanToolbarCatalog,
  statusOptionFormCatalog,
  mainTableCatalog,
  savedFiltersCatalog,
  calendarCatalog,
  timelineCatalog,
  itemSheetCatalog,
  itemChecklistsCatalog,
  itemPageCatalog,
  boardHeaderCatalog,
  itemCreateCatalog,
  boardSettingsCatalog,
  boardShareCatalog,
  notificationBellCatalog,
  authCatalog,
  docPageCatalog,
  accountNavCatalog,
  accountProfileCatalog,
  accountAppearanceCatalog,
  accountSecurityCatalog,
  accountNotificationsCatalog,
  accountWorkspacesCatalog,
  routerStatesCatalog,
  emailConfirmationCatalog,
];
