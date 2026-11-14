/**
 * @notrelix/feature-collaboration — Collaboration query keys.
 *
 * Type A: CRUD server state (comments, mentions, reactions, presence, attachments).
 */

export const collaborationQueryKeys = {
  // The Workspace is part of the key, so a switch of Workspace reads a different entry and a late response
  // for the previous Workspace lands under its own key, never in the one on screen (UC-COL-G15).
  comments: (workspaceId: string, resourceId: string) =>
    ["collaboration", "comments", workspaceId, resourceId] as const,
  mentions: (workspaceId: string) =>
    ["collaboration", "mentions", workspaceId] as const,
  reactions: (resourceId: string) =>
    ["collaboration", "reactions", resourceId] as const,
  presence: (workspaceId: string) =>
    ["collaboration", "presence", workspaceId] as const,
  attachments: (resourceId: string) =>
    ["collaboration", "attachments", resourceId] as const,
} as const;
