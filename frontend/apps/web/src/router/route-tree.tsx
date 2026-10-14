import {
  rootRoute,
  indexRoute,
  signInRoute,
  signUpRoute,
  forgotPasswordRoute,
  homeRoute,
  inviteRoute,
  workspaceRoute,
} from "./routes/base.routes";
import {
  workspaceIndexRoute,
  dashboardRoute,
  settingsRoute,
  membersRoute,
  teamsRoute,
  teamRoute,
  myWorkRoute,
  inboxRoute,
  reportsRoute,
  libraryRoute,
  spaceRoute,
} from "./routes/workspace.routes";
import { boardRoute, boardItemRoute } from "./routes/work-management.routes";
import { docRoute } from "./routes/documents.routes";
import { billingRoute } from "./routes/billing.routes";
import { searchRoute } from "./routes/search.routes";
import { chatRoute } from "./routes/chat.routes";
import {
  accountRoute,
  accountProfileRoute,
  accountSecurityRoute,
  accountAppearanceRoute,
  accountNotificationsRoute,
} from "./routes/account.routes";

export const routeTree = rootRoute.addChildren([
  indexRoute,
  signInRoute,
  signUpRoute,
  forgotPasswordRoute,
  homeRoute,
  inviteRoute,
  workspaceRoute.addChildren([
    workspaceIndexRoute,
    boardRoute,
    boardItemRoute,
    docRoute,
    dashboardRoute,
    settingsRoute,
    membersRoute,
    teamsRoute,
    teamRoute,
    myWorkRoute,
    inboxRoute,
    reportsRoute,
    libraryRoute,
    spaceRoute,
    billingRoute,
    searchRoute,
    chatRoute,
    accountRoute.addChildren([
      accountProfileRoute,
      accountSecurityRoute,
      accountAppearanceRoute,
      accountNotificationsRoute,
    ]),
  ]),
]);
