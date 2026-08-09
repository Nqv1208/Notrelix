import { useMemo, useState } from "react";
import type { BoardGroup, Card } from "@notrelix/work-management-core";
import type { KanbanFiltersState } from "@notrelix/work-management-core";

export function useKanbanFilters(groups: BoardGroup[]) {
  const [filters, setFilters] = useState<KanbanFiltersState>({
    status: [],
    priority: [],
    assigneeId: [],
    labelId: [],
  });

  const filteredGroups = useMemo(() => {
    const hasActiveFilters =
      filters.status.length > 0 ||
      filters.priority.length > 0 ||
      filters.assigneeId.length > 0 ||
      filters.labelId.length > 0;

    if (!hasActiveFilters) return groups;

    return groups.map((group) => ({
      ...group,
      cards: group.cards.filter((card) => {
        if (
          filters.status.length > 0 &&
          !filters.status.includes(card.status)
        ) {
          return false;
        }
        if (
          filters.priority.length > 0 &&
          (!card.priority || !filters.priority.includes(card.priority))
        ) {
          return false;
        }
        if (filters.assigneeId.length > 0) {
          const cardUserIds = card.members.map((m) => m.userId);
          const matches = filters.assigneeId.some((id) =>
            cardUserIds.includes(id),
          );
          if (!matches) return false;
        }
        if (filters.labelId.length > 0) {
          const cardLabelIds = card.labels.map((l) => l.id);
          const matches = filters.labelId.some((id) =>
            cardLabelIds.includes(id),
          );
          if (!matches) return false;
        }
        return true;
      }),
    }));
  }, [filters, groups]);

  const setFilterValues = (key: keyof KanbanFiltersState, values: string[]) => {
    setFilters((current) => ({
      ...current,
      [key]: values,
    }));
  };

  const clearFilters = () => {
    setFilters({
      status: [],
      priority: [],
      assigneeId: [],
      labelId: [],
    });
  };

  return {
    filters,
    setFilterValues,
    clearFilters,
    filteredGroups,
  };
}
