import { useState } from "react";
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Skeleton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@notrelix/ui-web";
import {
  CHAT_CHANNEL_ACTIONS,
  CHAT_PERMISSION_SUBJECTS,
  type ChatChannelAction,
  type ChatChannelPermissions,
  type ChatPermissionEntry,
  type ChatPermissionSubject,
} from "../../core/types/chat";
import {
  allowOffered,
  cellOf,
  cellsOf,
  denyOffered,
  entriesOf,
  isReadOnlyMatrix,
  readOnlyCells,
  sameCells,
  withCell,
  type ChatPermissionCell,
  type ChatPermissionCells,
} from "../../core/model/permission-model";
import type { ChatCopy } from "./chat.copy";
import { fillCopy } from "./use-chat-copy";

export interface ChatPermissionsDialogProps {
  readonly open: boolean;
  readonly copy: ChatCopy;
  /** The stored overrides; absent while loading or after a failed read. */
  readonly permissions: ChatChannelPermissions | undefined;
  readonly loading: boolean;
  readonly loadFailed: boolean;
  readonly busy: boolean;
  /** An error from the last save, already worded for the user. */
  readonly error: string | null;
  readonly onOpenChange: (open: boolean) => void;
  readonly onRetry: () => void;
  readonly onSave: (
    entries: readonly ChatPermissionEntry[],
    expectedVersion: number,
  ) => void;
}

const STATES: readonly ChatPermissionCell[] = ["default", "allow", "deny"];

/**
 * The Permissions of a channel (UC-CHT-H01): a role matrix with the actions as rows and the roles as columns, each cell the default of
 * the role, an allow or a deny. Only a channel manager, an Owner or an Admin opens it. The matrix is edited as a draft and sent as the
 * whole set; a newer version from the server replaces the draft (the dialog is keyed by it).
 */
export function ChatPermissionsDialog({
  open,
  copy,
  permissions,
  loading,
  loadFailed,
  busy,
  error,
  onOpenChange,
  onRetry,
  onSave,
}: ChatPermissionsDialogProps) {
  const text = copy.permissions;
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle>{text.title}</DialogTitle>
          <DialogDescription>{text.description}</DialogDescription>
        </DialogHeader>
        {permissions ? (
          <PermissionMatrix
            key={permissions.version}
            copy={copy}
            permissions={permissions}
            busy={busy}
            error={error}
            onCancel={() => onOpenChange(false)}
            onSave={onSave}
          />
        ) : loadFailed ? (
          <div className="flex flex-col items-start gap-3">
            <p role="alert" className="text-sm text-destructive">
              {text.loadFailed}
            </p>
            <Button type="button" variant="outline" onClick={onRetry}>
              {text.retry}
            </Button>
          </div>
        ) : (
          <div
            role="status"
            aria-label={text.loading}
            className="flex flex-col gap-2"
            aria-busy={loading}
          >
            <Skeleton className="h-8 w-full" />
            <Skeleton className="h-8 w-full" />
            <Skeleton className="h-8 w-full" />
          </div>
        )}
      </DialogContent>
    </Dialog>
  );
}

function PermissionMatrix({
  copy,
  permissions,
  busy,
  error,
  onCancel,
  onSave,
}: {
  readonly copy: ChatCopy;
  readonly permissions: ChatChannelPermissions;
  readonly busy: boolean;
  readonly error: string | null;
  readonly onCancel: () => void;
  readonly onSave: ChatPermissionsDialogProps["onSave"];
}) {
  const text = copy.permissions;
  const stored = cellsOf(permissions.entries);
  const [cells, setCells] = useState<ChatPermissionCells>(stored);
  const dirty = !sameCells(cells, stored);
  const readOnly = isReadOnlyMatrix(cells);

  function change(
    subject: ChatPermissionSubject,
    action: ChatChannelAction,
    cell: ChatPermissionCell,
  ) {
    setCells((current) => withCell(current, subject, action, cell));
  }

  return (
    <form
      className="flex flex-col gap-4"
      onSubmit={(event) => {
        event.preventDefault();
        if (busy || !dirty) return;
        onSave(entriesOf(cells), permissions.version);
      }}
    >
      <div className="flex flex-wrap items-center gap-2">
        <Button
          type="button"
          variant="outline"
          size="sm"
          aria-pressed={readOnly}
          aria-describedby="chat-permissions-readonly-hint"
          disabled={busy}
          onClick={() => setCells((current) => readOnlyCells(current))}
        >
          {text.readOnlyPreset}
        </Button>
        <Button
          type="button"
          variant="ghost"
          size="sm"
          disabled={busy || Object.keys(cells).length === 0}
          onClick={() => setCells({})}
        >
          {text.reset}
        </Button>
        <p
          id="chat-permissions-readonly-hint"
          className="min-w-0 flex-1 text-xs text-muted-foreground"
        >
          {text.readOnlyPresetHint}
        </p>
      </div>

      <Table aria-label={text.matrixLabel}>
        <TableHeader>
          <TableRow>
            <TableHead scope="col">{text.actionHeader}</TableHead>
            {CHAT_PERMISSION_SUBJECTS.map((subject) => (
              <TableHead key={subject} scope="col">
                {text.roles[subject]}
              </TableHead>
            ))}
          </TableRow>
        </TableHeader>
        <TableBody>
          {CHAT_CHANNEL_ACTIONS.map((action) => (
            <TableRow key={action}>
              <TableHead scope="row" className="font-medium">
                {text.actions[action]}
              </TableHead>
              {CHAT_PERMISSION_SUBJECTS.map((subject) => {
                const value = cellOf(cells, subject, action);
                return (
                  <TableCell key={subject}>
                    <select
                      aria-label={fillCopy(text.cell, {
                        action: text.actions[action],
                        role: text.roles[subject],
                      })}
                      value={value}
                      disabled={busy}
                      onChange={(event) =>
                        change(
                          subject,
                          action,
                          event.target.value as ChatPermissionCell,
                        )
                      }
                      className="h-8 w-full min-w-24 rounded-md border border-input bg-background px-2 text-sm text-foreground focus-visible:outline-2 focus-visible:outline-ring disabled:opacity-60"
                    >
                      {STATES.filter(
                        (state) =>
                          state === value ||
                          state === "default" ||
                          (state === "allow" &&
                            allowOffered(subject, action)) ||
                          (state === "deny" && denyOffered(subject, action)),
                      ).map((state) => (
                        <option key={state} value={state}>
                          {text.states[state]}
                        </option>
                      ))}
                    </select>
                  </TableCell>
                );
              })}
            </TableRow>
          ))}
        </TableBody>
      </Table>
      <p className="text-xs text-muted-foreground">{text.managerInviteNote}</p>

      {error ? (
        <p role="alert" className="text-sm text-destructive">
          {error}
        </p>
      ) : null}
      <DialogFooter>
        <Button type="button" variant="ghost" onClick={onCancel}>
          {text.cancel}
        </Button>
        <Button type="submit" disabled={busy || !dirty}>
          {text.save}
        </Button>
      </DialogFooter>
    </form>
  );
}
