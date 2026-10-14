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

export type ItemPageStatus = "loading" | "error" | "ready";

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
}

/** The viewer's level on the board and who added them, for the read-only notice. */
export interface ItemPageAccess {
  readonly level: BoardAccessLevel;
  readonly grantedBy?: string | null;
  /** When an explicit grant ends (ISO); absent = it does not. */
  readonly expiresAt?: string | null;
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
}
