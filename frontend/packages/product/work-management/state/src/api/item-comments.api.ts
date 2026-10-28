import type { NotrelixClient } from "@notrelix/contracts";
import { endpoints } from "@notrelix/contracts";
import type { OperationRequestBody } from "@notrelix/contracts";
import type { CreateCardUpdateInput } from "@notrelix/work-management-core";
import { mapCommentDtoToCardUpdate } from "./board-api-mappers";

type CreateCommentOp = "Collaboration.Comments.CreateBoardItemComment";
type CreateCommentBody = OperationRequestBody<CreateCommentOp>;

type UpdateCommentOp = "Collaboration.Comments.Update";
type UpdateCommentBody = OperationRequestBody<UpdateCommentOp>;

export function createCommentApi(client: NotrelixClient) {
  const api = client.api;
  return {
    async getCardUpdates(cardId: string) {
      const comments = await api.get<any>(
        endpoints.boardItems.comments(cardId),
      );
      return (comments || []).map((comment: any) =>
        mapCommentDtoToCardUpdate(comment, cardId),
      );
    },

    async createCardUpdate(input: CreateCardUpdateInput) {
      const body: CreateCommentBody = {
        contentMd: input.body,
        // The people the comment names are notified (mention chain).
        mentionedUserIds: input.mentionUserIds?.length
          ? [...input.mentionUserIds]
          : undefined,
        parentCommentId: input.parentCommentId,
      };
      const id = await api.post<any>(
        endpoints.boardItems.comments(input.cardId),
        body,
      );
      return {
        id,
        cardId: input.cardId,
        author: {
          id: "current-user",
          userId: "current-user",
          name: "You",
          initials: "Y",
          color: "var(--primary)",
        },
        body: input.body,
        mentionUserIds: input.mentionUserIds,
        attachmentIds: input.attachmentIds,
        createdAt: new Date().toISOString(),
      };
    },

    async updateCardUpdate(updateId: string, bodyText: string): Promise<void> {
      const body: UpdateCommentBody = {
        contentMd: bodyText,
      };
      await api.patch<void>(`/comments/${updateId}`, body);
    },

    async setCommentReaction(
      cardId: string,
      commentId: string,
      emoji: string,
      on: boolean,
    ): Promise<void> {
      const path = `${endpoints.boardItems.comments(cardId)}/${commentId}/reactions/${encodeURIComponent(emoji)}`;
      if (on) await api.put<void>(path);
      else await api.delete<void>(path);
    },

    async deleteCardUpdate(updateId: string): Promise<void> {
      await api.delete<void>(`/comments/${updateId}`);
    },
  };
}
