import { createContext, useContext, useMemo, type ReactNode } from "react";
import type { FieldDefinition } from "@notrelix/work-management-core";

/** What the Kanban view chose to show on every card face: its fields, their order and which hide while empty. */
export interface KanbanFace {
  readonly fields: readonly FieldDefinition[];
  readonly cardFieldIds: readonly string[];
  readonly cardHideEmpty: readonly string[];
}

const EMPTY: KanbanFace = { fields: [], cardFieldIds: [], cardHideEmpty: [] };
const KanbanFaceContext = createContext<KanbanFace>(EMPTY);

export function KanbanFaceProvider({
  fields,
  cardFieldIds,
  cardHideEmpty,
  children,
}: KanbanFace & { children: ReactNode }) {
  const value = useMemo(
    () => ({ fields, cardFieldIds, cardHideEmpty }),
    [fields, cardFieldIds, cardHideEmpty],
  );
  return (
    <KanbanFaceContext.Provider value={value}>
      {children}
    </KanbanFaceContext.Provider>
  );
}

export function useKanbanFace(): KanbanFace {
  return useContext(KanbanFaceContext);
}
