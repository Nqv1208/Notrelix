import { createRouter as createTanStackRouter, createRoute, createRootRoute } from '@tanstack/react-router';

// Import route components
import { SignInPage } from './routes/sign-in';
import { SignUpPage } from './routes/sign-up';
import { ForgotPasswordPage } from './routes/forgot-password';
import { HomePage } from './routes/home';
import { IndexPage } from './routes/index';
import { InvitePage } from './routes/invite/$token';
import { WorkspaceLayout } from './routes/workspaces/$workspaceId/route';
import { WorkspaceHomePage } from './routes/workspaces/$workspaceId/index';
import { BoardPage } from './routes/workspaces/$workspaceId/boards/$boardId';
import { DocPage } from './routes/workspaces/$workspaceId/docs/$docId';
import { DashboardPage } from './routes/workspaces/$workspaceId/dashboard';
import { SettingsPage } from './routes/workspaces/$workspaceId/settings';
import { MembersPage } from './routes/workspaces/$workspaceId/members';
import { RootLayout } from './routes/__root';

// Create routes
const rootRoute = createRootRoute({
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
});

const workspaceIndexRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: '/',
  component: WorkspaceHomePage,
});

const boardRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: '/boards/$boardId',
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
  ]),
]);

export function createRouter() {
  const router = createTanStackRouter({
    routeTree,
    defaultPreload: 'intent',
    scrollRestoration: true,
  });

  return router;
}

export const router = createRouter();
