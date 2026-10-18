import { useCallback } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  createNavigationService,
  type NavigationApiClient,
  type NavigationEndpoints,
} from "../../../core/api/navigation.service";
import {
  navigationEntryKey,
  type NavigationReference,
  type NavigationResourceKind,
} from "../../../core/model/navigation";
import { workspaceQueryKeys } from "../../../query/keys";

interface NavigationMutationDeps {
  api: NavigationApiClient;
  endpoints: NavigationEndpoints;
  /** Idempotency key source; one key per open attempt. */
  createIdempotencyKey?: () => string;
}

export interface RecordRecentInput {
  readonly workspaceId: string;
  readonly kind: NavigationResourceKind;
  readonly resourceId: string;
}

/**
 * Records that the member opened a Board or Doc. Fire-and-forget: the
 * returned function never throws and never blocks navigation; a failure only
 * means the Recent list is not refreshed.
 */
export function createUseRecordRecent({
  api,
  endpoints,
  createIdempotencyKey = () => crypto.randomUUID(),
}: NavigationMutationDeps) {
  const service = createNavigationService(api, endpoints);

  return function useRecordRecent() {
    const queryClient = useQueryClient();

    return useCallback(
      (input: RecordRecentInput): void => {
        const idempotencyKey = createIdempotencyKey();
        const queryKey = workspaceQueryKeys.recents(input.workspaceId);
        service
          .recordRecent(
            input.workspaceId,
            input.kind,
            input.resourceId,
            idempotencyKey,
          )
          .then(async () => {
            // An initial fetch still in flight was read before this write;
            // invalidating would dedupe onto it, so cancel it first.
            await queryClient.cancelQueries({ queryKey });
            await queryClient.invalidateQueries({ queryKey });
          })
          .catch(() => {
            // Recording a recent is best-effort (WX-SHELL-001).
          });
      },
      [queryClient],
    );
  };
}

export interface TogglePinInput {
  readonly kind: NavigationResourceKind;
  readonly resourceId: string;
  /** The desired state: true pins, false unpins. */
  readonly pinned: boolean;
}

/**
 * Pins or unpins for the current member. Pin and unpin are naturally
 * idempotent on the producer, so a retry cannot duplicate an effect. The pin
 * list is patched optimistically and rolled back on failure.
 */
export function createUseTogglePin({ api, endpoints }: NavigationMutationDeps) {
  const service = createNavigationService(api, endpoints);

  return function useTogglePin(workspaceId: string) {
    const queryClient = useQueryClient();
    const key = workspaceQueryKeys.pins(workspaceId);

    return useMutation({
      mutationFn: ({ kind, resourceId, pinned }: TogglePinInput) =>
        pinned
          ? service.pin(workspaceId, kind, resourceId)
          : service.unpin(workspaceId, kind, resourceId),
      onMutate: async ({ kind, resourceId, pinned }: TogglePinInput) => {
        await queryClient.cancelQueries({ queryKey: key });
        const previous = queryClient.getQueryData<NavigationReference[]>(key);
        if (previous) {
          const target = navigationEntryKey(kind, resourceId);
          const rest = previous.filter(
            (reference) =>
              navigationEntryKey(reference.kind, reference.resourceId) !==
              target,
          );
          queryClient.setQueryData<NavigationReference[]>(
            key,
            pinned
              ? [{ kind, resourceId, at: new Date().toISOString() }, ...rest]
              : rest,
          );
        }
        return { previous };
      },
      onError: (_error, _input, context) => {
        if (context?.previous) queryClient.setQueryData(key, context.previous);
      },
      onSettled: () => queryClient.invalidateQueries({ queryKey: key }),
    });
  };
}
