import { useEffect, useId, useMemo, useRef, useState } from "react";
import { ChevronLeft, Search, X } from "lucide-react";
import {
  Avatar,
  AvatarFallback,
  Button,
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogTitle,
  Input,
  Label,
  Skeleton,
  cn,
} from "@notrelix/ui-web";
import { chatFailureOf } from "../../core/api/chat-failure";
import {
  CHAT_SEARCH_LIMITS,
  type ChatSearchFilters,
  type ChatSearchHit,
} from "../../core/types/chat";
import {
  activeFilterCount,
  dayEndExclusiveIso,
  dayStartIso,
  nextHitIndex,
  pushRecentSearch,
  rangeIsValid,
  searchTextState,
  segmentSnippet,
} from "../../core/model/search-model";
import type { ChatHooks } from "../query/create-chat-hooks";
import type { ChatCopy } from "./chat.copy";
import { avatarToneOf, initialsOf } from "./chat-initials";
import { fillCopy, pluralCopy } from "./use-chat-copy";

/** A place the caller can search in: a channel or a conversation they can read. */
export interface ChatSearchSpace {
  readonly id: string;
  readonly label: string;
  readonly kind: ChatSearchHit["spaceKind"];
}

export interface ChatSearchPanelProps {
  readonly open: boolean;
  readonly onOpenChange: (open: boolean) => void;
  readonly hooks: ChatHooks;
  readonly workspaceId: string;
  readonly copy: ChatCopy;
  readonly locale: string;
  readonly timeZone?: string;
  /** The channels and conversations the caller can read (their own lists). A filter on a place that leaves this list is dropped. */
  readonly spaces: readonly ChatSearchSpace[];
  /** Names of the Workspace members by user id. */
  readonly people: ReadonlyMap<string, string>;
  readonly authorName: (userId: string) => string;
  /** Opens a hit: the channel or conversation, the message and, for a thread reply, its thread. */
  readonly onOpenHit: (hit: ChatSearchHit) => void;
}

const DEBOUNCE_MS = 300;

interface DraftFilters {
  readonly spaceId: string;
  readonly authorId: string;
  readonly fromDay: string;
  readonly toDay: string;
  readonly hasFile: boolean;
  readonly mentionsMe: boolean;
}

const NO_FILTERS: DraftFilters = {
  spaceId: "",
  authorId: "",
  fromDay: "",
  toDay: "",
  hasFile: false,
  mentionsMe: false,
};

const CHIP =
  "inline-flex h-7 items-center rounded-full border border-input bg-background px-[11px] text-[12.5px] text-muted-foreground focus-visible:outline-2 focus-visible:outline-ring";
const CHIP_ON = "border-primary bg-primary/10 font-semibold text-link";

/**
 * The message search (UC-CHT-H07, CHT-022). A dialog with a debounced text box (a combobox over a listbox), filters, and the hits
 * newest first. The server decides what can be found; the panel only shows what it returns: plain-text snippets drawn as text with
 * the matches in `mark`, never as markup. Recent searches live in memory only.
 */
