import type { ReactNode } from "react";
import { describe, expect, it } from "vitest";
import { I18nProvider, type Locale } from "@notrelix/i18n";
import { renderHook } from "@notrelix/testing";
import { accountNotificationsCatalog } from "../account-notifications.catalog";
import { en, vi } from "../account-notifications.copy";
import { useAccountNotificationsCopy } from "../use-account-notifications-copy";

function copyFor(locale: Locale) {
  const wrapper = ({ children }: { children: ReactNode }) => (
    <I18nProvider
      catalogs={[accountNotificationsCatalog]}
      initialLocale={locale}
    >
      {children}
    </I18nProvider>
  );
  return renderHook(() => useAccountNotificationsCopy(), { wrapper }).result
    .current;
}

describe("useAccountNotificationsCopy", () => {
  it("selects the Vietnamese copy for the vi locale", () => {
    expect(copyFor("vi")).toBe(vi);
    expect(copyFor("vi").title).toBe("Thông báo");
  });

  it("selects the English copy for the en locale", () => {
    expect(copyFor("en")).toBe(en);
    expect(copyFor("en").title).toBe("Notifications");
  });

  it("registers the copy as the account-notifications catalog in both locales", () => {
    expect(accountNotificationsCatalog.namespace).toBe("account-notifications");
    expect(Object.keys(accountNotificationsCatalog.messages).sort()).toEqual([
      "en",
      "vi",
    ]);
  });
});
