import { useCallback, useState } from "react";
import { toast } from "sonner";
import {
  formatCopy,
  useWorkspaceHomeCopy,
} from "@notrelix/features-workspace/web";
import type { MyWorkItem } from "@notrelix/features-workspace/core";
import { toastFailure } from "./failure-toast";
import { useAttemptKey, useCompleteItem } from "./work-management-bridge";

/** Completes My work Items with one idempotency key per attempt. */
export function useMyWorkCompletion(workspaceId: string) {
  const copy = useWorkspaceHomeCopy();
  const complete = useCompleteItem(workspaceId);
  const nextAttemptKey = useAttemptKey();
  const [completingIds, setCompletingIds] = useState<ReadonlySet<string>>(
    () => new Set(),
  );

  const onComplete = useCallback(
    (item: MyWorkItem) => {
      setCompletingIds((ids) => new Set(ids).add(item.id));
      complete.mutate(
        { item, attemptKey: nextAttemptKey() },
        {
          onSuccess: () =>
            toast.success(
              formatCopy(copy.toasts.completed, { name: item.name }),
            ),
          onError: (error) =>
            toastFailure(
              formatCopy(copy.toasts.completeFailed, { name: item.name }),
              error,
              copy.toasts.completeFailedHint,
            ),
          onSettled: () =>
            setCompletingIds((ids) => {
              const next = new Set(ids);
              next.delete(item.id);
              return next;
            }),
        },
      );
    },
    [complete, copy, nextAttemptKey],
  );

  return { completingIds, onComplete };
}
