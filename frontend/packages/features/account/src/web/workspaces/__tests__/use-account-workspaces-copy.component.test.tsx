import type { ReactNode } from "react";
import { describe, expect, it } from "vitest";
import { I18nProvider, type Locale } from "@notrelix/i18n";
import { renderHook } from "@notrelix/testing";
import { accountWorkspacesCatalog } from "../account-workspaces.catalog";
import { en, vi } from "../account-workspaces.copy";
import { useAccountWorkspacesCopy } from "../use-account-workspaces-copy";

function copyFor(locale: Locale) {
  const wrapper = ({ children }: { children: ReactNode }) => (
    <I18nProvider catalogs={[accountWorkspacesCatalog]} initialLocale={locale}>
      {children}
    </I18nProvider>
  );
  return renderHook(() => useAccountWorkspacesCopy(), { wrapper }).result
    .current;
}

describe("useAccountWorkspacesCopy", () => {
  it("selects the Vietnamese copy for the vi locale", () => {
    expect(copyFor("vi")).toBe(vi);
    expect(copyFor("vi").title).toBe("Workspace của bạn");
  });

  it("selects the English copy for the en locale", () => {
    expect(copyFor("en")).toBe(en);
    expect(copyFor("en").title).toBe("Your workspaces");
  });

  it("registers the copy as the account-workspaces catalog in both locales", () => {
    expect(accountWorkspacesCatalog.namespace).toBe("account-workspaces");
    expect(Object.keys(accountWorkspacesCatalog.messages).sort()).toEqual([
      "en",
      "vi",
    ]);
  });
});
