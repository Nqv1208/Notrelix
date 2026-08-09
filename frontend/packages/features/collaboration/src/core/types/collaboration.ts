export interface Comment {
  id: string;
  resourceId: string;
  resourceType: "page" | "block" | "card";
  authorId: string;
  authorName: string;
  body: string;
  mentionIds: string[];
  resolved: boolean;
  parentId?: string;
  createdAt: string;
  updatedAt: string;
}

export interface Reaction {
  id: string;
  resourceId: string;
  userId: string;
  emoji: string;
  createdAt: string;
}

export interface Presence {
  userId: string;
  name: string;
  avatarUrl?: string;
  status: "active" | "idle" | "offline";
  lastSeenAt: string;
}

export interface Attachment {
  id: string;
  resourceId: string;
  fileName: string;
  fileSize: number;
  mimeType: string;
  url: string;
  uploadedBy: string;
  createdAt: string;
}
