import type { ReactNode } from "react";
import { describe, expect, it } from "vitest";
import { I18nProvider, type Locale } from "@notrelix/i18n";
import { renderHook } from "@notrelix/testing";
import { notificationBellCatalog } from "../notification-bell.catalog";
import { en, vi } from "../notification-bell.copy";
import { useNotificationBellCopy } from "../use-notification-bell-copy";

function copyFor(locale: Locale) {
  const wrapper = ({ children }: { children: ReactNode }) => (
    <I18nProvider catalogs={[notificationBellCatalog]} initialLocale={locale}>
      {children}
    </I18nProvider>
  );
  return renderHook(() => useNotificationBellCopy(), { wrapper }).result
    .current;
}

describe("useNotificationBellCopy", () => {
  it("selects the Vietnamese copy for the vi locale", () => {
    expect(copyFor("vi")).toBe(vi);
    expect(copyFor("vi").bell).toBe("Thông báo");
  });

  it("selects the English copy for the en locale", () => {
    expect(copyFor("en")).toBe(en);
    expect(copyFor("en").bell).toBe("Notifications");
  });

  it("registers the copy as the notification-bell catalog in both locales", () => {
    expect(notificationBellCatalog.namespace).toBe("notification-bell");
    expect(Object.keys(notificationBellCatalog.messages).sort()).toEqual([
      "en",
      "vi",
    ]);
  });
});
