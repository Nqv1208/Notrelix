import { useState } from "react";
import { AtSign, Lock, MessageCircle, MessageSquareOff } from "lucide-react";
import { Skeleton, Textarea } from "@notrelix/ui-web";
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
}: {
  update: CardUpdate;
  replies: readonly CardUpdate[];
  names: readonly string[];
}) {
  const { card, callbacks, capabilities } = useCardPopup();
  const sheet = useItemSheetCopy();
  const [replying, setReplying] = useState(false);
  const [showAll, setShowAll] = useState(false);
  const canReply = canWriteComment(capabilities);
  const shown = showAll ? replies : replies.slice(0, VISIBLE_REPLIES);
  const more = replies.length - shown.length;
  return (
    <>
      <Bubble
        update={update}
        names={names}
        canReply={canReply}
        onReply={() => setReplying(true)}
      />
      {replies.length > 0 || replying ? (
        <div
          role="group"
          aria-label={fillSheetCopy(sheet.thread.replies, {
            name: update.author.name,
          })}
          className="ml-10 flex flex-col gap-3.5"
        >
          {shown.map((reply) => (
            <Bubble
              key={reply.id}
              update={reply}
              names={names}
              canReply={false}
              onReply={() => undefined}
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

function Bubble({
  update,
  names,
  canReply,
  onReply,
}: {
  update: CardUpdate;
  names: readonly string[];
  canReply: boolean;
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
  const edit = useCommentEdit(update.body, (body) =>
    callbacks.onUpdateUpdate(update.id, body),
  );
  const react =
    canReply || canWriteComment(capabilities)
      ? callbacks.onToggleReaction
      : undefined;
  return (
    <div className="flex gap-2.5">
      <PersonMark member={update.author} size={32} />
      <div className="min-w-0 flex-1 text-[13.5px]">
        <div>
          <b className="font-[650]">{update.author.name}</b>{" "}
          <time dateTime={update.createdAt} className={FEED_LINK}>
            {describeSheetTime(update.createdAt, now, sheet, timeZone)}
          </time>
        </div>
        {edit.editing ? (
          <div className="mt-1.5 flex flex-col gap-2">
            <Textarea
              value={edit.body}
              aria-label={sheet.comment.editLabel}
              onChange={(e) => edit.setBody(e.target.value)}
              className="min-h-16 resize-none text-[14px]"
            />
            <div className="flex justify-end gap-1.5">
              <PanelBtn small variant="ghost" onClick={edit.cancel}>
                {sheet.comment.cancel}
              </PanelBtn>
              <PanelBtn small variant="primary" onClick={edit.save}>
                {sheet.comment.save}
              </PanelBtn>
            </div>
          </div>
        ) : (
          <div className="mt-1.5 rounded-[10px] border border-line bg-popover px-3 py-2.5 text-[14px] leading-[1.5] whitespace-pre-wrap">
            <Mentions body={update.body} names={names} />
          </div>
        )}
        {(update.reactions?.length ?? 0) > 0 || react ? (
          <div className="mt-1.5">
            <ReactionBar
              reactions={update.reactions ?? []}
              copy={sheet.thread}
              onToggle={
                react ? (emoji, on) => react(update.id, emoji, on) : undefined
              }
            />
          </div>
        ) : null}
        {!edit.editing && (own || canReply) ? (
          <div className="mt-1.5 flex gap-3">
            {own ? (
              <button
                type="button"
                className={TEXT_ACTION}
                onClick={edit.start}
              >
                {t.edit}
              </button>
            ) : null}
            {own ? (
              <button
                type="button"
                className={TEXT_ACTION}
                onClick={() => callbacks.onDeleteUpdate(update.id)}
              >
                {t.delete}
              </button>
            ) : null}
            {canReply && !update.parentId ? (
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
  const { card, data, callbacks, capabilities, copy, readOnly } =
    useCardPopup();
  const t = copy.related.comments;
  const [open, setOpen] = useState(false);
  const disabled = !canWriteComment(capabilities);
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
  if (disabled)
    return (
      <div
        aria-disabled="true"
        className="box-border flex h-11 w-full items-center gap-1.5 rounded-[10px] border border-dashed border-line-strong bg-state-hover-strong px-3.5 text-[14px] text-meta-foreground"
      >
        <Lock aria-hidden="true" className="size-4 shrink-0" />
        {capabilities.frozen || readOnly === "archived"
          ? t.readOnlyArchived
          : t.readOnlyPermission}
      </div>
    );
  if (!open)
    return (
      <button
        type="button"
        onClick={() => setOpen(true)}
        className="box-border flex h-11 w-full items-center rounded-[10px] border border-line-strong bg-popover px-3.5 text-left text-[14px] text-meta-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
      >
        {t.composerPlaceholder}
      </button>
    );
  return (
    <div className="relative rounded-[10px] border border-primary bg-popover ring-[3px] ring-brand-subtle">
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

/** The right column: comment composer, the comments and the recorded activity, newest first. */
export function CommentsColumn() {
  const { card, data, callbacks, copy, mode } = useCardPopup();
  const t = copy.related.comments;
  const sheet = useItemSheetCopy();
  const [details, setDetails] = useState(false);
  const create = mode === "create";
  const { top, repliesOf } = splitThreads(data.updates);
  const names = (data.people?.people ?? card.members).map((p) => p.name);
  const feed = [
    ...top.map((update) => ({
      at: update.createdAt,
      update,
      event: null as CardActivity | null,
    })),
    ...data.activity.map((event) => ({
      at: event.createdAt,
      update: null as CardUpdate | null,
      event,
    })),
  ].sort((a, b) => b.at.localeCompare(a.at));
  const loading = data.updatesLoading || data.activityLoading;

  return (
    <>
      <div className="flex items-center gap-2.5">
        <span className="text-muted-foreground">
          <MessageCircle aria-hidden="true" className="size-5" />
        </span>
        <h3 className="m-0 flex-1 text-[16px] font-[650]">
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
          <b className="text-[14px] text-foreground">{t.createTitle}</b>
          <span>{t.createBody}</span>
        </div>
      ) : loading && feed.length === 0 ? (
        <div
          role="status"
          aria-label={t.loading}
          className="flex flex-col gap-3.5"
        >
          <Skeleton className="h-16 rounded-[10px]" />
          <Skeleton className="h-10 rounded-[10px]" />
        </div>
      ) : (
        <div className="mt-1 flex flex-col gap-3.5">
          {data.activityError ? (
            <p
              role="alert"
              className="m-0 flex items-center gap-2 text-[13px] text-muted-foreground"
            >
              <span className="flex-1">{sheet.activityEvents.loadError}</span>
              <SmallBtn onClick={callbacks.onRefreshActivity}>
                {sheet.activityEvents.retry}
              </SmallBtn>
            </p>
          ) : null}
          {feed.length === 0 && !data.activityError ? (
            <p className="m-0 text-[13.5px] text-meta-foreground">{t.empty}</p>
          ) : null}
          {feed.map((entry) =>
            entry.update ? (
              <CommentEntry
                key={entry.update.id}
                update={entry.update}
                replies={repliesOf(entry.update.id)}
                names={names}
              />
            ) : (
              <EventEntry
                key={entry.event!.id}
                event={entry.event!}
                details={details}
              />
            ),
          )}
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
