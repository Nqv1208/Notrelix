import type {
  IdentityTone,
  MarkToneId,
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

/** The member's pinned Boards and Docs, joined with the authorized lists. */
export type NavigationListState =
  | { readonly status: "loading" }
  | { readonly status: "error" }
  | { readonly status: "ready"; readonly entries: readonly NavigationEntry[] };

export interface ShellWorkspace {
  readonly id: string;
  readonly name: string;
  /** Initials chip tone (WX-DEC-014), from `markToneFor(id)`. */
  readonly mark: MarkToneId;
}

export interface ShellCurrentWorkspace extends ShellWorkspace {
  /** The member's role in this workspace, when known. */
  readonly role: WorkspaceMember["role"] | null;
  /** What the user card says instead of the role ("Quản trị Phòng Kế toán"). */
  readonly roleLabel?: string | null;
  /** Department Spaces the member can see; `null`/absent = not known. */
  readonly departmentCount?: number | null;
  /** Workspace members; `null`/absent = not known. */
  readonly memberCount?: number | null;
}

/**
 * Sidebar counts (My work, Inbox). `null` = unavailable: the row shows no
 * badge and never a guessed "0".
 */
export interface ShellSidebarCounts {
  readonly myWork: number | null;
  readonly inbox: number | null;
}

/**
 * Group label in the workspace sidebar (Final-Space `.grp`): 12/600 meta text
 * with a 10 px inline margin, 16 px above and 6 px below (4 plus the gap).
 */
export const SIDEBAR_SECTION_LABEL_CLASS =
  "mx-2.5 mt-4 mb-1.5 flex min-h-6 items-center justify-between text-metadata font-semibold text-meta-foreground";

/** Badge text for a count: none for `null` or 0, "99+" above 99. */
export function sidebarCountBadge(count: number | null): string | null {
  if (count === null || !Number.isFinite(count) || count <= 0) return null;
  return count > 99 ? "99+" : String(Math.floor(count));
}

export interface ShellUser {
  readonly id: string;
  readonly name: string;
  readonly email: string | null;
  readonly avatarUrl: string | null;
  readonly identity: IdentityTone;
}

export type ShellPaletteAction =
  | "toggle-theme"
  | "show-shortcuts"
  | "create-workspace"
  | "create-doc"
  | "switch-workspace";

/** One selected palette result. */
export type ShellPaletteSelection =
  | { readonly type: "navigate"; readonly destination: WorkspaceDestination }
  | { readonly type: "action"; readonly action: ShellPaletteAction };

export type ShellAppearance = "light" | "dark";

/**
 * Identity mark classes per tone. Literal strings so Tailwind generates them.
 * Identity colors mark people (avatars) and Board/Doc tiles; workspace and
 * department chips use {@link MARK_TONE_CLASS} (WX-DEC-014). Never status.
 */
export const IDENTITY_MARK_CLASS: Readonly<Record<IdentityTone, string>> = {
  blue: "bg-identity-blue text-identity-blue-foreground",
  coral: "bg-identity-coral text-identity-coral-foreground",
  gold: "bg-identity-gold text-identity-gold-foreground",
  sage: "bg-identity-sage text-identity-sage-foreground",
  ember: "bg-identity-ember text-identity-ember-foreground",
};

/**
 * Mark chip classes per tone for workspace and department (Space) initials
 * (WX-DEC-014). Literal strings so Tailwind generates them. Never status.
 */
export const MARK_TONE_CLASS: Readonly<Record<MarkToneId, string>> = {
  blue: "bg-mark-blue text-mark-blue-foreground",
  gold: "bg-mark-gold text-mark-gold-foreground",
  coral: "bg-mark-coral text-mark-coral-foreground",
  sage: "bg-mark-sage text-mark-sage-foreground",
  crimson: "bg-mark-crimson text-mark-crimson-foreground",
  violet: "bg-mark-violet text-mark-violet-foreground",
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

/** Picks the `_one` form for exactly 1 and `_other` otherwise, then fills it. */
export function pluralCopy(
  forms: { readonly one: string; readonly other: string },
  count: number,
  values: Readonly<Record<string, string | number>> = {},
): string {
  return formatCopy(count === 1 ? forms.one : forms.other, {
    ...values,
    count,
  });
}

/** Workspace setup links a role may open (WX-SHELL-001, GOV role matrix). */
export type WorkspaceSetupLink = "settings" | "members" | "teams" | "billing";

/**
 * Setup links per workspace role: owners and admins see every link, members
 * see Members and Teams, guests and an unknown role see none. The links only
 * shape the UI; every page and write is authorized by the server.
 */
export function workspaceSetupLinks(
  role: WorkspaceMember["role"] | null,
): readonly WorkspaceSetupLink[] {
  switch (role) {
    case "owner":
    case "admin":
      return ["settings", "members", "teams", "billing"];
    case "member":
      return ["members", "teams"];
    default:
      return [];
  }
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
      reports: copy.sections.reports,
      members: copy.sections.members,
      teams: copy.sections.teams,
      settings: copy.sections.settings,
      billing: copy.sections.billing,
      account: copy.sections.account,
    },
    views: copy.views,
    loading: copy.breadcrumb.loading,
    unavailable: copy.breadcrumb.unavailable,
    teamDetails: copy.breadcrumb.teamDetails,
  };
}

/** A document page row of the sidebar's document tree. */
export interface SidebarDocPage {
  readonly id: string;
  readonly title: string;
  readonly children: readonly SidebarDocPage[];
}

/** A department with the document pages placed in it (the current document's). */
export interface SidebarDocsDepartment {
  readonly id: string;
  readonly name: string;
  readonly markTone: MarkToneId;
  readonly pages: readonly SidebarDocPage[];
}

/**
 * The document tree under the Docs row (Final-Docs `.tree`): shown on a
 * document page, for the department that holds it.
 */
export interface SidebarDocsTree {
  readonly departments: readonly SidebarDocsDepartment[];
  /** The open document (the one row drawn as selected). */
  readonly activeDocId: string | null;
  /** Expanded node ids (departments and pages); the host persists them. */
  readonly expandedIds: ReadonlySet<string>;
  readonly onExpandedChange: (id: string, expanded: boolean) => void;
}
