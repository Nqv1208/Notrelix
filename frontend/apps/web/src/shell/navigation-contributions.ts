/**
 * navigation-contributions.ts
 *
 * Shell navigation metadata registry.
 * Provides deterministic, data-only navigation contribution descriptors
 * consumed by WorkspaceSidebar and other shell components.
 *
 * Rules (08-TYPE-SAFE-ROUTER-CLOSURE-SPEC.md):
 *  - Navigation metadata only. Does not own route construction.
 *  - ROUTE-007: contribution IDs must be unique.
 */

import {
  CreditCard,
  LayoutDashboard,
  MessageSquareText,
  Settings,
  Users,
} from "lucide-react";
import type { LucideIcon } from "lucide-react";

export interface NavigationContribution {
  /** Unique stable ID for the navigation item. */
  readonly id: string;
  readonly label: string;
  readonly icon: LucideIcon;
  /**
   * TanStack Router route path (template, not resolved).
   * WorkspaceSidebar resolves `$workspaceId` from context.
   */
  readonly to: WorkspaceNavigationPath;
}

export type WorkspaceNavigationPath =
  | "/workspaces/$workspaceId/dashboard"
  | "/workspaces/$workspaceId/chat"
  | "/workspaces/$workspaceId/members"
  | "/workspaces/$workspaceId/billing"
  | "/workspaces/$workspaceId/settings";

export function resolveWorkspaceNavigationPath(
  item: NavigationContribution,
  workspaceId: string,
): string {
  return item.to.replace("$workspaceId", workspaceId);
}

export function isWorkspaceNavigationItemActive(
  item: NavigationContribution,
  pathname: string,
  workspaceId: string,
): boolean {
  const target = resolveWorkspaceNavigationPath(item, workspaceId);

  return pathname === target || pathname.startsWith(`${target}/`);
}

/** Primary workspace navigation items with independently addressable routes. */
export const PRIMARY_NAV_CONTRIBUTIONS: readonly NavigationContribution[] = [
  {
    id: "nav-overview",
    label: "Overview",
    icon: LayoutDashboard,
    to: "/workspaces/$workspaceId/dashboard",
  },
  {
    id: "nav-chat",
    label: "Chat",
    icon: MessageSquareText,
    to: "/workspaces/$workspaceId/chat",
  },
] as const;

/** Workspace administration items shown below primary navigation. */
export const WORKSPACE_NAV_CONTRIBUTIONS: readonly NavigationContribution[] = [
  {
    id: "nav-members",
    label: "Members",
    icon: Users,
    to: "/workspaces/$workspaceId/members",
  },
  {
    id: "nav-billing",
    label: "Billing",
    icon: CreditCard,
    to: "/workspaces/$workspaceId/billing",
  },
];

/** Support navigation items shown at the bottom of the sidebar. */
export const SUPPORT_NAV_CONTRIBUTIONS: readonly NavigationContribution[] = [
  {
    id: "nav-settings",
    label: "Settings",
    icon: Settings,
    to: "/workspaces/$workspaceId/settings",
  },
] as const;
