/**
 * The onboarding setup as the pages hold it (the Onboarding mockup). The
 * draft is the whole form: it is what "Lưu và thoát" saves and what the setup
 * resumes from. Nothing here calls the network.
 */
export const ONBOARDING_STEPS = [
  "workspace",
  "departments",
  "invite",
  "board",
] as const;

export type OnboardingFormStep = (typeof ONBOARDING_STEPS)[number];
export type OnboardingStepId = OnboardingFormStep | "done";

export type CompanySize = "s" | "m" | "l" | "xl" | "xxl";
export const COMPANY_SIZES: readonly CompanySize[] = [
  "s",
  "m",
  "l",
  "xl",
  "xxl",
];

/** The six identity colors a department can wear (never a status meaning). */
export type DepartmentColor =
  "blue" | "gold" | "coral" | "sage" | "crimson" | "violet";
export const DEPARTMENT_COLORS: readonly DepartmentColor[] = [
  "blue",
  "gold",
  "coral",
  "sage",
  "crimson",
  "violet",
];

/** The order new departments take colors in: the mockup's first four, then the rest. */
export const DEFAULT_COLOR_CYCLE: readonly DepartmentColor[] = [
  "coral",
  "gold",
  "blue",
  "sage",
  "crimson",
  "violet",
];

export type InviteRole = "Admin" | "Member" | "Viewer";
export const INVITE_ROLES: readonly InviteRole[] = [
  "Admin",
  "Member",
  "Viewer",
];

export interface DepartmentDraft {
  readonly key: string;
  readonly name: string;
  readonly color: DepartmentColor;
}

export interface InviteRowDraft {
  readonly id: string;
  readonly email: string;
  /** Key of one of the departments; null = no department. */
  readonly departmentKey: string | null;
  readonly role: InviteRole;
}

export type BoardTemplateId =
  "project" | "approval" | "hiring" | "support" | "blank";

/** What a template starts with, in the workspace's language (the producer's words). */
export interface BoardTemplateInfo {
  readonly id: BoardTemplateId;
  readonly stages: readonly string[];
  readonly fields: readonly string[];
  /** Sample rows (support). */
  readonly sample: readonly (readonly string[])[];
  /** Relative bar lengths in percent (hiring). */
  readonly bars: readonly number[];
}

export interface OnboardingDraftData {
  readonly workspace: {
    readonly name: string;
    readonly slug: string;
    readonly size: CompanySize;
    readonly locale: "vi" | "en";
  };
  readonly departments: readonly DepartmentDraft[];
  readonly invites: readonly InviteRowDraft[];
  readonly board: {
    readonly templateId: BoardTemplateId;
    readonly name: string;
    readonly departmentKey: string | null;
  };
}

/** The address check next to the field. */
export type AddressCheck =
  | { readonly kind: "idle" }
  | { readonly kind: "checking" }
  | { readonly kind: "free" }
  | { readonly kind: "taken"; readonly suggestion: string | null }
  | { readonly kind: "invalid" };

export interface OnboardingCatalog {
  /** The line of business read from the company name; null = none known. */
  readonly industry: "food" | null;
  readonly extras: readonly { readonly key: string; readonly name: string }[];
  readonly boardTemplates: readonly BoardTemplateInfo[];
}

/** What was set up, for the last page. */
export interface OnboardingSummary {
  readonly companyName: string;
  readonly address: string;
  readonly departments: readonly {
    readonly name: string;
    readonly color: DepartmentColor;
  }[];
  readonly invitesSent: number;
  readonly board: {
    readonly name: string;
    readonly templateId: BoardTemplateId;
    readonly departmentName: string | null;
  } | null;
}

/** Tone classes of a department mark (the identity marks of the product). */
export const DEPARTMENT_TONE_CLASS: Readonly<Record<DepartmentColor, string>> =
  {
    blue: "bg-mark-blue text-mark-blue-foreground",
    gold: "bg-mark-gold text-mark-gold-foreground",
    coral: "bg-mark-coral text-mark-coral-foreground",
    sage: "bg-mark-sage text-mark-sage-foreground",
    crimson: "bg-mark-crimson text-mark-crimson-foreground",
    violet: "bg-mark-violet text-mark-violet-foreground",
  };

/** "Kinh doanh" → "KD"; one word → its first two letters. */
export function departmentInitials(name: string): string {
  const words = name.trim().split(/\s+/).filter(Boolean);
  if (words.length === 0) return "?";
  if (words.length === 1) return (words[0] ?? "").slice(0, 2).toUpperCase();
  return words
    .slice(0, 2)
    .map((word) => word.charAt(0))
    .join("")
    .toUpperCase();
}

const LEGAL_WORDS =
  /^(công|ty|cp|cổ|phần|tnhh|mtv|jsc|ltd|co\.?|company|inc\.?)$/i;

/**
 * The company as people say it: the legal form is dropped and, when more than
 * two words are left, the last two are kept ("Công ty CP Thực phẩm Sao Mai"
 * → "Sao Mai").
 */
export function shortCompanyName(name: string): string {
  const words = name.trim().split(/\s+/).filter(Boolean);
  const rest = words.filter((word) => !LEGAL_WORDS.test(word));
  const kept = rest.length > 2 ? rest.slice(-2) : rest;
  return (kept.length > 0 ? kept : words).join(" ");
}

export function companyInitials(name: string): string {
  const short = shortCompanyName(name);
  return departmentInitials(short);
}

export const EMAIL_SHAPE = /^[^\s@]{1,64}@[^\s@]{1,255}\.[^\s@]{1,63}$/;

/** The emails found in pasted text: split on new lines, commas, semicolons and spaces. */
export function emailsFromText(text: string): string[] {
  const seen = new Set<string>();
  for (const part of text.split(/[\s,;]+/)) {
    const email = part.trim().toLowerCase();
    if (email) seen.add(email);
  }
  return Array.from(seen);
}

export function emptyDraft(): OnboardingDraftData {
  return {
    workspace: { name: "", slug: "", size: "l", locale: "vi" },
    departments: [],
    invites: [],
    board: { templateId: "project", name: "", departmentKey: null },
  };
}

/** A lowercase address made of a company name: no accents, dashes between words. */
export function slugFromName(name: string): string {
  return shortCompanyName(name)
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .replace(/[đĐ]/g, "d")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "")
    .slice(0, 30);
}
