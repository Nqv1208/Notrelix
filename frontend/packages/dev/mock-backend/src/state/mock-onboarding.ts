/**
 * What the onboarding pages of the 2026-10 Onboarding mockup need from the
 * mock: the address check, the saved draft of a half-done setup and the
 * getting-started list of the Home page. Pure data and small state; the
 * handlers compose the store with it.
 */

/** Addresses nobody may take: reserved words and the ones the mock world uses. */
const TAKEN_SLUGS = new Set([
  "minhan",
  "notrelix",
  "admin",
  "app",
  "api",
  "demo",
  "support",
]);

export const SLUG_PATTERN = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;

export function isSlugTaken(
  slug: string,
  existing: readonly string[],
): boolean {
  return TAKEN_SLUGS.has(slug) || existing.includes(slug);
}

/** A free address close to a taken one; the mockup's own pair is kept. */
export function suggestSlug(
  slug: string,
  taken: (candidate: string) => boolean,
): string {
  const candidates = [
    ...(slug === "saomai" ? ["saomai-food"] : []),
    `${slug}-vn`,
    `${slug}-co`,
    `${slug}-team`,
  ];
  const free = candidates.find((candidate) => !taken(candidate));
  if (free) return free;
  let n = 2;
  while (taken(`${slug}-${n}`)) n += 1;
  return `${slug}-${n}`;
}

/** The six identity colours a department can wear (names are the mockup's). */
export const DEPARTMENT_COLORS = [
  "blue",
  "gold",
  "coral",
  "sage",
  "crimson",
  "violet",
] as const;

export type DepartmentColor = (typeof DEPARTMENT_COLORS)[number];

export interface OnboardingDraft {
  readonly step: "workspace" | "departments" | "invite" | "board" | "done";
  readonly workspaceId: string | null;
  /** The form as the person left it; the client owns its shape. */
  readonly data: Readonly<Record<string, unknown>>;
}

export type GettingStartedKey = "first-item" | "invite-more" | "mobile";

export class MockOnboarding {
  private drafts = new Map<string, OnboardingDraft>();
  private colors = new Map<string, DepartmentColor>();
  private done = new Map<string, Set<GettingStartedKey>>();
  private dismissed = new Set<string>();
  private invitedBy = new Map<string, number>();

  reset(): void {
    this.drafts.clear();
    this.colors.clear();
    this.done.clear();
    this.dismissed.clear();
    this.invitedBy.clear();
  }

  draftOf(userId: string): OnboardingDraft | null {
    return this.drafts.get(userId) ?? null;
  }
  saveDraft(userId: string, draft: OnboardingDraft): void {
    this.drafts.set(userId, draft);
  }
  clearDraft(userId: string): void {
    this.drafts.delete(userId);
  }

  setColor(spaceId: string, color: DepartmentColor): void {
    this.colors.set(spaceId, color);
  }
  colorOf(spaceId: string): DepartmentColor | null {
    return this.colors.get(spaceId) ?? null;
  }

  markDone(workspaceId: string, key: GettingStartedKey): void {
    const set = this.done.get(workspaceId) ?? new Set<GettingStartedKey>();
    set.add(key);
    this.done.set(workspaceId, set);
  }
  isDone(workspaceId: string, key: GettingStartedKey): boolean {
    return this.done.get(workspaceId)?.has(key) ?? false;
  }
  dismiss(workspaceId: string): void {
    this.dismissed.add(workspaceId);
  }
  isDismissed(workspaceId: string): boolean {
    return this.dismissed.has(workspaceId);
  }

  /** The workspaces that began with the wizard show the getting-started list. */
  startList(workspaceId: string, invites: number): void {
    this.invitedBy.set(workspaceId, invites);
  }
  hasList(workspaceId: string): boolean {
    return this.invitedBy.has(workspaceId);
  }
  invitesOf(workspaceId: string): number {
    return this.invitedBy.get(workspaceId) ?? 0;
  }
}
