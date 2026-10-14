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
  cn,
} from "@notrelix/ui-web";
import type {
  CardActivity,
  CardDetail,
  CardUpdate,
} from "@notrelix/work-management-core";
import { identityMarkClass } from "../shared/item-display";
import { describeSheetTime } from "./item-sheet-format";
import type { ItemSheetCopy } from "./item-sheet.copy";
import { useCommentEdit } from "./use-comment-edit";
import type {
  TaskDetailCallbacks,
  TaskDetailCapabilities,
} from "./task-detail-types";

/** "Bình luận" subtab: the item's comments, oldest first. */
export function TaskUpdatesTab({
  updates,
  events = [],
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
  /** Recorded changes (due date, Status…) shown among the comments by time. */
  events?: readonly CardActivity[];
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
  if (updates.length === 0 && events.length === 0) {
    return (
      <p className="m-0 text-sm text-muted-foreground">
        {copy.sections.noComments}
      </p>
    );
  }
  return (
    <ul className="m-0 flex list-none flex-col p-0">
      {[
        ...updates.map((update) => ({ kind: "comment" as const, update })),
        ...events.map((event) => ({ kind: "event" as const, event })),
      ]
        .sort((a, b) =>
          (a.kind === "comment"
            ? a.update.createdAt
            : a.event.createdAt
          ).localeCompare(
            b.kind === "comment" ? b.update.createdAt : b.event.createdAt,
          ),
        )
        .map((entry) =>
          entry.kind === "comment" ? (
            <li key={entry.update.id}>
              <CommentItem
                update={entry.update}
                canEdit={capabilities.canEditFields}
                canDelete={capabilities.canDelete}
                copy={copy}
                time={describeSheetTime(
                  entry.update.createdAt,
                  now,
                  copy,
                  timeZone,
                )}
                onSave={(body) => onUpdateUpdate(entry.update.id, body)}
                onDelete={() => onDeleteUpdate(entry.update.id)}
              />
            </li>
          ) : (
            <li
              key={entry.event.id}
              data-f="event"
              className="flex items-center gap-2 py-1.5 pl-1 text-crumb text-meta-foreground"
            >
              <span
                aria-hidden="true"
                className="size-1.5 shrink-0 rounded-full bg-primary"
              />
              <span>
                <span className="font-medium text-muted-foreground">
                  {entry.event.actor}
                </span>{" "}
                {entry.event.action} ·{" "}
                <time dateTime={entry.event.createdAt}>
                  {describeSheetTime(
                    entry.event.createdAt,
                    now,
                    copy,
                    timeZone,
                  )}
                </time>
              </span>
            </li>
          ),
        )}
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
  const edit = useCommentEdit(update.body, onSave);
  const t = copy.comment;

  return (
    <article className="group/comment flex gap-2.5 py-1.5 text-body leading-[1.45]">
      <Avatar className="size-6.5 shrink-0">
        <AvatarFallback
          className={cn(
            "text-avatar-md",
            identityMarkClass(update.author.userId || update.author.id),
          )}
        >
          {update.author.initials}
        </AvatarFallback>
      </Avatar>
      <div className="min-w-0 flex-1">
        <div className="flex items-baseline">
          <span className="font-semibold text-foreground">
            {update.author.name}
          </span>
          <time
            dateTime={update.createdAt}
            className="ml-1.5 text-metadata text-meta-foreground"
          >
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
                  className="ml-auto size-6 text-muted-foreground opacity-0 group-hover/comment:opacity-100 focus-visible:opacity-100 data-[state=open]:opacity-100 pointer-coarse:opacity-100"
                >
                  <MoreHorizontal aria-hidden="true" className="size-3.5" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                {canEdit ? (
                  <DropdownMenuItem onSelect={edit.start}>
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
        {edit.editing ? (
          <div className="mt-2 flex flex-col gap-2">
            <Textarea
              value={edit.body}
              aria-label={t.editLabel}
              onChange={(event) => edit.setBody(event.target.value)}
              className="min-h-16 resize-none text-sm"
            />
            <div className="flex justify-end gap-1.5">
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={edit.cancel}
              >
                {t.cancel}
              </Button>
              <Button type="button" size="sm" onClick={edit.save}>
                {t.save}
              </Button>
            </div>
          </div>
        ) : (
          <p className="m-0 mt-[3px] leading-[1.55] whitespace-pre-wrap text-foreground">
            {update.body}
          </p>
        )}
      </div>
    </article>
  );
}
