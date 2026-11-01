/**
 * Account context handlers.
 *
 * Operations:
 *   account.preferences.get    — GET   /profile/preferences (Identity.Profile.GetPreferences)
 *   account.preferences.update — PATCH /profile/preferences (Identity.Profile.UpdatePreferences)
 *   account.profile.update     — PATCH /profile
 *   [the pages beyond the contracts: account-pages.handlers.ts]
 *
 * Plan: 06-HANDLERS-PROJECTIONS.md §Account context
 */

import { defineMockOperation } from "../../operations/types";
import { ok, validationError } from "../../transport/create-response";
import { TIME_ZONES } from "../../state/mock-account-pages";
import { identityProblem } from "../identity/auth-flow.handlers";
import { accountPagesOperations } from "./account-pages.handlers";
import { accountSecurityOperations } from "./account-security.handlers";

const LOCALES = ["vi", "en"] as const;
const THEMES = ["light", "dark", "system"] as const;

/** E.164 as the producer keeps it: `+`, then 8 to 15 digits, the first not zero. */
function normalizePhone(raw: string): string | null | "invalid" {
  const stripped = raw.replace(/[\s().-]/g, "");
  if (stripped === "") return null;
  return /^\+[1-9]\d{7,14}$/.test(stripped) ? stripped : "invalid";
}

export const accountOperations = [
  ...accountPagesOperations,
  ...accountSecurityOperations,
  defineMockOperation({
    id: "account.preferences.get",
    contract: {
      kind: "openapi",
      operationId: "Identity.Profile.GetPreferences",
    },
    method: "GET",
    route: "/profile/preferences",
    async handle({ store }) {
      return ok({
        ...store.accountPages.preferencesOf(store.getCurrentUser().id),
      });
    },
  }),

  defineMockOperation<
    Record<string, never>,
    {
      expectedVersion?: number;
      locale?: string;
      timezone?: string;
      theme?: string;
      phone?: string | null;
      phoneVisibleToWorkspaceMembers?: boolean;
      colourBlindLabels?: boolean;
    }
  >({
    id: "account.preferences.update",
    contract: {
      kind: "openapi",
      operationId: "Identity.Profile.UpdatePreferences",
    },
    method: "PATCH",
    route: "/profile/preferences",
    async handle({ body, store }) {
      const userId = store.getCurrentUser().id;
      const current = store.accountPages.preferencesOf(userId);
      const errors: Record<string, string[]> = {};
      if (typeof body?.expectedVersion !== "number")
        errors.expectedVersion = ["Quote the version the values were read at."];
      if (
        body?.locale !== undefined &&
        !(LOCALES as readonly string[]).includes(body.locale)
      )
        errors.locale = ["Unsupported locale."];
      if (
        body?.timezone !== undefined &&
        !(TIME_ZONES as readonly string[]).includes(body.timezone)
      )
        errors.timezone = ["Unknown time zone."];
      if (
        body?.theme !== undefined &&
        !(THEMES as readonly string[]).includes(body.theme)
      )
        errors.theme = ["Unknown theme."];
      const phone =
        body?.phone === undefined || body.phone === null
          ? body?.phone
          : normalizePhone(body.phone);
      if (phone === "invalid") errors.phone = ["Not a phone number."];
      if (Object.keys(errors).length > 0) return validationError(errors);
      if (body?.expectedVersion !== current.version)
        return identityProblem(
          412,
          "identity.preferences.version-conflict",
          "The preferences changed since they were read.",
        );
      const next = {
        ...current,
        ...(body?.locale !== undefined ? { locale: body.locale } : {}),
        ...(body?.timezone !== undefined ? { timezone: body.timezone } : {}),
        ...(body?.theme !== undefined
          ? { theme: body.theme as (typeof THEMES)[number] }
          : {}),
        ...(phone !== undefined ? { phone: phone as string | null } : {}),
        ...(body?.phoneVisibleToWorkspaceMembers !== undefined
          ? {
              phoneVisibleToWorkspaceMembers:
                body.phoneVisibleToWorkspaceMembers,
            }
          : {}),
        ...(body?.colourBlindLabels !== undefined
          ? { colourBlindLabels: body.colourBlindLabels }
          : {}),
      };
      // A no-op update keeps the version.
      const changed = JSON.stringify(next) !== JSON.stringify(current);
      const saved = changed
        ? { ...next, version: current.version + 1 }
        : current;
      store.accountPages.savePreferences(userId, saved);
      return ok({ ...saved });
    },
  }),
  defineMockOperation({
    id: "account.profile.update",
    contract: {
      kind: "openapi",
      operationId: "Identity.Profile.UpdateProfile",
    } as any,
    method: "PATCH",
    route: "/profile",
    async handle({ body, store }) {
      const user = store.getCurrentUser();
      const data = (body ?? {}) as { name?: string; email?: string };
      const updated = store.updateUserProfile(user.id, data);
      return ok({
        id: updated?.id ?? user.id,
        email: updated?.email ?? user.email,
        name: updated?.name ?? user.name,
        avatarUrl: updated?.avatarUrl ?? user.avatarUrl,
      });
    },
  }),
];
