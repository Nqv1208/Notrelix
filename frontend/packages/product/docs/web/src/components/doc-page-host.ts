import type { Page } from "@notrelix/docs-core";
import type {
  DocPerson,
  DocShareView,
  DocSpaceView,
  DocTaskRef,
  DocTaskResolution,
} from "./doc-page-models";

/**
 * Facts the Documents context does not own, composed by the host app:
 * the department (Workspaces), people (Identity), the tasks a page refers to
 * (Work Management) and who the viewer is. Absent facts hide their part of
 * the page instead of guessing.
 */
export interface DocPageHost {
  /** The department that holds the page; `null` at the workspace level. */
  readonly space: DocSpaceView | null;
  /** A member's initials and colour; unknown people get a neutral entry. */
  readonly personFor: (userId: string) => DocPerson;
  /** Referenced tasks by id; an id the viewer cannot read is restricted. */
  readonly tasks: ReadonlyMap<string, DocTaskResolution>;
  /** Whether the task reads have settled (restricted vs still loading). */
  readonly tasksReady: boolean;
  /** The board the page's task table reads from. */
  readonly board: { readonly name: string; readonly href?: string } | null;
  /** Tasks matching a typed key or title, for the mention list. */
  readonly suggestTasks: (query: string) => readonly DocTaskRef[];
  /** The share dialog's facts; `null` keeps Share disabled. */
  readonly share: DocShareView | null;
  /** Why the viewer sees a private department's page, when they do. */
  readonly viewerReason: "workspace-admin" | null;
  /** Whether the viewer may edit the page. */
  readonly canEdit: boolean;
  /** The workspace's own locale-independent "now" (tests fix the clock). */
  readonly now: Date;
  /** Opens another page (host routing). */
  readonly openPage?: (pageId: string) => void;
  /** Link of a board, from the host router. */
  readonly pageHref?: (pageId: string) => string;
}

export interface DocPageHostInput {
  readonly workspaceId: string;
  readonly pageId: string;
  readonly page: Page | null;
  /** Task ids the page refers to, in reading order. */
  readonly taskIds: readonly string[];
  /** Linked board ids found on the page's blocks. */
  readonly boardIds: readonly string[];
  /** The text after "@" while the mention list is open, else `null`. */
  readonly mentionQuery: string | null;
}

export type UseDocPageHost = (input: DocPageHostInput) => DocPageHost;
