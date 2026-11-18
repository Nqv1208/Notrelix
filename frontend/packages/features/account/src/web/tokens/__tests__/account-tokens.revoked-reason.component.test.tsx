import { describe, expect, it, vi } from "vitest";
import { renderPureUi, screen, within } from "@notrelix/testing";

import { toApiTokenPage } from "../../../core/api/account.service";
import type { AccountApiToken } from "../../../core/types/account";
import { accountTokensCopy } from "../account-tokens.copy";
import {
  AccountTokensSurface,
  type AccountTokensSurfaceProps,
} from "../account-tokens-surface";

const NOW = Date.parse("2026-11-19T10:00:00Z");

const token = (
  id: string,
  name: string,
  over: Partial<AccountApiToken> = {},
): AccountApiToken => ({
  id,
  name,
  status: "revoked",
  lastUsedAt: null,
  expiresAt: "2027-02-12T00:00:00Z",
  createdAt: "2026-11-01T00:00:00Z",
  suffix: "a41f",
  revokedReason: null,
  ...over,
});

function props(
  locale: "vi" | "en",
  tokens: readonly AccountApiToken[],
): AccountTokensSurfaceProps {
  return {
    copy: accountTokensCopy(locale),
    locale,
    section: { status: "ready", data: { tokens, hasMore: false } },
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

const rowOf = (name: string) =>
  within(screen.getByText(name).closest('[role="row"]') as HTMLElement);

describe.each(["vi", "en"] as const)(
  "API token revoked reason (%s)",
  (locale) => {
    const copy = accountTokensCopy(locale);

    it.each([
      ["member-removed", "Removed"],
      ["member-suspended", "Suspended"],
      ["member-left", "Left"],
    ] as const)(
      "says why a token stopped when its issuer %s",
      (reason, name) => {
        renderPureUi(
          <AccountTokensSurface
            {...props(locale, [token("t1", name, { revokedReason: reason })])}
          />,
        );
        const row = rowOf(name);
        expect(row.getAllByText(copy.revokedTag).length).toBeGreaterThan(0);
        expect(
          row.getByText(new RegExp(copy.revokedReason[reason])),
        ).toBeTruthy();
        expect(row.queryByRole("button")).toBeNull();
      },
    );

    it("shows no reason for a token a person revoked or one revoked before the reason was kept", () => {
      renderPureUi(
        <AccountTokensSurface
          {...props(locale, [
            token("t1", "By hand", { revokedReason: "manual" }),
            token("t2", "Before reasons", { revokedReason: null }),
          ])}
        />,
      );
      for (const name of ["By hand", "Before reasons"]) {
        const row = rowOf(name);
        expect(row.getAllByText(copy.revokedTag).length).toBeGreaterThan(0);
        for (const text of Object.values(copy.revokedReason)) {
          expect(row.queryByText(new RegExp(text))).toBeNull();
        }
      }
    });
  },
);

describe("API token list mapping", () => {
  it("reads the producer's revoked reasons", () => {
    const page = toApiTokenPage({
      items: [
        { id: "a", status: "Revoked", revokedReason: "MemberLeft" },
        { id: "b", status: "Revoked", revokedReason: "MemberRemoved" },
        { id: "c", status: "Revoked", revokedReason: "MemberSuspended" },
        { id: "d", status: "Revoked", revokedReason: "Manual" },
        { id: "e", status: "Active", revokedReason: null },
        { id: "f", status: "Revoked" },
      ],
    });
    expect(page.items.map((item) => item.revokedReason)).toEqual([
      "member-left",
      "member-removed",
      "member-suspended",
      "manual",
      null,
      null,
    ]);
  });
});
