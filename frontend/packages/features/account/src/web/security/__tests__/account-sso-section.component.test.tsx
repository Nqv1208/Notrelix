import { describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { AccountScopeProvider } from "@notrelix/query";
import { fireEvent, render, screen, waitFor } from "@notrelix/testing";

import type { AccountService } from "../../../core/api/account.service";
import type { SsoSettings } from "../../../core/sso";
import { accountSecurityCopy } from "../account-security.copy";
import { AccountSsoSection } from "../account-sso-section";

const copy = accountSecurityCopy("en");
const t = copy.sso;

const base: SsoSettings = {
  canManage: true,
  redirectUri: "https://api.example/api/v1/auth/sso/callback",
  requireSso: false,
  provider: null,
  domains: [],
};

const refusal = (status: number, code?: string) =>
  Object.assign(new Error("refused"), { status, code });

function mount(service: Partial<AccountService>) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  render(
    <QueryClientProvider client={queryClient}>
      <AccountScopeProvider accountId="acc-1">
        <AccountSsoSection
          copy={copy}
          service={service as unknown as AccountService}
        />
      </AccountScopeProvider>
    </QueryClientProvider>,
  );
}

describe("AccountSsoSection", () => {
  it("shows loading, then the empty setup", async () => {
    mount({ getSso: vi.fn(async () => base) });
    expect(screen.getByRole("status").textContent).toContain(t.loading);
    expect(await screen.findByText(t.provider.emptyText)).toBeTruthy();
  });

  it("turns a 403 on the read into the permission-denied state", async () => {
    mount({
      getSso: vi.fn(async () => {
        throw refusal(403);
      }),
    });
    expect(await screen.findByText(t.forbidden)).toBeTruthy();
    expect(screen.queryByRole("button")).toBeNull();
  });

  it("also treats canManage=false as permission denied", async () => {
    mount({ getSso: vi.fn(async () => ({ ...base, canManage: false })) });
    expect(await screen.findByText(t.forbidden)).toBeTruthy();
  });

  it("shows a request failure with a retry that reads again", async () => {
    const getSso = vi
      .fn<() => Promise<SsoSettings>>()
      .mockRejectedValueOnce(refusal(500))
      .mockResolvedValue(base);
    mount({ getSso });
    expect((await screen.findByRole("alert")).textContent).toContain(
      t.loadError,
    );
    fireEvent.click(screen.getByRole("button", { name: copy.retry }));
    expect(await screen.findByText(t.provider.emptyText)).toBeTruthy();
    expect(getSso).toHaveBeenCalledTimes(2);
  });

  it("saves the provider, then shows it as saved without ever showing the secret back", async () => {
    const saveSsoProvider = vi.fn(async () => ({
      status: "Active" as const,
      validationError: null,
    }));
    mount({ getSso: vi.fn(async () => base), saveSsoProvider });
    fireEvent.click(
      await screen.findByRole("button", { name: t.provider.setUp }),
    );
    fireEvent.change(screen.getByLabelText(t.provider.name), {
      target: { value: "Entra" },
    });
    fireEvent.change(screen.getByLabelText(t.provider.issuer), {
      target: { value: "https://login.example/t" },
    });
    fireEvent.change(screen.getByLabelText(t.provider.clientId), {
      target: { value: "cid" },
    });
    fireEvent.change(screen.getByLabelText(t.provider.secret), {
      target: { value: "S3CRET-VALUE" },
    });
    fireEvent.click(screen.getByRole("button", { name: t.provider.save }));
    await screen.findByText(t.provider.savedActive);
    expect(screen.getByText(t.provider.secretSaved)).toBeTruthy();
    expect(screen.getByText(t.tag.active)).toBeTruthy();
    expect(document.body.innerHTML).not.toContain("S3CRET-VALUE");
    // Editing again: the field starts empty, the stored secret is only a fact.
    fireEvent.click(screen.getByRole("button", { name: t.provider.edit }));
    expect(
      (screen.getByLabelText(t.provider.secret) as HTMLInputElement).value,
    ).toBe("");
  });

  it("turns a 403 on a write into the permission-denied state", async () => {
    mount({
      getSso: vi.fn(async () => ({
        ...base,
        provider: {
          name: "E",
          issuerUrl: "https://i.example",
          clientId: "c",
          hasClientSecret: true,
          status: "Active" as const,
          validationError: null,
          jitProvisioning: true,
        },
        domains: [
          {
            id: "d",
            domain: "a.example",
            status: "Verified" as const,
            txtHost: "a.example",
            txtValue: null,
            verifiedAt: "2026-01-01T00:00:00Z",
            claimed: false,
          },
        ],
      })),
      setSsoRequired: vi.fn(async () => {
        throw refusal(403);
      }),
    });
    fireEvent.click(
      await screen.findByRole("switch", { name: t.require.title }),
    );
    expect(await screen.findByText(t.forbidden)).toBeTruthy();
  });

  it("handles the 409 not-ready answer of the require switch", async () => {
    mount({
      getSso: vi.fn(async () => ({
        ...base,
        provider: {
          name: "E",
          issuerUrl: "https://i.example",
          clientId: "c",
          hasClientSecret: true,
          status: "Active" as const,
          validationError: null,
          jitProvisioning: true,
        },
        domains: [
          {
            id: "d",
            domain: "a.example",
            status: "Verified" as const,
            txtHost: "a.example",
            txtValue: null,
            verifiedAt: "2026-01-01T00:00:00Z",
            claimed: false,
          },
        ],
      })),
      setSsoRequired: vi.fn(async () => {
        throw refusal(409, "accounts.sso.not-ready");
      }),
    });
    fireEvent.click(
      await screen.findByRole("switch", { name: t.require.title }),
    );
    expect((await screen.findByRole("alert")).textContent).toBe(
      t.require.notReady,
    );
    // The switch did not move.
    expect(
      screen
        .getByRole("switch", { name: t.require.title })
        .getAttribute("aria-checked"),
    ).toBe("false");
  });

  it("adds a domain to the list, then shows it Verified after a verify", async () => {
    const pending = {
      id: "d9",
      domain: "minhan.vn",
      status: "Pending" as const,
      txtHost: "minhan.vn",
      txtValue: "notrelix-verify=zzz",
      verifiedAt: null,
      claimed: false,
    };
    const addSsoDomain = vi.fn(async () => pending);
    const verifySsoDomain = vi.fn(async () => ({
      ...pending,
      status: "Verified" as const,
      txtValue: null,
    }));
    mount({ getSso: vi.fn(async () => base), addSsoDomain, verifySsoDomain });
    fireEvent.change(await screen.findByLabelText(t.domains.label), {
      target: { value: "minhan.vn" },
    });
    fireEvent.click(screen.getByRole("button", { name: t.domains.add }));
    expect(await screen.findByText("notrelix-verify=zzz")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Verify minhan.vn" }));
    await waitFor(() =>
      expect(screen.queryByText("notrelix-verify=zzz")).toBeNull(),
    );
    expect(screen.getByText(t.domains.verified)).toBeTruthy();
  });
});
