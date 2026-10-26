import type {
  Board,
  BoardAccessLevel,
  CardDetail,
} from "@notrelix/work-management-core";
import type { ItemSheetCopy } from "../card-detail/item-sheet.copy";
import type {
  ItemSheetMark,
  TaskDetailCallbacks,
  TaskDetailCapabilities,
  TaskDetailData,
} from "../card-detail/task-detail-types";
import type { ItemPageCopy } from "./item-page.copy";

/**
 * What the page shows: the frame while it loads, a failure, an item that is
 * not there or not visible to the viewer ("notFound", same words for both),
 * or the item.
 */
export type ItemPageStatus = "loading" | "error" | "notFound" | "ready";

/** The activity filter of the page: everything, comments only, or history only. */
export type ItemPageTab = "all" | "comments" | "history";

export type ItemPageViewKind = "kanban" | "table" | "calendar" | "timeline";

/**
 * Where the item sits in its lane (a Status option): "Việc 1/3 trong Đang
 * làm", and the neighbours K and J step to. Absent when the lane is unknown.
 */
export interface ItemPagePosition {
  /** 1-based place in the lane. */
  readonly index: number;
  readonly total: number;
  readonly laneName: string;
}

/** The signed-in person, for the composer's mark. */
export interface ItemPagePerson {
  readonly id: string;
  readonly userId: string;
  readonly initials: string;
}

export interface ItemPageSpace {
  readonly name: string;
  readonly mark: ItemSheetMark;
  readonly href?: string;
}

/** Navigation around the item; every link is optional and stays inert without it. */
export interface ItemPageNavigation {
  readonly position?: ItemPagePosition;
  readonly boardHref?: string;
  /** Link of each view that shows the item, in the board route. */
  readonly viewHrefs?: Readonly<Partial<Record<ItemPageViewKind, string>>>;
  /** Back to the board (the arrow, the crumb and an archive). */
  readonly onBack: () => void;
  readonly onOpenBoard?: () => void;
  readonly onOpenSpace?: () => void;
  /** "Thu về bảng bên": the board with the side sheet open on this item. */
  readonly onCollapse?: () => void;
  readonly onOpenView?: (view: ItemPageViewKind) => void;
  /** Opens the board settings where the status list is edited. */
  readonly onEditStatuses?: () => void;
}

/** The viewer's level on the board and who added them, for the read-only notice. */
export interface ItemPageAccess {
  readonly level: BoardAccessLevel;
  readonly grantedBy?: string | null;
}

/** The property rows of the page's right column. */
export type ItemPageProperty =
  "status" | "assignee" | "startDate" | "due" | "priority";

/**
 * A write the server refused because the stored value had moved on (HTTP
 * 409): neither value is lost, the viewer picks one.
 */
export interface ItemPageConflict {
  /** The property row that shows the two values. */
  readonly property: ItemPageProperty;
  readonly fieldName: string;
  /** The value the viewer chose, as shown. */
  readonly mine: string;
  /** The stored value, as shown. */
  readonly theirs: string;
}

/** The property picker the page opens from the keyboard (S, P). */
export type ItemPagePicker = "status" | "priority";

/** The people the Assignee picker lists (gap `CTR-GAP-WM-ITEM-STATES`). */
export interface ItemPagePeople {
  readonly people: readonly {
    readonly userId: string;
    readonly name: string;
    readonly department: string | null;
    readonly openCount: number | null;
    readonly onBoard: boolean;
    readonly level?: "viewer" | "commenter" | "editor" | "manager" | null;
  }[];
  /** Teams a comment can mention, with how many of their people see the board. */
  readonly teams?: readonly {
    readonly id: string;
    readonly name: string;
    readonly memberCount: number;
    readonly viewableCount: number;
    readonly viewableUserIds: readonly string[];
  }[];
  readonly loading?: boolean;
  /** The viewer manages the board and may invite someone who cannot see it. */
  readonly canInvite: boolean;
  readonly onInvite?: (userId: string) => void;
}

