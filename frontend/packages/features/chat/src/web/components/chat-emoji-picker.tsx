import {
  useEffect,
  useId,
  useMemo,
  useRef,
  useState,
  type KeyboardEvent,
} from "react";
import { Search, Smile } from "lucide-react";
import {
  Button,
  Input,
  Popover,
  PopoverContent,
  PopoverTrigger,
  cn,
} from "@notrelix/ui-web";
import {
  EMOJI_CATEGORIES,
  EMOJI_ENTRIES,
  type EmojiCategoryId,
  type EmojiEntry,
} from "../../core/model/emoji-data";
import { searchEmoji } from "../../core/model/emoji-model";
import type { ChatCopy } from "./chat.copy";
import { rememberEmoji, useRecentEmoji } from "./chat-emoji-recent";

/** Emoji per row of the grid (the arrow keys move by this much). */
const COLUMNS = 8;
/** The recent row is one row of the grid. */
const RECENT_SHOWN = COLUMNS;

const CATEGORY_GLYPH: Record<EmojiCategoryId, string> = {
  smileys: "😀",
  gestures: "👍",
  animals: "🐶",
  food: "🍔",
  activities: "⚽",
  travel: "🚗",
  objects: "💡",
  symbols: "❤️",
};

/** The English words of an emoji, for its accessible name (the glyph alone is read inconsistently). */
function nameOf(entry: EmojiEntry): string {
  return entry.id.replace(/_/g, " ");
}

type Cell = { readonly section: "recent" | "grid"; readonly index: number };

export interface EmojiPickerProps {
  readonly copy: ChatCopy;
  /** Called with the chosen emoji; the host closes the popover. */
  readonly onPick: (char: string) => void;
  /** Emoji shown as pressed (the reactions the caller already has). */
  readonly pressed?: readonly string[];
  /** The accessible name of the picker; defaults to the copy's. */
  readonly label?: string;
}

/**
 * The full emoji picker: a search box, the recent row, category tabs and a grid. The search box takes the
 * focus when it opens; ArrowDown moves into the grid, the arrow keys then move by cell (Home and End jump
 * to the ends) and ArrowUp from the first row returns to the search box. Enter or Space picks.
 */
