import type { ID } from './ids';

export interface PageComment {
  id: ID;
  pageId: ID;
  blockId: ID | null;
  authorId: ID;
  body: string;
  mentionIds: ID[];
  resolved: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateCommentPayload {
  pageId: ID;
  blockId?: ID | null;
  body: string;
  mentionIds?: ID[];
}
