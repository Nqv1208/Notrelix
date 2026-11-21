import { describe, expect, it, vi } from "vitest";
import {
  fireEvent,
  renderPureUi,
  screen,
  waitFor,
  within,
} from "@notrelix/testing";

import type { SsoDomain, SsoSettings } from "../../../core/sso";
import { accountSecurityCopy } from "../account-security.copy";
import {
  AccountSsoPanel,
  type AccountSsoActions,
  type AccountSsoState,
} from "../account-sso-panel";

const TYPED_VALUE =
  String.fromCharCode(65, 97, 49, 33) + Math.random().toString(36).slice(2);

const copy = accountSecurityCopy("en");
const t = copy.sso;

const pendingDomain: SsoDomain = {
  id: "d-1",
  domain: "minhan.vn",
  status: "Pending",
  txtHost: "minhan.vn",
  txtValue: "notrelix-verify=tok-123",
  verifiedAt: null,
  claimed: false,
};
const verifiedDomain: SsoDomain = {
  id: "d-2",
  domain: "minhan.com",
  status: "Verified",
  txtHost: "minhan.com",
  txtValue: null,
  verifiedAt: "2026-11-01T00:00:00Z",
  claimed: false,
};

function settings(overrides: Partial<SsoSettings> = {}): SsoSettings {
  return {
    canManage: true,
    redirectUri: "https://api.example/api/v1/auth/sso/callback",
    requireSso: false,
    provider: null,
    domains: [],
    ...overrides,
  };
}

const activeProvider: NonNullable<SsoSettings["provider"]> = {
  name: "Minh An Entra",
  issuerUrl: "https://login.example/tenant/v2.0",
  clientId: "client-1",
  hasClientSecret: true,
  status: "Active",
  validationError: null,
  jitProvisioning: true,
};

function actionsOf(
  overrides: Partial<AccountSsoActions> = {},
): AccountSsoActions {
  return {
    saveProvider: vi.fn(async () => ({
      ok: true as const,
      value: { status: "Active" as const, validationError: null },
    })),
    removeProvider: vi.fn(async () => ({ ok: true as const, value: null })),
    addDomain: vi.fn(async () => ({
      ok: true as const,
      value: pendingDomain,
    })),
    verifyDomain: vi.fn(async () => ({
      ok: true as const,
      value: { ...pendingDomain, status: "Verified" as const, txtValue: null },
    })),
    removeDomain: vi.fn(async () => ({ ok: true as const, value: null })),
    setRequired: vi.fn(async () => ({ ok: true as const, value: null })),
    retry: vi.fn(),
    ...overrides,
  };
}

function render(state: AccountSsoState, actions = actionsOf()) {
  renderPureUi(<AccountSsoPanel copy={copy} state={state} actions={actions} />);
  return actions;
}
const ready = (overrides: Partial<SsoSettings> = {}): AccountSsoState => ({
  status: "ready",
  data: settings(overrides),
});

describe("AccountSsoPanel states", () => {
  it("says it is loading", () => {
    render({ status: "loading" });
    expect(screen.getByRole("status").textContent).toContain(t.loading);
  });

  it("offers a retry when the setup could not be read", () => {
    const actions = render({ status: "error" });
    expect(screen.getByRole("alert").textContent).toContain(t.loadError);
    fireEvent.click(screen.getByRole("button", { name: copy.retry }));
    expect(actions.retry).toHaveBeenCalledTimes(1);
  });

  it("states the permission is missing and draws no control", () => {
    render({ status: "forbidden" });
    expect(screen.getByText(t.forbidden)).toBeTruthy();
    expect(screen.queryByRole("button")).toBeNull();
    expect(screen.queryByRole("switch")).toBeNull();
  });

  it("shows the empty setup with the way in, and no domain yet", () => {
    render(ready());
    expect(screen.getByText(t.tag.notSet)).toBeTruthy();
    expect(screen.getByText(t.provider.emptyText)).toBeTruthy();
    expect(screen.getByText(t.domains.empty)).toBeTruthy();
  });
});

