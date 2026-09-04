import { useState } from "react";
import {
  Check,
  MessageSquareText,
  MoreHorizontal,
  Pencil,
  Trash2,
  X,
} from "lucide-react";
import { Avatar, AvatarFallback } from "@notrelix/ui-web";
import { Button } from "@notrelix/ui-web";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@notrelix/ui-web";
import { Textarea } from "@notrelix/ui-web";
import { Skeleton } from "@notrelix/ui-web";
import type { CardDetail, CardUpdate } from "@notrelix/work-management-core";
import { TaskDetailEmptyState } from "./task-detail-empty-state";
import { UpdateComposer } from "./update-composer";
import type {
  TaskDetailCallbacks,
  TaskDetailCapabilities,
} from "./task-detail-types";

export function TaskUpdatesTab({
  card,
  updates,
  isLoading,
  capabilities,
  onCreateUpdate,
  onUpdateUpdate,
  onDeleteUpdate,
}: {
  card: CardDetail;
  updates: readonly CardUpdate[];
  isLoading: boolean;
  capabilities: TaskDetailCapabilities;
  onCreateUpdate: TaskDetailCallbacks["onCreateUpdate"];
  onUpdateUpdate: TaskDetailCallbacks["onUpdateUpdate"];
  onDeleteUpdate: TaskDetailCallbacks["onDeleteUpdate"];
}) {
  return (
    <div className="flex flex-col gap-3 p-3.5">
      <UpdateComposer
        cardId={card.id}
        members={card.members.length > 0 ? card.members : card.watchers}
        disabled={!capabilities.canEditFields}
        onCreateUpdate={onCreateUpdate}
      />

      {isLoading ? (
        <div className="flex flex-col gap-2">
          <Skeleton className="h-14 rounded-lg" />
          <Skeleton className="h-14 rounded-lg" />
        </div>
      ) : updates.length === 0 ? (
        <TaskDetailEmptyState
          icon={MessageSquareText}
          title="No updates yet"
          description="Share decisions, blockers, and context so everyone can follow the task."
        />
      ) : (
        <div className="flex flex-col gap-1.5 mt-1">
          {updates.map((update) => (
            <UpdateItem
              key={update.id}
              update={update}
              canEdit={capabilities.canEditFields}
              canDelete={capabilities.canDelete}
              onSave={(body) => onUpdateUpdate(update.id, body)}
              onDelete={() => onDeleteUpdate(update.id)}
            />
          ))}
        </div>
      )}
    </div>
  );
}

function UpdateItem({
  update,
  canEdit,
  canDelete,
  onSave,
  onDelete,
}: {
  update: CardUpdate;
  canEdit: boolean;
  canDelete: boolean;
  onSave: (body: string) => void;
  onDelete: () => void;
}) {
  const [editing, setEditing] = useState(false);
  const [body, setBody] = useState(update.body);

  return (
    <article className="group/comment flex items-start gap-2.5 rounded-lg px-2 py-2 transition-colors hover:bg-muted/30">
      <Avatar className="size-7 shrink-0 mt-0.5">
        <AvatarFallback className="bg-foreground text-[10px] font-bold text-background">
          {update.author.initials}
        </AvatarFallback>
      </Avatar>
      <div className="min-w-0 flex-1">
        <div className="flex items-baseline justify-between gap-2">
          <div className="flex items-baseline gap-2">
            <span className="text-xs font-semibold text-foreground">
              {update.author.name}
            </span>
            <span className="text-[10px] text-muted-foreground/70">
              {new Date(update.createdAt).toLocaleDateString(undefined, {
                month: "short",
                day: "numeric",
                hour: "2-digit",
                minute: "2-digit",
              })}
            </span>
          </div>
          <div className="opacity-0 transition-opacity shrink-0 group-hover/comment:opacity-100">
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button
                  variant="ghost"
                  size="icon-sm"
                  className="size-6 p-0 hover:bg-muted"
                  aria-label="Update actions"
                  disabled={!canEdit && !canDelete}
                >
                  <MoreHorizontal className="size-3.5 text-muted-foreground" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="w-24">
                {canEdit ? (
                  <DropdownMenuItem
                    onClick={() => setEditing(true)}
                    className="text-xs py-1"
                  >
                    <Pencil className="mr-1.5 size-3.5" />
                    Edit
                  </DropdownMenuItem>
                ) : null}
                {canDelete ? (
                  <DropdownMenuItem
                    className="text-destructive text-xs py-1"
                    onClick={onDelete}
                  >
                    <Trash2 className="mr-1.5 size-3.5" />
                    Delete
                  </DropdownMenuItem>
                ) : null}
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </div>
        {editing ? (
          <div className="mt-2 space-y-2">
            <Textarea
              value={body}
              onChange={(event) => setBody(event.target.value)}
              className="min-h-16 text-xs resize-none bg-background p-2"
            />
            <div className="flex justify-end gap-1.5">
              <Button
                variant="ghost"
                size="sm"
                className="h-7 px-2 text-[10px] gap-1"
                onClick={() => {
                  setBody(update.body);
                  setEditing(false);
                }}
              >
                <X className="size-3" />
                Cancel
              </Button>
              <Button
                size="sm"
                className="h-7 px-2 text-[10px] gap-1"
                onClick={() => {
                  const next = body.trim();
                  if (next) onSave(next);
                  setEditing(false);
                }}
              >
                <Check className="size-3" />
                Save
              </Button>
            </div>
          </div>
        ) : (
          <p className="mt-1 text-xs leading-relaxed text-muted-foreground whitespace-pre-wrap">
            {update.body}
          </p>
        )}
      </div>
    </article>
  );
}
