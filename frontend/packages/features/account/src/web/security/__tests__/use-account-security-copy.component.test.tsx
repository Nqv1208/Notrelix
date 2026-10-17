import type { ReactNode } from "react";
import { describe, expect, it } from "vitest";
import { I18nProvider, type Locale } from "@notrelix/i18n";
import { renderHook } from "@notrelix/testing";
import { accountSecurityCatalog } from "../account-security.catalog";
import { en, vi } from "../account-security.copy";
import { useAccountSecurityCopy } from "../use-account-security-copy";

function copyFor(locale: Locale) {
  const wrapper = ({ children }: { children: ReactNode }) => (
    <I18nProvider catalogs={[accountSecurityCatalog]} initialLocale={locale}>
      {children}
    </I18nProvider>
  );
  return renderHook(() => useAccountSecurityCopy(), { wrapper }).result.current;
}

describe("useAccountSecurityCopy", () => {
  it("selects the Vietnamese copy for the vi locale", () => {
    expect(copyFor("vi")).toBe(vi);
    expect(copyFor("vi").title).toBe("Bảo mật");
  });

  it("selects the English copy for the en locale", () => {
    expect(copyFor("en")).toBe(en);
    expect(copyFor("en").title).toBe("Security");
  });

  it("registers the copy as the account-security catalog in both locales", () => {
    expect(accountSecurityCatalog.namespace).toBe("account-security");
    expect(Object.keys(accountSecurityCatalog.messages).sort()).toEqual([
      "en",
      "vi",
    ]);
  });
});
