import { describe, expect, it, vi } from "vitest";
import { renderPureUi, screen, within } from "@notrelix/testing";

import { accountTokensCopy } from "../account-tokens.copy";
import {
  AccountTokensSurface,
  type AccountTokensSurfaceProps,
} from "../account-tokens-surface";

const NOW = Date.parse("2026-11-19T10:00:00Z");
const HOUR = 3_600_000;

function props(locale: "vi" | "en"): AccountTokensSurfaceProps {
  const token = (id: string, name: string, lastUsedAt: string | null) => ({
    id,
    name,
    status: "active" as const,
    lastUsedAt,
    expiresAt: "2027-02-12T00:00:00Z",
    createdAt: "2026-11-01T00:00:00Z",
    suffix: "a41f",
  });
  return {
    copy: accountTokensCopy(locale),
    locale,
    section: {
      status: "ready",
      data: {
        tokens: [
          token("t1", "CI deploy", new Date(NOW - 2 * HOUR).toISOString()),
          token("t2", "Never called", null),
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
  };
}

describe.each(["vi", "en"] as const)("API tokens last used (%s)", (locale) => {
  const copy = accountTokensCopy(locale);

  it("shows a last-used column", () => {
    renderPureUi(<AccountTokensSurface {...props(locale)} />);
    expect(
      screen.getByRole("columnheader", { name: copy.columns.lastUsed }),
    ).toBeTruthy();
  });

  it("shows how long ago a used token was last called", () => {
    renderPureUi(<AccountTokensSurface {...props(locale)} />);
    const expected = new Intl.RelativeTimeFormat(locale, {
      numeric: "auto",
    }).format(-2, "hour");
    const row = screen.getByText("CI deploy").closest('[role="row"]');
    expect(row).not.toBeNull();
    expect(
      within(row as HTMLElement).getAllByText(expected).length,
    ).toBeGreaterThan(0);
  });

  it("says never used for a token no call has used", () => {
    renderPureUi(<AccountTokensSurface {...props(locale)} />);
    const row = screen.getByText("Never called").closest('[role="row"]');
    expect(row).not.toBeNull();
    expect(
      within(row as HTMLElement).getAllByText(copy.neverUsed).length,
    ).toBeGreaterThan(0);
  });
});
