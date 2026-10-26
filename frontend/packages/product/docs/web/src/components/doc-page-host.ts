import type { BlockType, Page } from "@notrelix/docs-core";
import type { DocsRealtime } from "@notrelix/docs-collaboration";
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
  /** The viewer's own user id (not shown among the people on the page). */
  readonly viewerId?: string;
  /** Whether the viewer may edit the page. */
  readonly canEdit: boolean;
  /** The block types the producer stores; omitted offers every type. */
  readonly blockTypes?: readonly BlockType[];
  /** Gives a workspace member access to the page; omitted offers no invite. */
  readonly onShareInvite?: (userId: string) => Promise<void>;
  /** Whether a checklist line can become a work item; omitted offers it. */
  readonly canConvertTodo?: boolean;
  /** Boards the viewer may add items to (the convert-to-task dialog); omitted lists none. */
  readonly convertBoards?: readonly {
    readonly id: string;
    readonly title: string;
  }[];
  /** Groups of the board chosen in the convert dialog; `null` while they load. */
  readonly convertGroups?:
    readonly { readonly id: string; readonly name: string }[] | null;
  /** The name of a department (Space) by id, for lists of pages. */
  readonly spaceNameFor?: (spaceId: string) => string | null;
  /** Tells the viewer something that happened outside their action (a toast). */
  readonly notify?: (message: string) => void;
  /** Leaves the open page (after it moved to the trash). */
  readonly leaveDoc?: () => void;
  /** The realtime transport; omitted, the page does not follow others' changes. */
  readonly realtime?: DocsRealtime;
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
  /** The board chosen in the convert-to-task dialog, else `null`. */
  readonly convertBoardId: string | null;
}

export type UseDocPageHost = (input: DocPageHostInput) => DocPageHost;
