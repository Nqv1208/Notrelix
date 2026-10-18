import type { ReactNode } from "react";
import { describe, expect, it } from "vitest";
import { I18nProvider, type Locale } from "@notrelix/i18n";
import { renderHook } from "@notrelix/testing";
import { accountProfileCatalog } from "../account-profile.catalog";
import { en, vi } from "../account-profile.copy";
import { useAccountProfileCopy } from "../use-account-profile-copy";

function copyFor(locale: Locale) {
  const wrapper = ({ children }: { children: ReactNode }) => (
    <I18nProvider catalogs={[accountProfileCatalog]} initialLocale={locale}>
      {children}
    </I18nProvider>
  );
  return renderHook(() => useAccountProfileCopy(), { wrapper }).result.current;
}

describe("useAccountProfileCopy", () => {
  it("selects the Vietnamese copy for the vi locale", () => {
    expect(copyFor("vi")).toBe(vi);
    expect(copyFor("vi").title).toBe("Hồ sơ");
  });

  it("selects the English copy for the en locale", () => {
    expect(copyFor("en")).toBe(en);
    expect(copyFor("en").title).toBe("Profile");
  });

  it("registers the copy as the account-profile catalog in both locales", () => {
    expect(accountProfileCatalog.namespace).toBe("account-profile");
    expect(Object.keys(accountProfileCatalog.messages).sort()).toEqual([
      "en",
      "vi",
    ]);
  });
});
