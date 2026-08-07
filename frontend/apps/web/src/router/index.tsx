import { createRouter as createTanStackRouter, createRoute, createRootRouteWithContext } from '@tanstack/react-router';
import { ErrorState, LoadingState, NotFoundState } from '@notrelix/ui-web';
import type { AppRouterContext } from './context';
import { requireWorkspaceId } from './guards/require-workspace-membership';
import { boardSearchSchema } from './board-search-schema';

// Import route components
import { SignInPage } from '../routes/sign-in';
import { SignUpPage } from '../routes/sign-up';
import { ForgotPasswordPage } from '../routes/forgot-password';
import { HomePage } from '../routes/home';
import { IndexPage } from '../routes/index';
import { InvitePage } from '../routes/invite/$token';
import { WorkspaceLayout } from '../routes/workspaces/$workspaceId/route';
import { WorkspaceHomePage } from '../routes/workspaces/$workspaceId/index';
import { BoardPage } from '../routes/workspaces/$workspaceId/boards/$boardId';
import { DocPage } from '../routes/workspaces/$workspaceId/docs/$docId';
import { DashboardPage } from '../routes/workspaces/$workspaceId/dashboard';
import { SettingsPage } from '../routes/workspaces/$workspaceId/settings';
import { MembersPage } from '../routes/workspaces/$workspaceId/members';
import { BillingPage } from '../routes/workspaces/$workspaceId/billing';
import { AccountLayout } from '../routes/workspaces/$workspaceId/account';
import { AccountProfilePage } from '../routes/workspaces/$workspaceId/account/profile';
import { AccountSecurityPage } from '../routes/workspaces/$workspaceId/account/security';
import { AccountAppearancePage } from '../routes/workspaces/$workspaceId/account/appearance';
import { AccountNotificationsPage } from '../routes/workspaces/$workspaceId/account/notifications';
import { SearchResultsPage } from '../routes/workspaces/$workspaceId/search';
import { ChatPage } from '../routes/workspaces/$workspaceId/chat';
import { RootLayout } from '../routes/__root';

// Create routes
const rootRoute = createRootRouteWithContext<AppRouterContext>()({
  component: RootLayout,
});

const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/',
  component: IndexPage,
});

const signInRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/sign-in',
  component: SignInPage,
});

const signUpRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/sign-up',
  component: SignUpPage,
});

const forgotPasswordRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/forgot-password',
  component: ForgotPasswordPage,
});

const homeRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/home',
  component: HomePage,
});

const inviteRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/invite/$token',
  component: InvitePage,
});

const workspaceRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/workspaces/$workspaceId',
  component: WorkspaceLayout,
  beforeLoad: ({ params }) => {
    requireWorkspaceId(params);
  },
});

const workspaceIndexRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: '/',
  component: WorkspaceHomePage,
});

const boardRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: '/boards/$boardId',
  validateSearch: (search) => boardSearchSchema.parse(search),
  component: BoardPage,
});

const docRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: '/docs/$docId',
  component: DocPage,
});

const dashboardRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: '/dashboard',
  component: DashboardPage,
});

const settingsRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: '/settings',
  component: SettingsPage,
});

const membersRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: '/members',
  component: MembersPage,
});

const billingRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: '/billing',
  component: BillingPage,
});

const accountRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: '/account',
  component: AccountLayout,
});

const accountProfileRoute = createRoute({
  getParentRoute: () => accountRoute,
  path: '/profile',
  component: AccountProfilePage,
});

const accountSecurityRoute = createRoute({
  getParentRoute: () => accountRoute,
  path: '/security',
  component: AccountSecurityPage,
});

const accountAppearanceRoute = createRoute({
  getParentRoute: () => accountRoute,
  path: '/appearance',
  component: AccountAppearancePage,
});

const accountNotificationsRoute = createRoute({
  getParentRoute: () => accountRoute,
  path: '/notifications',
  component: AccountNotificationsPage,
});

const searchRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: '/search',
  component: SearchResultsPage,
});

const chatRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: '/chat',
  component: ChatPage,
});

// Build route tree
const routeTree = rootRoute.addChildren([
  indexRoute,
  signInRoute,
  signUpRoute,
  forgotPasswordRoute,
  homeRoute,
  inviteRoute,
  workspaceRoute.addChildren([
    workspaceIndexRoute,
    boardRoute,
    docRoute,
    dashboardRoute,
    settingsRoute,
    membersRoute,
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

export function createRouter() {
  const router = createTanStackRouter({
    routeTree,
    context: undefined as unknown as AppRouterContext,
    defaultPreload: 'intent',
    defaultPendingComponent: () => (
      <LoadingState title="Loading" description="Preparing workspace..." />
    ),
    defaultErrorComponent: ({ error }) => (
      <ErrorState error={error} title="Route error" />
    ),
    defaultNotFoundComponent: () => (
      <NotFoundState title="Page not found" description="The requested route does not exist." />
    ),
    scrollRestoration: true,
  });

  return router;
}

export const router = createRouter();
export { boardSearchSchema };
