import { useState } from "react";
import { MoreHorizontal } from "lucide-react";
import {
  Avatar,
  AvatarFallback,
  Button,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  Skeleton,
  Textarea,
} from "@notrelix/ui-web";
import type { CardDetail, CardUpdate } from "@notrelix/work-management-core";
import { describeSheetTime } from "./item-sheet-format";
import type { ItemSheetCopy } from "./item-sheet.copy";
import type {
  TaskDetailCallbacks,
  TaskDetailCapabilities,
} from "./task-detail-types";

/** "Bình luận" subtab: the item's comments, oldest first. */
export function TaskUpdatesTab({
  updates,
  isLoading,
  capabilities,
  copy,
  now,
  timeZone,
  onUpdateUpdate,
  onDeleteUpdate,
}: {
  card: CardDetail;
  updates: readonly CardUpdate[];
  isLoading: boolean;
  capabilities: TaskDetailCapabilities;
  copy: ItemSheetCopy;
  now: Date;
  timeZone?: string;
  onCreateUpdate: TaskDetailCallbacks["onCreateUpdate"];
  onUpdateUpdate: TaskDetailCallbacks["onUpdateUpdate"];
  onDeleteUpdate: TaskDetailCallbacks["onDeleteUpdate"];
}) {
  if (isLoading) {
    return (
      <div className="flex flex-col gap-2" aria-busy="true">
        <Skeleton className="h-12 rounded-lg" />
        <Skeleton className="h-12 rounded-lg" />
      </div>
    );
  }
  if (updates.length === 0) {
    return (
      <p className="m-0 text-sm text-muted-foreground">
        {copy.sections.noComments}
      </p>
    );
  }
  return (
    <ul className="m-0 flex list-none flex-col p-0">
      {updates.map((update) => (
        <li key={update.id}>
          <CommentItem
            update={update}
            canEdit={capabilities.canEditFields}
            canDelete={capabilities.canDelete}
            copy={copy}
            time={describeSheetTime(update.createdAt, now, copy, timeZone)}
            onSave={(body) => onUpdateUpdate(update.id, body)}
            onDelete={() => onDeleteUpdate(update.id)}
          />
        </li>
      ))}
    </ul>
  );
}

function CommentItem({
  update,
  canEdit,
  canDelete,
  copy,
  time,
  onSave,
  onDelete,
}: {
  update: CardUpdate;
  canEdit: boolean;
  canDelete: boolean;
  copy: ItemSheetCopy;
  time: string;
  onSave: (body: string) => void;
  onDelete: () => void;
}) {
  const [editing, setEditing] = useState(false);
  const [body, setBody] = useState(update.body);
  const t = copy.comment;

  return (
    <article className="group/comment flex gap-2.5 py-1.5 text-sm">
      <Avatar className="mt-0.5 size-6.5 shrink-0">
        <AvatarFallback className="bg-foreground text-caption font-bold text-background">
          {update.author.initials}
        </AvatarFallback>
      </Avatar>
      <div className="min-w-0 flex-1">
        <div className="flex items-baseline gap-1.5">
          <span className="font-semibold text-foreground">
            {update.author.name}
          </span>
          <time dateTime={update.createdAt} className="text-xs text-muted-foreground">
            {time}
          </time>
          {canEdit || canDelete ? (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button
                  type="button"
                  variant="ghost"
                  size="icon-sm"
                  aria-label={t.actions}
                  className="ml-auto size-6 text-muted-foreground opacity-0 group-hover/comment:opacity-100 focus-visible:opacity-100"
                >
                  <MoreHorizontal aria-hidden="true" className="size-3.5" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                {canEdit ? (
                  <DropdownMenuItem onSelect={() => setEditing(true)}>
                    {t.edit}
                  </DropdownMenuItem>
                ) : null}
                {canDelete ? (
                  <DropdownMenuItem onSelect={onDelete}>
                    {t.delete}
                  </DropdownMenuItem>
                ) : null}
              </DropdownMenuContent>
            </DropdownMenu>
          ) : null}
        </div>
        {editing ? (
          <div className="mt-2 flex flex-col gap-2">
            <Textarea
              value={body}
              aria-label={t.editLabel}
              onChange={(event) => setBody(event.target.value)}
              className="min-h-16 resize-none text-sm"
            />
            <div className="flex justify-end gap-1.5">
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={() => {
                  setBody(update.body);
                  setEditing(false);
                }}
              >
                {t.cancel}
              </Button>
              <Button
                type="button"
                size="sm"
                onClick={() => {
                  const next = body.trim();
                  if (next) onSave(next);
                  setEditing(false);
                }}
              >
                {t.save}
              </Button>
            </div>
          </div>
        ) : (
          <p className="m-0 mt-0.5 leading-relaxed whitespace-pre-wrap text-foreground">
            {update.body}
          </p>
        )}
      </div>
    </article>
  );
}