describe("provider setup", () => {
  it("grows the form in place with focus on its first field", () => {
    render(ready());
    fireEvent.click(screen.getByRole("button", { name: t.provider.setUp }));
    expect(document.activeElement).toBe(screen.getByLabelText(t.provider.name));
    // The redirect URI to register at the provider is shown, with a copy button.
    expect(
      (screen.getByLabelText(t.provider.redirect) as HTMLInputElement).value,
    ).toBe("https://api.example/api/v1/auth/sso/callback");
    expect(
      screen.getByRole("button", {
        name: `${t.provider.copy}: ${t.provider.redirect}`,
      }),
    ).toBeTruthy();
  });

  it("tells what is missing before anything is sent", async () => {
    const actions = render(ready());
    fireEvent.click(screen.getByRole("button", { name: t.provider.setUp }));
    fireEvent.click(screen.getByRole("button", { name: t.provider.save }));
    expect(screen.getByText(t.provider.nameRequired)).toBeTruthy();
    expect(screen.getByText(t.provider.issuerInvalid)).toBeTruthy();
    expect(screen.getByText(t.provider.clientIdRequired)).toBeTruthy();
    expect(screen.getByText(t.provider.secretRequired)).toBeTruthy();
    expect(actions.saveProvider).not.toHaveBeenCalled();
    expect(
      screen.getByLabelText(t.provider.name).getAttribute("aria-invalid"),
    ).toBe("true");
  });

  it("refuses an issuer that is not https", () => {
    const actions = render(ready());
    fireEvent.click(screen.getByRole("button", { name: t.provider.setUp }));
    fireEvent.change(screen.getByLabelText(t.provider.name), {
      target: { value: "Entra" },
    });
    fireEvent.change(screen.getByLabelText(t.provider.issuer), {
      target: { value: "http://login.example" },
    });
    fireEvent.change(screen.getByLabelText(t.provider.clientId), {
      target: { value: "c" },
    });
    fireEvent.change(screen.getByLabelText(t.provider.secret), {
      target: { value: "s" },
    });
    fireEvent.click(screen.getByRole("button", { name: t.provider.save }));
    expect(screen.getByText(t.provider.issuerInvalid)).toBeTruthy();
    expect(actions.saveProvider).not.toHaveBeenCalled();
  });

  it("sends the setup, keeps the secret out of the page after the save and reports the result", async () => {
    const actions = render(ready());
    fireEvent.click(screen.getByRole("button", { name: t.provider.setUp }));
    fireEvent.change(screen.getByLabelText(t.provider.name), {
      target: { value: "Entra" },
    });
    fireEvent.change(screen.getByLabelText(t.provider.issuer), {
      target: { value: "https://login.example/t/v2.0" },
    });
    fireEvent.change(screen.getByLabelText(t.provider.clientId), {
      target: { value: "client-9" },
    });
    const secret = screen.getByLabelText(t.provider.secret) as HTMLInputElement;
    expect(secret.type).toBe("password");
    fireEvent.change(secret, { target: { value: TYPED_VALUE } });
    fireEvent.click(screen.getByRole("button", { name: t.provider.save }));
    await waitFor(() =>
      expect(actions.saveProvider).toHaveBeenCalledWith({
        name: "Entra",
        issuerUrl: "https://login.example/t/v2.0",
        clientId: "client-9",
        clientSecret: TYPED_VALUE,
        jitProvisioning: true,
      }),
    );
    await screen.findByText(t.provider.savedActive);
    expect(document.body.innerHTML).not.toContain(TYPED_VALUE);
    expect(
      (document.body.querySelector("input[type=password]") as unknown) ?? null,
    ).toBeNull();
  });

  it("shows 'Secret saved' for a stored secret, never fills the field and keeps it when left empty", async () => {
    const actions = render(ready({ provider: activeProvider }));
    expect(screen.getByText(t.provider.secretSaved)).toBeTruthy();
    expect(screen.getByText(t.tag.active)).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: t.provider.edit }));
    const secret = screen.getByLabelText(t.provider.secret) as HTMLInputElement;
    expect(secret.value).toBe("");
    expect(screen.getByText(t.provider.secretKeep)).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: t.provider.save }));
    await waitFor(() => expect(actions.saveProvider).toHaveBeenCalled());
    const sent = (actions.saveProvider as ReturnType<typeof vi.fn>).mock
      .calls[0]?.[0];
    expect(sent).not.toHaveProperty("clientSecret");
    expect(sent).toMatchObject({
      name: "Minh An Entra",
      jitProvisioning: true,
    });
  });

  it("lets the Owner turn first-sign-in account creation off", async () => {
    const actions = render(ready({ provider: activeProvider }));
    fireEvent.click(screen.getByRole("button", { name: t.provider.edit }));
    fireEvent.click(screen.getByRole("switch", { name: t.provider.jit }));
    fireEvent.click(screen.getByRole("button", { name: t.provider.save }));
    await waitFor(() =>
      expect(actions.saveProvider).toHaveBeenCalledWith(
        expect.objectContaining({ jitProvisioning: false }),
      ),
    );
  });

  it("states why a Draft provider is not active", () => {
    render(
      ready({
        provider: {
          ...activeProvider,
          status: "Draft",
          validationError: "The discovery document could not be read.",
        },
      }),
    );
    expect(screen.getByText(t.tag.draft)).toBeTruthy();
    expect(screen.getByRole("alert").textContent).toContain(
      "The discovery document could not be read.",
    );
  });

  it("shows a refused issuer next to the field and keeps the form open", async () => {
    const actions = render(
      ready(),
      actionsOf({
        saveProvider: vi.fn(async () => ({
          ok: false as const,
          failure: "invalid-issuer" as const,
        })),
      }),
    );
    fireEvent.click(screen.getByRole("button", { name: t.provider.setUp }));
    fireEvent.change(screen.getByLabelText(t.provider.name), {
      target: { value: "E" },
    });
    fireEvent.change(screen.getByLabelText(t.provider.issuer), {
      target: { value: "https://10.0.0.1" },
    });
    fireEvent.change(screen.getByLabelText(t.provider.clientId), {
      target: { value: "c" },
    });
    fireEvent.change(screen.getByLabelText(t.provider.secret), {
      target: { value: "s" },
    });
    fireEvent.click(screen.getByRole("button", { name: t.provider.save }));
    await screen.findByText(t.provider.invalidIssuer);
    expect(actions.saveProvider).toHaveBeenCalledTimes(1);
    expect(screen.getByRole("button", { name: t.provider.save })).toBeTruthy();
  });

  it.each([
    ["invalid-name", t.provider.name, t.provider.nameRequired],
    ["invalid-client", t.provider.clientId, t.provider.clientIdRequired],
    ["client-secret-required", t.provider.secret, t.provider.secretRequired],
  ] as const)(
    "shows the server's %s refusal next to its field",
    async (failure, label, text) => {
      render(
        ready({ provider: activeProvider }),
        actionsOf({
          saveProvider: vi.fn(async () => ({ ok: false as const, failure })),
        }),
      );
      fireEvent.click(screen.getByRole("button", { name: t.provider.edit }));
      fireEvent.click(screen.getByRole("button", { name: t.provider.save }));
      await screen.findByText(text);
      expect(document.activeElement).toBe(screen.getByLabelText(label));
    },
  );

  it("says the provider cannot be replaced while single sign-on is required (409)", async () => {
    render(
      ready({ provider: activeProvider, requireSso: true }),
      actionsOf({
        saveProvider: vi.fn(async () => ({
          ok: false as const,
          failure: "in-use" as const,
        })),
      }),
    );
    fireEvent.click(screen.getByRole("button", { name: t.provider.edit }));
    fireEvent.click(screen.getByRole("button", { name: t.provider.save }));
    expect(
      (await screen.findByText(t.provider.inUseReplace)).getAttribute("role"),
    ).toBe("alert");
  });

  it("asks before it removes the provider, then removes it", async () => {
    const actions = render(ready({ provider: activeProvider }));
    fireEvent.click(screen.getByRole("button", { name: t.provider.remove }));
    expect(actions.removeProvider).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button", { name: t.provider.removeYes }));
    await waitFor(() => expect(actions.removeProvider).toHaveBeenCalled());
    await screen.findByText(t.provider.removed);
  });
});

