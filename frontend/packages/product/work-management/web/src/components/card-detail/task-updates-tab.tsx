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
  cn,
} from "@notrelix/ui-web";
import type {
  CardActivity,
  CardDetail,
  CardUpdate,
} from "@notrelix/work-management-core";
import { identityMarkClass } from "../shared/item-display";
import { describeSheetTime } from "./item-sheet-format";
import { fillSheetCopy, pluralSheetCopy } from "./item-sheet.copy";
import type { ItemSheetCopy } from "./item-sheet.copy";
import { ReactionBar } from "../comment-thread/reaction-bar";
import { ReplyBox } from "../comment-thread/reply-box";
import { VISIBLE_REPLIES, splitThreads } from "../comment-thread/thread-model";
import { useCommentEdit } from "./use-comment-edit";
import { canWriteComment } from "./task-detail-types";
import type {
  TaskDetailCallbacks,
  TaskDetailCapabilities,
} from "./task-detail-types";

/** "Bình luận" subtab: the item's comments, oldest first. */
export function TaskUpdatesTab({
  card,
  updates,
  events = [],
  isLoading,
  capabilities,
  copy,
  now,
  timeZone,
  onCreateUpdate,
  onToggleReaction,
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
  onToggleReaction?: TaskDetailCallbacks["onToggleReaction"];
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
  const { top, repliesOf } = splitThreads(updates);
  return (
    <ul className="m-0 flex list-none flex-col p-0">
      {[
        ...top.map((update) => ({ kind: "comment" as const, update })),
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
              <CommentThread
                cardId={card.id}
                update={entry.update}
                replies={repliesOf(entry.update.id)}
                capabilities={capabilities}
                copy={copy}
                now={now}
                timeZone={timeZone}
                onCreateUpdate={onCreateUpdate}
                onToggleReaction={onToggleReaction}
                onUpdateUpdate={onUpdateUpdate}
                onDeleteUpdate={onDeleteUpdate}
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

/** A comment with its replies (oldest first, three shown) and the box a reply is written in. */
function CommentThread({
  cardId,
  update,
  replies,
  capabilities,
  copy,
  now,
  timeZone,
  onCreateUpdate,
  onToggleReaction,
  onUpdateUpdate,
  onDeleteUpdate,
}: {
  cardId: string;
  update: CardUpdate;
  replies: readonly CardUpdate[];
  capabilities: TaskDetailCapabilities;
  copy: ItemSheetCopy;
  now: Date;
  timeZone?: string;
  onCreateUpdate: TaskDetailCallbacks["onCreateUpdate"];
  onToggleReaction?: TaskDetailCallbacks["onToggleReaction"];
  onUpdateUpdate: TaskDetailCallbacks["onUpdateUpdate"];
  onDeleteUpdate: TaskDetailCallbacks["onDeleteUpdate"];
}) {
  const [replying, setReplying] = useState(false);
  const [showAll, setShowAll] = useState(false);
  const shown = showAll ? replies : replies.slice(0, VISIBLE_REPLIES);
  const more = replies.length - shown.length;
  const canReply = canWriteComment(capabilities);
  const item = (entry: CardUpdate) => (
    <CommentItem
      key={entry.id}
      update={entry}
      canEdit={capabilities.canEditFields}
      canDelete={capabilities.canDelete}
      canReply={canReply}
      copy={copy}
      time={describeSheetTime(entry.createdAt, now, copy, timeZone)}
      onSave={(body) => onUpdateUpdate(entry.id, body)}
      onDelete={() => onDeleteUpdate(entry.id)}
      onReply={() => setReplying(true)}
      onToggleReaction={
        canReply && onToggleReaction
          ? (emoji, on) => onToggleReaction(entry.id, emoji, on)
          : undefined
      }
    />
  );
  return (
    <>
      {item(update)}
      {replies.length > 0 || replying ? (
        <div
          role="group"
          aria-label={fillSheetCopy(copy.thread.replies, {
            name: update.author.name,
          })}
          className="ml-9"
        >
          {shown.map(item)}
          {more > 0 ? (
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={() => setShowAll(true)}
            >
              {pluralSheetCopy(
                copy.thread.showMore_one,
                copy.thread.showMore_other,
                more,
              )}
            </Button>
          ) : null}
          {replying ? (
            <div className="pb-2 pl-9">
              <ReplyBox
                cardId={cardId}
                parentCommentId={update.id}
                authorName={update.author.name}
                copy={copy.thread}
                onCreateUpdate={onCreateUpdate}
                onClose={() => {
                  setReplying(false);
                  setShowAll(true);
                }}
              />
            </div>
          ) : null}
        </div>
      ) : null}
    </>
  );
}

function CommentItem({
  update,
  canEdit,
  canDelete,
  canReply,
  copy,
  time,
  onSave,
  onDelete,
  onReply,
  onToggleReaction,
}: {
  update: CardUpdate;
  canEdit: boolean;
  canDelete: boolean;
  canReply: boolean;
  copy: ItemSheetCopy;
  time: string;
  onSave: (body: string) => void;
  onDelete: () => void;
  onReply: () => void;
  onToggleReaction?: (emoji: string, on: boolean) => void;
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
          {canEdit || canDelete || canReply ? (
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
                {canReply ? (
                  <DropdownMenuItem onSelect={onReply}>
                    {copy.thread.reply}
                  </DropdownMenuItem>
                ) : null}
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
        {(update.reactions?.length ?? 0) > 0 || onToggleReaction ? (
          <div className="mt-1.5">
            <ReactionBar
              reactions={update.reactions ?? []}
              copy={copy.thread}
              onToggle={onToggleReaction}
            />
          </div>
        ) : null}
      </div>
    </article>
  );
}
