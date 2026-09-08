import type { Comment } from "../core/types/collaboration";

export function comment(
  overrides: Partial<Comment> & Pick<Comment, "id" | "body">,
): Comment {
  return {
    resourceId: overrides.resourceId ?? "res-1",
    resourceType: overrides.resourceType ?? "page",
    authorId: overrides.authorId ?? "user-1",
    authorName: overrides.authorName ?? "Ada Lovelace",
    mentionIds: overrides.mentionIds ?? [],
    resolved: overrides.resolved ?? false,
    createdAt: overrides.createdAt ?? "2026-01-15T10:30:00.000Z",
    updatedAt: overrides.updatedAt ?? "2026-01-15T10:30:00.000Z",
    ...overrides,
  };
}

export function resourceCommentsDefaultScenario(): Comment[] {
  return [
    comment({
      id: "comment-1",
      authorId: "user-1",
      authorName: "Ada Lovelace",
      body: "Can we link this to the launch checklist?",
    }),
    comment({
      id: "comment-2",
      authorId: "current-user",
      authorName: "You",
      body: "Resolved after adding the owner evidence section.",
      resolved: true,
    }),
  ];
}

export function resourceCommentsEmptyScenario(): Comment[] {
  return [];
}

export function resourceCommentsEdgeDataScenario(): Comment[] {
  return [
    comment({
      id: "comment-edge",
      authorId: "enterprise-reviewer",
      authorName: "Enterprise Reviewer",
      body: "The regional readiness section needs explicit sign-off from security, legal, and workspace operations before this can be published.",
    }),
    comment({
      id: "comment-edge-2",
      authorId: "current-user",
      authorName: "You",
      body: "Tracking sign-off in the decision log.",
    }),
  ];
}
