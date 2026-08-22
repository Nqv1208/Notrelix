import type { NotrelixClient } from "@notrelix/contracts";
import { endpoints } from "@notrelix/contracts";
import type { OperationRequestBody, OperationResponse } from "@notrelix/contracts";
import type { CommentDtoApi } from "@notrelix/work-management-core";
import type { CreateCardUpdateInput } from "@notrelix/work-management-core";
import { mapCommentDtoToCardUpdate } from "@notrelix/work-management-core";

type GetCommentsOp = "Collaboration.Comments.GetBoardItemComments";
type GetCommentsResponse = OperationResponse<GetCommentsOp, 200>;

type CreateCommentOp = "Collaboration.Comments.CreateBoardItemComment";
type CreateCommentBody = OperationRequestBody<CreateCommentOp>;

type UpdateCommentOp = "Collaboration.Comments.Update";
type UpdateCommentBody = OperationRequestBody<UpdateCommentOp>;

type DeleteCommentOp = "Collaboration.Comments.Delete";

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
      const body: CreateCommentBody = { contentMd: input.body };
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

    async deleteCardUpdate(updateId: string): Promise<void> {
      await api.delete<void>(`/comments/${updateId}`);
    },
  };
}