export function EmojiPicker({
  copy,
  onPick,
  pressed = [],
  label,
}: EmojiPickerProps) {
  const searchId = useId();
  const [query, setQuery] = useState("");
  const [category, setCategory] = useState<EmojiCategoryId>("smileys");
  const [cell, setCell] = useState<Cell>({ section: "grid", index: 0 });
  const searchRef = useRef<HTMLInputElement | null>(null);
  const buttons = useRef(new Map<string, HTMLButtonElement>());
  const wantsFocus = useRef(false);
  const recentChars = useRecentEmoji();

  const searching = query.trim() !== "";
  const recent = useMemo(
    () =>
      searching
        ? []
        : recentChars
            .slice(0, RECENT_SHOWN)
            .flatMap((char) =>
              EMOJI_ENTRIES.filter((entry) => entry.char === char),
            ),
    [recentChars, searching],
  );
  const grid = useMemo(
    () =>
      searching
        ? searchEmoji(query)
        : EMOJI_ENTRIES.filter((entry) => entry.category === category),
    [searching, query, category],
  );

  useEffect(() => {
    searchRef.current?.focus();
  }, []);

  // Roving tabindex: the active cell is the one tab stop; arrow keys move the focus after the render.
  useEffect(() => {
    if (!wantsFocus.current) return;
    wantsFocus.current = false;
    buttons.current.get(`${cell.section}-${cell.index}`)?.focus();
  }, [cell]);

  const safeCell: Cell =
    cell.section === "recent" && recent.length === 0
      ? { section: "grid", index: 0 }
      : cell.section === "grid"
        ? { section: "grid", index: Math.min(cell.index, grid.length - 1) }
        : cell;

  function move(next: Cell) {
    wantsFocus.current = true;
    setCell(next);
  }

  function onGridKey(event: KeyboardEvent<HTMLElement>) {
    const { section, index } = safeCell;
    const length = section === "recent" ? recent.length : grid.length;
    let handled = true;
    switch (event.key) {
      case "ArrowRight":
        if (index + 1 < length) move({ section, index: index + 1 });
        break;
      case "ArrowLeft":
        if (index > 0) move({ section, index: index - 1 });
        break;
      case "ArrowDown":
        if (index + COLUMNS < length) move({ section, index: index + COLUMNS });
        else if (section === "recent" && grid.length > 0)
          move({ section: "grid", index: Math.min(index, grid.length - 1) });
        break;
      case "ArrowUp":
        if (index - COLUMNS >= 0) move({ section, index: index - COLUMNS });
        else if (section === "grid" && recent.length > 0)
          move({
            section: "recent",
            index: Math.min(index, recent.length - 1),
          });
        else searchRef.current?.focus();
        break;
      case "Home":
        move({ section, index: 0 });
        break;
      case "End":
        move({ section, index: length - 1 });
        break;
      default:
        handled = false;
    }
    if (handled) event.preventDefault();
  }

  function choose(entry: EmojiEntry) {
    rememberEmoji(entry.char);
    onPick(entry.char);
  }

  function renderCell(entry: EmojiEntry, at: Cell) {
    const active =
      safeCell.section === at.section && safeCell.index === at.index;
    const isPressed = pressed.includes(entry.char);
    return (
      <button
        key={`${at.section}-${entry.id}`}
        ref={(node) => {
          const key = `${at.section}-${at.index}`;
          if (node) buttons.current.set(key, node);
          else buttons.current.delete(key);
        }}
        type="button"
        tabIndex={active ? 0 : -1}
        aria-label={nameOf(entry)}
        aria-pressed={pressed.length > 0 ? isPressed : undefined}
        title={`:${entry.id}:`}
        onFocus={() => setCell(at)}
        onClick={() => choose(entry)}
        className={cn(
          "flex size-8 items-center justify-center rounded-md text-lg transition-colors hover:bg-accent focus-visible:ring-2 focus-visible:ring-focus-ring focus-visible:outline-none",
          isPressed && "bg-secondary",
        )}
      >
        <span aria-hidden="true">{entry.char}</span>
      </button>
    );
  }

  return (
    <div
      role="group"
      aria-label={label ?? copy.emoji.pickerLabel}
      className="flex flex-col gap-2"
      data-testid="chat-emoji-picker"
    >
      <div className="relative">
        <Search
          className="pointer-events-none absolute start-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground"
          aria-hidden="true"
        />
        <Input
          id={searchId}
          ref={searchRef}
          type="search"
          role="searchbox"
          value={query}
          aria-label={copy.emoji.searchLabel}
          placeholder={copy.emoji.searchPlaceholder}
          autoComplete="off"
          className="h-8 ps-8"
          onChange={(event) => setQuery(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === "ArrowDown" && grid.length > 0) {
              event.preventDefault();
              move(
                recent.length > 0
                  ? { section: "recent", index: 0 }
                  : { section: "grid", index: 0 },
              );
            }
          }}
        />
      </div>

      {searching ? null : (
        <div
          role="group"
          aria-label={copy.emoji.categories[category]}
          className="flex items-center gap-0.5 border-b border-border pb-1"
        >
          {EMOJI_CATEGORIES.map((id) => (
            <button
              key={id}
              type="button"
              aria-label={copy.emoji.categories[id]}
              title={copy.emoji.categories[id]}
              aria-pressed={category === id}
              onClick={() => {
                setCategory(id);
                setCell({ section: "grid", index: 0 });
              }}
              className={cn(
                "flex size-7 items-center justify-center rounded-md text-base transition-colors hover:bg-accent focus-visible:ring-2 focus-visible:ring-focus-ring focus-visible:outline-none",
                category === id && "bg-secondary",
              )}
            >
              <span aria-hidden="true">{CATEGORY_GLYPH[id]}</span>
            </button>
          ))}
        </div>
      )}

      <div className="max-h-56 overflow-y-auto">
        {recent.length > 0 ? (
          <section aria-labelledby={`${searchId}-recent`}>
            <h3
              id={`${searchId}-recent`}
              className="px-1 pb-1 text-xs font-semibold text-muted-foreground"
            >
              {copy.emoji.recent}
            </h3>
            <div
              role="group"
              aria-labelledby={`${searchId}-recent`}
              data-testid="chat-emoji-recent"
              onKeyDown={onGridKey}
              className="mb-2 grid grid-cols-8 gap-0.5"
            >
              {recent.map((entry, index) =>
                renderCell(entry, { section: "recent", index }),
              )}
            </div>
          </section>
        ) : null}
        {grid.length === 0 ? (
          <p
            role="status"
            className="px-1 py-6 text-center text-sm text-muted-foreground"
          >
            {copy.emoji.noResults}
          </p>
        ) : (
          <section aria-labelledby={`${searchId}-grid`}>
            <h3
              id={`${searchId}-grid`}
              className="px-1 pb-1 text-xs font-semibold text-muted-foreground"
            >
              {searching
                ? copy.emoji.gridLabel
                : copy.emoji.categories[category]}
            </h3>
            <div
              role="group"
              aria-labelledby={`${searchId}-grid`}
              data-testid="chat-emoji-grid"
              onKeyDown={onGridKey}
              className="grid grid-cols-8 gap-0.5"
            >
              {grid.map((entry, index) =>
                renderCell(entry, { section: "grid", index }),
              )}
            </div>
          </section>
        )}
      </div>
    </div>
  );
}

export interface EmojiPickerButtonProps {
  readonly copy: ChatCopy;
  readonly disabled?: boolean;
  /** Called with the emoji when one is chosen. */
  readonly onPick: (char: string) => void;
  /** Called when the popover has closed; the composer gives the textarea its focus back here. */
  readonly onClosed: () => void;
}

/** The composer toolbar's emoji button and its popover. */
export function EmojiPickerButton({
  copy,
  disabled,
  onPick,
  onClosed,
}: EmojiPickerButtonProps) {
  const [open, setOpen] = useState(false);
  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          className="size-10 rounded-full text-muted-foreground hover:text-foreground max-xl:size-8 max-xl:rounded-[8px] max-xl:mt-1.5 max-xl:mb-2"
          disabled={disabled}
          aria-label={copy.emoji.open}
          title={copy.emoji.open}
        >
          <Smile aria-hidden="true" />
        </Button>
      </PopoverTrigger>
      <PopoverContent
        align="start"
        side="top"
        className="w-80 gap-2 p-2"
        onOpenAutoFocus={(event) => {
          // The picker puts the focus in its search box itself.
          event.preventDefault();
        }}
        onCloseAutoFocus={(event) => {
          event.preventDefault();
          onClosed();
        }}
      >
        <EmojiPicker
          copy={copy}
          onPick={(char) => {
            onPick(char);
            setOpen(false);
          }}
        />
      </PopoverContent>
    </Popover>
  );
}
