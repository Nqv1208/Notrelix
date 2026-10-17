import type { ReactNode } from "react";
import { describe, expect, it } from "vitest";
import { I18nProvider, type Locale } from "@notrelix/i18n";
import { renderHook } from "@notrelix/testing";
import { notrelixSystemRailCatalog } from "../notrelix-system-rail.catalog";
import { en, vi } from "../notrelix-system-rail.copy";
import { useNotrelixSystemRailCopy } from "../use-notrelix-system-rail-copy";

function copyFor(locale: Locale) {
  const wrapper = ({ children }: { children: ReactNode }) => (
    <I18nProvider catalogs={[notrelixSystemRailCatalog]} initialLocale={locale}>
      {children}
    </I18nProvider>
  );
  return renderHook(() => useNotrelixSystemRailCopy(), { wrapper }).result
    .current;
}

describe("useNotrelixSystemRailCopy", () => {
  it("selects the Vietnamese copy for the vi locale", () => {
    expect(copyFor("vi")).toBe(vi);
    expect(copyFor("vi").label).toBe("Hệ thống Notrelix");
  });

  it("selects the English copy for the en locale", () => {
    expect(copyFor("en")).toBe(en);
    expect(copyFor("en").label).toBe("Notrelix system");
  });

  it("registers the copy as the workspace-system-rail catalog in both locales", () => {
    expect(notrelixSystemRailCatalog.namespace).toBe("workspace-system-rail");
    expect(Object.keys(notrelixSystemRailCatalog.messages).sort()).toEqual([
      "en",
      "vi",
    ]);
  });
});
