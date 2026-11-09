import { useEffect, useRef, useState, type ReactNode } from "react";
import {
  AtSign,
  CloudOff,
  Lock,
  MessageCircle,
  MessageSquareOff,
  RotateCw,
} from "lucide-react";
import { Textarea, cn } from "@notrelix/ui-web";
import {
  personInitials,
  type CardActivity,
  type CardUpdate,
} from "@notrelix/work-management-core";
import { describeActivityEvent } from "../../card-detail/activity-words";
import { describeSheetTime } from "../../card-detail/item-sheet-format";
import { fillSheetCopy } from "../../card-detail/item-sheet.copy";
import { useItemSheetCopy } from "../../card-detail/use-item-sheet-copy";
import { useCommentDraft } from "../../card-detail/update-composer";
import { useCommentEdit } from "../../card-detail/use-comment-edit";
import {
  canChangeOwnComment,
  canDeleteComment,
  canWriteComment,
} from "../../card-detail/task-detail-types";
import { ReactionBar } from "../../comment-thread/reaction-bar";
import { ReplyBox } from "../../comment-thread/reply-box";
import {
  VISIBLE_REPLIES,
  splitThreads,
} from "../../comment-thread/thread-model";
import {
  MentionList,
  useMentionField,
} from "../../item-page/item-page-mention";
import type { ItemPagePeople } from "../../item-page/item-page-types";
import { itemPageCopy } from "../../item-page/item-page.copy";
import { useCardPopup } from "../card-popup-context";
import { IconBtn, PanelBtn, PersonMark, SmallBtn } from "../popup-ui";

const FEED_LINK =
  "text-[12.5px] text-brand-subtle-foreground underline decoration-1 underline-offset-2";
const TEXT_ACTION =
  "cursor-pointer border-0 bg-transparent p-0 text-[12.5px] text-muted-foreground underline focus-visible:outline-2 focus-visible:outline-primary";

/** The body with the names of people mentioned drawn in the link ink. */
function Mentions({ body, names }: { body: string; names: readonly string[] }) {
  const hits = names.filter((name) => body.includes(`@${name}`));
  if (hits.length === 0) return <>{body}</>;
  const pattern = new RegExp(
    `(${hits.map((n) => `@${n.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}`).join("|")})`,
  );
  return (
    <>
      {body.split(pattern).map((part, index) =>
        index % 2 === 1 ? (
          <span
            key={index}
            className="font-semibold text-brand-subtle-foreground"
          >
            {part}
          </span>
        ) : (
          part
        ),
      )}
    </>
  );
}

function CommentEntry({
  update,
  replies,
  names,
  deleted = false,
}: {
  update: CardUpdate;
  replies: readonly CardUpdate[];
  names: readonly string[];
  /** The comment was deleted: its replies stay under a placeholder (COL-006). */
  deleted?: boolean;
}) {
  const { card, callbacks, capabilities, copy } = useCardPopup();
  const sheet = useItemSheetCopy();
  const [replying, setReplying] = useState(false);
  const [showAll, setShowAll] = useState(false);
  const canReply = canWriteComment(capabilities) && !deleted;
  const shown = showAll ? replies : replies.slice(0, VISIBLE_REPLIES);
  const more = replies.length - shown.length;
  return (
    <>
      {deleted ? (
        <DeletedBubble />
      ) : (
        <Bubble
          update={update}
          names={names}
          canReply={canReply}
          replyCount={replies.length}
          onReply={() => setReplying(true)}
        />
      )}
      {replies.length > 0 || replying ? (
        <div
          role="group"
          aria-label={
            deleted
              ? copy.related.comments.deleted
              : fillSheetCopy(sheet.thread.replies, {
                  name: update.author.name,
                })
          }
          className="ml-10 flex flex-col gap-3.5"
        >
          {shown.map((reply) => (
            <Bubble
              key={reply.id}
              update={reply}
              names={names}
              canReply={canReply}
              replyCount={0}
              onReply={() => setReplying(true)}
            />
          ))}
          {more > 0 ? (
            <button
              type="button"
              className={TEXT_ACTION}
              onClick={() => setShowAll(true)}
            >
              {fillSheetCopy(
                more === 1
                  ? sheet.thread.showMore_one
                  : sheet.thread.showMore_other,
                { count: more },
              )}
            </button>
          ) : null}
          {replying ? (
            <ReplyBox
              cardId={card.id}
              parentCommentId={update.id}
              authorName={update.author.name}
              copy={sheet.thread}
              onCreateUpdate={callbacks.onCreateUpdate}
              onClose={() => {
                setReplying(false);
                setShowAll(true);
              }}
            />
          ) : null}
        </div>
      ) : null}
    </>
  );
}

