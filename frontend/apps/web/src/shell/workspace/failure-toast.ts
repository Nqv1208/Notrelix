import { toast } from "sonner";
import {
  describeWorkspaceFailure,
  type WorkspaceFailure,
} from "@notrelix/features-workspace/core";

/**
 * A precise failure toast (DESIGN.md §30.2, §34): the title says what did
 * not happen; the description is the producer's own rule or validation
 * message when it sent one, otherwise the copy's recovery hint.
 */
export function toastFailure(
  title: string,
  error: unknown,
  hint: string | ((failure: WorkspaceFailure) => string),
): WorkspaceFailure {
  const failure = describeWorkspaceFailure(error);
  const fieldMessage = Object.values(failure.fieldErrors).flat()[0] ?? null;
  const producerMessage =
    failure.kind === "rule" || failure.kind === "validation"
      ? ((failure.detail && failure.kind === "rule" ? failure.detail : null) ??
        fieldMessage ??
        failure.detail)
      : null;
  toast.error(title, {
    description:
      producerMessage ?? (typeof hint === "function" ? hint(failure) : hint),
  });
  return failure;
}
