import { act } from "react";
import { createRoot, type Root } from "react-dom/client";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";

const h = vi.hoisted(() => ({
  workspace: null as null | Record<string, unknown>,
  role: "member" as string | null,
}));

vi.mock("@/providers/workspace-provider", () => ({
  useWorkspaceContext: () => ({ workspaceId: "w1", workspace: h.workspace }),
}));
vi.mock("../use-workspace-viewer", () => ({
  useWorkspaceViewer: () => ({ role: h.role }),
}));

import { AccountReadOnlyBannerHost } from "../account-read-only-banner-host";

(
  globalThis as typeof globalThis & { IS_REACT_ACT_ENVIRONMENT?: boolean }
).IS_REACT_ACT_ENVIRONMENT = true;

const navigation = {
  getHref: (destination: { section: string }) => `/w/w1/${destination.section}`,
  onNavigate: () => {},
};

let root: Root | null = null;
let host: HTMLDivElement | null = null;
afterEach(() => {
  act(() => root?.unmount());
  host?.remove();
  root = null;
});
beforeEach(() => {
  h.role = "member";
  h.workspace = { id: "w1", name: "Minh An", accountStatus: "Suspended" };
});

async function mount() {
  host = document.createElement("div");
  document.body.append(host);
  root = createRoot(host);
  await act(async () => {
    root!.render(
      <I18nProvider catalogs={[]} initialLocale="en">
        <AccountReadOnlyBannerHost navigation={navigation} />
      </I18nProvider>,
    );
  });
}
const text = () => host?.textContent ?? "";
const billing = () => host?.querySelector("a[href$='/billing']");

describe("AccountReadOnlyBannerHost (D06)", () => {
  it("shows nothing for an Active Account, or when the status is not known", async () => {
    h.workspace = { id: "w1", accountStatus: "Active" };
    await mount();
    expect(text()).toBe("");
    act(() => root?.unmount());
    host?.remove();
    h.workspace = { id: "w1" };
    await mount();
    expect(text()).toBe("");
  });

  it("FUI[account.suspended:member] a plain member sees the Suspended banner and no billing link", async () => {
    for (const role of ["member", "guest", "admin", "owner"]) {
      h.role = role;
      await mount();
      expect(text()).toContain("This Account is suspended.");
      expect(!!billing()).toBe(role === "admin" || role === "owner");
      act(() => root?.unmount());
      host?.remove();
    }
  });

  it("FUI[account.suspended:closed] a Closed Account says closed", async () => {
    h.workspace = { id: "w1", accountStatus: "Closed" };
    await mount();
    expect(text()).toContain("This Account is closed.");
  });
});