/** Where a deleted comment that still has replies stood: no author, no text, no actions. */
function DeletedBubble() {
  const { copy } = useCardPopup();
  return (
    <div className="flex gap-2.5" data-comment-deleted="">
      <span
        aria-hidden="true"
        className="box-border size-8 shrink-0 rounded-full border border-dashed border-line-strong"
      />
      <p className="m-0 box-border min-w-0 flex-1 rounded-[10px] border border-dashed border-line-strong px-3 py-2.5 text-[13.5px] leading-[1.45] text-meta-foreground">
        {copy.related.comments.deleted}
      </p>
    </div>
  );
}

function Bubble({
  update,
  names,
  canReply,
  replyCount,
  onReply,
}: {
  update: CardUpdate;
  names: readonly string[];
  canReply: boolean;
  /** Replies that stay when this comment is deleted, named in the confirm. */
  replyCount: number;
  onReply: () => void;
}) {
  const { data, callbacks, capabilities, copy, now, timeZone } = useCardPopup();
  const t = copy.related.comments;
  const sheet = useItemSheetCopy();
  const own = canChangeOwnComment(
    capabilities,
    update.author.userId,
    data.viewerUserId,
  );
  // A Board manager may delete a comment they did not write; editing stays with the author.
  const deletable = canDeleteComment(
    capabilities,
    update.author.userId,
    data.viewerUserId,
  );
  // The version is the one the list returned; a stale one is refused and the list is read again.
  const edit = useCommentEdit(update.body, (body) =>
    callbacks.onUpdateUpdate(update.id, body, update.version ?? 0),
  );
  const react =
    canReply || canWriteComment(capabilities)
      ? callbacks.onToggleReaction
      : undefined;
  const [confirming, setConfirming] = useState(false);
  const keepRef = useRef<HTMLButtonElement>(null);
  const deleteRef = useRef<HTMLButtonElement>(null);
  const editRef = useRef<HTMLButtonElement>(null);
  const wasEditing = useRef(false);
  useEffect(() => {
    // The confirm opens on its safe choice.
    if (confirming) keepRef.current?.focus();
  }, [confirming]);
  useEffect(() => {
    // Leaving the edit hands focus back to Edit.
    if (wasEditing.current && !edit.editing) editRef.current?.focus();
    wasEditing.current = edit.editing;
  }, [edit.editing]);
  const closeConfirm = () => {
    setConfirming(false);
    requestAnimationFrame(() => deleteRef.current?.focus());
  };
  const question =
    replyCount === 0
      ? t.deleteQuestion
      : fillSheetCopy(
          replyCount === 1
            ? t.deleteQuestionReplies_one
            : t.deleteQuestionReplies_other,
          { count: replyCount },
        );
  return (
    <div className="flex gap-2.5" data-comment={update.id}>
      <PersonMark member={update.author} size={32} />
      <div className="min-w-0 flex-1 text-[13.5px]">
        <div>
          <b className="font-[650]">{update.author.name}</b>{" "}
          <time dateTime={update.createdAt} className={FEED_LINK}>
            {describeSheetTime(update.createdAt, now, sheet, timeZone)}
          </time>
          {update.updatedAt ? (
            <span
              data-comment-edited=""
              className="text-[12.5px] text-meta-foreground"
            >
              {" · "}
              {t.edited}
            </span>
          ) : null}
        </div>
        {edit.editing ? (
          <div
            className="mt-1.5 flex flex-col gap-2"
            data-comment-editing=""
            data-popup-esc=""
          >
            <div className="rounded-[10px] border border-primary bg-popover ring-[3px] ring-brand-subtle">
              <Textarea
                autoFocus
                value={edit.body}
                aria-label={sheet.comment.editLabel}
                onChange={(e) => edit.setBody(e.target.value)}
                onKeyDown={(event) => {
                  if (event.key === "Escape") {
                    event.stopPropagation();
                    edit.cancel();
                  } else if (
                    event.key === "Enter" &&
                    (event.metaKey || event.ctrlKey) &&
                    edit.canSave
                  ) {
                    event.preventDefault();
                    edit.save();
                  }
                }}
                className="min-h-16 resize-none border-0 bg-transparent px-3 py-2.5 text-[14px] leading-[1.5] shadow-none focus-visible:ring-0"
              />
            </div>
            <div className="flex justify-end gap-1.5">
              <PanelBtn small variant="ghost" onClick={edit.cancel}>
                {t.cancel}
              </PanelBtn>
              <PanelBtn
                small
                variant="primary"
                disabled={!edit.canSave}
                onClick={edit.save}
              >
                {t.save}
              </PanelBtn>
            </div>
          </div>
        ) : (
          <div className="mt-1.5 rounded-[10px] border border-line bg-popover px-3 py-2.5 text-[14px] leading-[1.5] whitespace-pre-wrap">
            <Mentions body={update.body} names={names} />
          </div>
        )}
        {confirming ? (
          <div
            role="group"
            aria-label={t.delete}
            data-comment-confirm=""
            data-popup-esc=""
            onKeyDown={(event) => {
              if (event.key === "Escape") {
                event.stopPropagation();
                closeConfirm();
              }
            }}
            className="mt-1.5 flex flex-col gap-2 rounded-[10px] bg-danger-subtle px-3 py-2.5 dark:bg-danger-text/12"
          >
            <p className="m-0 text-[13px] leading-[1.45] text-foreground">
              {question}
            </p>
            <div className="flex justify-end gap-1.5">
              <PanelBtn
                ref={keepRef}
                small
                variant="ghost"
                onClick={closeConfirm}
              >
                {t.cancel}
              </PanelBtn>
              <PanelBtn
                small
                variant="danger"
                // WM-Comments draws the destructive button filled (`.btn.dng`).
                className="border-danger-text bg-danger-text font-semibold text-danger-foreground hover:bg-danger-text hover:brightness-95"
                onClick={() => {
                  setConfirming(false);
                  callbacks.onDeleteUpdate(update.id, update.version ?? 0);
                }}
              >
                {t.deleteConfirm}
              </PanelBtn>
            </div>
          </div>
        ) : null}
        {(update.reactions?.length ?? 0) > 0 ||
        react ||
        (!edit.editing && (own || deletable || canReply)) ? (
          <div className="mt-1.5 flex flex-wrap items-center gap-3">
            {(update.reactions?.length ?? 0) > 0 || react ? (
              <ReactionBar
                reactions={update.reactions ?? []}
                copy={sheet.thread}
                onToggle={
                  react ? (emoji, on) => react(update.id, emoji, on) : undefined
                }
              />
            ) : null}
            {!edit.editing && own ? (
              <button
                ref={editRef}
                type="button"
                className={TEXT_ACTION}
                onClick={() => {
                  setConfirming(false);
                  edit.start();
                }}
              >
                {t.edit}
              </button>
            ) : null}
            {!edit.editing && deletable ? (
              <button
                ref={deleteRef}
                type="button"
                className={TEXT_ACTION}
                aria-expanded={confirming}
                onClick={() => setConfirming(true)}
              >
                {t.delete}
              </button>
            ) : null}
            {!edit.editing && canReply ? (
              <button type="button" className={TEXT_ACTION} onClick={onReply}>
                {t.reply}
              </button>
            ) : null}
          </div>
        ) : null}
      </div>
    </div>
  );
}

