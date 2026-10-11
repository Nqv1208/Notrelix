import type { ReactNode } from "react";
import { describe, expect, it } from "vitest";
import { I18nProvider, type Locale } from "@notrelix/i18n";
import { renderHook } from "@notrelix/testing";
import { accountNavCatalog } from "../account-nav.catalog";
import { en, vi } from "../account-nav.copy";
import { useAccountNavCopy } from "../use-account-nav-copy";

function copyFor(locale: Locale) {
  const wrapper = ({ children }: { children: ReactNode }) => (
    <I18nProvider catalogs={[accountNavCatalog]} initialLocale={locale}>
      {children}
    </I18nProvider>
  );
  return renderHook(() => useAccountNavCopy(), { wrapper }).result.current;
}

describe("useAccountNavCopy", () => {
  it("selects the copy of the active locale", () => {
    expect(copyFor("vi")).toBe(vi);
    expect(copyFor("en")).toBe(en);
    expect(copyFor("vi").security).toBe("Bảo mật");
  });

  it("covers the same entries in both locales", () => {
    expect(Object.keys(vi).sort()).toEqual(Object.keys(en).sort());
    expect(accountNavCatalog.namespace).toBe("account-nav");
  });
});
