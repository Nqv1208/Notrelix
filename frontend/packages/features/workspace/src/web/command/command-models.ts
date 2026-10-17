import type {
  IdentityTone,
  MarkToneId,
  WorkspaceDestination,
} from "../../core/model/navigation";
import type { PaletteKind } from "../../core/model/command-palette";
import type { WorkStatusBucket } from "../home/home-models";

/** The department (Space) a resource belongs to, as the mark shows it. */
export interface PaletteDepartment {
  readonly name: string;
  readonly markTone: MarkToneId;
}

/**
 * An Item the member can open: from the My work read, the only Item read the
 * workspace has. `key`, `status` and `due` are drawn when the host has them.
 */
export interface PaletteWorkEntry {
  readonly id: string;
  readonly title: string;
  /** Item key such as `ERP-142`. */
  readonly key?: string | null;
  readonly boardName?: string | null;
  readonly department?: PaletteDepartment | null;
  /** Why the member can see it: it is assigned to them. */
  readonly mine?: boolean;
  /** Who is in charge, when it is somebody else. */
  readonly assignee?: string | null;
  /** Where the typed text matched inside it ("khớp trong việc con: …"). */
  readonly matchNote?: string | null;
  readonly status?: {
    readonly bucket: WorkStatusBucket;
    readonly label: string;
  } | null;
  readonly due?: {
    readonly tone: "overdue" | "today" | "later";
    readonly label: string;
  } | null;
  readonly destination: WorkspaceDestination;
}

export interface PaletteDocEntry {
  readonly id: string;
  readonly title: string;
  readonly department?: PaletteDepartment | null;
  /** The parent Doc's title, when the Doc is nested. */
  readonly location?: string | null;
  /** "3 ngày trước": when the Doc last changed. */
  readonly editedLabel?: string | null;
  /** Who changed it last. */
  readonly editedBy?: string | null;
  /** Where the typed text matched inside it. */
  readonly matchNote?: string | null;
}

export interface PaletteBoardEntry {
  readonly id: string;
  readonly name: string;
  readonly department?: PaletteDepartment | null;
  /** Where the typed text matched inside it ("có 4 việc khớp …"). */
  readonly matchNote?: string | null;
}

/** A recently opened Board, Doc or Item. */
export interface PaletteRecentEntry {
  readonly kind: "board" | "doc" | "work";
  readonly id: string;
  readonly name: string;
  readonly department?: PaletteDepartment | null;
  /** "20 phút trước". */
  readonly whenLabel: string | null;
  /** Item key (kind `work`). */
  readonly key?: string | null;
  /** Item status name (kind `work`). */
  readonly statusLabel?: string | null;
  /** Where it opens; a Board or Doc opens by id when absent. */
  readonly destination?: WorkspaceDestination;
}

export interface PalettePerson {
  readonly id: string;
  readonly name: string;
  readonly identity: IdentityTone;
  readonly roleLabel: string;
}

/** The workspace the palette searches. */
export interface PaletteWorkspace {
  readonly name: string;
  readonly markTone: MarkToneId;
  /** The other workspaces of the member (names). */
  readonly others: readonly string[];
}

/** A command that is running (Final-Command `dang-chay`). */
export interface PaletteRunning {
  readonly typed: string;
  readonly title: string;
  readonly detail: string;
  /** Called by "Hoàn tác"; absent = the button is drawn inert. */
  readonly onUndo?: () => void;
}

export type { PaletteKind };
