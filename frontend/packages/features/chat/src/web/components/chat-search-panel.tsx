import { useEffect, useId, useMemo, useRef, useState } from "react";
import { Lock, Hash, MessageSquare, Search, Users } from "lucide-react";
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
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

function KindIcon({ kind }: { readonly kind: ChatSearchHit["spaceKind"] }) {
  const Icon =
    kind === "channel_private"
      ? Lock
      : kind === "channel_public"
        ? Hash
        : kind === "group"
          ? Users
          : MessageSquare;
  return (
    <Icon
      className="size-3.5 shrink-0 text-muted-foreground"
      aria-hidden="true"
    />
  );
}

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
        className="flex max-h-[85vh] flex-col gap-4 overflow-hidden sm:max-w-2xl"
        aria-describedby={`${ids}-description`}
      >
        <DialogHeader>
          <DialogTitle>{text.title}</DialogTitle>
          <DialogDescription id={`${ids}-description`}>
            {text.description}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-1.5">
          <Label htmlFor={inputId}>{text.inputLabel}</Label>
          <div className="relative">
            <Search
              className="pointer-events-none absolute start-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground"
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
              className="ps-9"
              onChange={(event) => setTyped(event.target.value)}
              onKeyDown={handleKey}
            />
          </div>
          <p
            id={`${ids}-hint`}
            className={cn(
              "text-xs",
              state.kind === "too-short" || state.kind === "too-long"
                ? "text-destructive"
                : "text-muted-foreground",
            )}
          >
            {state.kind === "too-short"
              ? text.tooShort
              : state.kind === "too-long"
                ? text.tooLong
                : text.hint}
          </p>
        </div>

        <fieldset className="flex flex-wrap items-end gap-3">
          <legend className="sr-only">{text.filtersLabel}</legend>
          <div className="space-y-1">
            <Label htmlFor={`${ids}-space`} className="text-xs">
              {text.spaceLabel}
            </Label>
            <select
              id={`${ids}-space`}
              value={draft.spaceId}
              onChange={(event) =>
                setDraft({ ...draft, spaceId: event.target.value })
              }
              className="h-9 max-w-48 rounded-md border border-input bg-background px-2 text-sm text-foreground focus-visible:outline-2 focus-visible:outline-ring"
            >
              <option value="">{text.allSpaces}</option>
              {spaces.map((space) => (
                <option key={space.id} value={space.id}>
                  {space.label}
                </option>
              ))}
            </select>
          </div>
          <div className="space-y-1">
            <Label htmlFor={`${ids}-author`} className="text-xs">
              {text.authorLabel}
            </Label>
            <select
              id={`${ids}-author`}
              value={draft.authorId}
              onChange={(event) =>
                setDraft({ ...draft, authorId: event.target.value })
              }
              className="h-9 max-w-40 rounded-md border border-input bg-background px-2 text-sm text-foreground focus-visible:outline-2 focus-visible:outline-ring"
            >
              <option value="">{text.anyone}</option>
              {[...people.entries()].map(([id, name]) => (
                <option key={id} value={id}>
                  {name}
                </option>
              ))}
            </select>
          </div>
          <div className="space-y-1">
            <Label htmlFor={`${ids}-from`} className="text-xs">
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
              className="w-36"
            />
          </div>
          <div className="space-y-1">
            <Label htmlFor={`${ids}-to`} className="text-xs">
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
              className="w-36"
            />
          </div>
          <label className="flex h-9 items-center gap-1.5 text-sm">
            <input
              type="checkbox"
              checked={draft.hasFile}
              onChange={(event) =>
                setDraft({ ...draft, hasFile: event.target.checked })
              }
            />
            {text.hasFile}
          </label>
          <label className="flex h-9 items-center gap-1.5 text-sm">
            <input
              type="checkbox"
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
        {!rangeOk ? (
          <p role="alert" className="-mt-2 text-xs text-destructive">
            {text.rangeInvalid}
          </p>
        ) : null}

        <p role="status" aria-live="polite" className="sr-only">
          {announcement}
        </p>

        <div className="min-h-24 flex-1 overflow-y-auto" ref={listRef}>
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
              className="flex flex-col gap-1"
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
                      "cursor-pointer rounded-md border border-transparent px-3 py-2",
                      selected &&
                        "border-border bg-accent text-accent-foreground",
                    )}
                  >
                    <div className="flex flex-wrap items-center gap-x-2 gap-y-0.5 text-xs text-muted-foreground">
                      <span className="flex min-w-0 items-center gap-1">
                        <KindIcon kind={hit.spaceKind} />
                        <span className="truncate font-medium text-foreground">
                          {space}
                        </span>
                      </span>
                      {hit.spaceArchived ? (
                        <span className="rounded-full border border-border px-1.5">
                          {text.archivedBadge}
                        </span>
                      ) : null}
                      {hit.threadRootId !== null && !hit.alsoSentToChannel ? (
                        <span className="rounded-full border border-border px-1.5">
                          {text.threadBadge}
                        </span>
                      ) : null}
                      <span>{author}</span>
                      <time dateTime={hit.createdAt}>
                        {timeOf(hit.createdAt)}
                      </time>
                    </div>
                    <p
                      className="mt-0.5 break-words text-sm"
                      aria-hidden="true"
                    >
                      {hit.snippet.cutBefore ? "…" : ""}
                      {segments.map((segment, at) =>
                        segment.match ? (
                          <mark
                            key={at}
                            className="rounded-sm bg-warning-subtle px-0.5 text-foreground"
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
