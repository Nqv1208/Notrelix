import {
  useState,
  useRef,
  useCallback,
  useEffect,
  type KeyboardEvent,
} from "react";
import { Bell, BellOff, CalendarDays, MoreHorizontal, X } from "lucide-react";
import { Avatar, AvatarFallback } from "@notrelix/ui-web";
import { Badge } from "@notrelix/ui-web";
import { Button } from "@notrelix/ui-web";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@notrelix/ui-web";
import { Tooltip, TooltipContent, TooltipTrigger } from "@notrelix/ui-web";
import { Popover, PopoverContent, PopoverTrigger } from "@notrelix/ui-web";
import { Calendar } from "@notrelix/ui-web";
import {
  useDeleteCard,
  useDuplicateCard,
  useUpdateCard,
  useUpdateFieldValue,
} from "@notrelix/work-management-state";
import type { Board, CardDetail } from "@notrelix/work-management-core";
import { cn } from "@notrelix/ui-web";
import { formatDate, getOptionToneClass } from "../views/table/table-utils";

export function TaskDetailHeader({
  board,
  card,
  onClose,
}: {
  board: Board;
  card: CardDetail;
  onClose: () => void;
}) {
  const [prevTitle, setPrevTitle] = useState(card.title);
  const [title, setTitle] = useState(card.title);
  const [isWatched, setIsWatched] = useState(card.isWatched);
  const titleRef = useRef<HTMLDivElement>(null);
  const updateCard = useUpdateCard(card.boardId, card.workspaceId);
  const deleteCard = useDeleteCard(card.boardId, card.workspaceId);
  const duplicateCard = useDuplicateCard(card.boardId, card.workspaceId);
  const updateFieldValue = useUpdateFieldValue(card.boardId, card.workspaceId);

  if (card.title !== prevTitle) {
    setPrevTitle(card.title);
    setTitle(card.title);
  }

  const personField = board.fieldDefinitions.find(
    (f) => f.fieldType === "person" || f.id.endsWith("field-person"),
  );
  const statusField = board.fieldDefinitions.find((f) =>
    f.id.endsWith("field-status"),
  );
  const priorityField = board.fieldDefinitions.find((f) =>
    f.id.endsWith("field-priority"),
  );
  const dueDateField = board.fieldDefinitions.find(
    (f) => f.fieldType === "date" || f.id.endsWith("field-due-date"),
  );

  const selectedUserIds = new Set(card.members.map((m) => m.userId));
  const status = statusField?.options.find(
    (option) => option.id === card.status,
  );
  const priority = priorityField?.options.find(
    (option) => option.id === card.priority,
  );

  // Sync contentEditable text when card.title changes externally
  useEffect(() => {
    if (titleRef.current && titleRef.current.textContent !== card.title) {
      titleRef.current.textContent = card.title;
    }
  }, [card.title]);

  const commitTitle = useCallback(() => {
    const nextTitle = (titleRef.current?.textContent ?? "").trim();
    if (!nextTitle || nextTitle === card.title) {
      setTitle(card.title);
      if (titleRef.current) titleRef.current.textContent = card.title;
      return;
    }
    setTitle(nextTitle);
    updateCard.mutate({ cardId: card.id, patch: { title: nextTitle } });
  }, [card.id, card.title, updateCard]);

  function handleTitleKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (event.key === "Enter") {
      event.preventDefault();
      event.currentTarget.blur();
    }
    if (event.key === "Escape") {
      if (titleRef.current) titleRef.current.textContent = card.title;
      setTitle(card.title);
      event.currentTarget.blur();
    }
  }

  function toggleMember(memberId: string) {
    if (!personField) return;
    const next = new Set(selectedUserIds);
    if (next.has(memberId)) next.delete(memberId);
    else next.add(memberId);
    updateFieldValue.mutate({
      cardId: card.id,
      fieldDefinitionId: personField.id,
      value: Array.from(next),
    });
  }

  function updateStatus(statusId: string) {
    if (!statusField) return;
    updateFieldValue.mutate({
      cardId: card.id,
      fieldDefinitionId: statusField.id,
      value: statusId,
    });
  }

  function updatePriority(priorityId: string) {
    if (!priorityField) return;
    updateFieldValue.mutate({
      cardId: card.id,
      fieldDefinitionId: priorityField.id,
      value: priorityId,
    });
  }

  function updateDueDate(date: Date | undefined) {
    if (!dueDateField) return;
    updateFieldValue.mutate({
      cardId: card.id,
      fieldDefinitionId: dueDateField.id,
      value: date ? date.toISOString() : null,
    });
  }

  return (
    <header className="sticky top-0 z-20 border-b border-border bg-popover">
      {/* Title row — title + actions */}
      <div className="flex items-start gap-2 px-5 pt-3.5 pb-1">
        <div
          ref={titleRef}
          role="textbox"
          aria-label="Edit task title"
          contentEditable
          suppressContentEditableWarning
          spellCheck={false}
          onBlur={commitTitle}
          onKeyDown={handleTitleKeyDown}
          className={cn(
            "min-w-0 flex-1 outline-none",
            "text-lg leading-snug font-semibold tracking-tight",
            "text-foreground",
            "bg-transparent",
            "rounded-md px-1.5 py-1",
            "cursor-text",
            "empty:before:content-['Untitled'] empty:before:text-muted-foreground/40",
            "hover:bg-muted/30",
            "focus:bg-muted/20 focus:ring-1 focus:ring-border/60 focus:ring-offset-0",
            "transition-all duration-150",
            "[word-break:break-word]",
          )}
        >
          {card.title}
        </div>

        <div className="flex items-center gap-0.5 shrink-0 mt-0.5">
          <Tooltip>
            <TooltipTrigger asChild>
              <Button
                variant="ghost"
                size="icon-sm"
                aria-label={isWatched ? "Unfollow task" : "Follow task"}
                onClick={() => setIsWatched((current) => !current)}
              >
                {isWatched ? (
                  <Bell className="size-4" />
                ) : (
                  <BellOff className="size-4" />
                )}
              </Button>
            </TooltipTrigger>
            <TooltipContent>
              {isWatched ? "Following" : "Follow"}
            </TooltipContent>
          </Tooltip>
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon-sm" aria-label="Task menu">
                <MoreHorizontal className="size-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem>Copy task link</DropdownMenuItem>
              <DropdownMenuItem>Move to group</DropdownMenuItem>
              <DropdownMenuItem onClick={() => duplicateCard.mutate(card.id)}>
                Duplicate task
              </DropdownMenuItem>
              <DropdownMenuItem
                className="text-destructive"
                onClick={() => {
                  deleteCard.mutate(card.id);
                  onClose();
                }}
              >
                Archive task
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
          <Button
            variant="ghost"
            size="icon-sm"
            aria-label="Close task details"
            onClick={onClose}
          >
            <X className="size-4" />
          </Button>
        </div>
      </div>

      {/* Meta */}
      <div className="flex flex-wrap items-center gap-1.5 px-5 pb-3">
        {/* Assignees */}
        <Popover>
          <PopoverTrigger asChild>
            <button
              type="button"
              className="inline-flex items-center gap-1.5 rounded-md px-2 py-1 text-sm text-muted-foreground transition-colors hover:bg-muted/40 hover:text-foreground"
            >
              {card.members.length > 0 ? (
                <>
                  <div className="flex -space-x-1.5">
                    {card.members.slice(0, 3).map((member) => (
                      <Avatar
                        key={member.id}
                        className="inline-flex size-5 ring-2 ring-popover"
                      >
                        <AvatarFallback
                          className="text-[8px] font-bold text-primary-foreground"
                          style={{ backgroundColor: member.color }}
                        >
                          {member.initials}
                        </AvatarFallback>
                      </Avatar>
                    ))}
                  </div>
                  <span className="text-foreground">
                    {card.members[0]?.name}
                    {card.members.length > 1
                      ? ` +${card.members.length - 1}`
                      : ""}
                  </span>
                </>
              ) : (
                <span>Unassigned</span>
              )}
            </button>
          </PopoverTrigger>
          <PopoverContent
            align="start"
            className="w-64 p-2"
            onClick={(event) => event.stopPropagation()}
          >
            <div className="px-2 py-1 text-xs font-semibold uppercase text-muted-foreground">
              Assignees
            </div>
            <div className="space-y-0.5">
              {board.members.map((member) => (
                <button
                  type="button"
                  key={member.id}
                  className="flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-left text-sm transition hover:bg-accent"
                  onClick={() => toggleMember(member.userId)}
                >
                  <Avatar className="size-6">
                    <AvatarFallback
                      className="text-[9px] font-bold text-primary-foreground"
                      style={{ backgroundColor: member.color }}
                    >
                      {member.initials}
                    </AvatarFallback>
                  </Avatar>
                  <span className="min-w-0 flex-1 truncate">{member.name}</span>
                  {selectedUserIds.has(member.userId) ? (
                    <Badge
                      variant="secondary"
                      className="rounded-full px-1.5 py-0 text-[10px]"
                    >
                      ✓
                    </Badge>
                  ) : null}
                </button>
              ))}
            </div>
          </PopoverContent>
        </Popover>

        {/* Status */}
        {statusField && (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <button
                type="button"
                className={cn(
                  "inline-flex items-center gap-1.5 rounded-md px-2 py-1 text-sm transition-colors hover:bg-muted/40",
                  status ? "text-foreground" : "text-muted-foreground",
                )}
              >
                <span
                  className="size-2 rounded-full shrink-0"
                  style={{
                    backgroundColor: status?.color ?? "currentColor",
                    opacity: status ? 1 : 0.4,
                  }}
                />
                <span>{status ? status.label : "No status"}</span>
              </button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="start">
              {statusField.options.map((item) => (
                <DropdownMenuItem
                  key={item.id}
                  onClick={() => updateStatus(item.id)}
                >
                  <span
                    className="size-2 rounded-full"
                    style={{ backgroundColor: item.color }}
                  />
                  {item.label}
                </DropdownMenuItem>
              ))}
            </DropdownMenuContent>
          </DropdownMenu>
        )}

        {/* Priority */}
        {priorityField && (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <button
                type="button"
                className={cn(
                  "inline-flex items-center gap-1.5 rounded-md px-2 py-1 text-sm transition-colors hover:bg-muted/40",
                  priority ? "text-foreground" : "text-muted-foreground",
                )}
              >
                <span
                  className="size-2 rounded-full shrink-0"
                  style={{
                    backgroundColor: priority?.color ?? "currentColor",
                    opacity: priority ? 1 : 0.4,
                  }}
                />
                <span>{priority ? priority.label : "No priority"}</span>
              </button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="start">
              {priorityField.options.map((item) => (
                <DropdownMenuItem
                  key={item.id}
                  onClick={() => updatePriority(item.id)}
                >
                  <span
                    className="size-2 rounded-full"
                    style={{ backgroundColor: item.color }}
                  />
                  {item.label}
                </DropdownMenuItem>
              ))}
            </DropdownMenuContent>
          </DropdownMenu>
        )}

        {/* Due date */}
        <Popover>
          <PopoverTrigger asChild>
            <button
              type="button"
              className={cn(
                "inline-flex items-center gap-1.5 rounded-md px-2 py-1 text-sm transition-colors hover:bg-muted/40",
                card.dueDate ? "text-foreground" : "text-muted-foreground",
              )}
            >
              <CalendarDays className="size-3.5 shrink-0" />
              <span>{card.dueDate ? formatDate(card.dueDate) : "No date"}</span>
            </button>
          </PopoverTrigger>
          <PopoverContent
            align="start"
            className="w-auto p-0"
            onClick={(event) => event.stopPropagation()}
          >
            <div className="flex flex-col">
              <Calendar
                mode="single"
                selected={card.dueDate ? new Date(card.dueDate) : undefined}
                onSelect={updateDueDate}
                initialFocus
              />
              {card.dueDate && (
                <div className="border-t p-2 flex justify-end bg-muted/20">
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-8 text-xs text-destructive hover:text-destructive hover:bg-destructive/10"
                    onClick={() => updateDueDate(undefined)}
                  >
                    Clear date
                  </Button>
                </div>
              )}
            </div>
          </PopoverContent>
        </Popover>
      </div>
    </header>
  );
}