export function ChatSearchPanel(props: ChatSearchPanelProps) {
  const {
    open,
    onOpenChange,
    hooks,
    workspaceId,
    copy,
    locale,
    timeZone,
    spaces,
    people,
    authorName,
    onOpenHit,
  } = props;
  const text = copy.search;
  const ids = useId();
  const inputId = `${ids}-input`;
  const listId = `${ids}-list`;
  const optionId = (index: number) => `${ids}-option-${index}`;

  const [typed, setTyped] = useState("");
  const [debounced, setDebounced] = useState("");
  const [draft, setDraft] = useState<DraftFilters>(NO_FILTERS);
  const [active, setActive] = useState(-1);
  const [datesOpen, setDatesOpen] = useState(false);
  const [recent, setRecent] = useState<readonly string[]>([]);
  const listRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    const timer = setTimeout(() => setDebounced(typed), DEBOUNCE_MS);
    return () => clearTimeout(timer);
  }, [typed]);

  // A place the caller can no longer read cannot stay as a filter.
  useEffect(() => {
    if (draft.spaceId && !spaces.some((space) => space.id === draft.spaceId)) {
      setDraft((current) => ({ ...current, spaceId: "" }));
    }
  }, [spaces, draft.spaceId]);

  const state = searchTextState(debounced);
  const readyText = state.kind === "ready" ? state.q : "";
  const rangeOk = rangeIsValid(draft.fromDay, draft.toDay);
  const filters = useMemo<ChatSearchFilters>(() => {
    const from = draft.fromDay ? dayStartIso(draft.fromDay) : null;
    const to = draft.toDay ? dayEndExclusiveIso(draft.toDay) : null;
    return {
      q: readyText,
      ...(draft.spaceId ? { spaceId: draft.spaceId } : {}),
      ...(draft.authorId ? { authorId: draft.authorId } : {}),
      ...(from ? { from } : {}),
      ...(to ? { to } : {}),
      ...(draft.hasFile ? { hasAttachment: true } : {}),
      ...(draft.mentionsMe ? { mentionsMe: true } : {}),
    };
  }, [draft, readyText]);

  const enabled = open && state.kind === "ready" && rangeOk;
  const query = hooks.useSearchMessages(workspaceId, filters, enabled);
  const hits = useMemo<readonly ChatSearchHit[]>(
    () => (query.data ? query.data.pages.flatMap((page) => page.items) : []),
    [query.data],
  );

  // A new search starts with no highlighted hit.
  useEffect(() => {
    setActive(-1);
  }, [filters]);

  useEffect(() => {
    if (active < 0) return;
    document
      .getElementById(optionId(active))
      ?.scrollIntoView?.({ block: "nearest" });
    // optionId is derived from the stable id prefix.
  }, [active]);

  const spaceById = useMemo(
    () => new Map(spaces.map((space) => [space.id, space])),
    [spaces],
  );

  function spaceLabelOf(hit: ChatSearchHit): string {
    const known = spaceById.get(hit.spaceId);
    if (known) return known.label;
    if (hit.spaceName) return hit.spaceName;
    return hit.spaceKind === "group" ? text.groupLabel : text.directLabel;
  }

  function timeOf(iso: string): string {
    return new Intl.DateTimeFormat(locale, {
      dateStyle: "medium",
      timeStyle: "short",
      ...(timeZone ? { timeZone } : {}),
    }).format(new Date(iso));
  }

  /** The board's short stamp: the time today, "Hôm qua", the weekday this week, the date before that. */
  function whenOf(iso: string): string {
    const zone = timeZone ? { timeZone } : {};
    const day = (value: Date) =>
      new Intl.DateTimeFormat("en-CA", zone).format(value);
    const at = new Date(iso);
    const now = new Date();
    const age = Math.round(
      (Date.parse(day(now)) - Date.parse(day(at))) / 86_400_000,
    );
    if (age <= 0)
      return new Intl.DateTimeFormat(locale, {
        hour: "2-digit",
        minute: "2-digit",
        ...zone,
      }).format(at);
    if (age === 1) return copy.time.yesterday;
    if (age < 7)
      return new Intl.DateTimeFormat(locale, {
        weekday: "long",
        ...zone,
      }).format(at);
    return new Intl.DateTimeFormat(locale, {
      day: "2-digit",
      month: "2-digit",
      year: "numeric",
      ...zone,
    }).format(at);
  }

  function choose(hit: ChatSearchHit) {
    setRecent((current) => pushRecentSearch(current, typed));
    onOpenHit(hit);
    onOpenChange(false);
  }

  function handleKey(event: React.KeyboardEvent<HTMLInputElement>) {
    if (event.key === "ArrowDown" || event.key === "ArrowUp") {
      if (hits.length === 0) return;
      event.preventDefault();
      setActive((current) =>
        nextHitIndex(
          current,
          hits.length,
          event.key as "ArrowDown" | "ArrowUp",
        ),
      );
    } else if (event.key === "Enter") {
      if (hits.length === 0) return;
      event.preventDefault();
      const hit = hits[active >= 0 ? active : 0];
      if (hit) choose(hit);
    }
  }

  const failure = query.isError ? chatFailureOf(query.error) : null;
  const lostAccess =
    failure !== null &&
    (failure.kind === "forbidden" || failure.kind === "not-found");
  const showResults = enabled && query.isSuccess && hits.length > 0;
  const pending =
    enabled &&
    (query.isPending || query.isFetching) &&
    !query.isFetchingNextPage &&
    hits.length === 0 &&
    !query.isError;
  const empty = enabled && query.isSuccess && hits.length === 0;
  const filtersOn = activeFilterCount(filters) > 0;

  const announcement = pending
    ? text.loading
    : showResults
      ? pluralCopy(
          query.hasNextPage
            ? {
                one: text.resultCountMore_one,
                other: text.resultCountMore_other,
              }
            : { one: text.resultCount_one, other: text.resultCount_other },
          hits.length,
        )
      : empty
        ? text.noResults
        : "";

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        showCloseButton={false}
        className="flex flex-col gap-0 overflow-hidden p-0 max-sm:top-0 max-sm:left-0 max-sm:h-dvh max-sm:max-h-none max-sm:w-full max-sm:max-w-none max-sm:translate-x-0 max-sm:translate-y-0 max-sm:rounded-none sm:max-h-[85vh] sm:max-w-[720px] sm:rounded-2xl"
        aria-describedby={`${ids}-description`}
      >
        <div className="flex h-14 shrink-0 items-center gap-2 border-b border-border px-3 sm:px-5">
          <DialogClose asChild>
            <Button
              type="button"
              size="icon-sm"
              variant="ghost"
              className="order-first text-muted-foreground sm:order-last"
              aria-label={text.close}
              title={text.close}
            >
              <ChevronLeft className="sm:hidden" aria-hidden="true" />
              <X className="max-sm:hidden" aria-hidden="true" />
            </Button>
          </DialogClose>
          <div className="min-w-0 flex-1">
            <DialogTitle className="truncate text-[17px] leading-[1.45] font-[650]">
              {text.title}
            </DialogTitle>
            <DialogDescription
              id={`${ids}-description`}
              className="truncate text-xs leading-[1.45]"
            >
              {text.description}
            </DialogDescription>
          </div>
        </div>

        <div className="shrink-0 px-4 pt-3.5 pb-2">
          <Label htmlFor={inputId} className="sr-only">
            {text.inputLabel}
          </Label>
          <div className="flex h-11 items-center gap-2 rounded-[12px] border border-input bg-background px-3 focus-within:border-primary">
            <Search
              className="size-4 shrink-0 text-muted-foreground"
              aria-hidden="true"
            />
            <Input
              id={inputId}
              type="search"
              role="combobox"
              autoComplete="off"
              autoFocus
              value={typed}
              maxLength={CHAT_SEARCH_LIMITS.maxLength}
              placeholder={text.placeholder}
              aria-expanded={showResults}
              aria-controls={listId}
              aria-autocomplete="list"
              aria-activedescendant={
                showResults && active >= 0 ? optionId(active) : undefined
              }
              aria-describedby={`${ids}-hint`}
              className="h-full min-w-0 flex-1 rounded-none border-0 bg-transparent px-0 shadow-none ring-0 focus-visible:border-0 focus-visible:ring-0 [&::-webkit-search-cancel-button]:hidden"
              onChange={(event) => setTyped(event.target.value)}
              onKeyDown={handleKey}
            />
            {showResults ? (
              <span
                aria-hidden="true"
                className="text-[11.5px] font-medium tabular-nums text-muted-foreground"
              >
                {hits.length}
                {query.hasNextPage ? "+" : ""}
              </span>
            ) : null}
            {typed ? (
              <Button
                type="button"
                size="icon-sm"
                variant="ghost"
                className="size-7 text-muted-foreground"
                aria-label={text.clearText}
                title={text.clearText}
                onClick={() => {
                  setTyped("");
                  setDebounced("");
                }}
              >
                <X aria-hidden="true" />
              </Button>
            ) : null}
          </div>
          <p
            id={`${ids}-hint`}
            className={cn(
              "text-xs",
              state.kind === "too-short" || state.kind === "too-long"
                ? "mt-1.5 text-destructive"
                : "sr-only",
            )}
          >
            {state.kind === "too-short"
              ? text.tooShort
              : state.kind === "too-long"
                ? text.tooLong
                : text.hint}
          </p>

          <fieldset className="mt-2.5 flex flex-wrap items-center gap-1.5">
            <legend className="sr-only">{text.filtersLabel}</legend>
            <Label htmlFor={`${ids}-space`} className="sr-only">
              {text.spaceLabel}
            </Label>
            <select
              id={`${ids}-space`}
              value={draft.spaceId}
              onChange={(event) =>
                setDraft({ ...draft, spaceId: event.target.value })
              }
              className={cn(CHIP, "max-w-44", draft.spaceId && CHIP_ON)}
            >
              <option value="">{text.allSpaces}</option>
              {spaces.map((space) => (
                <option key={space.id} value={space.id}>
                  {space.label}
                </option>
              ))}
            </select>
            <Label htmlFor={`${ids}-author`} className="sr-only">
              {text.authorLabel}
            </Label>
            <select
              id={`${ids}-author`}
              value={draft.authorId}
              onChange={(event) =>
                setDraft({ ...draft, authorId: event.target.value })
              }
              className={cn(CHIP, "max-w-40", draft.authorId && CHIP_ON)}
            >
              <option value="">{text.anyone}</option>
              {[...people.entries()].map(([id, name]) => (
                <option key={id} value={id}>
                  {name}
                </option>
              ))}
            </select>
            <button
              type="button"
              aria-expanded={
                datesOpen || draft.fromDay !== "" || draft.toDay !== ""
              }
              onClick={() => setDatesOpen((current) => !current)}
              className={cn(CHIP, (draft.fromDay || draft.toDay) && CHIP_ON)}
            >
              {text.dateChip}
            </button>
            <label
              className={cn(
                CHIP,
                "cursor-pointer has-[:checked]:border-primary has-[:checked]:bg-primary/10 has-[:checked]:font-semibold has-[:checked]:text-link has-[:focus-visible]:outline-2",
              )}
            >
              <input
                type="checkbox"
                className="sr-only"
                checked={draft.hasFile}
                onChange={(event) =>
                  setDraft({ ...draft, hasFile: event.target.checked })
                }
              />
              {text.hasFile}
            </label>
            <label
              className={cn(
                CHIP,
                "cursor-pointer has-[:checked]:border-primary has-[:checked]:bg-primary/10 has-[:checked]:font-semibold has-[:checked]:text-link has-[:focus-visible]:outline-2",
              )}
            >
              <input
                type="checkbox"
                className="sr-only"
                checked={draft.mentionsMe}
                onChange={(event) =>
                  setDraft({ ...draft, mentionsMe: event.target.checked })
                }
              />
              {text.mentionsMe}
            </label>
            {filtersOn || draft.fromDay || draft.toDay ? (
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={() => setDraft(NO_FILTERS)}
              >
                {text.clearFilters}
              </Button>
            ) : null}
          </fieldset>
          {datesOpen || draft.fromDay || draft.toDay ? (
            <div className="mt-2 flex flex-wrap items-center gap-1.5">
              <Label htmlFor={`${ids}-from`} className="sr-only">
                {text.fromLabel}
              </Label>
              <Input
                id={`${ids}-from`}
                type="date"
                value={draft.fromDay}
                aria-invalid={!rangeOk}
                onChange={(event) =>
                  setDraft({ ...draft, fromDay: event.target.value })
                }
                className={cn(CHIP, "w-[8.5rem]", draft.fromDay && CHIP_ON)}
              />
              <Label htmlFor={`${ids}-to`} className="sr-only">
                {text.toLabel}
              </Label>
              <Input
                id={`${ids}-to`}
                type="date"
                value={draft.toDay}
                aria-invalid={!rangeOk}
                onChange={(event) =>
                  setDraft({ ...draft, toDay: event.target.value })
                }
                className={cn(CHIP, "w-[8.5rem]", draft.toDay && CHIP_ON)}
              />
            </div>
          ) : null}
        </div>
        {!rangeOk ? (
          <p role="alert" className="px-4 pb-2 text-xs text-destructive">
            {text.rangeInvalid}
          </p>
        ) : null}

        <p role="status" aria-live="polite" className="sr-only">
          {announcement}
        </p>

        <div
          className="min-h-24 flex-1 overflow-y-auto border-t border-border"
          ref={listRef}
        >
          {state.kind === "empty" && recent.length > 0 ? (
            <section aria-labelledby={`${ids}-recent`}>
              <h3
                id={`${ids}-recent`}
                className="px-1 pb-1 text-xs font-semibold text-muted-foreground"
              >
                {text.recentTitle}
              </h3>
              <ul className="flex flex-wrap gap-2">
                {recent.map((item) => (
                  <li key={item}>
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      onClick={() => {
                        setTyped(item);
                        setDebounced(item);
                      }}
                    >
                      {item}
                    </Button>
                  </li>
                ))}
              </ul>
            </section>
          ) : null}

          {pending ? (
            <div role="status" aria-label={text.loading} className="space-y-2">
              <Skeleton className="h-14 w-full" />
              <Skeleton className="h-14 w-full" />
              <Skeleton className="h-14 w-full" />
            </div>
          ) : null}

          {query.isError ? (
            <div
              role="alert"
              className="flex flex-wrap items-center gap-3 text-sm"
            >
              <span>
                {lostAccess
                  ? text.lostAccess
                  : failure?.kind === "rate-limited"
                    ? text.rateLimited
                    : text.error}
              </span>
              {lostAccess ? null : (
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => void query.refetch()}
                >
                  {text.retry}
                </Button>
              )}
            </div>
          ) : null}

          {empty ? (
            <div className="space-y-1 py-6 text-center">
              <p className="text-sm font-medium">{text.noResults}</p>
              <p className="text-xs text-muted-foreground">
                {text.noResultsHint}
              </p>
            </div>
          ) : null}

          {showResults ? (
            <div
              id={listId}
              role="listbox"
              aria-label={text.resultsLabel}
              className="flex flex-col"
            >
              {hits.map((hit, index) => {
                const segments = segmentSnippet(hit.snippet);
                const space = spaceLabelOf(hit);
                const author = authorName(hit.authorId);
                const selected = index === active;
                return (
                  <div
                    key={hit.messageId}
                    id={optionId(index)}
                    role="option"
                    aria-selected={selected}
                    aria-label={fillCopy(text.optionLabel, {
                      author,
                      space,
                      time: timeOf(hit.createdAt),
                      text: hit.snippet.text,
                    })}
                    data-message-id={hit.messageId}
                    onMouseDown={(event) => event.preventDefault()}
                    onClick={() => choose(hit)}
                    onMouseEnter={() => setActive(index)}
                    className={cn(
                      "cursor-pointer border-b border-border px-4 py-3",
                      selected && "bg-primary/5",
                    )}
                  >
                    <div className="flex items-center gap-2">
                      <Avatar className="size-[18px]" aria-hidden="true">
                        <AvatarFallback
                          className={cn(
                            "text-[9px] font-[650]",
                            avatarToneOf(hit.authorId),
                          )}
                        >
                          {initialsOf(author)}
                        </AvatarFallback>
                      </Avatar>
                      <span className="truncate text-[13px] font-bold">
                        {author}
                      </span>
                      <span className="inline-flex h-5 min-w-0 shrink items-center truncate rounded-full bg-muted px-[9px] text-xs font-semibold text-muted-foreground">
                        {hit.spaceKind === "channel_public" ||
                        hit.spaceKind === "channel_private"
                          ? "# "
                          : ""}
                        {space}
                      </span>
                      {hit.spaceArchived ? (
                        <span className="rounded-full border border-border px-1.5 text-xs text-muted-foreground">
                          {text.archivedBadge}
                        </span>
                      ) : null}
                      {hit.threadRootId !== null && !hit.alsoSentToChannel ? (
                        <span className="rounded-full border border-border px-1.5 text-xs text-muted-foreground">
                          {text.threadBadge}
                        </span>
                      ) : null}
                      <span className="flex-1" />
                      <time
                        dateTime={hit.createdAt}
                        className="text-[11.5px] font-medium tracking-[.01em] whitespace-nowrap text-muted-foreground tabular-nums"
                      >
                        {whenOf(hit.createdAt)}
                      </time>
                    </div>
                    <p
                      className="mt-1.5 text-sm leading-[1.5] break-words"
                      aria-hidden="true"
                    >
                      {hit.snippet.cutBefore ? "…" : ""}
                      {segments.map((segment, at) =>
                        segment.match ? (
                          <mark
                            key={at}
                            className="rounded-[3px] bg-warning-subtle px-0.5 text-warning-subtle-foreground"
                          >
                            {segment.text}
                          </mark>
                        ) : (
                          <span key={at}>{segment.text}</span>
                        ),
                      )}
                      {hit.snippet.cutAfter ? "…" : ""}
                    </p>
                  </div>
                );
              })}
            </div>
          ) : null}

          {showResults && query.hasNextPage ? (
            <div className="flex justify-center py-2">
              <Button
                type="button"
                variant="outline"
                size="sm"
                disabled={query.isFetchingNextPage}
                onClick={() => void query.fetchNextPage()}
              >
                {query.isFetchingNextPage ? text.loadingMore : text.showMore}
              </Button>
            </div>
          ) : null}
        </div>
      </DialogContent>
    </Dialog>
  );
}
