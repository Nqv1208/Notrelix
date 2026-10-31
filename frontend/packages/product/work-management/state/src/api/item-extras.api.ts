import type { NotrelixClient } from "@notrelix/contracts";
import { endpoints } from "@notrelix/contracts";
import { personInitials } from "@notrelix/work-management-core";

/** A Documents page linked to an item that the caller may open. */
export interface LinkedPageDto {
  readonly pageId: string;
  readonly title: string;
  readonly linkedAt: string;
}

/** The linked pages the caller sees, and how many more are linked that they may not see. */
export interface LinkedPagesDto {
  readonly pages: readonly LinkedPageDto[];
  readonly hiddenCount: number;
}

/** What a reminder reached: who, how many, and when the next one is possible. */
export interface RemindResultDto {
  readonly recipients: number;
  readonly nextRemindAt: string;
}

/** The error code the server answers when the 24 hour reminder limit is hit. */
export const REMIND_TOO_SOON_CODE = "work.item.remind-too-soon";

/** A member of the workspace (`Workspaces.Members.ListMembers`). */
interface WorkspaceMember {
  readonly userId?: string;
  readonly name?: string | null;
  readonly status?: string | null;
}

/** A member of the board (`WorkManagement.Boards.GetOverview`). */
interface BoardMember {
  readonly userId?: string;
  readonly role?: string | null;
}

/** What a board role lets the person do, or null for a role this page does not know. */
function boardLevel(
  role: string | null | undefined,
): PeopleDirectoryDto["people"][number]["level"] {
  const name = (role ?? "").trim().toLowerCase();
  return name === "manager" ||
    name === "editor" ||
    name === "commenter" ||
    name === "viewer"
    ? name
    : null;
}

/**
 * Item sheet reads and writes beyond the generated contract (gap
 * `CTR-GAP-WS-ITEM-SHEET`, see docs/superpowers/be-blockers-2026-10.md,
 * "BE requirement from FE: Board/Kanban"): the child items of an item and
 * whether the member follows it. Follow and reopen share the My work gap
 * (`CTR-GAP-WS-MYWORK-EXT`).
 */
export interface ItemChildItemDto {
  readonly id: string;
  readonly key: string | null;
  readonly title: string;
  readonly done: boolean;
  readonly dueAt: string | null;
  readonly assignee: {
    readonly id: string;
    readonly userId: string;
    readonly name: string;
    readonly initials: string;
  } | null;
}

/** A child item as `WorkManagement.BoardItems.ListChildren` sends it. */
interface ChildItemWire {
  readonly id: string;
  readonly key?: string | null;
  readonly title: string;
  readonly done?: boolean;
  readonly dueAt?: string | null;
  readonly assigneeUserId?: string | null;
  readonly assigneeName?: string | null;
}

function toChildItem(wire: ChildItemWire): ItemChildItemDto {
  const name = wire.assigneeName ?? "";
  return {
    id: wire.id,
    key: wire.key ?? null,
    title: wire.title,
    done: wire.done === true,
    dueAt: wire.dueAt ?? null,
    assignee: wire.assigneeUserId
      ? {
          id: wire.assigneeUserId,
          userId: wire.assigneeUserId,
          name,
          initials: personInitials(name),
        }
      : null,
  };
}

/** A phase or a milestone of a board (gap `CTR-GAP-WM-PLAN-ITEMS`). */
export interface BoardPlanItemDto {
  readonly id: string;
  readonly kind: "phase" | "milestone";
  readonly title: string;
  /** First and last Hanoi day "yyyy-MM-dd"; equal for a milestone. */
  readonly startDay: string;
  readonly endDay: string;
  readonly category: "todo" | "doing" | "review" | "done";
  /** The board group the Timeline lists it in. */
  readonly groupId: string;
  /** Completion, 0 to 100. */
  readonly percent: number;
  /** Row of the item among its group's rows (0 = first). */
  readonly index: number;
}

/** The plan of a board: markers, finish-to-start links and completions. */
export interface BoardPlanDto {
  readonly items: readonly BoardPlanItemDto[];
  /** `to` waits for `from`; ids of items or plan items. */
  readonly links: readonly { readonly from: string; readonly to: string }[];
  /** Completion of items whose progress is not their child items' share. */
  readonly percents: Readonly<Record<string, number>>;
  /** Group id by item id, where the plan lists an item outside its own group. */
  readonly groups: Readonly<Record<string, string>>;
}