function EventEntry({
  event,
  details,
}: {
  event: CardActivity;
  details: boolean;
}) {
  const { copy, now, timeZone } = useCardPopup();
  const t = copy.related.comments;
  const sheet = useItemSheetCopy();
  const described = describeActivityEvent(event, sheet.activityEvents);
  const sentence = described?.verb ?? event.verb ?? event.action;
  const diff = described?.detail?.kind === "diff" ? described.detail : null;
  const member = {
    userId: event.actor,
    name: event.actor,
    initials: personInitials(event.actor),
  };
  return (
    <div className="flex gap-2.5">
      <PersonMark member={member} size={24} className="mt-0.5" />
      <div className="min-w-0 flex-1 text-[13px] text-muted-foreground">
        <b className="font-[650] text-foreground">{event.actor}</b> {sentence}
        {details && diff ? (
          <span className="mt-0.5 block text-[12.5px] text-meta-foreground">
            {fillSheetCopy(t.movedFrom, { from: diff.from, to: diff.to })}
          </span>
        ) : null}
        <br />
        <time dateTime={event.createdAt} className={FEED_LINK}>
          {describeSheetTime(event.createdAt, now, sheet, timeZone)}
        </time>
      </div>
    </div>
  );
}

function Composer() {
  const { card, data, callbacks, capabilities, copy, readOnly, mode } =
    useCardPopup();
  const t = copy.related.comments;
  const [open, setOpen] = useState(false);
  // A card deleted or moved while open takes no comment: the unsent text stays in the notice to copy.
  const disabled = !canWriteComment(capabilities) || readOnly === "gone";
  const draft = useCommentDraft({
    cardId: card.id,
    disabled,
    onCreateUpdate: (input, options) =>
      callbacks.onCreateUpdate(input, {
        ...options,
        onSuccess: () => {
          options?.onSuccess?.();
          setOpen(false);
        },
      }),
  });
  const pageCopy = itemPageCopy(copy.locale === "vi" ? "vi" : "en");
  const members = card.members.length > 0 ? card.members : card.watchers;
  const offered: ItemPagePeople = data.people ?? {
    people: members.map((m) => ({
      userId: m.userId,
      name: m.name,
      department: null,
      openCount: null,
      onBoard: true,
    })),
    canInvite: false,
  };
  const mention = useMentionField({
    draft,
    people: offered,
    copy: pageCopy,
    viewerUserId: data.viewerUserId,
  });
  // A card that is not created yet has no comments: the composer waits, as the artboard draws it.
  if (mode === "create")
    return (
      <div
        aria-disabled="true"
        className="box-border flex h-11 w-full shrink-0 items-center rounded-[10px] border border-dashed border-line-strong bg-state-hover-strong px-3.5 text-[14px] text-meta-foreground"
      >
        {t.composerPlaceholder}
      </div>
    );
  if (disabled)
    return (
      <div
        aria-disabled="true"
        className="box-border flex h-11 w-full shrink-0 items-center gap-1.5 rounded-[10px] border border-dashed border-line-strong bg-state-hover-strong px-3.5 text-[14px] text-meta-foreground"
      >
        <Lock aria-hidden="true" className="size-4 shrink-0" />
        {capabilities.frozen || readOnly === "archived"
          ? t.readOnlyArchived
          : readOnly === "item-archived"
            ? t.readOnlyItemArchived
            : readOnly === "gone"
              ? t.readOnlyGone
              : t.readOnlyPermission}
      </div>
    );
  if (!open)
    return (
      <button
        type="button"
        onClick={() => setOpen(true)}
        className="box-border flex h-11 w-full shrink-0 items-center rounded-[10px] border border-line-strong bg-popover px-3.5 text-left text-[14px] text-meta-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
      >
        {t.composerPlaceholder}
      </button>
    );
  return (
    <div className="relative shrink-0 rounded-[10px] border border-primary bg-popover ring-[3px] ring-brand-subtle">
      {mention.listOpen ? (
        <MentionList
          candidates={mention.candidates}
          query={mention.token?.query ?? ""}
          activeIndex={mention.activeIndex}
          copy={pageCopy}
          onPick={mention.pick}
          onHover={mention.setActive}
        />
      ) : null}
      <Textarea
        ref={mention.fieldRef}
        autoFocus
        value={draft.body}
        rows={3}
        aria-label={t.composerLabel}
        aria-controls={mention.listOpen ? "item-page-mention-list" : undefined}
        placeholder={t.composerPlaceholder}
        onChange={(event) => {
          draft.changeBody(event.target.value);
          mention.readToken(
            event.target.value,
            event.target.selectionStart ?? event.target.value.length,
          );
        }}
        onClick={(event) =>
          mention.readToken(
            event.currentTarget.value,
            event.currentTarget.selectionStart,
          )
        }
        onBlur={mention.dismiss}
        onKeyDown={(event) => {
          if (mention.handleKey(event)) return;
          if (event.key === "Escape" && draft.body === "") {
            event.stopPropagation();
            setOpen(false);
            return;
          }
          draft.handleKeyDown(event);
        }}
        className="min-h-[76px] resize-none border-0 bg-transparent px-3.5 py-2.5 text-[14px] shadow-none focus-visible:ring-0"
      />
      <div className="flex items-center gap-1.5 px-2 pb-2">
        <IconBtn
          small
          aria-label={t.mentionLabel}
          title={t.mentionLabel}
          disabled={offered.people.length === 0}
          onClick={mention.openAtEnd}
        >
          <AtSign aria-hidden="true" className="size-4" />
        </IconBtn>
        <span className="flex-1" />
        <PanelBtn small variant="ghost" onClick={() => setOpen(false)}>
          {t.cancel}
        </PanelBtn>
        <PanelBtn
          small
          variant="primary"
          disabled={!draft.canSubmit}
          onClick={draft.submit}
        >
          {t.send}
        </PanelBtn>
      </div>
    </div>
  );
}

