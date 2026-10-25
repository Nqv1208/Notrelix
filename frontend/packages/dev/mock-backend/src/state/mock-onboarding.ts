/**
 * What the onboarding pages of the 2026-10 Onboarding mockup need (gap
 * `CTR-GAP-ONBOARD-FLOW`, see docs/superpowers/be-blockers-2026-10.md,
 * "BE requirement from FE: Onboarding"): the address check, the department
 * and board-template suggestions, the saved draft of a half-done setup, the
 * getting-started list of the Home page and the welcome page of an invited
 * person. Pure data and small state; the handlers compose the store with it.
 */

export type CompanySize = "s" | "m" | "l" | "xl" | "xxl";

export const COMPANY_SIZES: readonly CompanySize[] = [
  "s",
  "m",
  "l",
  "xl",
  "xxl",
];

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

export interface DepartmentSuggestion {
  readonly key: string;
  readonly name: string;
}

const BY_SIZE: Readonly<
  Record<Exclude<CompanySize, "xxl">, readonly DepartmentSuggestion[]>
> = {
  s: [
    { key: "kinh-doanh", name: "Kinh doanh" },
    { key: "van-hanh", name: "Vận hành" },
  ],
  m: [
    { key: "kinh-doanh", name: "Kinh doanh" },
    { key: "ke-toan", name: "Kế toán" },
    { key: "van-hanh", name: "Vận hành" },
  ],
  l: [
    { key: "kinh-doanh", name: "Kinh doanh" },
    { key: "ke-toan", name: "Kế toán" },
    { key: "san-xuat", name: "Sản xuất" },
    { key: "nhan-su", name: "Nhân sự" },
  ],
  xl: [
    { key: "kinh-doanh", name: "Kinh doanh" },
    { key: "ke-toan", name: "Kế toán" },
    { key: "san-xuat", name: "Sản xuất" },
    { key: "nhan-su", name: "Nhân sự" },
    { key: "cong-nghe", name: "Công nghệ" },
  ],
};

const FOOD_EXTRAS: readonly DepartmentSuggestion[] = [
  { key: "mua-hang", name: "Mua hàng" },
  { key: "kho-van", name: "Kho vận" },
  { key: "kiem-soat-chat-luong", name: "Kiểm soát chất lượng" },
];

const GENERAL_EXTRAS: readonly DepartmentSuggestion[] = [
  { key: "marketing", name: "Marketing" },
  { key: "cham-soc-khach-hang", name: "Chăm sóc khách hàng" },
];

/** The extras follow the company's line of business, read from its name. */
export function departmentSuggestions(size: CompanySize, companyName: string) {
  const food = /thực phẩm|food|nước chấm/i.test(companyName);
  return {
    industry: food ? "food" : null,
    departments: BY_SIZE[size === "xxl" ? "xl" : size],
    extras: food ? FOOD_EXTRAS : GENERAL_EXTRAS,
  };
}

export interface BoardTemplate {
  readonly id: "project" | "approval" | "hiring" | "support" | "blank";
  readonly name: string;
  readonly summary: string;
  /** The columns, stages or steps the template starts with. */
  readonly stages: readonly string[];
  readonly fields: readonly string[];
  /** Sample rows (support) or relative bar lengths in percent (hiring). */
  readonly sample: readonly (readonly string[])[];
  readonly bars: readonly number[];
}

export const BOARD_TEMPLATES: readonly BoardTemplate[] = [
  {
    id: "project",
    name: "Dự án",
    summary: "4 trạng thái · Người làm, Hạn, Ưu tiên",
    stages: ["Cần làm", "Đang làm", "Chờ duyệt", "Hoàn thành"],
    fields: ["Người làm", "Hạn", "Ưu tiên"],
    sample: [],
    bars: [],
  },
  {
    id: "approval",
    name: "Quy trình duyệt",
    summary: "Duyệt hai cấp · Số tiền, Người duyệt",
    stages: ["Gửi", "Trưởng phòng", "Kế toán", "Xong"],
    fields: ["Số tiền", "Người duyệt", "Hạn duyệt", "Chứng từ"],
    sample: [],
    bars: [],
  },
  {
    id: "hiring",
    name: "Tuyển dụng",
    summary: "Ứng viên qua bốn vòng",
    stages: ["Ứng tuyển", "Phỏng vấn", "Đề nghị", "Nhận việc"],
    fields: [],
    sample: [],
    bars: [100, 96, 40, 20],
  },
  {
    id: "support",
    name: "Chăm sóc khách hàng",
    summary: "Yêu cầu theo kênh, mức độ",
    stages: [],
    fields: ["Khách hàng", "Kênh", "Mức độ"],
    sample: [
      ["Đại lý Phú Mỹ", "Zalo", "Gấp"],
      ["Siêu thị An Bình", "Email", "Thường"],
      ["Chị Hạnh", "Điện thoại", "Thường"],
    ],
    bars: [],
  },
  {
    id: "blank",
    name: "Trống",
    summary: "Chỉ có cột Tên",
    stages: [],
    fields: ["Tên"],
    sample: [],
    bars: [],
  },
];

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

export interface JoinProfile {
  displayName: string;
  notifyEmail: "digest" | "mentions";
}

export class MockOnboarding {
  private drafts = new Map<string, OnboardingDraft>();
  private colors = new Map<string, DepartmentColor>();
  private done = new Map<string, Set<GettingStartedKey>>();
  private dismissed = new Set<string>();
  private invitedBy = new Map<string, number>();
  private profiles = new Map<string, JoinProfile>();

  reset(): void {
    this.drafts.clear();
    this.colors.clear();
    this.done.clear();
    this.dismissed.clear();
    this.invitedBy.clear();
    this.profiles.clear();
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

  profileOf(key: string): JoinProfile | undefined {
    return this.profiles.get(key);
  }
  saveProfile(key: string, profile: JoinProfile): void {
    this.profiles.set(key, profile);
  }
}