/** A link between two items as the Detail page groups them (gap `CTR-GAP-WM-ITEM-DETAIL`). */
export interface ItemLinkDto {
  readonly id: string;
  /** "blocked-by": this item waits for it; "blocks": it waits for this item. */
  readonly relation: "blocked-by" | "blocks" | "related";
  readonly item: {
    readonly id: string;
    readonly key: string | null;
    readonly title: string;
    readonly statusName: string | null;
    readonly statusCategory: "todo" | "in_progress" | "review" | "done" | null;
  };
}

/** Who follows an item (gap `CTR-GAP-WS-ITEM-SHEET`). */
export interface ItemFollowingDto {
  readonly following: boolean;
  readonly followers: readonly { userId: string; name: string }[];
}

/**
 * An approval asked about an item (`WorkManagement.Approvals.ListForItem`): who was asked and what they
 * answered. The status is the one of the request: pending until the named person decides.
 */
export interface ItemApprovalDto {
  readonly id: string;
  readonly status: "pending" | "approved" | "rejected" | "cancelled";
  readonly requestedByUserId: string;
  readonly approverUserId: string | null;
  readonly note: string | null;
  readonly requestedAt: string;
  readonly decidedAt: string | null;
  /** The approver's comment with the decision. */
  readonly decisionNote: string | null;
  /** The request's version, sent back as the expected version when it is cancelled. */
  readonly version: number;
}

interface ItemApprovalWire {
  readonly id: string;
  readonly status?: string | null;
  readonly description?: string | null;
  readonly requestedByUserId: string;
  readonly createdAt: string;
  readonly version: number;
  readonly steps?:
    | readonly {
        readonly approverUserId?: string | null;
        readonly decidedAt?: string | null;
        readonly note?: string | null;
      }[]
    | null;
}

function toItemApproval(wire: ItemApprovalWire): ItemApprovalDto {
  const status = (wire.status ?? "").toLowerCase();
  const step = wire.steps?.[0];
  return {
    id: wire.id,
    status:
      status === "approved" || status === "rejected" || status === "cancelled"
        ? status
        : "pending",
    requestedByUserId: wire.requestedByUserId,
    approverUserId: step?.approverUserId ?? null,
    note: wire.description ?? null,
    requestedAt: wire.createdAt,
    decidedAt: step?.decidedAt ?? null,
    decisionNote: step?.note ?? null,
    version: wire.version,
  };
}

/** Who completed or archived an item and when. */
export interface ItemLifecycleDto {
  readonly archived: { readonly at: string; readonly byName: string } | null;
  readonly completed: { readonly at: string; readonly byName: string } | null;
}

/**
 * The people an item can name (gap `CTR-GAP-WM-ITEM-STATES`): each person's
 * department, open items on the board and whether they see the board; each
 * team with how many of its people see it.
 */
export interface PeopleDirectoryDto {
  readonly people: readonly {
    readonly userId: string;
    readonly name: string;
    readonly department: string | null;
    readonly openCount: number;
    readonly onBoard: boolean;
    /** What the person may do on the board; null when they do not see it. */
    readonly level: "viewer" | "commenter" | "editor" | "manager" | null;
  }[];
  readonly teams: readonly {
    readonly id: string;
    readonly name: string;
    readonly memberCount: number;
    /** Members who see the board, and who they are. */
    readonly viewableCount: number;
    readonly viewableUserIds: readonly string[];
  }[];
  /** The caller may invite a person who does not see the board. */
  readonly canInvite: boolean;
}

/** What `WorkManagement.BoardItems.MoveToBoard` could not carry to the other board. */
export interface MoveToBoardResultDto {
  readonly boardId: string;
  readonly groupId: string;
  readonly itemKey: string;
  readonly droppedFields: readonly string[];
  readonly droppedLabels: number;
  readonly droppedAssignees: number;
}

/** An item in a board's trash. */
export interface BoardTrashEntryDto {
  readonly id: string;
  readonly name: string;
  readonly itemKey: string | null;
  readonly groupId: string;
  readonly deletedAt: string;
  readonly deletedBy: string | null;
  readonly restorableUntil: string;
}

