import type { ReactNode } from "react";
import { describe, expect, it } from "vitest";
import { I18nProvider, type Locale } from "@notrelix/i18n";
import { renderHook } from "@notrelix/testing";
import { kanbanToolbarCatalog } from "../kanban-toolbar.catalog";
import { en, vi } from "../kanban-toolbar.copy";
import { useKanbanToolbarCopy } from "../use-kanban-toolbar-copy";

function copyFor(locale: Locale) {
  const wrapper = ({ children }: { children: ReactNode }) => (
    <I18nProvider catalogs={[kanbanToolbarCatalog]} initialLocale={locale}>
      {children}
    </I18nProvider>
  );
  return renderHook(() => useKanbanToolbarCopy(), { wrapper }).result.current;
}

describe("useKanbanToolbarCopy", () => {
  it("selects the Vietnamese copy, with the mockup strings verbatim", () => {
    const copy = copyFor("vi");
    expect(copy).toBe(vi);
    expect(copy.search.placeholder).toBe("Tìm trong board…");
    expect(copy.search.label).toBe("Tìm trong board");
    expect(copy.filter.trigger).toBe("Lọc");
    expect(copy.groupBy.label).toBe("Nhóm theo");
    expect(copy.addItem).toBe("Thêm công việc");
    expect(copy.noResults.title).toBe("Không có việc khớp bộ lọc");
    expect(copy.noResults.clear).toBe("Bỏ lọc");
  });

  it("selects the English copy for the en locale", () => {
    expect(copyFor("en")).toBe(en);
    expect(copyFor("en").filter.trigger).toBe("Filter");
  });

  it("registers the toolbar copy as the kanban-toolbar catalog in both locales", () => {
    expect(kanbanToolbarCatalog.namespace).toBe("kanban-toolbar");
    expect(Object.keys(kanbanToolbarCatalog.messages).sort()).toEqual([
      "en",
      "vi",
    ]);
  });
});
