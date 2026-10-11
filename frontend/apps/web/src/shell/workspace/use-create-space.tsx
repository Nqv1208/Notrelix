import { useCallback, useMemo, useState } from "react";
import { toast } from "sonner";
import {
  appendPlacement,
  buildSpaceIndex,
  describeWorkspaceFailure,
} from "@notrelix/features-workspace/core";
import {
  CreateSpaceDialog,
  formatCopy,
  useWorkspaceSpacesCopy,
  type CreateSpaceValues,
} from "@notrelix/features-workspace/web";
import { useShellNavigation } from "./use-shell-navigation";
import { useSpaceHooks } from "./use-workspace-spaces";

type Parent = { readonly id: string; readonly name: string } | null;

/**
 * Create a Space (WSP-021): the dialog, the producer call (appended after
 * the last visible sibling), then navigation to the new Space. A rule or
 * validation failure (for example a duplicate sibling name) stays in the
 * dialog with the typed values kept.
 */
export function useCreateSpace(
  workspaceId: string,
  workspaceName: string | null,
) {
  const copy = useWorkspaceSpacesCopy();
  const navigation = useShellNavigation();
  const hooks = useSpaceHooks();
  const tree = hooks.useSpaceTree(workspaceId);
  const { create } = hooks.useSpaceMutations(workspaceId);
  const index = useMemo(() => buildSpaceIndex(tree.data ?? []), [tree.data]);
  // `undefined` = closed; `null` = a top-level Space.
  const [parent, setParent] = useState<Parent | undefined>(undefined);
  const [error, setError] = useState<string | null>(null);

  const open = useCallback((next: Parent) => {
    setError(null);
    setParent(next);
  }, []);

  const onSubmit = useCallback(
    (values: CreateSpaceValues) => {
      const parentId = parent?.id ?? null;
      setError(null);
      create.mutate(
        { ...values, parentId, ...appendPlacement(index, parentId) },
        {
          onSuccess: (id) => {
            setParent(undefined);
            toast.success(
              formatCopy(copy.toasts.created, { name: values.name }),
            );
            if (id) navigation.onNavigate({ section: "space", spaceId: id });
          },
          onError: (failure) => {
            const described = describeWorkspaceFailure(failure);
            const field =
              Object.values(described.fieldErrors).flat()[0] ?? null;
            setError(
              described.kind === "rule" || described.kind === "validation"
                ? (described.detail ?? field ?? copy.toasts.createFailed)
                : described.kind === "forbidden"
                  ? copy.toasts.forbidden
                  : copy.toasts.createFailed,
            );
          },
        },
      );
    },
    [copy, create, index, navigation, parent],
  );

  const dialog = (
    <CreateSpaceDialog
      copy={copy}
      open={parent !== undefined}
      parent={parent ?? null}
      workspaceName={workspaceName}
      pending={create.isPending}
      error={error}
      onOpenChange={(next) => {
        if (!next) setParent(undefined);
      }}
      onSubmit={onSubmit}
    />
  );

  return { open, dialog };
}