/** A grey block of the feed while it loads: the shape of what comes, not moving (Kanban-CardPopup-States-1, 1). */
function Block({ className }: { className: string }) {
  return (
    <span
      aria-hidden="true"
      data-feed-block=""
      className={cn("block shrink-0 bg-state-hover-strong", className)}
    />
  );
}

/** The feed while it loads: a comment and a history line drawn as grey blocks. */
function FeedSkeleton({ label }: { label: string }) {
  return (
    <div
      role="status"
      aria-label={label}
      data-feed-skeleton=""
      className="mt-1 flex flex-col gap-3.5"
    >
      <div className="flex gap-2.5">
        <Block className="size-8 rounded-full" />
        <div className="flex min-w-0 flex-1 flex-col gap-2">
          <Block className="h-3.5 w-[150px] max-w-full rounded-[6px]" />
          <Block className="h-[60px] w-full rounded-[10px]" />
        </div>
      </div>
      <div className="flex gap-2.5">
        <Block className="size-8 rounded-full" />
        <div className="flex min-w-0 flex-1 flex-col gap-2">
          <Block className="h-3.5 w-[190px] max-w-full rounded-[6px]" />
          <Block className="h-3.5 w-4/5 rounded-[6px]" />
        </div>
      </div>
    </div>
  );
}

