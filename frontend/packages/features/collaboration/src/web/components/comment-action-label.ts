import type { Comment } from "../../core/types/collaboration";

const EXCERPT_MAX_LENGTH = 40;

/**
 * Accessible name for a per-comment action. Names the comment by its author
 * and a short body excerpt so rows are distinguishable; never exposes the id.
 */
export function commentActionLabel(
  action: string,
  comment: Pick<Comment, "authorName" | "body">,
): string {
  const text = comment.body.replace(/\s+/g, " ").trim();
  const excerpt =
    text.length > EXCERPT_MAX_LENGTH
      ? `${text.slice(0, EXCERPT_MAX_LENGTH).trimEnd()}…`
      : text;
  return excerpt
    ? `${action} by ${comment.authorName}: "${excerpt}"`
    : `${action} by ${comment.authorName}`;
}
