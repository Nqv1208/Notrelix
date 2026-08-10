import { useMemo, useState } from "react";
import { useDebounce } from "./use-debounce";
import type { BoardGroup } from "@notrelix/work-management-core";

export function useKanbanSearch(groups: BoardGroup[], delay = 250) {
  const [query, setQuery] = useState("");
  const debouncedQuery = useDebounce(query, delay);

  const searchedGroups = useMemo(() => {
    const needle = debouncedQuery.trim().toLowerCase();
    if (!needle) return groups;

    return groups.map((group) => ({
      ...group,
      cards: group.cards.filter((card) => {
        const text = [
          card.title,
          card.descriptionMd,
          ...card.members.map((m) => m.name),
          ...card.labels.map((l) => l.name),
        ]
          .filter(Boolean)
          .join(" ")
          .toLowerCase();
        return text.includes(needle);
      }),
    }));
  }, [debouncedQuery, groups]);

  return {
    query,
    setQuery,
    debouncedQuery,
    searchedGroups,
  };
}
