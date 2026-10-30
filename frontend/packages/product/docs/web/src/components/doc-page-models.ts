/**
 * View models of the document page (`Final-Docs.dc.html`). The container maps
 * Documents state (page, blocks, comments, history) and the host-composed
 * facts (department, people, referenced tasks) into these; the pure surfaces
 * only render them.
 */
import type { BlockType, Mark } from "@notrelix/docs-core";

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
  | {
      readonly kind: "text";
      readonly text: string;
      readonly marks?: readonly Mark[];
    }
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
  readonly align?: "left" | "center" | "right" | "justify";
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
  /** The page's headings as links, in reading order. */
  | (DocBlockBase & {
      readonly kind: "tableOfContents";
      readonly entries: readonly {
        readonly id: string;
        readonly level: 1 | 2 | 3;
        readonly text: string;
      }[];
    })
  /** A link to another page; `title` is null while none is chosen or when it cannot be read. */
  | (DocBlockBase & {
      readonly kind: "pageReference";
      readonly pageId: string | null;
      readonly title: string | null;
      readonly icon: string | null;
      readonly href?: string;
      /** A page is named but the viewer cannot read it (never says whether it exists). */
      readonly unavailable: boolean;
    })
  | (DocBlockBase & {
      readonly kind: "liveTable";
      readonly boardId: string | null;
      /**
       * `unset`: no board chosen. `loading`: the board read is pending.
       * `unavailable`: the viewer cannot read the board (never says whether
       * it exists). `ready`: the board's own facts follow.
       */
      readonly state: "unset" | "loading" | "ready" | "unavailable";
      readonly boardName: string;
      readonly filterLabel: string;
      readonly boardHref?: string;
      /** Items on the board by status, for the compact summary. */
      readonly counts?: Readonly<Record<DocTaskStatus, number>>;
      readonly total?: number;
      readonly tasks: readonly DocTaskResolution[];
    });

/** What the host knows of an embedded board, read with the viewer's own access. */
export interface DocBoardEmbed {
  readonly state: "loading" | "ready" | "unavailable";
  readonly name: string;
  readonly href?: string;
  readonly counts: Readonly<Record<DocTaskStatus, number>>;
  readonly total: number;
  /** The first few items, in board order. */
  readonly tasks: readonly DocTaskRef[];
}

/** A board the picker offers for a board reference. */
export interface DocPickerBoardView {
  readonly id: string;
  readonly title: string;
}

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
  /** "Đỗ Khánh Linh sửa lúc 16:40 hôm qua" parts. */
  readonly editedBy: string | null;
  readonly editedAt: string | null;
  readonly presence: readonly DocPerson[];
}

export type DocSaveState = "saved" | "saving" | "error";

export interface DocTocEntry {
  readonly id: string;
  readonly level: 1 | 2 | 3 | 4;
  readonly text: string;
  readonly active?: boolean;
}

/** What a mention chip or a page reference in the text points at, from the host. */
export interface DocMarkTargets {
  /** The member behind a mention (a neutral entry when not known). */
  readonly person: (userId: string) => DocPerson;
  /** The page when the viewer can read it; `null` reads as unavailable. */
  readonly page: (pageId: string) => { readonly title: string } | null;
  readonly openPage?: (pageId: string) => void;
}

export interface DocCommentView {
  readonly id: string;
  readonly author: DocPerson;
  /** Text the comment is anchored to, when it carries one. */
  readonly quote: string | null;
  /** The quote is a stored text anchor (it can be revealed in the page). */
  readonly anchored?: boolean;
  /** The anchored text is no longer on the page. */
  readonly detached?: boolean;
  /** The thread being looked at (clicked in the text or in the panel). */
  readonly active?: boolean;
  readonly body: string;
  readonly when: string;
  /** The thread was marked resolved; its text is no longer highlighted. */
  readonly resolved?: boolean;
  /** The viewer wrote it (only the author is offered Delete). */
  readonly mine?: boolean;
  /** The viewer started the thread or may edit the page: only they resolve or reopen it. */
  readonly canResolve?: boolean;
}

export interface DocVersionView {
  readonly id: string;
  /** Why the entry exists; names it when it has no name of its own. */
  readonly kind: "automatic" | "named" | "restore";
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
  /**
   * Workspace members the viewer may give access to; omitted keeps the invite
   * field disabled (the viewer cannot manage the page's permissions).
   */
  readonly candidates?: readonly DocPerson[];
}

