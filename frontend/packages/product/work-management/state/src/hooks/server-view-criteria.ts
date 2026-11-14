import {
  ASSIGNEE_FIELD_ID,
  findPriorityField,
  findStatusField,
  LABEL_FIELD_ID,
  priorityFromOptionName,
  serializeCompoundFilter,
  type CompoundFilter,
  type FieldDefinition,
  type FilterCondition,
} from "@notrelix/work-management-core";
import {
  normaliseSearchText,
  type KanbanLaneFilter,
} from "./use-kanban-filters";

/**
 * The filter a paged read sends for the view: the toolbar facets (status, priority, people, labels; values of a
 * facet match any, facets combine) as conditions of the compound filter, joined with the member's own filter.
 * `""` when nothing narrows the board; `null` when the server cannot say it (an overdue facet, a priority facet
 * on a board without a priority field, facets beside an "any of" filter): the board is then read whole.
 */
export function serverFilterOf(
  facets: KanbanLaneFilter,
  compound: CompoundFilter | undefined,
  fields: readonly FieldDefinition[],
): string | null {
  if (facets.overdue) return null;
  const conditions: FilterCondition[] = [];
  const anyOf = (fieldId: string, values: readonly string[]) =>
    conditions.push({ fieldId, operator: "has_any", values: [...values] });
  if (facets.statusOptionIds.length > 0) {
    const status = findStatusField(fields);
    if (!status) return null;
    anyOf(status.id, facets.statusOptionIds);
  }
  if (facets.priorities.length > 0) {
    const priority = findPriorityField(fields);
    if (!priority) return null;
    // A card's priority is read from its option's name: the options whose name reads as a chosen priority.
    const options = priority.options
      .filter((option) => {
        const level = priorityFromOptionName(option.label);
        return level !== undefined && facets.priorities.includes(level);
      })
      .map((option) => option.id);
    if (options.length === 0) return null;
    anyOf(priority.id, options);
  }
  if (facets.assigneeIds.length > 0)
    anyOf(ASSIGNEE_FIELD_ID, facets.assigneeIds);
  if (facets.labelIds.length > 0) anyOf(LABEL_FIELD_ID, facets.labelIds);

  const own = compound?.conditions ?? [];
  if (conditions.length === 0) {
    return compound && own.length > 0 ? serializeCompoundFilter(compound) : "";
  }
  // The wire form has one join: facets (always all of) beside an "any of" filter of more than one condition cannot be said.
  if (compound?.join === "or" && own.length > 1) return null;
  return serializeCompoundFilter({
    join: "and",
    conditions: [...conditions, ...own],
  });
}

/**
 * The people whose name holds the search text, as the search box reads a card's people: their ids,
 * comma-separated, so the server matches their items too (it does not hold their names). `""` for none.
 */
export function searchMembersOf(
  query: string,
  people: Iterable<readonly [userId: string, name: string]>,
): string {
  const needle = normaliseSearchText(query);
  if (!needle) return "";
  const ids: string[] = [];
  for (const [userId, name] of people) {
    if (normaliseSearchText(name).includes(needle) && !ids.includes(userId))
      ids.push(userId);
  }
  return ids.join(",");
}
