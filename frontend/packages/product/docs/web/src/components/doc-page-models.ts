/**
 * View models of the document page (`Final-Docs.dc.html`). The container maps
 * Documents state (page, blocks, comments, history) and the host-composed
 * facts (department, people, referenced tasks) into these; the pure surfaces
 * only render them.
 */
import type { BlockType, PageStatus } from "@notrelix/docs-core";

/** The department/identity colours of the design (one fill in every appearance). */
export type DocTone = "blue" | "gold" | "coral" | "sage" | "crimson" | "violet";

export interface DocPerson {
  readonly id: string;
  readonly name: string;
  readonly initials: string;
  readonly tone: DocTone;
}

export type DocTaskStatus = "todo" | "doing" | "review" | "done";

/** How a task's due date reads next to it (the container does the date math). */
export type DocTaskDue =
  | { readonly kind: "overdue"; readonly days: number }
  | { readonly kind: "today" }
  | { readonly kind: "date"; readonly label: string }
  | { readonly kind: "done"; readonly label: string };

export interface DocTaskRef {
  readonly id: string;
  /** Item key such as `ERP-144`. */
  readonly key: string;
  readonly title: string;
  readonly status: DocTaskStatus;
  readonly assignee: DocPerson | null;
  readonly due: DocTaskDue | null;
  readonly href?: string;
}

/** A referenced task the viewer cannot read (never says whether it exists). */
export interface DocTaskRestricted {
  readonly id: string;
  readonly restricted: true;
}

export type DocTaskResolution = DocTaskRef | DocTaskRestricted;

export function isRestrictedTask(
  task: DocTaskResolution,
): task is DocTaskRestricted {
  return "restricted" in task;
}

/** Inline content of a text block. */
export type DocSegment =
  | { readonly kind: "text"; readonly text: string }
  | { readonly kind: "task"; readonly task: DocTaskResolution }
  /** Text a comment is anchored to (yellow mark). */
  | { readonly kind: "mark"; readonly text: string }
  /** Version compare: text added since the viewed version / removed since. */
  | { readonly kind: "ins"; readonly text: string }
  | { readonly kind: "del"; readonly text: string };

export type DocBlockFormat = {
  readonly bold?: boolean;
  readonly italic?: boolean;
  readonly underline?: boolean;
  readonly strike?: boolean;
  readonly align?: "left" | "center" | "right";
};

interface DocBlockBase {
  readonly id: string;
  /** Rows only present in the version being compared (green). */
  readonly inserted?: boolean;
}

/** A block of inline text: a paragraph, a list item or a quote. */
export interface DocTextBlock extends DocBlockBase {
  readonly kind: "paragraph" | "numbered" | "bulleted" | "quote";
  readonly type: BlockType;
  /** Segments as stored (text and task references). */
  readonly segments: readonly DocSegment[];
  readonly format?: DocBlockFormat;
}

export type DocBlockView =
  | (DocBlockBase & {
      readonly kind: "heading";
      readonly level: 1 | 2 | 3;
      readonly text: string;
      readonly format?: DocBlockFormat;
    })
  | DocTextBlock
  | (DocBlockBase & {
      readonly kind: "todo";
      readonly text: string;
      readonly checked: boolean;
      /** The work item the line was turned into (no more "Chuyển thành việc"). */
      readonly linkedTaskId?: string;
    })
  | (DocBlockBase & {
      readonly kind: "callout";
      readonly text: string;
    })
  | (DocBlockBase & {
      readonly kind: "code";
      readonly text: string;
    })
  | (DocBlockBase & { readonly kind: "divider" })
  | (DocBlockBase & {
      readonly kind: "liveTable";
      readonly boardName: string;
      readonly filterLabel: string;
      readonly boardHref?: string;
      readonly tasks: readonly DocTaskResolution[];
    });

/** The viewer-facing facts of the document's department (Space). */
export interface DocSpaceView {
  readonly id: string;
  readonly name: string;
  readonly mark: string;
  readonly tone: DocTone;
  readonly visibility: "private" | "workspace";
  /** A top-level department (named "Phòng …"), not a group inside one. */
  readonly department?: boolean;
}

export interface DocChip {
  readonly id: string;
  readonly kind: "scope" | "date" | "board";
  readonly label: string;
  readonly href?: string;
}

