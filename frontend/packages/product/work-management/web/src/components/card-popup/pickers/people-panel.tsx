import { useMemo, useState } from "react";
import { Plus, UserPlus, X } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import { personInitials } from "@notrelix/work-management-core";
import { foldForSearch } from "../../item-page/item-pickers";
import { fillPopupCopy } from "../card-popup.copy";
import { useCardPopup } from "../card-popup-context";
import { IconBtn, PanelBtn, PersonMark } from "../popup-ui";
import { Caption, Hint, PickerHead, SearchField } from "./picker-bits";

interface Person {
  readonly userId: string;
  readonly name: string;
}

/**
 * The assignee picker: search, the people already assigned (each with "Bỏ"), the other members of the
 * board (each with "+"), and "Giao cho tôi" at the foot. The list is the people who can see the board
 * (the directory's board members and the board's own members), plus the viewer and whoever is assigned.
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
    return [...byId.values()];
  }, [board.members, card.members, data.people, t.you, viewerId]);

  const needle = foldForSearch(query);
  const fits = (p: Person) => !needle || foldForSearch(p.name).includes(needle);
  // The assigned keep the card's order; the others put the viewer first.
  const given = assigned
    .map((id) => people.find((p) => p.userId === id))
    .filter((p): p is Person => p !== undefined && fits(p));
  const others = people
    .filter((p) => !assigned.includes(p.userId) && fits(p))
    .sort((a, b) => Number(b.userId === viewerId) - Number(a.userId === viewerId));

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
  const label = (person: Person) =>
    person.userId === viewerId && person.name !== t.you
      ? `${person.name} ${t.you}`
      : person.name;
  const mark = (person: Person) => (
    <PersonMark
      size={24}
      member={{
        userId: person.userId,
        name: person.name,
        initials: personInitials(person.name),
      }}
    />
  );
  const row = (person: Person, on: boolean) => (
    <div
      key={person.userId}
      className="group flex min-h-11 items-center gap-2.5 rounded-[8px] pr-1 pl-1.5 hover:bg-state-hover-strong"
    >
      {mark(person)}
      <span className="min-w-0 flex-1 truncate text-[14px] font-semibold">
        {label(person)}
      </span>
      <IconBtn
        small
        aria-label={fillPopupCopy(on ? t.removeOne : t.assignOne, {
          name: person.name,
        })}
        onClick={() => toggle(person.userId)}
      >
        {on ? (
          <X aria-hidden="true" className="size-4" />
        ) : (
          <Plus aria-hidden="true" className="size-4" />
        )}
      </IconBtn>
    </div>
  );

  return (
    <div>
      <PickerHead title={t.title} onClose={onClose} />
      <SearchField
        value={query}
        onChange={setQuery}
        label={t.searchLabel}
        placeholder={t.search}
      />
      {given.length > 0 ? (
        <>
          <Caption>{fillPopupCopy(t.assigned, { count: assigned.length })}</Caption>
          <div role="list" aria-label={t.assigned.split(" ·")[0]}>
            {given.map((person) => row(person, true))}
          </div>
        </>
      ) : null}
      {others.length > 0 ? (
        <>
          <Caption>{t.others}</Caption>
          <div role="list" aria-label={t.others}>
            {others.map((person) => row(person, false))}
          </div>
        </>
      ) : null}
      {given.length === 0 && others.length === 0 ? (
        <Hint className="mt-3 px-2.5 py-2">
          {fillPopupCopy(t.empty, { query })}
        </Hint>
      ) : null}
      {viewerId ? (
        <>
          <div className="-mx-3 mt-2 h-px bg-line" />
          <PanelBtn
            variant="ghost"
            className={cn("mt-2 w-full justify-start")}
            onClick={() => toggle(viewerId)}
          >
            <UserPlus aria-hidden="true" className="size-[13px]" />
            {mine ? t.unassignMe : t.assignMe}
          </PanelBtn>
        </>
      ) : null}
    </div>
  );
}