export function createItemExtrasApi(
  client: NotrelixClient,
  options: { readonly planProduced?: boolean } = {},
) {
  const { api } = client;
  const quiet = { skipGlobalErrorToast: true } as const;
  const key = () => crypto.randomUUID();
  // Every item write names the version it is based on; one that does not know it reads it just before.
  const versionOf = async (itemId: string): Promise<number> =>
    (await api.get<{ version?: number | null }>(`/board-items/${itemId}`))
      .version ?? 0;
  // A backend without a plan producer is not asked at all (`planProduced: false`);
  // one that answers 404 anyway is asked once. The calendar and the timeline
  // then show the board's items alone.
  let planProduced = options.planProduced ?? true;
  return {
    async getPlan(boardId: string): Promise<BoardPlanDto> {
      let dto: Partial<BoardPlanDto> | null = null;
      if (planProduced) {
        try {
          dto = await api.get<Partial<BoardPlanDto> | null>(
            `/boards/${boardId}/plan-items`,
            quiet,
          );
        } catch (error) {
          const status = (error as { status?: number } | null)?.status;
          if (status !== 404) throw error;
          planProduced = false;
        }
      }
      return {
        items: dto?.items ?? [],
        links: dto?.links ?? [],
        percents: dto?.percents ?? {},
        groups: dto?.groups ?? {},
      };
    },
    async getChildItems(itemId: string): Promise<ItemChildItemDto[]> {
      const dto = await api.get<{ items?: ChildItemWire[] } | null>(
        `/board-items/${itemId}/child-items`,
      );
      return (dto?.items ?? []).map(toChildItem);
    },
    async getFollowing(itemId: string): Promise<ItemFollowingDto> {
      const dto = await api.get<Partial<ItemFollowingDto> | null>(
        `/board-items/${itemId}/following`,
      );
      return {
        following: dto?.following === true,
        followers: dto?.followers ?? [],
      };
    },
    async getApprovals(itemId: string): Promise<ItemApprovalDto[]> {
      const dto = await api.get<ItemApprovalWire[] | null>(
        `/board-items/${itemId}/approval-requests`,
      );
      return (dto ?? []).map(toItemApproval);
    },
    /** The named approver answers (`WorkManagement.Approvals.Decide`); the comment is optional. */
    async decideApproval(
      workspaceId: string,
      requestId: string,
      decision: "approve" | "reject",
      comment: string | null,
    ): Promise<void> {
      await api.post<void>(
        `/workspaces/${workspaceId}/approvals/${requestId}/decision`,
        { decision, comment },
        { idempotencyKey: key(), ...quiet },
      );
    },
    /** The requester withdraws a pending request (`WorkManagement.Approvals.Cancel`). */
    async cancelApproval(
      requestId: string,
      expectedVersion: number,
    ): Promise<void> {
      await api.post<void>(
        `/approvals/${requestId}/cancel`,
        { expectedVersion },
        { idempotencyKey: key(), ...quiet },
      );
    },
    /** Asks a person to approve the item (`WorkManagement.Approvals.RequestForItem`). */
    async requestApproval(
      itemId: string,
      approverUserId: string,
      note: string | null,
    ): Promise<void> {
      await api.post<unknown>(
        `/board-items/${itemId}/approval-requests`,
        { approverUserId, note },
        { idempotencyKey: key(), ...quiet },
      );
    },
    async getLinks(itemId: string): Promise<ItemLinkDto[]> {
      const dto = await api.get<{ links?: ItemLinkDto[] } | null>(
        `/board-items/${itemId}/links`,
      );
      return dto?.links ?? [];
    },
    async addLink(
      itemId: string,
      link: { itemId: string; relation: ItemLinkDto["relation"] },
    ): Promise<void> {
      await api.post<void>(`/board-items/${itemId}/links`, link, {
        idempotencyKey: key(),
        ...quiet,
      });
    },
    async removeLink(itemId: string, linkId: string): Promise<void> {
      await api.delete<void>(`/board-items/${itemId}/links/${linkId}`, quiet);
    },
    /** Timeline dependency `from -> to`: `to` waits for `from`. */
    async addDependency(from: string, to: string): Promise<void> {
      await this.addLink(to, { itemId: from, relation: "blocked-by" });
    },
    /** Removes the dependency `from -> to`; false when it is already gone. */
    async removeDependency(from: string, to: string): Promise<boolean> {
      const links = await this.getLinks(to);
      const found = links.find(
        (link) => link.relation === "blocked-by" && link.item.id === from,
      );
      if (!found) return false;
      await this.removeLink(to, found.id);
      return true;
    },
    /** Adds a child item (sub-item) under an item. */
    async addChildItem(itemId: string, title: string): Promise<void> {
      await api.post<void>(
        `/board-items/${itemId}/child-items`,
        { title },
        { idempotencyKey: key(), ...quiet },
      );
    },
    /**
     * Read from the item itself (`WorkManagement.BoardItems.Get`): whether it is
     * archived or completed, when and by whom.
     */
    async getLifecycle(itemId: string): Promise<ItemLifecycleDto> {
      const dto = await api.get<{
        isArchived?: boolean;
        archivedAt?: string | null;
        archivedByName?: string | null;
        completedAt?: string | null;
        completedByName?: string | null;
        updatedAt?: string | null;
      } | null>(`/board-items/${itemId}`);
      return {
        archived: dto?.isArchived
          ? {
              at: dto.archivedAt ?? dto.updatedAt ?? "",
              byName: dto.archivedByName ?? "",
            }
          : null,
        completed: dto?.completedAt
          ? { at: dto.completedAt, byName: dto.completedByName ?? "" }
          : null,
      };
    },
    /**
     * `WorkManagement.BoardItems.Reopen`: a completed item returns to the status
     * it had before it was completed, else to the board's first open status.
     */
    async reopenItem(itemId: string): Promise<void> {
      await api.post<void>(
        `/board-items/${itemId}/reopen?expectedVersion=${await versionOf(itemId)}`,
        {},
        { idempotencyKey: key(), ...quiet },
      );
    },
    /** An archived item comes back with `WorkManagement.BoardItems.Unarchive`. */
    async restoreItem(itemId: string): Promise<void> {
      await api.post<void>(
        `/board-items/${itemId}/unarchive?expectedVersion=${await versionOf(itemId)}`,
        {},
        { idempotencyKey: key(), ...quiet },
      );
    },
    /**
     * The members of the workspace and which of them see the board, read from
     * the workspace members and the board itself. The producer has no
     * department, open-item count or team roll-up, so those stay empty;
     * adding a person to the board is the board's share dialog.
     */
    async getPeopleDirectory(
      boardId: string,
      workspaceId: string,
    ): Promise<PeopleDirectoryDto> {
      const [members, board] = await Promise.all([
        api.get<WorkspaceMember[]>(endpoints.workspaces.members(workspaceId)),
        api.get<{ members?: BoardMember[] | null }>(
          endpoints.boards.full(boardId),
        ),
      ]);
      const onBoard = new Map(
        (board.members ?? []).map((member) => [
          member.userId ?? "",
          boardLevel(member.role),
        ]),
      );
      return {
        people: (members ?? [])
          .filter((member) => member.status?.toLowerCase() !== "suspended")
          .map((member) => ({
            userId: member.userId ?? "",
            name: member.name ?? "",
            department: null,
            openCount: 0,
            onBoard: onBoard.has(member.userId ?? ""),
            level: onBoard.get(member.userId ?? "") ?? null,
          })),
        teams: [],
        canInvite: false,
      };
    },
    async setFollowing(itemId: string, following: boolean): Promise<void> {
      const options = { idempotencyKey: key(), ...quiet };
      if (following)
        await api.put<void>(
          `/board-items/${itemId}/following`,
          undefined,
          options,
        );
      else await api.delete<void>(`/board-items/${itemId}/following`, options);
    },
    /**
     * `WorkManagement.BoardItems.MoveToBoard`: the item lands in the target
     * group of the same name, else the board's default or first group, with
     * the target board's next key. The result names what could not carry over.
     */
    async moveToBoard(
      itemId: string,
      boardId: string,
    ): Promise<MoveToBoardResultDto> {
      const result = await api.post<Partial<MoveToBoardResultDto>>(
        `/board-items/${itemId}/move-to-board`,
        { boardId, expectedVersion: await versionOf(itemId) },
        { idempotencyKey: key(), ...quiet },
      );
      return {
        boardId: result?.boardId ?? boardId,
        groupId: result?.groupId ?? "",
        itemKey: result?.itemKey ?? "",
        droppedFields: result?.droppedFields ?? [],
        droppedLabels: result?.droppedLabels ?? 0,
        droppedAssignees: result?.droppedAssignees ?? 0,
      };
    },
    /** `WorkManagement.BoardItems.Trash`: items deleted from the board in the last 30 days. */
    async listTrash(boardId: string): Promise<BoardTrashEntryDto[]> {
      const rows = await api.get<BoardTrashEntryDto[]>(
        `/boards/${boardId}/items/trash`,
        quiet,
      );
      return rows ?? [];
    },
    /** `WorkManagement.BoardItems.Restore`: a deleted item comes back from the trash. */
    async restoreDeleted(itemId: string): Promise<void> {
      await api.post<void>(
        `/board-items/${itemId}/restore`,
        {},
        { idempotencyKey: key(), ...quiet },
      );
    },
    /**
     * `WorkManagement.BoardItems.RemindOwners`: everyone in charge except the
     * caller is told in their Inbox. At most one per item per caller per 24 hours;
     * the server answers 409 `work.item.remind-too-soon` after that.
     */
    async remindOwners(itemId: string): Promise<RemindResultDto> {
      const result = await api.post<Partial<RemindResultDto> | null>(
        `/board-items/${itemId}/remind`,
        {},
        { idempotencyKey: key(), ...quiet },
      );
      return {
        recipients: result?.recipients ?? 0,
        nextRemindAt: result?.nextRemindAt ?? "",
      };
    },
    /** `WorkManagement.BoardItems.Complete`: marks the item done. */
    async completeItem(itemId: string): Promise<void> {
      await api.post<void>(
        `/board-items/${itemId}/complete`,
        { expectedVersion: await versionOf(itemId) },
        { idempotencyKey: key(), ...quiet },
      );
    },
    /** `WorkManagement.BoardItems.ListLinkedPages`: pages the caller may open, plus how many are hidden. */
    async getLinkedPages(itemId: string): Promise<LinkedPagesDto> {
      const dto = await api.get<Partial<LinkedPagesDto> | null>(
        `/board-items/${itemId}/linked-pages`,
        quiet,
      );
      return { pages: dto?.pages ?? [], hiddenCount: dto?.hiddenCount ?? 0 };
    },
    /**
     * `Documents.Pages.ListWorkspacePages`: the pages of the workspace the
     * caller may see, for the picker that links one to an item.
     */
    async listWorkspacePages(
      workspaceId: string,
    ): Promise<{ id: string; title: string; parentTitle?: string }[]> {
      const rows = await api.get<
        | { id?: string; title?: string | null; parentTitle?: string | null }[]
        | null
      >(endpoints.pages.list(workspaceId), quiet);
      return (rows ?? [])
        .filter(
          (
            row,
          ): row is {
            id: string;
            title?: string | null;
            parentTitle?: string | null;
          } => Boolean(row.id),
        )
        .map((row) => ({
          id: row.id,
          title: row.title?.trim() || "",
          ...(row.parentTitle ? { parentTitle: row.parentTitle } : {}),
        }));
    },
    /** `WorkManagement.BoardItems.LinkPage`: linking a page twice changes nothing. */
    async linkPage(itemId: string, pageId: string): Promise<void> {
      await api.post<void>(
        `/board-items/${itemId}/link-page`,
        { pageId },
        { idempotencyKey: key(), ...quiet },
      );
    },
    /** `WorkManagement.BoardItems.UnlinkPage`: removes this page's link only. */
    async unlinkPage(itemId: string, pageId: string): Promise<void> {
      await api.delete<void>(`/board-items/${itemId}/link-page/${pageId}`, {
        idempotencyKey: key(),
        ...quiet,
      });
    },
    /** Ticks (`done`) or unticks a child item. */
    async setChildDone(childId: string, done: boolean): Promise<void> {
      if (done)
        await api.post<void>(
          `/board-items/${childId}/complete`,
          { expectedVersion: await versionOf(childId) },
          { idempotencyKey: key(), ...quiet },
        );
      else
        await api.post<void>(
          `/board-items/${childId}/reopen?expectedVersion=${await versionOf(childId)}`,
          {},
          { idempotencyKey: key(), ...quiet },
        );
    },
  };
}

