import { useState, type ReactNode } from "react";
import { toast } from "sonner";
import { chatFailureOf } from "../../core/api/chat-failure";
import type { ChatChannel, ChatChannelGroup } from "../../core/types/chat";
import type { ChatHooks } from "../query/create-chat-hooks";
import type { ChatCopy } from "./chat.copy";
import { CategoryNameDialog } from "./chat-group-dialogs";
import { ConfirmDialog } from "./chat-dialogs";
import type { ChatSidebarProps } from "./chat-sidebar";
import {
  useCollapsedGroups,
  type ChatCollapseStore,
} from "./use-collapsed-groups";
import { fillCopy } from "./use-chat-copy";

type NameDialog =
  | { readonly mode: "create" }
  | { readonly mode: "rename"; readonly group: ChatChannelGroup };

const TOAST_ID = "chat-categories";

export interface CategoryManagement {
  /** The props the sidebar takes for categories, drag and collapse. */
  readonly sidebar: Pick<
    ChatSidebarProps,
    | "groups"
    | "onCreateCategory"
    | "category"
    | "canReorderChannel"
    | "onReorderChannels"
    | "onReorderGroups"
    | "isCollapsed"
    | "onToggleCollapse"
  >;
  /** The create, rename and delete dialogs of the categories. */
  readonly dialogs: ReactNode;
}

/**
 * Category management for the chat workspace (UC-CHT-A02, A04): the dialogs and handlers behind the sidebar's New
 * category entry, the category menu and the drag. Only an Owner or an Admin is offered the category controls (the
 * default baseline of chat.group.manage; a Member with the right granted waits for an effective-permission read), and a
 * 403 from the server still reads as permission denied. A channel is dragged by a channel manager, an Owner or an Admin.
 */
export function useCategoryManagement({
  hooks,
  workspaceId,
  copy,
  groups,
  canManageGroups,
  canManageChannels,
  collapseStore,
}: {
  readonly hooks: ChatHooks;
  readonly workspaceId: string;
  readonly copy: ChatCopy;
  readonly groups: readonly ChatChannelGroup[] | undefined;
  /** An Owner or an Admin of the Workspace who is not a Guest. */
  readonly canManageGroups: boolean;
  /** Any non-Guest who may reorder channels they manage (the sidebar then checks each channel's role). */
  readonly canManageChannels: boolean;
  /** The host's key-value storage for the reader's own collapse of categories; without it every category shows open. */
  readonly collapseStore?: ChatCollapseStore;
}): CategoryManagement {
  const text = copy.groups;
  const creator = hooks.useCreateChannelGroup(workspaceId);
  const updater = hooks.useUpdateChannelGroups(workspaceId);
  const deleter = hooks.useDeleteChannelGroup(workspaceId);
  const reorderer = hooks.useReorderChannels(workspaceId);
  const collapse = useCollapsedGroups(workspaceId, collapseStore);
  const [dialog, setDialog] = useState<NameDialog | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [deleting, setDeleting] = useState<ChatChannelGroup | null>(null);

  function failureText(error: unknown): string {
    const kind = chatFailureOf(error).kind;
    if (kind === "name-conflict") return text.nameTaken;
    if (kind === "forbidden") return text.forbidden;
    if (kind === "network") return copy.feedback.network;
    if (kind === "stale") return text.stale;
    return text.failed;
  }

  /** A refused move shows once; the hook has already put the old order back and read the server again. */
  function reorderFailed(error: unknown) {
    const kind = chatFailureOf(error).kind;
    toast.error(
      kind === "forbidden"
        ? copy.feedback.forbidden
        : kind === "network"
          ? copy.feedback.network
          : kind === "stale"
            ? text.stale
            : text.reorderFailed,
      { id: TOAST_ID },
    );
  }

  function submitName(name: string) {
    if (!dialog) return;
    setError(null);
    if (dialog.mode === "create") {
      creator.mutate(
        { name },
        {
          onSuccess: () => setDialog(null),
          onError: (failure) => setError(failureText(failure)),
        },
      );
      return;
    }
    const { group } = dialog;
    if (name === group.name) {
      setDialog(null);
      return;
    }
    updater.mutate([{ group, name, position: group.position }], {
      onSuccess: () => setDialog(null),
      onError: (failure) => {
        if (chatFailureOf(failure).kind === "stale") {
          setDialog(null);
          toast.error(text.stale, { id: TOAST_ID });
          return;
        }
        setError(failureText(failure));
      },
    });
  }

  function confirmDelete() {
    if (!deleting) return;
    deleter.mutate(deleting, {
      onSuccess: () => setDeleting(null),
      onError: (failure) => {
        setDeleting(null);
        const kind = chatFailureOf(failure).kind;
        toast.error(
          kind === "forbidden"
            ? text.forbidden
            : kind === "stale"
              ? text.stale
              : kind === "network"
                ? copy.feedback.network
                : text.deleteFailed,
          { id: TOAST_ID },
        );
      },
    });
  }

  const dialogs = (
    <>
      {dialog ? (
        <CategoryNameDialog
          key={dialog.mode === "rename" ? dialog.group.id : "create"}
          open
          mode={dialog.mode}
          initialName={dialog.mode === "rename" ? dialog.group.name : ""}
          busy={creator.isPending || updater.isPending}
          error={error}
          copy={copy}
          onOpenChange={(open) => {
            if (!open) setDialog(null);
          }}
          onSubmit={submitName}
        />
      ) : null}
      <ConfirmDialog
        open={deleting !== null}
        title={text.deleteTitle}
        body={fillCopy(text.deleteBody, { name: deleting?.name ?? "" })}
        confirmLabel={text.deleteConfirm}
        cancelLabel={text.cancel}
        destructive
        busy={deleter.isPending}
        onOpenChange={(open) => {
          if (!open) setDeleting(null);
        }}
        onConfirm={confirmDelete}
      />
    </>
  );

  return {
    dialogs,
    sidebar: {
      groups,
      onCreateCategory: canManageGroups
        ? () => {
            setError(null);
            setDialog({ mode: "create" });
          }
        : undefined,
      category: canManageGroups
        ? {
            onRename: (group) => {
              setError(null);
              setDialog({ mode: "rename", group });
            },
            onDelete: setDeleting,
          }
        : undefined,
      canReorderChannel: (channel: ChatChannel) =>
        canManageChannels &&
        !channel.isArchived &&
        (canManageGroups || channel.role === "manager"),
      onReorderChannels: (placements) =>
        reorderer.mutate(placements, { onError: reorderFailed }),
      onReorderGroups: canManageGroups
        ? (moves) =>
            updater.mutate(
              moves.map((move) => ({
                group: move.group,
                name: move.group.name,
                position: move.position,
              })),
              { onError: reorderFailed },
            )
        : undefined,
      isCollapsed: collapse.isCollapsed,
      onToggleCollapse: collapse.toggle,
    },
  };
}
