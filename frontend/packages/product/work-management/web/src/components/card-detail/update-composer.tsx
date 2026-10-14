import { useState, type KeyboardEvent } from "react";
import { AtSign, Send } from "lucide-react";
import {
  Avatar,
  AvatarFallback,
  Button,
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
  Popover,
  PopoverAnchor,
  PopoverContent,
  PopoverTrigger,
  Textarea,
} from "@notrelix/ui-web";
import type { CardMember } from "@notrelix/work-management-core";
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
  members,
  disabled = false,
  onCreateUpdate,
}: {
  cardId: string;
  members: readonly CardMember[];
  disabled?: boolean;
  onCreateUpdate: TaskDetailCallbacks["onCreateUpdate"];
}) {
  const [body, setBody] = useState("");
  const [mentionUserIds, setMentionUserIds] = useState<string[]>([]);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [mentionOpen, setMentionOpen] = useState(false);
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
    if (next.endsWith("@") && members.length > 0) setMentionOpen(true);
  }

  function mention(member: CardMember) {
    setMentionUserIds((current) =>
      current.includes(member.userId) ? current : [...current, member.userId],
    );
    setBody((current) => {
      const withoutTrigger = current.endsWith("@")
        ? current.slice(0, -1)
        : current;
      return `${withoutTrigger}@${member.name} `;
    });
    setMentionOpen(false);
  }

  return {
    body,
    canSubmit,
    mentionOpen,
    setMentionOpen,
    changeBody,
    handleKeyDown,
    mention,
    submit,
  };
}

/**
 * Sheet footer composer: "Viết bình luận, gõ @ để nhắc ai đó…", ⌘↵ (or
 * Ctrl+↵) submits. Typing "@" opens the people picker; a pick inserts the
 * name and records the mention. It stays for a commenter.
 */
export function UpdateComposer({
  cardId,
  members,
  disabled = false,
  copy,
  onCreateUpdate,
}: {
  cardId: string;
  members: readonly CardMember[];
  disabled?: boolean;
  copy: ItemSheetCopy;
  onCreateUpdate: TaskDetailCallbacks["onCreateUpdate"];
}) {
  const {
    body,
    canSubmit,
    mentionOpen,
    setMentionOpen,
    changeBody,
    handleKeyDown,
    mention,
    submit,
  } = useCommentDraft({ cardId, members, disabled, onCreateUpdate });
  const t = copy.composer;

  return (
    <div className="flex shrink-0 items-end gap-2 border-t border-line bg-popover px-4.5 py-3">
      <Popover open={mentionOpen} onOpenChange={setMentionOpen}>
        <PopoverAnchor asChild>
          <div className="flex min-h-10.5 flex-1 items-center gap-2 rounded-[9px] border border-line-strong bg-background pr-2 pl-3 focus-within:ring-2 focus-within:ring-focus-ring">
            <Textarea
              value={body}
              rows={1}
              disabled={disabled}
              aria-label={t.label}
              placeholder={t.placeholder}
              onChange={(event) => changeBody(event.target.value)}
              onKeyDown={handleKeyDown}
              className="max-h-32 min-h-0 flex-1 resize-none border-0 bg-transparent px-0 py-2 text-body shadow-none focus-visible:ring-0"
            />
            <kbd className="shrink-0 rounded-[4px] border border-line-strong bg-keycap px-1.25 text-key leading-[18px] text-muted-foreground">
              {t.shortcut}
            </kbd>
          </div>
        </PopoverAnchor>
        <PopoverTrigger asChild>
          <Button
            type="button"
            variant="ghost"
            size="icon-sm"
            aria-label={t.mention}
            title={t.mention}
            disabled={disabled || members.length === 0}
            className="mb-1 text-muted-foreground"
          >
            <AtSign aria-hidden="true" className="size-4" />
          </Button>
        </PopoverTrigger>
        <PopoverContent align="start" className="w-60 p-0">
          <Command>
            <CommandInput placeholder={t.searchPeople} />
            <CommandList>
              <CommandEmpty>{t.noPeople}</CommandEmpty>
              <CommandGroup heading={t.people}>
                {members.map((member) => (
                  <CommandItem
                    key={member.userId}
                    value={member.name}
                    onSelect={() => mention(member)}
                  >
                    <Avatar className="size-5">
                      <AvatarFallback className="bg-foreground text-caption font-bold text-background">
                        {member.initials}
                      </AvatarFallback>
                    </Avatar>
                    {member.name}
                  </CommandItem>
                ))}
              </CommandGroup>
            </CommandList>
          </Command>
        </PopoverContent>
      </Popover>
      <Button
        type="button"
        size="icon-sm"
        aria-label={t.submit}
        title={t.submit}
        disabled={!canSubmit}
        onClick={submit}
        className="mb-1"
      >
        <Send aria-hidden="true" className="size-4" />
      </Button>
    </div>
  );
}
