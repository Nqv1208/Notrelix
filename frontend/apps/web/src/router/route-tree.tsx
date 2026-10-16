import {
  rootRoute,
  indexRoute,
  signInRoute,
  signUpRoute,
  forgotPasswordRoute,
  homeRoute,
  inviteRoute,
  onboardingRoute,
  welcomeRoute,
  workspaceRoute,
} from "./routes/base.routes";
import {
  workspaceIndexRoute,
  dashboardRoute,
  settingsRoute,
  membersRoute,
  teamsRoute,
  teamRoute,
  securityRoute,
  myWorkRoute,
  inboxRoute,
  reportsRoute,
  libraryRoute,
  spaceRoute,
} from "./routes/workspace.routes";
import {
  boardRoute,
  boardItemRoute,
  boardSettingsRoute,
} from "./routes/work-management.routes";
import { docRoute, docsIndexRoute } from "./routes/documents.routes";
import { billingRoute } from "./routes/billing.routes";
import { integrationsRoute } from "./routes/integrations.routes";
import { searchRoute } from "./routes/search.routes";
import { chatRoute } from "./routes/chat.routes";
import {
  accountRoute,
  accountProfileRoute,
  accountSecurityRoute,
  accountAppearanceRoute,
  accountNotificationsRoute,
  accountWorkspacesRoute,
} from "./routes/account.routes";

export const routeTree = rootRoute.addChildren([
  indexRoute,
  signInRoute,
  signUpRoute,
  forgotPasswordRoute,
  homeRoute,
  inviteRoute,
  onboardingRoute,
  welcomeRoute,
  workspaceRoute.addChildren([
    workspaceIndexRoute,
    boardRoute,
    boardItemRoute,
    boardSettingsRoute,
    docRoute,
    docsIndexRoute,
    dashboardRoute,
    settingsRoute,
    membersRoute,
    teamsRoute,
    teamRoute,
    securityRoute,
    myWorkRoute,
    inboxRoute,
    reportsRoute,
    libraryRoute,
    spaceRoute,
    billingRoute,
    integrationsRoute,
    searchRoute,
    chatRoute,
    accountRoute.addChildren([
      accountProfileRoute,
      accountSecurityRoute,
      accountAppearanceRoute,
      accountNotificationsRoute,
      accountWorkspacesRoute,
    ]),
  ]),
]);