describe("domains", () => {
  it("shows the TXT record of a Pending domain with copy buttons and a Verify button", () => {
    render(ready({ domains: [pendingDomain] }));
    expect(screen.getByText(t.domains.pending)).toBeTruthy();
    expect(screen.getByText(t.domains.txtTitle)).toBeTruthy();
    expect(screen.getByText("notrelix-verify=tok-123")).toBeTruthy();
    expect(
      screen.getByRole("button", {
        name: "Copy host of minhan.vn",
      }),
    ).toBeTruthy();
    expect(
      screen.getByRole("button", { name: "Copy value of minhan.vn" }),
    ).toBeTruthy();
    expect(
      screen.getByRole("button", { name: "Verify minhan.vn" }),
    ).toBeTruthy();
  });

  it("copies the TXT value", async () => {
    const writeText = vi.fn(async () => undefined);
    Object.defineProperty(navigator, "clipboard", {
      value: { writeText },
      configurable: true,
    });
    render(ready({ domains: [pendingDomain] }));
    fireEvent.click(
      screen.getByRole("button", { name: "Copy value of minhan.vn" }),
    );
    await waitFor(() =>
      expect(writeText).toHaveBeenCalledWith("notrelix-verify=tok-123"),
    );
    expect(
      (await screen.findAllByText(t.provider.copied)).length,
    ).toBeGreaterThan(0);
  });

  it("shows a Verified domain with no TXT record and no Verify button", () => {
    render(ready({ domains: [verifiedDomain] }));
    expect(screen.getByText(t.domains.verified)).toBeTruthy();
    expect(screen.queryByText(t.domains.txtTitle)).toBeNull();
    expect(screen.queryByRole("button", { name: /^Verify / })).toBeNull();
  });

  it("says a verify that found nothing is still pending, and keeps the TXT record", async () => {
    const actions = render(
      ready({ domains: [pendingDomain] }),
      actionsOf({
        verifyDomain: vi.fn(async () => ({
          ok: true as const,
          value: pendingDomain,
        })),
      }),
    );
    fireEvent.click(screen.getByRole("button", { name: "Verify minhan.vn" }));
    await waitFor(() =>
      expect(actions.verifyDomain).toHaveBeenCalledWith("d-1"),
    );
    const note = await screen.findByText(/haven't found the record yet/);
    expect(note.getAttribute("role")).toBe("status");
    expect(screen.getByText("notrelix-verify=tok-123")).toBeTruthy();
  });

  it("announces a verified domain and moves focus to its row", async () => {
    render(ready({ domains: [pendingDomain] }));
    fireEvent.click(screen.getByRole("button", { name: "Verify minhan.vn" }));
    await screen.findByText("minhan.vn is verified.");
    const row = screen
      .getByText("minhan.vn", { selector: "b" })
      .closest("[tabindex='-1']");
    expect(document.activeElement).toBe(row);
  });

  it("shows the claimed conflict and offers no Verify", () => {
    render(ready({ domains: [{ ...pendingDomain, claimed: true }] }));
    expect(screen.getByText(t.domains.claimed)).toBeTruthy();
    expect(
      screen.getByText(/already claimed by another account/).textContent,
    ).toContain("minhan.vn");
    expect(screen.queryByRole("button", { name: /^Verify / })).toBeNull();
  });

  it("turns a verify answered 'claimed' into the conflict note", async () => {
    render(
      ready({ domains: [pendingDomain] }),
      actionsOf({
        verifyDomain: vi.fn(async () => ({
          ok: true as const,
          value: { ...pendingDomain, claimed: true },
        })),
      }),
    );
    fireEvent.click(screen.getByRole("button", { name: "Verify minhan.vn" }));
    await screen.findByText(/already claimed by another account/);
  });

  it("turns a verify refused 409 'claimed' into the conflict note", async () => {
    render(
      ready({ domains: [pendingDomain] }),
      actionsOf({
        verifyDomain: vi.fn(async () => ({
          ok: false as const,
          failure: "claimed" as const,
        })),
      }),
    );
    fireEvent.click(screen.getByRole("button", { name: "Verify minhan.vn" }));
    await screen.findByText(/already claimed by another account/);
  });

  it("adds a domain, announces it, clears the field and focuses the new row", async () => {
    const actions = render(ready());
    const input = screen.getByLabelText(t.domains.label) as HTMLInputElement;
    fireEvent.change(input, { target: { value: "  MinhAn.vn " } });
    fireEvent.click(screen.getByRole("button", { name: t.domains.add }));
    await waitFor(() =>
      expect(actions.addDomain).toHaveBeenCalledWith("minhan.vn"),
    );
    await screen.findByText(/minhan\.vn added/);
    expect(input.value).toBe("");
  });

  it.each([
    ["claimed", t.domains.claimedAdd],
    ["public-provider", t.domains.publicProvider],
    ["invalid-domain", t.domains.invalid],
    ["failed", t.failed],
  ] as const)(
    "shows a refused add (%s) next to the field",
    async (failure, text) => {
      render(
        ready(),
        actionsOf({
          addDomain: vi.fn(async () => ({ ok: false as const, failure })),
        }),
      );
      fireEvent.change(screen.getByLabelText(t.domains.label), {
        target: { value: "gmail.com" },
      });
      fireEvent.click(screen.getByRole("button", { name: t.domains.add }));
      const alert = await screen.findByText(text);
      expect(alert.getAttribute("role")).toBe("alert");
      expect(
        screen.getByLabelText(t.domains.label).getAttribute("aria-invalid"),
      ).toBe("true");
    },
  );

  it("tells a malformed domain before it is sent", () => {
    const actions = render(ready());
    fireEvent.change(screen.getByLabelText(t.domains.label), {
      target: { value: "nodot" },
    });
    fireEvent.click(screen.getByRole("button", { name: t.domains.add }));
    expect(screen.getByText(t.domains.invalid)).toBeTruthy();
    expect(actions.addDomain).not.toHaveBeenCalled();
  });

  it("asks, then removes a domain; explains a refused removal in place", async () => {
    const actions = render(
      ready({ domains: [verifiedDomain] }),
      actionsOf({
        removeDomain: vi.fn(async () => ({
          ok: false as const,
          failure: "in-use" as const,
        })),
      }),
    );
    fireEvent.click(screen.getByRole("button", { name: "Remove minhan.com" }));
    expect(actions.removeDomain).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button", { name: t.domains.removeYes }));
    await waitFor(() =>
      expect(actions.removeDomain).toHaveBeenCalledWith("d-2"),
    );
    const row = screen
      .getByText("minhan.com", { selector: "b" })
      .closest("li") as HTMLElement;
    expect(await within(row).findByRole("alert")).toBeTruthy();
    expect(within(row).getByRole("alert").textContent).toBe(t.domains.inUse);
  });
});

describe("require single sign-on", () => {
  it("is switched off and explains why until the provider is Active and a domain Verified", () => {
    render(ready({ provider: activeProvider, domains: [pendingDomain] }));
    const sw = screen.getByRole("switch", { name: t.require.title });
    expect((sw as HTMLButtonElement).disabled).toBe(true);
    expect(sw.getAttribute("aria-describedby")).toContain("reason");
    expect(screen.getByText(t.require.reason)).toBeTruthy();
  });

  it("stays disabled for a Draft provider even with a Verified domain", () => {
    render(
      ready({
        provider: { ...activeProvider, status: "Draft" },
        domains: [verifiedDomain],
      }),
    );
    expect(
      (
        screen.getByRole("switch", {
          name: t.require.title,
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(true);
  });

  it("turns on when ready and announces it", async () => {
    const actions = render(
      ready({ provider: activeProvider, domains: [verifiedDomain] }),
    );
    const sw = screen.getByRole("switch", { name: t.require.title });
    expect((sw as HTMLButtonElement).disabled).toBe(false);
    fireEvent.click(sw);
    await waitFor(() => expect(actions.setRequired).toHaveBeenCalledWith(true));
    await screen.findByText(t.require.savedOn);
  });

  it("can always be turned off, and says what it does while on", async () => {
    const actions = render(
      ready({ requireSso: true, provider: activeProvider, domains: [] }),
    );
    expect(screen.getByText(t.tag.required)).toBeTruthy();
    expect(screen.getByText(t.require.inUse)).toBeTruthy();
    fireEvent.click(screen.getByRole("switch", { name: t.require.title }));
    await waitFor(() =>
      expect(actions.setRequired).toHaveBeenCalledWith(false),
    );
  });

  it.each([
    ["not-ready", t.require.notReady],
    ["in-use", t.require.inUse],
    ["failed", t.failed],
  ] as const)("handles the %s answer", async (failure, text) => {
    render(
      ready({ provider: activeProvider, domains: [verifiedDomain] }),
      actionsOf({
        setRequired: vi.fn(async () => ({ ok: false as const, failure })),
      }),
    );
    fireEvent.click(screen.getByRole("switch", { name: t.require.title }));
    const alert = await screen.findByRole("alert");
    expect(alert.textContent).toBe(text);
  });
});