/** A trashed page in the trash list. */
export interface DocTrashPageView {
  readonly id: string;
  readonly title: string;
  readonly icon: string | null;
}

export interface DocTrashView {
  readonly pages: readonly DocTrashPageView[];
  readonly loading: boolean;
  /** A restore or delete failed. */
  readonly failed: boolean;
  /** The page a write is running on. */
  readonly busyId: string | null;
  /** The list itself could not be read. */
  readonly listFailed: boolean;
  /** More trashed pages can be read. */
  readonly hasMore: boolean;
  /** The next page of the list is being read. */
  readonly loadingMore: boolean;
}

/** The confirmation before a page with pages under it goes to the trash. */
export interface DocTrashConfirmView {
  /** Pages under the open page that go with it. */
  readonly below: number;
  readonly busy: boolean;
  readonly failed: boolean;
}

/** A page the picker offers for a page reference. */
export interface DocPickerPageView {
  readonly id: string;
  readonly title: string;
  readonly icon: string | null;
  /** The department it sits in. */
  readonly detail: string | null;
}

/** The convert-to-task dialog: boards the viewer can add to and the chosen board's groups. */
export interface DocConvertView {
  readonly boards: readonly { readonly id: string; readonly title: string }[];
  /** The groups of the chosen board; `null` while they load. */
  readonly groups:
    readonly { readonly id: string; readonly name: string }[] | null;
  readonly boardId: string | null;
  readonly lineText: string;
  readonly busy: boolean;
  readonly failed: boolean;
}

/** What the history dialogs show while a write runs or after it failed. */
export interface DocHistoryActionView {
  /** The compared version can be restored (the viewer may edit). */
  readonly canRestore: boolean;
  readonly saving: boolean;
  readonly saveFailed: boolean;
  readonly restoring: boolean;
  /** `conflict` = the page changed since it was read (nothing was restored). */
  readonly restoreFailed: "conflict" | "failed" | null;
}

/** The "Save as template" dialog's write state. */
export interface DocTemplateActionView {
  readonly saving: boolean;
  readonly failed: boolean;
}

/** The import dialog's state: reading and cleaning the file, then creating the page. */
export interface DocImportView {
  readonly busy: boolean;
  /** Why the chosen file was refused or could not be imported. */
  readonly error: "unsupported" | "tooLarge" | "empty" | "failed" | null;
  readonly fileName: string | null;
  /** Images of a Word file that were not imported. */
  readonly imagesDropped: number;
}

export type DocPanelTab = "links" | "toc" | "comments";

export type DocOverlay =
  | { readonly kind: "none" }
  | { readonly kind: "fileMenu"; readonly downloadOpen?: boolean }
  | { readonly kind: "share" }
  | { readonly kind: "trash" }
  | { readonly kind: "trashPage"; readonly below: number }
  | { readonly kind: "saveVersion" }
  | { readonly kind: "restoreVersion" }
  | { readonly kind: "saveTemplate" }
  | { readonly kind: "importPage" }
  | { readonly kind: "shortcuts" }
  | { readonly kind: "wordCount" }
  | { readonly kind: "pageInfo" }
  | { readonly kind: "pageSetup" }
  | { readonly kind: "pagePicker"; readonly blockId: string }
  | { readonly kind: "boardPicker"; readonly blockId: string }
  | {
      readonly kind: "convertTask";
      readonly blockId: string;
      readonly boardId: string | null;
    }
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

/** A member's edit a stored change replaced, kept until they use or drop it. */
export interface DocRecoveryView {
  readonly id: string;
  /** The text the member had in the block (empty when they removed it). */
  readonly text: string;
}

