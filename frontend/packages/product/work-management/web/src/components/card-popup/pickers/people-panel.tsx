import { useMemo, useState } from "react";
import { Check, UserPlus } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import { personInitials } from "@notrelix/work-management-core";
import { foldForSearch } from "../../item-page/item-pickers";
import { fillPopupCopy } from "../card-popup.copy";
import { useCardPopup } from "../card-popup-context";
import { PanelBtn, PersonMark } from "../popup-ui";
import { Caption, Hint, PickerHead, SearchField } from "./picker-bits";

/** People shown before the search narrows the list. */
const SHOWN = 6;

interface Person {
  readonly userId: string;
  readonly name: string;
}

/**
 * The assignee picker: search, the viewer first, "Giao cho tôi" in one touch,
 * tick to assign and tick again to take someone off. The list is the people
 * who can see the board (the directory's board members and the board's own
 * members), plus the viewer and whoever is already assigned.
 */
export function PeoplePanel({ onClose }: { onClose: () => void }) {
  const { card, board, data, fields, copy, callbacks } = useCardPopup();
  const t = copy.pickers.people;
  const [query, setQuery] = useState("");
  const viewerId = data.viewerUserId ?? null;
  const assigned = card.members.map((member) => member.userId);

  const people = useMemo<Person[]>(() => {
    const byId = new Map<string, Person>();
    for (const member of board.members)
      byId.set(member.userId, { userId: member.userId, name: member.name });
    for (const person of data.people?.people ?? [])
      if (
        person.onBoard ||
        byId.has(person.userId) ||
        person.userId === viewerId
      )
        byId.set(person.userId, { userId: person.userId, name: person.name });
    for (const member of card.members)
      if (!byId.has(member.userId))
        byId.set(member.userId, { userId: member.userId, name: member.name });
    if (viewerId && !byId.has(viewerId))
      byId.set(viewerId, { userId: viewerId, name: t.you });
    const rest = [...byId.values()].filter((p) => p.userId !== viewerId);
    const me = viewerId ? byId.get(viewerId) : undefined;
    return me ? [me, ...rest] : rest;
  }, [board.members, card.members, data.people, t.you, viewerId]);

  const needle = foldForSearch(query);
  const matches = needle
    ? people.filter((p) => foldForSearch(p.name).includes(needle))
    : people;
  const shown = needle ? matches : matches.slice(0, SHOWN);

  const write = (ids: string[]) => {
    if (!fields.person) return;
    callbacks.onUpdateFieldValue({
      cardId: card.id,
      fieldDefinitionId: fields.person.id,
      value: ids,
    });
  };
  const toggle = (userId: string) =>
    write(
      assigned.includes(userId)
        ? assigned.filter((id) => id !== userId)
        : [...assigned, userId],
    );
  const mine = viewerId !== null && assigned.includes(viewerId);

  return (
    <div>
      <PickerHead title={t.title} onClose={onClose} />
      <SearchField
        value={query}
        onChange={setQuery}
        label={t.searchLabel}
        placeholder={t.search}
      />
      {viewerId ? (
        <PanelBtn className="mt-2.5 w-full" onClick={() => toggle(viewerId)}>
          <UserPlus aria-hidden="true" className="size-4" />
          {mine ? t.unassignMe : t.assignMe}
        </PanelBtn>
      ) : null}
      <Caption>
        {needle
          ? fillPopupCopy(t.results, { query })
          : fillPopupCopy(t.boardMembers, { count: people.length })}
      </Caption>
      <div role="listbox" aria-multiselectable="true" aria-label={t.title}>
        {shown.map((person) => {
          const on = assigned.includes(person.userId);
          return (
            <button
              key={person.userId}
              type="button"
              role="option"
              aria-selected={on}
              onClick={() => toggle(person.userId)}
              className={cn(
                "flex min-h-11 w-full items-center gap-2.5 rounded-[8px] border-0 bg-transparent px-2.5 text-left text-[14px] text-foreground hover:bg-state-hover-strong",
                on &&
                  "bg-brand-subtle font-semibold text-brand-subtle-foreground",
              )}
            >
              <PersonMark
                size={24}
                member={{
                  userId: person.userId,
                  name: person.name,
                  initials: personInitials(person.name),
                }}
              />
              <span>
                {person.name}
                {person.userId === viewerId && person.name !== t.you
                  ? ` ${t.you}`
                  : ""}
              </span>
              {on ? (
                <Check aria-hidden="true" className="ml-auto size-4" />
              ) : null}
            </button>
          );
        })}
        {shown.length === 0 ? (
          <Hint className="px-2.5 py-2">
            {fillPopupCopy(t.empty, { query })}
          </Hint>
        ) : null}
      </div>
      {!needle && matches.length > SHOWN ? (
        <Hint className="mt-2 mr-2.5 mb-0.5 ml-2.5">
          {fillPopupCopy(t.more, { count: matches.length - SHOWN })}
        </Hint>
      ) : null}
    </div>
  );
}
