/**
 * What the account pages of the 2026-10 Account mockup need beyond the
 * profile and preference contracts (gap `CTR-GAP-ACC-PAGES`, see
 * docs/superpowers/be-blockers-2026-10.md, "BE requirement from FE:
 * Account"): the profile details (job title, phone, time zone, date format,
 * photo), the security facts (single sign-on, two-factor, backup codes,
 * signed-in devices), the notification matrix with the quiet hours, and the
 * member's workspaces with the default one. Small per-user state; the
 * handlers compose the store with it.
 */

export const DATE_FORMATS = ["dd/mm/yyyy", "yyyy-mm-dd"] as const;
export type DateFormat = (typeof DATE_FORMATS)[number];

export const TIME_ZONES = [
  "UTC",
  "Asia/Ho_Chi_Minh",
  "Asia/Tokyo",
  "Europe/Paris",
] as const;

export interface ProfileDetails {
  jobTitle: string;
  phone: string;
  timezone: string;
  dateFormat: DateFormat;
  avatarUrl: string | null;
}

export type SessionKind = "desktop" | "phone" | "tablet";

export interface MockSession {
  readonly id: string;
  readonly device: string;
  readonly kind: SessionKind;
  readonly place: string;
  /** `null` for the device that asks (this device). */
  readonly lastActiveAt: string | null;
}

export interface SecurityDetails {
  twoFactorEnabled: boolean;
  backupCodesLeft: number;
  sessions: MockSession[];
}

export const NOTIFICATION_EVENTS = [
  "assigned",
  "mention",
  "approval",
  "overdue",
  "watching",
  "invitation",
  "digest",
] as const;
export type NotificationEvent = (typeof NOTIFICATION_EVENTS)[number];

export interface ChannelChoice {
  app: boolean;
  email: boolean;
  mobile: boolean;
}

export interface QuietHours {
  enabled: boolean;
  from: string;
  to: string;
  days: "weekdays" | "everyday";
}

export interface NotificationMatrix {
  rows: Record<NotificationEvent, ChannelChoice>;
  quietHours: QuietHours;
}

const choice = (app: boolean, email: boolean, mobile: boolean) => ({
  app,
  email,
  mobile,
});

const defaultMatrix = (): NotificationMatrix => ({
  rows: {
    assigned: choice(true, true, true),
    mention: choice(true, true, true),
    approval: choice(true, true, true),
    overdue: choice(true, true, false),
    watching: choice(true, false, false),
    invitation: choice(true, true, false),
    digest: choice(false, true, false),
  },
  quietHours: {
    enabled: true,
    from: "21:00",
    to: "07:00",
    days: "weekdays",
  },
});

const defaultDetails = (): ProfileDetails => ({
  jobTitle: "Trưởng nhóm triển khai",
  phone: "0912 468 135",
  timezone: "Asia/Ho_Chi_Minh",
  dateFormat: "dd/mm/yyyy",
  avatarUrl: null,
});

export const CURRENT_SESSION_ID = "session-macbook";

/** The devices signed in; times are minutes before the mock clock's now. */
const defaultSecurity = (offsetMinutes: (m: number) => string) =>
  ({
    twoFactorEnabled: true,
    backupCodesLeft: 8,
    sessions: [
      {
        id: CURRENT_SESSION_ID,
        device: "MacBook · Chrome",
        kind: "desktop",
        place: "Hà Nội",
        lastActiveAt: null,
      },
      {
        id: "session-iphone",
        device: "iPhone 15 · ứng dụng Notrelix",
        kind: "phone",
        place: "Hà Nội",
        lastActiveAt: offsetMinutes(-120),
      },
      {
        id: "session-ipad",
        device: "iPad · ứng dụng Notrelix",
        kind: "tablet",
        place: "Hà Nội",
        lastActiveAt: offsetMinutes(-3 * 24 * 60),
      },
    ],
  }) satisfies SecurityDetails;

export class MockAccountPages {
  private details = new Map<string, ProfileDetails>();
  private security = new Map<string, SecurityDetails>();
  private matrix = new Map<string, NotificationMatrix>();
  private defaultWorkspace = new Map<string, string>();
  private left = new Map<string, Set<string>>();

  reset(): void {
    this.details.clear();
    this.security.clear();
    this.matrix.clear();
    this.defaultWorkspace.clear();
    this.left.clear();
  }

  detailsOf(userId: string): ProfileDetails {
    let value = this.details.get(userId);
    if (!value) {
      value = defaultDetails();
      this.details.set(userId, value);
    }
    return value;
  }

  updateDetails(
    userId: string,
    patch: Partial<ProfileDetails>,
  ): ProfileDetails {
    const next = { ...this.detailsOf(userId), ...patch };
    this.details.set(userId, next);
    return next;
  }

  securityOf(
    userId: string,
    offsetMinutes: (m: number) => string,
  ): SecurityDetails {
    let value = this.security.get(userId);
    if (!value) {
      value = defaultSecurity(offsetMinutes);
      this.security.set(userId, value);
    }
    return value;
  }

  matrixOf(userId: string): NotificationMatrix {
    let value = this.matrix.get(userId);
    if (!value) {
      value = defaultMatrix();
      this.matrix.set(userId, value);
    }
    return value;
  }

  saveMatrix(userId: string, next: NotificationMatrix): NotificationMatrix {
    this.matrix.set(userId, next);
    return next;
  }

  defaultWorkspaceOf(userId: string): string | null {
    return this.defaultWorkspace.get(userId) ?? null;
  }

  setDefaultWorkspace(userId: string, workspaceId: string): void {
    this.defaultWorkspace.set(userId, workspaceId);
  }

  leave(userId: string, workspaceId: string): void {
    const set = this.left.get(userId) ?? new Set<string>();
    set.add(workspaceId);
    this.left.set(userId, set);
    if (this.defaultWorkspace.get(userId) === workspaceId)
      this.defaultWorkspace.delete(userId);
  }

  hasLeft(userId: string, workspaceId: string): boolean {
    return this.left.get(userId)?.has(workspaceId) ?? false;
  }
}