export interface DocPageCallbacks {
  readonly onRecoveryCopy?: (id: string) => void;
  readonly onRecoveryReapply?: (id: string) => void;
  readonly onRecoveryDismiss?: (id: string) => void;
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
  /** Opens the convert-to-task dialog for a checklist line. */
  readonly onTodoConvert?: (blockId: string) => void;
  readonly onConvertBoard?: (boardId: string) => void;
  readonly onConvertConfirm?: (
    blockId: string,
    target: { readonly boardId: string; readonly groupId: string },
  ) => void;
  /** Moves the open page to the trash (asks first when pages sit under it). */
  readonly onTrashPage?: () => void;
  /** The person confirmed moving the open page and the pages under it. */
  readonly onConfirmTrashPage?: () => void;
  readonly onOpenTrash?: () => void;
  readonly onLoadMoreTrash?: () => void;
  readonly onRetryTrash?: () => void;
  readonly onRestorePage?: (pageId: string) => void;
  readonly onDeletePage?: (pageId: string) => void;
  readonly onOpenPagePicker?: (blockId: string) => void;
  readonly onPickPage?: (blockId: string, pageId: string) => void;
  readonly onOpenBoardPicker?: (blockId: string) => void;
  readonly onPickBoard?: (blockId: string, boardId: string) => void;
  readonly onBlockDeleteEmpty?: (blockId: string) => void;
  readonly onSlashSelect?: (blockId: string, type: BlockType) => void;
  readonly onTocSelect?: (id: string) => void;
  readonly onMentionSelect?: (blockId: string, taskId: string) => void;
  readonly onFocusBlock?: (blockId: string | null) => void;
  readonly onOpenFileMenu?: () => void;
  readonly onNewDoc?: () => void;
  readonly onToggleDownload?: () => void;
  readonly onDownload?: (format: "pdf" | "markdown" | "html") => void;
  readonly onCloseOverlay?: () => void;
  readonly onOpenShare?: () => void;
  readonly onPanelTab?: (tab: DocPanelTab) => void;
  /** Opens or closes the side panel (the host remembers the choice). */
  readonly onPanelOpenChange?: (open: boolean) => void;
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
  /** "Save version": opens the name dialog. */
  readonly onSaveVersionOpen?: () => void;
  readonly onSaveVersion?: (name: string) => void;
  /** Asks to restore the compared version (the dialog confirms). */
  readonly onRestoreVersionOpen?: () => void;
  readonly onRestoreVersion?: () => void;
  readonly onSaveTemplateOpen?: () => void;
  readonly onSaveTemplate?: (name: string, description: string) => void;
  readonly onImportOpen?: () => void;
  readonly onImportFile?: (file: File) => void;
  readonly onCommentCreate?: (body: string) => void;
  /** Starts a comment on the selected text (the panel then asks for the words). */
  readonly onCommentStart?: () => void;
  /** Drops the selection a comment was being written on. */
  readonly onCommentCancel?: () => void;
  /** A comment was chosen in the panel: show its text on the page. */
  readonly onCommentSelect?: (id: string) => void;
  /** Highlighted text was clicked: show its thread in the panel. */
  readonly onOpenComment?: (id: string) => void;
  readonly onCommentDelete?: (id: string) => void;
  /** Marks a comment thread resolved. */
  readonly onCommentResolve?: (id: string) => void;
  /** Makes a resolved comment thread active again. */
  readonly onCommentReopen?: (id: string) => void;
  readonly onMentionInsert?: () => void;
  /** A page-level shortcut or menu item that opens something outside the text. */
  readonly onEditorCommand?: (
    command: "link" | "comment" | "wordCount" | "shortcuts" | "focusMode",
  ) => void;
  /** Makes a copy of the page (its text blocks; images and files stay behind). */
  readonly onDuplicatePage?: () => void;
  /** Moves the caret to the page title so it can be renamed. */
  readonly onRenamePage?: () => void;
  /** Cut, copy or paste for the Edit menu (the container owns the clipboard). */
  readonly onClipboardAction?: (
    action: "cut" | "copy" | "paste" | "pastePlain",
  ) => void;
  /** Shows a short message to the member (a toast). */
  readonly onNotice?: (message: string) => void;
  readonly onToggleFullWidth?: () => void;
  readonly onToggleFocusMode?: () => void;
  readonly onToggleToolbar?: () => void;
  readonly onShowShortcuts?: () => void;
  readonly onShowWordCount?: () => void;
  readonly onShowPageInfo?: () => void;
  readonly onPageSetupOpen?: () => void;
  readonly onOpenComments?: () => void;
  readonly onAddBlock?: (type: BlockType) => void;
  /** Gives a workspace member access to the page at the edit level. */
  readonly onShareInvite?: (userId: string) => Promise<void>;
}
