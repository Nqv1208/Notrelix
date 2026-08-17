import type { User } from "@notrelix/features-auth";
import type { WorkspaceInvitation, WorkspaceSummary } from "@notrelix/features-workspace";
import type { Notification } from "@notrelix/features-notifications";
import type { BlockDtoApi, CommentDtoApi as PageCommentDtoApi, PageDtoApi } from "@notrelix/docs-state";
import type { BoardColumnDtoApi, BoardDtoApi, BoardViewDtoApi, CardDtoApi, CommentDtoApi, ListDtoApi } from "@notrelix/work-management-core";

export interface MockMembershipState {
  readonly userId: string;
  readonly workspaceId: string;
  readonly role: "owner" | "admin" | "member" | "guest";
}

export interface MockDatabase {
  users: User[];
  workspaces: WorkspaceSummary[];
  memberships: MockMembershipState[];
  boards: BoardDtoApi[];
  boardViews: Record<string, BoardViewDtoApi>;
  lists: Array<ListDtoApi & { boardId: string }>;
  cards: CardDtoApi[];
  pages: PageDtoApi[];
  blocks: BlockDtoApi[];
  notifications: Notification[];
  invitations: WorkspaceInvitation[];
  cardComments: Array<CommentDtoApi & { cardId: string }>;
  pageComments: Array<PageCommentDtoApi & { pageId: string }>;
  columns: BoardColumnDtoApi[];
  labels: Array<{ id: string; boardId: string; name: string; color: string }>;
}
