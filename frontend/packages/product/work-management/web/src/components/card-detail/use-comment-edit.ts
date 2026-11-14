import { useState } from "react";

/**
 * Inline editing of one comment, shared by the side sheet and the item page:
 * start, type, save (only a non-empty change is sent) or cancel (the text
 * goes back to the saved comment). `canSave` is false for an empty or
 * unchanged text, and saving it only closes the edit.
 */
export function useCommentEdit(
  savedBody: string,
  onSave: (body: string) => void,
) {
  const [editing, setEditing] = useState(false);
  const [body, setBody] = useState(savedBody);
  const next = body.trim();
  const canSave = next !== "" && next !== savedBody.trim();

  return {
    editing,
    body,
    setBody,
    canSave,
    start: () => {
      setBody(savedBody);
      setEditing(true);
    },
    cancel: () => {
      setBody(savedBody);
      setEditing(false);
    },
    save: () => {
      if (canSave) onSave(next);
      setEditing(false);
    },
  };
}
