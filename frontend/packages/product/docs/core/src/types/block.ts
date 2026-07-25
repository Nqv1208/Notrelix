import type { ID } from './ids';

export type BlockType =
  | 'paragraph'
  | 'heading_1'
  | 'heading_2'
  | 'heading_3'
  | 'bulleted_list'
  | 'numbered_list'
  | 'todo'
  | 'quote'
  | 'divider'
  | 'code'
  | 'callout'
  | 'toggle'
  | 'image'
  | 'embed'
  | 'table'
  | 'board_reference'
  | 'page_reference';

export interface BlockProperties {
  text?: string;
  checked?: boolean;
  language?: string;
  url?: string;
  caption?: string;
  color?: string;
  icon?: string;
  title?: string;
  items?: string[];
  rows?: string[][];
  linkedPageId?: ID;
  linkedBoardId?: ID;
  linkedTaskId?: ID;
  mentionIds?: ID[];
  align?: 'left' | 'center' | 'right';
  fontFamily?: 'inter' | 'poppins' | 'serif' | 'mono';
  fontSize?: 'sm' | 'base' | 'lg' | 'xl';
  bold?: boolean;
  italic?: boolean;
  underline?: boolean;
  strike?: boolean;
  textColor?: 'default' | 'muted' | 'primary' | 'accent' | 'destructive';
  highlight?: 'none' | 'muted' | 'accent' | 'primary';
  commentsCount?: number;
}

export interface Block {
  id: ID;
  pageId: ID;
  type: BlockType;
  properties: BlockProperties;
  position: number;
  parentId: ID | null;
  children?: Block[];
  createdById: ID;
  updatedById: ID;
  createdAt: string;
  updatedAt: string;
}

export interface CreateBlockPayload {
  type: BlockType;
  properties?: BlockProperties;
  position?: number;
  parentId?: ID | null;
}

export interface UpdateBlockPayload {
  type?: BlockType;
  properties?: BlockProperties;
  position?: number;
  parentId?: ID | null;
}

export interface ReorderBlocksInput {
  pageId: ID;
  orderedBlockIds: ID[];
}
