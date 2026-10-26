import type { NotrelixClient } from "@notrelix/contracts";

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

/** Who completed or archived an item and when (gap `CTR-GAP-WM-ITEM-STATES`). */
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

type PeopleLevel = "viewer" | "commenter" | "editor" | "manager";

interface WorkspaceMemberRead {
  readonly userId: string;
  readonly name: string | null;
  readonly status: string | null;
}

interface TeamRead {
  readonly id: string;
  readonly name: string | null;
  readonly status: string | null;
  readonly memberCount: number;
}

interface BoardSharingRead {
  readonly visibility?: string | null;
  readonly canManage: boolean;
  readonly entries?: readonly { id: string; level: PeopleLevel }[] | null;
}

export function createItemExtrasApi(client: NotrelixClient) {
  const { api } = client;
  const quiet = { skipGlobalErrorToast: true } as const;
  const key = () => crypto.randomUUID();
  return {
    async getPlan(boardId: string): Promise<BoardPlanDto> {
      const dto = await api.get<Partial<BoardPlanDto> | null>(
        `/boards/${boardId}/plan-items`,
      );
      return {
        items: dto?.items ?? [],
        links: dto?.links ?? [],
        percents: dto?.percents ?? {},
        groups: dto?.groups ?? {},
      };
    },
    async getChildItems(itemId: string): Promise<ItemChildItemDto[]> {
      const dto = await api.get<{ items?: ItemChildItemDto[] } | null>(
        `/board-items/${itemId}/child-items`,
      );
      return dto?.items ?? [];
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
    /** Adds a child item (sub-item) under an item. */
    async addChildItem(itemId: string, title: string): Promise<void> {
      await api.post<void>(
        `/board-items/${itemId}/child-items`,
        { title },
        { idempotencyKey: key(), ...quiet },
      );
    },
    async getLifecycle(itemId: string): Promise<ItemLifecycleDto> {
      const dto = await api.get<Partial<ItemLifecycleDto> | null>(
        `/board-items/${itemId}/lifecycle`,
      );
      return {
        archived: dto?.archived ?? null,
        completed: dto?.completed ?? null,
      };
    },
    async restoreItem(itemId: string): Promise<void> {
      await api.post<void>(
        `/board-items/${itemId}/restore`,
        {},
        { idempotencyKey: key(), ...quiet },
      );
    },
    async reopenItem(itemId: string): Promise<void> {
      await api.post<void>(
        `/board-items/${itemId}/reopen`,
        {},
        { idempotencyKey: key(), ...quiet },
      );
    },
    /**
     * Who an item can name on a board, read from the producers that exist: the
     * workspace members, the Teams and the board's own sharing read. A person
     * the sharing read does not list reaches a private board by no route the
     * caller may see, so they count as not on it; on any other board everyone
     * in the workspace sees it. Open item counts and departments are not part
     * of those reads and stay empty.
     */
    async getPeopleDirectory(
      boardId: string,
      workspaceId: string,
    ): Promise<PeopleDirectoryDto> {
      const [members, teams, sharing] = await Promise.all([
        // A Guest may not list the workspace: they name nobody.
        api
          .get<WorkspaceMemberRead[] | null>(
            `/workspaces/${workspaceId}/members`,
            quiet,
          )
          .catch(() => null),
        api
          .get<TeamRead[] | null>(`/workspaces/${workspaceId}/teams`, quiet)
          .catch(() => null),
        api.get<BoardSharingRead | null>(`/boards/${boardId}/sharing`, quiet),
      ]);
      const isPrivate = (sharing?.visibility ?? "").toLowerCase() === "private";
      const levelOf = new Map<string, PeopleLevel>();
      for (const entry of sharing?.entries ?? [])
        levelOf.set(entry.id, entry.level);
      return {
        people: (members ?? [])
          .filter(
            (member) => (member.status ?? "active").toLowerCase() === "active",
          )
          .map((member) => {
            const level = levelOf.get(member.userId) ?? null;
            return {
              userId: member.userId,
              name: member.name ?? "",
              department: null,
              openCount: 0,
              onBoard: isPrivate ? level !== null : true,
              level,
            };
          }),
        teams: (teams ?? [])
          .filter(
            (team) => (team.status ?? "active").toLowerCase() === "active",
          )
          .filter((team) => team.memberCount > 0)
          .map((team) => ({
            id: team.id,
            name: team.name ?? "",
            memberCount: team.memberCount,
            viewableCount:
              !isPrivate || levelOf.has(team.id) ? team.memberCount : 0,
            viewableUserIds: [],
          })),
        canInvite: sharing?.canManage === true,
      };
    },
    /** Adds the person to the board: the producer's board-member write. */
    async invitePerson(boardId: string, userId: string): Promise<void> {
      await api.post<void>(
        `/boards/${boardId}/members`,
        { userId, role: "Member" },
        { idempotencyKey: key(), ...quiet },
      );
    },
    async setFollowing(itemId: string, following: boolean): Promise<void> {
      if (following)
        await api.put<void>(
          `/board-items/${itemId}/following`,
          undefined,
          quiet,
        );
      else await api.delete<void>(`/board-items/${itemId}/following`, quiet);
    },
    /**
     * Moves an item to another board (gap `CTR-GAP-WM-TABLE-BULK`, see
     * "BE requirement from FE: Table"): it lands in the target group of the
     * same name, else the first, with a new item key.
     */
    async moveToBoard(itemId: string, boardId: string): Promise<void> {
      await api.post<void>(
        `/board-items/${itemId}/move-to-board`,
        { boardId },
        { idempotencyKey: key(), ...quiet },
      );
    },
    /** Ticks (`done`) or unticks a child item. */
    async setChildDone(childId: string, done: boolean): Promise<void> {
      if (done)
        await api.post<void>(
          `/board-items/${childId}/complete`,
          {},
          { idempotencyKey: key(), ...quiet },
        );
      else
        await api.post<void>(
          `/board-items/${childId}/reopen`,
          {},
          { idempotencyKey: key(), ...quiet },
        );
    },
  };
}