/** A file on its way to the item, or one that did not make it. */
export interface ItemPageUpload {
  readonly id: string;
  readonly name: string;
  readonly size: number;
  readonly status: "uploading" | "done" | "error";
  /** 0 to 100 while uploading; absent when the transfer reports none. */
  readonly progress?: number | null;
  /**
   * Why it failed: refused before sending (too big, blocked type), refused by
   * the server, or the transfer broke.
   */
  readonly error?:
    | "tooBig"
    | "blockedType"
    | "serverTooLarge"
    | "unsupportedType"
    | "limitReached"
    | "forbidden"
    | "notFound"
    | "failed"
    | null;
  /** False when sending again cannot change the answer; the retry button is absent. */
  readonly retryable?: boolean;
  /** The limit it broke, as shown. */
  readonly limit?: string;
}

/** Whether the item is done or archived, and who did it (gap `CTR-GAP-WM-ITEM-STATES`). */
export interface ItemPageLifecycle {
  readonly completed?: {
    /** ISO instant. */
    readonly at: string;
    readonly byName: string;
  } | null;
  readonly archived?: {
    readonly at: string;
    readonly byName: string;
  } | null;
  /** Reopens a done item; the banner's button stays inert without it. */
  readonly onReopen?: () => void;
  /** Brings an archived item back; the banner's button is absent without it. */
  readonly onRestore?: () => void;
}

/**
 * The page has lost the server: how many writes wait, and which property
 * rows and sections carry the "chờ lưu" mark.
 */
export interface ItemPageOffline {
  readonly pendingCount: number;
  readonly properties?: readonly ItemPageProperty[];
  /** A sub-item write is waiting. */
  readonly childItems?: boolean;
}

export interface ItemPageSurfaceProps {
  readonly status: ItemPageStatus;
  readonly board: Board;
  readonly card: CardDetail | null;
  readonly capabilities: TaskDetailCapabilities;
  readonly detailData?: TaskDetailData;
  /** The same callbacks the side sheet takes; `onClose` is unused here. */
  readonly callbacks: TaskDetailCallbacks;
  readonly navigation: ItemPageNavigation;
  readonly copy: ItemPageCopy;
  /** Wording shared with the side sheet (property labels, priorities, times). */
  readonly sheetCopy: ItemSheetCopy;
  /** Injected clock for due and activity wording. */
  readonly now: Date;
  /** IANA time zone for calendar days; the runtime zone when omitted. */
  readonly timeZone?: string;
  readonly space?: ItemPageSpace;
  readonly viewer?: ItemPagePerson;
  /** Who created the item; unknown when the read does not say. */
  readonly creatorName?: string | null;
  readonly access?: ItemPageAccess | null;
  readonly conflict?: ItemPageConflict | null;
  readonly onResolveConflict?: (choice: "theirs" | "mine") => void;
  readonly activeTab?: ItemPageTab;
  readonly onSelectTab?: (tab: ItemPageTab) => void;
  /** Done or archived; an archived item is read-only whatever the capabilities. */
  readonly lifecycle?: ItemPageLifecycle | null;
  /** Present while the page cannot reach the server. */
  readonly offline?: ItemPageOffline | null;
  /** Files being sent or refused; the Files list shows them above the saved ones. */
  readonly uploads?: readonly ItemPageUpload[];
  readonly onRetryUpload?: (uploadId: string) => void;
  readonly onDismissUpload?: (uploadId: string) => void;
  /** Who the Assignee picker lists; the board's members without it. */
  readonly people?: ItemPagePeople;
  /** The item key the link carried, shown before the item arrives. */
  readonly itemKey?: string | null;
  /** Loading has taken longer than expected. */
  readonly slow?: boolean;
  /** "Tìm bằng ⌘K" of the not-found view. */
  readonly onOpenSearch?: () => void;
  /** "Về trang chủ" of the not-found view. */
  readonly onGoHome?: () => void;
}
