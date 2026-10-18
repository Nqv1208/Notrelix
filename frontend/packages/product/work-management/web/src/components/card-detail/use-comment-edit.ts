import { useState } from "react";

/**
 * Inline editing of one comment, shared by the side sheet and the item page:
 * start, type, save (only a non-empty change is sent) or cancel (the text
 * goes back to the saved comment).
 */
export function useCommentEdit(
  savedBody: string,
  onSave: (body: string) => void,
) {
  const [editing, setEditing] = useState(false);
  const [body, setBody] = useState(savedBody);

  return {
    editing,
    body,
    setBody,
    start: () => setEditing(true),
    cancel: () => {
      setBody(savedBody);
      setEditing(false);
    },
    save: () => {
      const next = body.trim();
      if (next) onSave(next);
      setEditing(false);
    },
  };
}
