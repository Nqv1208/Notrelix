import type { ReactNode } from "react";
import { describe, expect, it } from "vitest";
import { I18nProvider, type Locale } from "@notrelix/i18n";
import { renderHook } from "@notrelix/testing";
import { accountAppearanceCatalog } from "../account-appearance.catalog";
import { en, vi } from "../account-appearance.copy";
import { useAccountAppearanceCopy } from "../use-account-appearance-copy";

function copyFor(locale: Locale) {
  const wrapper = ({ children }: { children: ReactNode }) => (
    <I18nProvider catalogs={[accountAppearanceCatalog]} initialLocale={locale}>
      {children}
    </I18nProvider>
  );
  return renderHook(() => useAccountAppearanceCopy(), { wrapper }).result
    .current;
}

describe("useAccountAppearanceCopy", () => {
  it("selects the Vietnamese copy for the vi locale", () => {
    expect(copyFor("vi")).toBe(vi);
    expect(copyFor("vi").title).toBe("Giao diện");
  });

  it("selects the English copy for the en locale", () => {
    expect(copyFor("en")).toBe(en);
    expect(copyFor("en").title).toBe("Appearance");
  });

  it("registers the copy as the account-appearance catalog in both locales", () => {
    expect(accountAppearanceCatalog.namespace).toBe("account-appearance");
    expect(Object.keys(accountAppearanceCatalog.messages).sort()).toEqual([
      "en",
      "vi",
    ]);
  });
});
