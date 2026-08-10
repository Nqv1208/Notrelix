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
  Bell,
  Home,
  Inbox,
  LifeBuoy,
  MessageSquareText,
  Settings,
  UserRoundCheck,
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
  readonly to: string;
}

/** Primary workspace navigation items shown in the top nav section. */
export const PRIMARY_NAV_CONTRIBUTIONS: readonly NavigationContribution[] = [
  {
    id: "nav-home",
    label: "Home",
    icon: Home,
    to: "/workspaces/$workspaceId",
  },
  {
    id: "nav-my-work",
    label: "My Work",
    icon: UserRoundCheck,
    to: "/workspaces/$workspaceId",
  },
  {
    id: "nav-inbox",
    label: "Inbox",
    icon: Inbox,
    to: "/workspaces/$workspaceId",
  },
  {
    id: "nav-notifications",
    label: "Notifications",
    icon: Bell,
    to: "/workspaces/$workspaceId",
  },
  {
    id: "nav-chat-rooms",
    label: "Chat Rooms",
    icon: MessageSquareText,
    to: "/workspaces/$workspaceId",
  },
] as const;

/** Support navigation items shown at the bottom of the sidebar. */
export const SUPPORT_NAV_CONTRIBUTIONS: readonly NavigationContribution[] = [
  {
    id: "nav-help",
    label: "Help / Support",
    icon: LifeBuoy,
    to: "/workspaces/$workspaceId",
  },
  {
    id: "nav-settings",
    label: "Settings",
    icon: Settings,
    to: "/workspaces/$workspaceId/settings",
  },
] as const;
