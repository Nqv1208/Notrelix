import type { ReactNode } from "react";
import { describe, expect, it } from "vitest";
import { I18nProvider, type Locale } from "@notrelix/i18n";
import { renderHook } from "@notrelix/testing";
import { kanbanCardCatalog } from "../kanban-card.catalog";
import { en, vi } from "../kanban-card.copy";
import { useKanbanCardCopy } from "../use-kanban-card-copy";

function copyFor(locale: Locale) {
  const wrapper = ({ children }: { children: ReactNode }) => (
    <I18nProvider catalogs={[kanbanCardCatalog]} initialLocale={locale}>
      {children}
    </I18nProvider>
  );
  return renderHook(() => useKanbanCardCopy(), { wrapper }).result.current;
}

describe("useKanbanCardCopy", () => {
  it("selects the Vietnamese copy for the vi locale", () => {
    expect(copyFor("vi")).toBe(vi);
    expect(copyFor("vi").overdue).toBe("Quá hạn");
  });

  it("selects the English copy for the en locale", () => {
    expect(copyFor("en")).toBe(en);
    expect(copyFor("en").overdue).toBe("Overdue");
  });

  it("registers the card copy as the kanban-card catalog in both locales", () => {
    expect(kanbanCardCatalog.namespace).toBe("kanban-card");
    expect(Object.keys(kanbanCardCatalog.messages).sort()).toEqual([
      "en",
      "vi",
    ]);
  });
});
