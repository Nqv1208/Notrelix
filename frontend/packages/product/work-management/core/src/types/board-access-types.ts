/**
 * The caller's access to a board, as the board header and the Kanban read it.
 */

export type BoardAccessLevel = "viewer" | "commenter" | "editor" | "manager";

/** Where the caller's access comes from (`mySource.kind`). */
export type BoardAccessSourceKind =
  | "owner"
  | "workspace-admin"
  | "space"
  | "team"
  | "explicit"
  | "guest"
  | "workspace";

/** A group of people who can open the board (`sources[]`). */
export interface BoardAccessSource {
  readonly kind: "space" | "team" | "user" | "guest";
  /** Present only when the caller may see the subject. */
  readonly id: string | null;
  /** Present only when the caller may see the subject. */
  readonly name: string | null;
  readonly count: number;
}

/**
 * The caller's access to a board and who else can open it. `peopleCount`
 * includes guests; when `peopleCountComplete` is false it is a lower bound
 * (sources the backend cannot enumerate yet are missing).
 */
export interface BoardAccess {
  readonly boardId: string;
  readonly version: number;
  readonly myLevel: BoardAccessLevel;
  readonly mySource: {
    readonly kind: BoardAccessSourceKind;
    readonly grantedBy: {
      readonly userId: string;
      readonly name: string;
    } | null;
    /** When an explicit access ends (gap field, CTR-GAP-WS-BOARD-ACCESS); `null` = never. */
    readonly expiresAt?: string | null;
  };
  readonly peopleCount: number;
  readonly guestCount: number;
  readonly peopleCountComplete: boolean;
  readonly sources: readonly BoardAccessSource[];
}
