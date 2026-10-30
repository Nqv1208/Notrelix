import type { NotrelixClient } from "@notrelix/contracts";
import type {
  CardCover,
  CardRepeatRule,
  CardVoteValue,
} from "@notrelix/work-management-core";

/** One person who voted, with when. */
export interface VoterDto {
  readonly userId: string;
  readonly name: string;
  readonly avatarUrl: string | null;
  readonly votedAt: string;
}

export interface ItemVotesDto extends CardVoteValue {
  readonly voters: readonly VoterDto[];
}

/** One board member who opened the item, with when they last did. */
export interface ViewerDto {
  readonly userId: string;
  readonly name: string;
  readonly avatarUrl: string | null;
  readonly lastViewedAt: string;
}

export interface ItemViewersDto {
  readonly count: number;
  readonly viewers: readonly ViewerDto[];
}

/** A checklist of a board's item, to pick one to copy. */
export interface BoardChecklistEntryDto {
  readonly id: string;
  readonly title: string;
  readonly itemId: string;
  readonly itemKey: string | null;
  readonly itemName: string;
  readonly itemCount: number;
}

/**
 * The item settings and interactions of the card popup: cover, repeat, reminder, votes, viewers, copying a
 * checklist and clearing the links. Settings are written against the item version.
 */
export function createItemCapabilitiesApi(client: NotrelixClient) {
  const { api } = client;
  const quiet = { skipGlobalErrorToast: true } as const;
  const key = () => crypto.randomUUID();
  const path = (itemId: string) => `/board-items/${itemId}`;
  return {
    /** Sets the cover; `null` removes it. */
    async setCover(
      itemId: string,
      cover: CardCover | null,
      expectedVersion: number,
    ): Promise<void> {
      await api.put<void>(
        `${path(itemId)}/cover`,
        {
          color: cover?.color ?? null,
          attachmentId: cover?.attachmentId ?? null,
          style: cover?.style ?? null,
          expectedVersion,
        },
        { idempotencyKey: key(), ...quiet },
      );
    },
    async setRepeat(
      itemId: string,
      rule: CardRepeatRule | null,
      expectedVersion: number,
    ): Promise<void> {
      await api.put<void>(
        `${path(itemId)}/repeat`,
        { rule, expectedVersion },
        { idempotencyKey: key(), ...quiet },
      );
    },
    async setReminder(
      itemId: string,
      offsetMinutes: number,
      expectedVersion: number,
    ): Promise<void> {
      await api.put<void>(
        `${path(itemId)}/reminder`,
        { offsetMinutes, expectedVersion },
        { idempotencyKey: key(), ...quiet },
      );
    },
    async setVote(
      itemId: string,
      fieldId: string,
      voted: boolean,
    ): Promise<CardVoteValue> {
      return api.put<CardVoteValue>(
        `${path(itemId)}/votes/${fieldId}`,
        { voted },
        { idempotencyKey: key(), ...quiet },
      );
    },
    async getVotes(itemId: string, fieldId: string): Promise<ItemVotesDto> {
      return api.get<ItemVotesDto>(`${path(itemId)}/votes/${fieldId}`, quiet);
    },
    /** Records that the member opened the item; the server skips guests and repeats within minutes. */
    async recordView(itemId: string): Promise<void> {
      await api.post<void>(`${path(itemId)}/views`, undefined, quiet);
    },
    async getViewers(itemId: string): Promise<ItemViewersDto> {
      return api.get<ItemViewersDto>(`${path(itemId)}/viewers`, quiet);
    },
    async getBoardChecklists(
      boardId: string,
    ): Promise<readonly BoardChecklistEntryDto[]> {
      return (
        (await api.get<BoardChecklistEntryDto[] | null>(
          `/boards/${boardId}/checklists`,
          quiet,
        )) ?? []
      );
    },
    /** Creates a checklist on the item with the items of another checklist of the board; returns its id. */
    async copyChecklist(
      itemId: string,
      sourceChecklistId: string,
      title?: string,
    ): Promise<string> {
      return api.post<string>(
        `${path(itemId)}/checklists/copy`,
        { sourceChecklistId, title: title ?? null },
        { idempotencyKey: key(), ...quiet },
      );
    },
    /** Removes every item link of the item at once. */
    async clearLinks(itemId: string): Promise<number> {
      return api.delete<number>(`${path(itemId)}/links`, {
        idempotencyKey: key(),
        ...quiet,
      });
    },
  };
}
