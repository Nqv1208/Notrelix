import { useId, useMemo, useState } from "react";
import { Search } from "lucide-react";
import { Avatar, AvatarFallback, Checkbox, Input, cn } from "@notrelix/ui-web";
import type { ChatCopy } from "./chat.copy";
import { avatarToneOf, initialsOf } from "./chat-initials";

export interface PickablePerson {
  readonly id: string;
  readonly name: string;
}

export interface ChatPersonPickerProps {
  readonly people: readonly PickablePerson[];
  readonly selected: ReadonlySet<string>;
  /** Persons that cannot be picked any more (the group is full): the rest of the list is disabled. */
  readonly full?: boolean;
  readonly copy: ChatCopy;
  readonly emptyText?: string;
  readonly onToggle: (id: string, picked: boolean) => void;
}

/** Normalises a name for matching: no case, no diacritics. */
function fold(value: string): string {
  return value
    .normalize("NFD")
    .replace(/\p{Diacritic}/gu, "")
    .replace(/đ/gi, "d")
    .toLowerCase();
}

/**
 * A searchable multi-select of Workspace members (the new-message dialog and the add-people dialog). Every
 * row is a labelled checkbox, so it works with the keyboard and with screen readers.
 */
export function ChatPersonPicker({
  people,
  selected,
  full = false,
  copy,
  emptyText,
  onToggle,
}: ChatPersonPickerProps) {
  const [query, setQuery] = useState("");
  const searchId = useId();
  const visible = useMemo(() => {
    const needle = fold(query.trim());
    return needle.length === 0
      ? people
      : people.filter((person) => fold(person.name).includes(needle));
  }, [people, query]);

  return (
    <div className="flex min-h-0 flex-col gap-2">
      <label htmlFor={searchId} className="sr-only">
        {copy.dm.dialog.searchLabel}
      </label>
      <div className="relative">
        <Search
          aria-hidden="true"
          className="pointer-events-none absolute start-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground"
        />
        <Input
          id={searchId}
          value={query}
          onChange={(event) => setQuery(event.target.value)}
          placeholder={copy.dm.dialog.searchPlaceholder}
          autoComplete="off"
          className="ps-9"
        />
      </div>
      {visible.length === 0 ? (
        <p role="status" className="px-1 py-3 text-sm text-muted-foreground">
          {people.length === 0
            ? (emptyText ?? copy.dm.dialog.noPeople)
            : copy.dm.dialog.empty}
        </p>
      ) : (
        <ul
          aria-label={copy.dm.dialog.listLabel}
          className="flex max-h-64 flex-col gap-0.5 overflow-y-auto"
        >
          {visible.map((person) => {
            const picked = selected.has(person.id);
            const blocked = full && !picked;
            return (
              <li key={person.id}>
                <label
                  className={cn(
                    "flex min-h-11 items-center gap-3 rounded-md px-2 py-1.5 text-sm",
                    blocked
                      ? "cursor-not-allowed opacity-60"
                      : "cursor-pointer hover:bg-muted",
                    picked && "bg-state-selected",
                  )}
                >
                  <Checkbox
                    checked={picked}
                    disabled={blocked}
                    onCheckedChange={(value) =>
                      onToggle(person.id, value === true)
                    }
                  />
                  <Avatar className="size-8" aria-hidden="true">
                    <AvatarFallback
                      className={cn(
                        "text-xs font-semibold",
                        avatarToneOf(person.id),
                      )}
                    >
                      {initialsOf(person.name)}
                    </AvatarFallback>
                  </Avatar>
                  <span className="min-w-0 flex-1 truncate font-medium">
                    {person.name}
                  </span>
                </label>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}
