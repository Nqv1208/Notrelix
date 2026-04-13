export const routes = {
  home: "/",
  contact: "/contact",
  auth: {
    signIn: "/sign-in",
    register: "/sign-up",
    forgotPassword: "/forgot-password",
    terms: "/terms",
    privacy: "/privacy",
  },
  dashboard: {
    root: "/dashboard",
    search: "/dashboard/search",
    notifications: "/dashboard/notifications",
    calendar: "/dashboard/calendar",
    settings: "/dashboard/settings",
    trash: "/dashboard/trash",
    workspacePage: (workspaceId: string, pageId: string) =>
      `/dashboard/workspace/${workspaceId}/page/${pageId}`,
  },
} as const;
