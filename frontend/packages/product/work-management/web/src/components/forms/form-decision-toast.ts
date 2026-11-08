import { toast } from "sonner";
import type { FormWriteResult } from "@notrelix/work-management-core";

/** How long a reject, spam mark or delete of a response can be taken back. */
export const DECISION_UNDO_MS = 8000;

/**
 * The toast of a decision on a response (reject, spam, delete) with its undo: the undo returns the response to the
 * New tab. A refused undo says so and leaves the response as the decision left it.
 */
export function toastDecisionWithUndo(
  message: string,
  copy: { readonly undo: string; readonly undoFailed: string },
  undo: () => Promise<FormWriteResult>,
): void {
  toast(message, {
    duration: DECISION_UNDO_MS,
    action: {
      label: copy.undo,
      onClick: () =>
        void undo().then((result) => {
          if (!result.ok) toast.error(copy.undoFailed);
        }),
    },
  });
}
