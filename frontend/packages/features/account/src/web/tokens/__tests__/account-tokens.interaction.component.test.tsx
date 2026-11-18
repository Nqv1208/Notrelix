import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";

import { accountTokensCopy } from "../account-tokens.copy";
import {
  AccountTokensSurface,
  type AccountTokensSurfaceProps,
} from "../account-tokens-surface";

const NOW = Date.parse("2026-11-17T10:00:00Z");

function props(
  locale: "vi" | "en",
  over: Partial<AccountTokensSurfaceProps> = {},
): AccountTokensSurfaceProps {
  return {
    copy: accountTokensCopy(locale),
    locale,
    section: {
      status: "ready",
      data: {
        tokens: [
          {
            id: "t1",
            name: "CI deploy",
            status: "active",
            lastUsedAt: null,
            expiresAt: "2027-02-12T00:00:00Z",
            createdAt: "2026-11-01T00:00:00Z",
            suffix: "a41f",
          },
          {
            id: "t2",
            name: "Old job",
            status: "active",
            lastUsedAt: null,
            expiresAt: null,
            createdAt: "2026-10-01T00:00:00Z",
            suffix: null,
          },
        ],
        hasMore: false,
      },
    },
    denied: false,
    now: NOW,
    flow: null,
    busy: false,
    loadingMore: false,
    notice: null,
    actionError: false,
    onStart: vi.fn(),
    onSubmitForm: vi.fn(),
    onSubmitProof: vi.fn(),
    onProvider: vi.fn(),
    onCloseFlow: vi.fn(),
    onRevoke: vi.fn().mockResolvedValue(undefined),
    onLoadMore: vi.fn(),
    onRetry: vi.fn(),
    ...over,
  };
}

describe.each(["vi", "en"] as const)("API tokens (%s)", (locale) => {
  const copy = accountTokensCopy(locale);

  it("lists a token by its last four characters and flags one issued without an expiry", () => {
    renderPureUi(<AccountTokensSurface {...props(locale)} />);
    expect(screen.getByText(/••••••a41f/)).toBeTruthy();
    expect(screen.getAllByText(copy.noExpiry).length).toBeGreaterThan(0);
  });

  it("shows no way to create for a person who may not manage tokens", () => {
    renderPureUi(<AccountTokensSurface {...props(locale, { denied: true })} />);
    expect(screen.getByText(copy.denied.title)).toBeTruthy();
    expect(screen.queryByRole("button", { name: copy.create })).toBeNull();
  });

  it("asks before revoking and only then revokes the chosen token", async () => {
    const shown = props(locale);
    renderPureUi(<AccountTokensSurface {...shown} />);
    fireEvent.click(
      screen.getByRole("button", {
        name: copy.revokeLabel.replace("{{name}}", "CI deploy"),
      }),
    );
    expect(shown.onRevoke).not.toHaveBeenCalled();
    fireEvent.click(
      screen.getByRole("button", { name: copy.confirmRevoke.action }),
    );
    await vi.waitFor(() => expect(shown.onRevoke).toHaveBeenCalledTimes(1));
    expect(
      (shown.onRevoke as ReturnType<typeof vi.fn>).mock.calls[0]?.[0].id,
    ).toBe("t1");
  });

  it("holds the secret in the one-time step, which Escape does not leave", () => {
    const shown = props(locale, {
      flow: {
        kind: "reveal",
        secret: "ntk_v1.SECRET-VALUE-0001",
        name: "CI deploy",
        expiresAt: "2027-02-12T00:00:00Z",
      },
    });
    renderPureUi(<AccountTokensSurface {...shown} />);
    expect(screen.getByText("ntk_v1.SECRET-VALUE-0001")).toBeTruthy();
    expect(screen.getByText(copy.reveal.warnTitle)).toBeTruthy();
    fireEvent.keyDown(screen.getByRole("dialog"), { key: "Escape" });
    expect(shown.onCloseFlow).not.toHaveBeenCalled();
  });

  it("never prints the secret outside the one-time step", () => {
    renderPureUi(<AccountTokensSurface {...props(locale)} />);
    expect(document.body.textContent).not.toMatch(/ntk_v1\.[A-Za-z]/);
  });

  const dateIn = (days: number) =>
    new Intl.DateTimeFormat(locale, {
      day: "2-digit",
      month: "2-digit",
      year: "numeric",
    }).format(new Date(NOW + days * 86_400_000));

  it("offers lives of at most 365 days and says the cap", () => {
    renderPureUi(
      <AccountTokensSurface
        {...props(locale, {
          flow: { kind: "form", name: "", days: 90, error: null },
        })}
      />,
    );
    const options = screen
      .getAllByRole("option")
      .map((option) => (option as HTMLOptionElement).value);
    expect(options).toEqual(["30", "90", "365"]);
    expect(screen.getByText(new RegExp(copy.form.hint))).toBeTruthy();
  });

  it("names the limit when the producer refuses the life", () => {
    renderPureUi(
      <AccountTokensSurface
        {...props(locale, {
          flow: {
            kind: "proof",
            factor: "password",
            providers: [],
            error: "lifetime",
          },
        })}
      />,
    );
    expect(screen.getByText(copy.proof.lifetime)).toBeTruthy();
    expect(copy.proof.lifetime).toContain("365");
  });

  it("shows the date the new token ends in the one-time step", () => {
    renderPureUi(
      <AccountTokensSurface
        {...props(locale, {
          flow: {
            kind: "reveal",
            secret: "ntk_v1.SECRET-VALUE-0001",
            name: "CI deploy",
            expiresAt: new Date(NOW + 365 * 86_400_000).toISOString(),
          },
        })}
      />,
    );
    expect(
      screen.getByText(
        copy.reveal.detail
          .replace("{{name}}", "CI deploy")
          .replace("{{date}}", dateIn(365)),
      ),
    ).toBeTruthy();
  });
});
