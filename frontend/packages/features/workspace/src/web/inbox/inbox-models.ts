import type { IdentityTone } from "../../core/model/navigation";
import type { MyWorkDepartment, WorkStatusBucket } from "../home/home-models";

/** What a notification asks of the member (the chip under it). */
export type InboxNeed = "approve" | "reply" | "overdue";

/** The glyph on the avatar's corner and the colour behind it. */
export type InboxKind =
  | "approve"
  | "mention"
  | "assign"
  | "overdue"
  | "invite"
  | "comment"
  | "change"
  | "system";

export type InboxTab = "need" | "mention" | "assigned" | "follow" | "all";

export const INBOX_TABS: readonly InboxTab[] = [
  "need",
  "mention",
  "assigned",
  "follow",
  "all",
];

export interface InboxPerson {
  readonly name: string;
  readonly tone: IdentityTone;
}

/** A line of a notification: the strong parts are the people and the item. */
export interface InboxSegment {
  readonly text: string;
  readonly strong?: boolean;
}

/** Where the notification happened: a department mark and a label. */
export interface InboxContext {
  readonly department: MyWorkDepartment | null;
  readonly label: string;
}

/** The change a notification reports, one line each (`ins` marks what changed). */
export interface InboxChange {
  readonly label: string;
  readonly value: string;
  /** The part of the value that is new, drawn on the soft success tone. */
  readonly changed?: string;
}

export interface InboxCardView {
  readonly title: string;
  readonly status?: {
    readonly bucket: WorkStatusBucket;
    readonly label: string;
  };
  /** `Ưu tiên Cao` is built from this. */
  readonly priority?: string;
  readonly due?: string;
  readonly comments?: number;
  readonly changes: readonly InboxChange[];
  /** The member can approve or ask for changes (an approval request). */
  readonly approvable: boolean;
}

export interface InboxCommentView {
  readonly person: InboxPerson;
  readonly time: string;
  readonly text: string;
}

/** The reading pane of a notification. */
export interface InboxDetailView {
  /** Department, Board and key in the crumb; `null` draws no crumb. */
  readonly crumb: {
    readonly department: MyWorkDepartment | null;
    readonly board: string;
    readonly key: string | null;
  } | null;
  readonly event: {
    readonly person: InboxPerson | null;
    readonly segments: readonly InboxSegment[];
    /** "Hôm nay 10:28 · bạn là người duyệt duy nhất · …". */
    readonly when: string;
  };
  readonly card: InboxCardView | null;
  readonly comment: InboxCommentView | null;
  /** The name in the reply field's placeholder. */
  readonly replyTo: string | null;
  readonly openHref: string | null;
}

/** A run of small changes grouped into one row. */
export interface InboxBundle {
  readonly count: number;
  readonly summary: string;
}

export interface InboxItemView {
  readonly id: string;
  readonly unread: boolean;
  /** When it happened (ISO); drives the day header and the time. */
  readonly at: string;
  readonly kind: InboxKind;
  /** `null` draws the system avatar. */
  readonly person: InboxPerson | null;
  /** The avatar's text when there is no person ("+4"). */
  readonly avatarText?: string;
  readonly segments: readonly InboxSegment[];
  readonly context: InboxContext | null;
  readonly need: InboxNeed | null;
  /** The member follows the item (the Following tab). */
  readonly following: boolean;
  readonly bundle: InboxBundle | null;
  /** Where the row leads when it is a link; `null` selects it in place. */
  readonly href: string | null;
  readonly detail: InboxDetailView | null;
}