const failureOf = (error: unknown) =>
  (typeof error === "object" && error !== null ? error : {}) as {
    status?: unknown;
    code?: unknown;
    message?: unknown;
  };

/** Why the server refused a move to another board, from what it answered. */
export function classifyMoveError(
  error: unknown,
): "deps" | "conflict" | "archived" | "error" {
  const { status, code, message } = failureOf(error);
  if (code === "work-management.item.has-dependencies") return "deps";
  if (/archiv/i.test(`${String(code ?? "")} ${String(message ?? "")}`))
    return "archived";
  if (status === 409) return "conflict";
  return "error";
}

/** Why the server refused an archive: a newer edit, no right to, the item gone, or the network. */
export function classifyArchiveError(
  error: unknown,
): "conflict" | "denied" | "gone" | "error" {
  const { status } = failureOf(error);
  if (status === 412 || status === 409) return "conflict";
  if (status === 403) return "denied";
  if (status === 404) return "gone";
  return "error";
}

/** Why the server refused a reminder, and when it can be sent again when it says so. */
export function classifyRemindError(
  error: unknown,
):
  | { kind: "tooSoon"; nextRemindAt: string | null }
  | { kind: "nobody" }
  | { kind: "error" } {
  const { status, code, message } = failureOf(error);
  if (code === REMIND_TOO_SOON_CODE) {
    const when = /at (\S+?)\.?$/.exec(String(message ?? ""))?.[1] ?? null;
    return { kind: "tooSoon", nextRemindAt: when };
  }
  if (status === 400) return { kind: "nobody" };
  return { kind: "error" };
}