/** The column's empty and error block, in the shape of the create card's empty comments. */
function FeedNotice({
  icon,
  title,
  body,
  action,
  tone = "neutral",
  alert = false,
}: {
  icon: ReactNode;
  title: string;
  body: string;
  action?: ReactNode;
  tone?: "neutral" | "danger";
  alert?: boolean;
}) {
  return (
    <div
      role={alert ? "alert" : undefined}
      data-feed-notice={tone}
      className="flex flex-col items-center gap-2 px-3 py-8 text-center text-[13.5px] text-muted-foreground"
    >
      <span
        className={cn(
          "grid size-11 place-items-center rounded-full",
          tone === "danger"
            ? "bg-danger-subtle text-danger-text dark:bg-danger-text/12"
            : "bg-state-hover-strong text-meta-foreground",
        )}
      >
        {icon}
      </span>
      <b className="text-[14px] leading-[1.45] text-foreground">{title}</b>
      <span className="leading-[1.45]">{body}</span>
      {action ? <span className="mt-1">{action}</span> : null}
    </div>
  );
}

/** The right column: comment composer, the comments and the recorded activity, newest first. */
export function CommentsColumn() {
  const { card, data, callbacks, capabilities, copy, mode, readOnly } =
    useCardPopup();
  const t = copy.related.comments;
  const [details, setDetails] = useState(false);
  const create = mode === "create";
  const { top, repliesOf, orphans } = splitThreads(data.updates);
  const names = (data.people?.people ?? card.members).map((p) => p.name);
  const feed = [
    ...top.map((update) => ({
      at: update.createdAt,
      update,
      // A deleted comment the server kept for its replies shows the placeholder, with no author or text.
      deleted: update.deleted === true,
      event: null as CardActivity | null,
    })),
    // Replies whose comment was deleted stay together under its placeholder, at the first reply's time.
    ...orphans.map((thread) => ({
      at: thread.replies[0]!.createdAt,
      update: { ...thread.replies[0]!, id: thread.parentId } as CardUpdate,
      deleted: true,
      event: null as CardActivity | null,
    })),
    ...data.activity.map((event) => ({
      at: event.createdAt,
      update: null as CardUpdate | null,
      deleted: false,
      event,
    })),
  ].sort((a, b) => b.at.localeCompare(a.at));
  const loading = data.updatesLoading || data.activityLoading;

  return (
    <>
      <div className="flex shrink-0 items-center gap-2.5">
        <span className="text-muted-foreground">
          <MessageCircle aria-hidden="true" className="size-5" />
        </span>
        <h3
          className={cn(
            "m-0 flex-1 leading-[1.45] font-[650]",
            // Kanban-CardPopup-Create draws the comments title at 15 px; an open card keeps 16.
            create ? "text-[15px] max-md:text-[16px]" : "text-[16px]",
          )}
        >
          {copy.shell.comments}
        </h3>
        <SmallBtn
          className="max-md:h-11"
          aria-pressed={details}
          disabled={create}
          onClick={() => setDetails((v) => !v)}
        >
          {t.showDetails}
        </SmallBtn>
      </div>
      <Composer />
      {create ? (
        <div className="flex flex-col items-center gap-2 px-3 py-8 text-center text-[13.5px] text-muted-foreground">
          <span className="grid size-11 place-items-center rounded-full bg-state-hover-strong text-meta-foreground">
            <MessageSquareOff aria-hidden="true" className="size-5" />
          </span>
          <b className="text-[14px] leading-[1.45] text-foreground">
            {t.createTitle}
          </b>
          <span className="leading-[1.45]">{t.createBody}</span>
        </div>
      ) : loading && feed.length === 0 ? (
        <FeedSkeleton label={t.loading} />
      ) : (
        <div className="mt-1 flex flex-col gap-3.5" data-comment-feed="">
          {data.updatesError ? (
            <FeedNotice
              alert
              tone="danger"
              icon={<CloudOff aria-hidden="true" className="size-5" />}
              title={t.updatesErrorTitle}
              body={t.updatesErrorBody}
              action={
                <PanelBtn
                  variant="primary"
                  className="max-md:h-11"
                  onClick={() => callbacks.onRefreshUpdates?.()}
                >
                  <RotateCw aria-hidden="true" className="size-4" />
                  {t.retry}
                </PanelBtn>
              }
            />
          ) : null}
          {data.activityError ? (
            <FeedNotice
              alert
              tone="danger"
              icon={<CloudOff aria-hidden="true" className="size-5" />}
              title={t.errorTitle}
              body={feed.length > 0 ? t.errorBodyComments : t.errorBody}
              action={
                <PanelBtn
                  variant="primary"
                  className="max-md:h-11"
                  onClick={callbacks.onRefreshActivity}
                >
                  <RotateCw aria-hidden="true" className="size-4" />
                  {t.retry}
                </PanelBtn>
              }
            />
          ) : null}
          {feed.length === 0 && !data.activityError && !data.updatesError ? (
            <FeedNotice
              icon={<MessageCircle aria-hidden="true" className="size-5" />}
              title={t.emptyTitle}
              body={
                canWriteComment(capabilities) && !readOnly
                  ? t.emptyBody
                  : t.emptyBodyReadOnly
              }
            />
          ) : null}
          {feed.map((entry) =>
            entry.update ? (
              <CommentEntry
                key={entry.update.id}
                update={entry.update}
                replies={repliesOf(entry.update.id)}
                names={names}
                deleted={entry.deleted}
              />
            ) : (
              <EventEntry
                key={entry.event!.id}
                event={entry.event!}
                details={details}
              />
            ),
          )}
          {data.updatesMore ? (
            <SmallBtn
              className="self-center max-md:h-11"
              disabled={data.updatesMore.loading}
              onClick={data.updatesMore.onMore}
            >
              {data.updatesMore.loading ? t.loading : t.more}
            </SmallBtn>
          ) : null}
        </div>
      )}
    </>
  );
}

/** On a phone the composer is pinned under the scrolling card instead of sitting in the column. */
export function CommentsBar() {
  const { mode } = useCardPopup();
  if (mode === "create") return null;
  return (
    <div className="flex-none border-t border-line bg-popover px-4 py-2.5 md:hidden">
      <Composer />
    </div>
  );
}
