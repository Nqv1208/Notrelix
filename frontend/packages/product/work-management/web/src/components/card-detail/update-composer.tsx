import { useState, type KeyboardEvent } from "react";
import { AtSign, Send } from "lucide-react";
import { Button, Textarea } from "@notrelix/ui-web";
import type { CardMember } from "@notrelix/work-management-core";
import { MentionList, useMentionField } from "../item-page/item-page-mention";
import type { ItemPagePeople } from "../item-page/item-page-types";
import { itemPageCopy } from "../item-page/item-page.copy";
import type { ItemSheetCopy } from "./item-sheet.copy";
import type { TaskDetailCallbacks } from "./task-detail-types";

/**
 * The comment draft both composers share: the text, the people it mentions
 * and the send state. ⌘↵ (or Ctrl+↵) submits; a failed post keeps the text
 * for a retry; typing "@" opens the people picker and a pick inserts the name
 * and records the mention.
 */
export function useCommentDraft({
  cardId,
  disabled = false,
  onCreateUpdate,
}: {
  cardId: string;
  disabled?: boolean;
  onCreateUpdate: TaskDetailCallbacks["onCreateUpdate"];
}) {
  const [body, setBody] = useState("");
  const [mentionUserIds, setMentionUserIds] = useState<string[]>([]);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const canSubmit = body.trim().length > 0 && !disabled && !isSubmitting;

  function submit() {
    const nextBody = body.trim();
    if (!nextBody || disabled || isSubmitting) return;
    setIsSubmitting(true);
    onCreateUpdate(
      { cardId, body: nextBody, mentionUserIds, attachmentIds: [] },
      {
        onSuccess: () => {
          setBody("");
          setMentionUserIds([]);
        },
        // Success or failure: a failed post keeps its text for a retry.
        onSettled: () => setIsSubmitting(false),
      },
    );
  }

  function handleKeyDown(event: KeyboardEvent<HTMLTextAreaElement>) {
    if (event.key === "Enter" && (event.metaKey || event.ctrlKey)) {
      event.preventDefault();
      submit();
    }
  }

  function changeBody(next: string) {
    setBody(next);
  }

  /** Replaces the typed "@…" at `range` with the chosen name and records who is reached. */
  function insertMention(
    range: { readonly from: number; readonly to: number },
    label: string,
    userIds: readonly string[],
  ) {
    setBody(
      (current) =>
        `${current.slice(0, range.from)}@${label} ${current.slice(range.to)}`,
    );
    setMentionUserIds((current) => [
      ...current,
      ...userIds.filter((id) => !current.includes(id)),
    ]);
  }

  return {
    body,
    canSubmit,
    changeBody,
    handleKeyDown,
    insertMention,
    submit,
  };
}

/**
 * Sheet footer composer: "Viết bình luận, gõ @ để nhắc ai đó…", ⌘↵ (or
 * Ctrl+↵) submits. Typing "@" (or the @ button) lists the people who can view
 * the board; a pick inserts the name and records the mention. It stays for a
 * commenter. Without `people` the item's own members are offered.
 */
export function UpdateComposer({
  cardId,
  members,
  people,
  viewerUserId,
  disabled = false,
  copy,
  onCreateUpdate,
}: {
  cardId: string;
  members: readonly CardMember[];
  /** Everyone who can view the board; absent: `members`. */
  people?: ItemPagePeople;
  viewerUserId?: string | null;
  disabled?: boolean;
  copy: ItemSheetCopy;
  onCreateUpdate: TaskDetailCallbacks["onCreateUpdate"];
}) {
  const draft = useCommentDraft({ cardId, disabled, onCreateUpdate });
  // The sheet is pure (no provider): the page's words come from the sheet's locale.
  const pageCopy = itemPageCopy(copy.locale === "vi" ? "vi" : "en");
  const offered: ItemPagePeople = people ?? {
    people: members.map((member) => ({
      userId: member.userId,
      name: member.name,
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
    viewerUserId,
  });
  const t = copy.composer;

  return (
    <div className="flex shrink-0 items-end gap-2 border-t border-line bg-popover px-4.5 py-3">
      <div className="relative flex min-h-10.5 flex-1 items-center gap-2 rounded-[9px] border border-line-strong bg-background pr-2 pl-3 focus-within:ring-2 focus-within:ring-focus-ring">
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
          value={draft.body}
          rows={1}
          disabled={disabled}
          aria-label={t.label}
          aria-controls={
            mention.listOpen ? "item-page-mention-list" : undefined
          }
          placeholder={t.placeholder}
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
            draft.handleKeyDown(event);
          }}
          className="max-h-32 min-h-0 flex-1 resize-none border-0 bg-transparent px-0 py-2 text-body shadow-none focus-visible:ring-0"
        />
      </div>
      <Button
        type="button"
        variant="ghost"
        size="icon-sm"
        aria-label={t.mention}
        title={t.mention}
        disabled={disabled || offered.people.length === 0}
        onClick={mention.openAtEnd}
        className="mb-1 text-muted-foreground"
      >
        <AtSign aria-hidden="true" className="size-4" />
      </Button>
      <Button
        type="button"
        size="icon-sm"
        aria-label={t.submit}
        title={t.submit}
        disabled={!draft.canSubmit}
        onClick={draft.submit}
        className="mb-1"
      >
        <Send aria-hidden="true" className="size-4" />
      </Button>
    </div>
  );
}
