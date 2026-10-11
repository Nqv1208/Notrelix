import type {
  IdentityTone,
  NavigationEntry,
  WorkspaceDestination,
} from "../../core/model/navigation";
import type { WorkspaceBreadcrumbLabels } from "../../core/model/breadcrumb";
import type { WorkspaceMember } from "../../core/types/workspace";
import type { WorkspaceShellCopy } from "./workspace-shell.copy";

/**
 * Host navigation port for pure shell surfaces. Surfaces render real `href`s
 * (open in new tab keeps working) and hand plain clicks to `onNavigate`; the
 * host maps destinations to its router.
 */
export interface ShellNavigation {
  readonly getHref: (destination: WorkspaceDestination) => string;
  readonly onNavigate: (destination: WorkspaceDestination) => void;
}

export type NavigationListState =
  | { readonly status: "loading" }
  | { readonly status: "error" }
  | { readonly status: "ready"; readonly entries: readonly NavigationEntry[] };

export interface ShellWorkspace {
  readonly id: string;
  readonly name: string;
  readonly identity: IdentityTone;
}

export interface ShellCurrentWorkspace extends ShellWorkspace {
  /** The member's role in this workspace, when known. */
  readonly role: WorkspaceMember["role"] | null;
}

export interface ShellUser {
  readonly id: string;
  readonly name: string;
  readonly email: string | null;
  readonly avatarUrl: string | null;
  readonly identity: IdentityTone;
}

export interface ShellPaletteResource {
  readonly id: string;
  readonly name: string;
}

export interface ShellPaletteMember {
  readonly id: string;
  readonly name: string;
  readonly identity: IdentityTone;
  readonly roleLabel: string;
}

export type ShellPaletteAction =
  "toggle-theme" | "show-shortcuts" | "create-workspace";

/** One selected palette result. */
export type ShellPaletteSelection =
  | { readonly type: "navigate"; readonly destination: WorkspaceDestination }
  | { readonly type: "search"; readonly query: string }
  | { readonly type: "action"; readonly action: ShellPaletteAction };

export type ShellAppearance = "light" | "dark";

/**
 * Identity mark classes per tone. Literal strings so Tailwind generates them.
 * Identity colors mark workspaces, people and resources; never status.
 */
export const IDENTITY_MARK_CLASS: Readonly<Record<IdentityTone, string>> = {
  blue: "bg-identity-blue text-identity-blue-foreground",
  coral: "bg-identity-coral text-identity-coral-foreground",
  gold: "bg-identity-gold text-identity-gold-foreground",
  sage: "bg-identity-sage text-identity-sage-foreground",
  ember: "bg-identity-ember text-identity-ember-foreground",
};

/** Identity tone as an icon color on a neutral tile (Boards and Docs). */
export const IDENTITY_ICON_CLASS: Readonly<Record<IdentityTone, string>> = {
  blue: "text-identity-blue",
  coral: "text-identity-coral",
  gold: "text-identity-gold",
  sage: "text-identity-sage",
  ember: "text-identity-ember",
};

/** True for a plain primary click that the host router should handle. */
export function isPlainClick(event: {
  button: number;
  metaKey: boolean;
  ctrlKey: boolean;
  shiftKey: boolean;
  altKey: boolean;
  defaultPrevented: boolean;
}): boolean {
  return (
    !event.defaultPrevented &&
    event.button === 0 &&
    !event.metaKey &&
    !event.ctrlKey &&
    !event.shiftKey &&
    !event.altKey
  );
}

/** Fills `{{name}}` placeholders in catalog copy. Unknown ones stay visible. */
export function formatCopy(
  template: string,
  values: Readonly<Record<string, string | number>>,
): string {
  return template.replace(/\{\{(\w+)\}\}/g, (match, name: string) =>
    name in values ? String(values[name]) : match,
  );
}

/** Breadcrumb labels from the shell copy. */
export function workspaceBreadcrumbLabels(
  copy: WorkspaceShellCopy,
): WorkspaceBreadcrumbLabels {
  return {
    sections: {
      home: copy.sections.home,
      "my-work": copy.sections.myWork,
      inbox: copy.sections.inbox,
      search: copy.sections.search,
      library: copy.sections.library,
      members: copy.sections.members,
      teams: copy.sections.teams,
      settings: copy.sections.settings,
      billing: copy.sections.billing,
      account: copy.sections.account,
    },
    views: copy.views,
    loading: copy.breadcrumb.loading,
    unavailable: copy.breadcrumb.unavailable,
  };
}