export interface DocHeaderView {
  readonly title: string;
  readonly crumbs: readonly string[];
  readonly status: PageStatus;
  /** "Đỗ Khánh Linh sửa lúc 16:40 hôm qua" parts. */
  readonly editedBy: string | null;
  readonly editedAt: string | null;
  readonly presence: readonly DocPerson[];
}

export type DocSaveState = "saved" | "saving" | "error";

export interface DocTocEntry {
  readonly id: string;
  readonly level: 1 | 2;
  readonly text: string;
  readonly active?: boolean;
}

export interface DocCommentView {
  readonly id: string;
  readonly author: DocPerson;
  /** Text the comment is anchored to, when it carries one. */
  readonly quote: string | null;
  readonly body: string;
  readonly when: string;
}

export interface DocVersionView {
  readonly id: string;
  readonly author: DocPerson;
  readonly time: string;
  /** Day group heading ("Hôm qua", "10/10"). */
  readonly day: string;
  /** Full stamp for the compare bar ("10/10 15:42"). */
  readonly stamp: string;
  readonly name?: string;
  readonly summary: string;
  readonly current?: boolean;
}

export interface DocRelatedDoc {
  readonly id: string;
  readonly kind: "doc" | "task";
  readonly title: string;
  readonly detail: string;
  readonly href?: string;
}

export interface DocShareMember {
  readonly id: string;
  readonly kind: "person" | "team";
  readonly person?: DocPerson;
  readonly name: string;
  readonly detail: string;
  readonly outside?: boolean;
  readonly you?: boolean;
  /** The member who created the page (shows the creator label). */
  readonly creator?: boolean;
  readonly level: "manage" | "edit" | "comment" | "view";
  /** The page creator's level is fixed and not a menu. */
  readonly fixed?: boolean;
}

export interface DocShareView {
  readonly space: DocSpaceView;
  readonly spaceMemberCount: number | null;
  readonly members: readonly DocShareMember[];
}

export type DocPanelTab = "links" | "toc" | "comments";

export type DocOverlay =
  | { readonly kind: "none" }
  | { readonly kind: "fileMenu"; readonly downloadOpen?: boolean }
  | { readonly kind: "share" }
  | {
      readonly kind: "slash";
      readonly blockId: string;
      readonly query: string;
      /** The text typed in the block so far. */
      readonly text: string;
      readonly before: string;
    }
  | {
      readonly kind: "mention";
      readonly blockId: string;
      readonly query: string;
      readonly text: string;
      /** The text before the caret, where the "@query" ends. */
      readonly before: string;
    };

export interface DocPageCallbacks {
  readonly onTitleCommit?: (title: string) => void;
  readonly onBlockCommit?: (blockId: string, text: string) => void;
  readonly onBlockTrigger?: (
    blockId: string,
    trigger: {
      readonly kind: "slash" | "mention";
      readonly query: string;
      /** The stored text of the block and the part of it before the caret. */
      readonly text: string;
      readonly before: string;
    },
  ) => void;
  readonly onBlockTriggerClose?: () => void;
  readonly onTodoToggle?: (blockId: string, checked: boolean) => void;
  readonly onTodoConvert?: (blockId: string) => void;
  readonly onBlockDeleteEmpty?: (blockId: string) => void;
  readonly onSlashSelect?: (blockId: string, type: BlockType) => void;
  readonly onTocSelect?: (id: string) => void;
  readonly onMentionSelect?: (blockId: string, taskId: string) => void;
  readonly onFocusBlock?: (blockId: string | null) => void;
  readonly onOpenFileMenu?: () => void;
  readonly onNewDoc?: () => void;
  readonly onToggleDownload?: () => void;
  readonly onDownload?: (format: "pdf" | "markdown") => void;
  readonly onCloseOverlay?: () => void;
  readonly onOpenShare?: () => void;
  readonly onPanelTab?: (tab: DocPanelTab) => void;
  readonly onSaveRetry?: () => void;
  readonly onFormat?: (
    format:
      "bold" | "italic" | "underline" | "strike" | "left" | "center" | "right",
  ) => void;
  readonly onParagraphStyle?: (type: BlockType) => void;
  readonly onPrint?: () => void;
  readonly onOpenVersions?: () => void;
  readonly onCloseVersions?: () => void;
  readonly onSelectVersion?: (id: string) => void;
  readonly onCommentCreate?: (body: string) => void;
  readonly onCommentDelete?: (id: string) => void;
  readonly onMentionInsert?: () => void;
  readonly onOpenComments?: () => void;
  readonly onAddBlock?: (type: BlockType) => void;
}
