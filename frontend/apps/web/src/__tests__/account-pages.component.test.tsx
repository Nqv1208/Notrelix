import { act } from "react";
import { createRoot, type Root } from "react-dom/client";
import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { AccountScopeProvider } from "@notrelix/query";
import { I18nProvider, type Locale } from "@notrelix/i18n";
import { ThemeProvider } from "@notrelix/ui-web";
import { webCatalogs } from "../i18n/catalogs";

type Handler = (url: string, body?: unknown) => unknown;

const runtime = vi.hoisted(() => ({
  get: undefined as Handler | undefined,
  patch: undefined as Handler | undefined,
  put: undefined as Handler | undefined,
  post: undefined as Handler | undefined,
  del: undefined as Handler | undefined,
}));

vi.mock("@notrelix/runtime-web", () => ({
  useAppRuntime: () => ({
    api: {
      api: {
        get: async (url: string) => runtime.get?.(url),
        patch: async (url: string, body: unknown) => runtime.patch?.(url, body),
        put: async (url: string, body: unknown) => runtime.put?.(url, body),
        post: async (url: string, body: unknown) => runtime.post?.(url, body),
        delete: async (url: string) => runtime.del?.(url),
      },
      endpoints: {
        auth: {
          profile: "/auth/me",
          securitySummary: "/auth/security/summary",
          securityActivity: "/auth/security/activity",
          stepUpRequirement: "/auth/security/step-up/requirement",
          stepUpCompleteMfa: "/auth/security/step-up/complete-mfa",
          stepUpCompletePassword: "/auth/security/step-up/complete-password",
          sessions: "/auth/sessions",
          sessionRevoke: (id: string) => `/auth/sessions/${id}/revoke`,
          sessionsRevokeOthers: "/auth/sessions/revoke-others",
          mfaEnrollmentStart: "/auth/mfa/enrollment/start",
          mfaEnrollmentVerify: "/auth/mfa/enrollment/verify",
          mfaDisable: "/auth/mfa/disable",
          mfaRecoveryCodes: "/auth/mfa/recovery-codes/regenerate",
          changePassword: "/auth/change-password",
        },
        users: {
          updateProfile: "/profile",
          preferences: "/profile/preferences",
        },
        workspaces: {
          detail: (id: string) => `/workspaces/${id}`,
          myJobTitle: (id: string) => `/workspaces/${id}/members/me/job-title`,
          myMembership: (id: string) => `/workspaces/${id}/members/me`,
        },
        notifications: {
          list: "/notifications",
          read: (id: string) => `/notifications/${id}/read`,
          readAll: "/notifications/read-all",
          preferences: (id: string) =>
            `/workspaces/${id}/notification-preferences`,
        },
      },
    },
  }),
}));

vi.mock("@tanstack/react-router", async (importOriginal) => ({
  ...(await importOriginal<typeof import("@tanstack/react-router")>()),
  useParams: () => ({ workspaceId: "w-1" }),
  useNavigate: () => vi.fn(),
}));

import { AccountAppearancePage } from "../routes/workspaces/$workspaceId/account/appearance";
import { AccountNotificationsPage } from "../routes/workspaces/$workspaceId/account/notifications";
import { AccountSecurityPage } from "../routes/workspaces/$workspaceId/account/security";
import { AccountProfilePage } from "../routes/workspaces/$workspaceId/account/profile";
import { AccountWorkspacesPage } from "../routes/workspaces/$workspaceId/account/workspaces";

(
  globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }
).IS_REACT_ACT_ENVIRONMENT = true;

let root: Root | undefined;
let host: HTMLElement | undefined;

afterEach(() => {
  act(() => root?.unmount());
  host?.remove();
  root = undefined;
  host = undefined;
  runtime.get = undefined;
  runtime.patch = undefined;
  runtime.put = undefined;
  runtime.post = undefined;
  runtime.del = undefined;
});

async function mount(page: React.ReactElement, locale: Locale) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  host = document.createElement("div");
  document.body.appendChild(host);
  root = createRoot(host);
  await act(async () => {
    root?.render(
      <QueryClientProvider client={queryClient}>
        <AccountScopeProvider accountId="acc-test">
          <I18nProvider catalogs={webCatalogs} initialLocale={locale}>
            <ThemeProvider target={null}>{page}</ThemeProvider>
          </I18nProvider>
        </AccountScopeProvider>
      </QueryClientProvider>,
    );
  });
}

const text = () => host?.textContent ?? "";

/** Polls inside `act` so React state updates from queries stay wrapped. */
async function until(assertion: () => void, attempts = 60) {
  for (let attempt = 0; ; attempt += 1) {
    try {
      assertion();
      return;
    } catch (error) {
      if (attempt >= attempts) throw error;
      await act(async () => {
        await new Promise((resolve) => setTimeout(resolve, 10));
      });
    }
  }
}

/** Types into the first enabled text field (the name field of the profile). */
async function typeInFirstField(value: string) {
  const input = host?.querySelector(
    "input[id]:not([disabled])",
  ) as HTMLInputElement | null;
  if (!input) throw new Error("No input");
  const setter = Object.getOwnPropertyDescriptor(
    HTMLInputElement.prototype,
    "value",
  )?.set;
  await act(async () => {
    setter?.call(input, value);
    input.dispatchEvent(new Event("input", { bubbles: true }));
  });
}

async function clickButton(label: string) {
  const button = Array.from(host?.querySelectorAll("button") ?? []).find(
    (element) => element.textContent === label,
  );
  if (!button) throw new Error(`No button "${label}"`);
  await act(async () => {
    button.click();
  });
}

/** Radios are native inputs; switches carry the switch role. */
const controlSelector = (role: "radio" | "switch") =>
  role === "radio" ? 'input[type="radio"]' : '[role="switch"]';

const stateOf = (element: HTMLElement | undefined) =>
  element instanceof HTMLInputElement
    ? String(element.checked)
    : element?.getAttribute("aria-checked");

/** Clicks the radio or switch with this accessible name. */
async function clickControl(role: "radio" | "switch", name: string) {
  const control = Array.from(
    host?.querySelectorAll<HTMLElement>(controlSelector(role)) ?? [],
  ).find((element) => {
    const labelledBy = element.getAttribute("aria-labelledby");
    const label = labelledBy
      ? host?.querySelector(`[id="${labelledBy}"]`)?.textContent
      : (host?.querySelector(`label[for="${element.id}"]`)?.textContent ??
        element.closest("label")?.textContent ??
        element.textContent);
    return label === name || label?.startsWith(name);
  });
  if (!control) throw new Error(`No ${role} "${name}"`);
  await act(async () => {
    control.click();
  });
  return control;
}

const checked = (role: "radio" | "switch", name: string) =>
  stateOf(
    Array.from(
      host?.querySelectorAll<HTMLElement>(controlSelector(role)) ?? [],
    ).find((element) => {
      const labelledBy = element.getAttribute("aria-labelledby");
      const label = labelledBy
        ? host?.querySelector(`[id="${labelledBy}"]`)?.textContent
        : (host?.querySelector(`label[for="${element.id}"]`)?.textContent ??
          element.closest("label")?.textContent);
      return label?.startsWith(name);
    }),
  );

/** A routed GET: the profile and every account-page read. */
function reads(table: Record<string, unknown>): Handler {
  return (url) => {
    if (!(url in table)) throw new Error(`No fixture for ${url}`);
    const value = table[url];
    if (value instanceof Error) throw value;
    return value;
  };
}

const profile = {
  id: "u-1",
  name: "Ada Lovelace",
  email: "ada@example.com",
  avatarUrl: null,
};
const prefs = {
  locale: "vi",
  timezone: "Asia/Ho_Chi_Minh",
  theme: "light",
  phone: "+84912468135",
  phoneVisibleToWorkspaceMembers: false,
  version: 2,
};
const member = {
  userId: "u-1",
  jobTitle: "Implementation lead",
  version: 4,
  joinedAt: "2024-03-04T00:00:00.000Z",
};
const workspace = { id: "w-1", name: "Minh An Group" };
const base = {
  "/auth/me": profile,
  "/profile/preferences": prefs,
  "/workspaces/w-1": workspace,
  "/workspaces/w-1/members/me": member,
};

describe("AccountProfilePage container", () => {
  it("passes the Vietnamese copy and locale formatters to the surface", async () => {
    runtime.get = reads(base);
    await mount(<AccountProfilePage />, "vi");
    await until(() => expect(text()).toContain("Múi giờ"));
    expect(text()).toContain("Thành phố Hồ Chí Minh");
    // The join date is formatted for vi, not for en.
    expect(text()).toMatch(/Tham gia từ 04\/03\/2024/);
    expect(text()).not.toContain("Member since");
  });

  it("saves the name, the preferences with their version and the job title with the membership version", async () => {
    const stored = { ...profile };
    const patch = vi.fn((url: string, body: unknown) =>
      url === "/profile"
        ? Object.assign(stored, body)
        : { ...prefs, ...(body as object), version: 3 },
    );
    const put = vi.fn(() => ({}));
    // A read returns a copy: the cache must see a new object after the write.
    runtime.get = (url) =>
      url === "/auth/me"
        ? { ...stored }
        : (base as Record<string, unknown>)[url];
    runtime.patch = patch;
    runtime.put = put;
    await mount(<AccountProfilePage />, "en");
    await until(() => expect(text()).toContain("Time zone"));

    await typeInFirstField("  Ada King  ");
    await clickButton("Save changes");

    await until(() => expect(text()).toContain("Profile updated."));
    expect(patch).toHaveBeenCalledWith("/profile", { name: "Ada King" });
    // The phone and zone did not change, so no preferences write goes out.
    expect(patch).not.toHaveBeenCalledWith(
      "/profile/preferences",
      expect.anything(),
    );
    expect(put).not.toHaveBeenCalled();
  });

  it("shows the localized error with a retry when the profile cannot load", async () => {
    runtime.get = reads({ ...base, "/auth/me": new Error("boom") });
    await mount(<AccountProfilePage />, "vi");
    await until(() => expect(text()).toContain("Không thể tải hồ sơ của bạn."));

    runtime.get = reads(base);
    await clickButton("Thử lại");
    await until(() => expect(text()).toContain("Múi giờ"));
  });
});

describe("AccountAppearancePage container", () => {
  it("passes the Vietnamese copy and the current device appearance to the surface", async () => {
    runtime.get = reads(base);
    await mount(<AccountAppearancePage />, "vi");
    await until(() => expect(text()).toContain("Thu gọn theo mặc định"));
    expect(text()).toContain("Chế độ hiển thị");
    expect(text()).toContain("Màu nhấn");
    expect(text()).not.toContain("Accent color");
    expect(checked("radio", "Sáng")).toBe("true");
    expect(checked("radio", "Xanh Notrelix")).toBe("true");
    expect(checked("radio", "Tiếng Việt")).toBe("true");
    await until(() => expect(text()).toContain("Minh An Group"));
  });

  it("applies a mode choice to the theme runtime and mirrors light/dark/system to the account with the version", async () => {
    const patch = vi.fn((_url: string, body: unknown) => ({
      ...prefs,
      ...(body as object),
    }));
    runtime.get = reads(base);
    runtime.patch = patch;
    await mount(<AccountAppearancePage />, "en");
    await until(() => expect(text()).toContain("Accent color"));
    // The mirror quotes the version it read the preferences at.
    await until(() => expect(text()).toContain("Collapsed by default"));

    await clickControl("radio", "Dark");
    expect(checked("radio", "Dark")).toBe("true");
    await until(() =>
      expect(patch).toHaveBeenCalledWith("/profile/preferences", {
        theme: "dark",
        expectedVersion: 2,
      }),
    );
    await until(() => expect(text()).toContain("Saved."));

    patch.mockClear();
    await clickControl("radio", "High contrast");
    expect(checked("radio", "High contrast")).toBe("true");
    expect(patch).not.toHaveBeenCalled();
  });

  it("applies an accent choice and switches the language through the runtimes", async () => {
    runtime.get = reads(base);
    runtime.patch = (_url, body) => ({ ...prefs, ...(body as object) });
    await mount(<AccountAppearancePage />, "en");
    await until(() => expect(text()).toContain("Accent color"));
    await clickControl("radio", "Coral");
    expect(checked("radio", "Coral")).toBe("true");
    expect(checked("radio", "Notrelix Blue")).toBe("false");
    await clickControl("radio", "Tiếng Việt");
    await until(() => expect(text()).toContain("Màu nhấn"));
  });

  it("keeps the sidebar default in this browser only and never sends it to the account", async () => {
    const patch = vi.fn();
    runtime.get = reads(base);
    runtime.patch = patch;
    await mount(<AccountAppearancePage />, "en");
    await until(() => expect(text()).toContain("Collapsed by default"));

    await clickControl("switch", "Collapsed by default");
    await until(() => expect(text()).toContain("Saved."));
    expect(patch).not.toHaveBeenCalled();
    expect(checked("switch", "Collapsed by default")).toBe("true");
  });
});

describe("AccountSecurityPage container", () => {
  const summary = {
    serverTime: "2026-10-12T03:40:00.000Z",
    hasPasswordCredential: true,
    passwordChangedAt: null,
    mfa: { isEnabled: true, recoveryCodesRemaining: 8 },
    activeSessionCount: 2,
    linkedProviders: [],
  };
  const sessions = [
    {
      id: "s-1",
      status: 0,
      createdAt: "2026-10-12T03:00:00.000Z",
      expiresAt: "2026-11-12T03:00:00.000Z",
      userAgent:
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 Chrome/126.0 Safari/537.36",
      isCurrent: true,
    },
    {
      id: "s-2",
      status: 0,
      createdAt: "2026-10-12T01:40:00.000Z",
      expiresAt: "2026-11-12T03:00:00.000Z",
      userAgent: "Mozilla/5.0 (iPhone; CPU iPhone OS 17_5) Mobile Safari/604.1",
      isCurrent: false,
    },
    {
      id: "s-3",
      status: 1,
      createdAt: "2026-10-01T01:40:00.000Z",
      expiresAt: "2026-11-01T03:00:00.000Z",
      userAgent: "Mozilla/5.0 (iPad) Safari/604.1",
      isCurrent: false,
    },
  ];
  const table = {
    "/auth/me": profile,
    "/workspaces/w-1": workspace,
    "/auth/security/summary": summary,
    "/auth/sessions": sessions,
    "/auth/security/activity": {
      items: [
        {
          id: "a-1",
          type: "SignInSucceeded",
          occurredAt: "2026-10-12T03:00:00.000Z",
          ipAddress: "10.0.0.1",
          userAgent: "Chrome/126.0 Macintosh",
        },
      ],
      nextCursor: null,
    },
  };

  it("derives devices from the user agent, shows only active sessions and the activity", async () => {
    runtime.get = reads(table);
    await mount(<AccountSecurityPage />, "vi");
    await until(() => expect(text()).toContain("Xác thực hai yếu tố"));
    expect(text()).toContain("2 phiên đang hoạt động");
    expect(text()).toContain("Chrome · macOS");
    expect(text()).toContain("Đăng nhập 2 giờ trước");
    expect(text()).toContain("Hoạt động bảo mật gần đây");
    expect(text()).toContain("Đã bật");
  });

  it("asks for a proof before creating new backup codes and shows them once", async () => {
    const post = vi.fn((url: string) =>
      url === "/auth/security/step-up/requirement"
        ? { requiredFactor: 0, challengeToken: "challenge-1" }
        : url === "/auth/security/step-up/complete-mfa"
          ? { proofToken: "proof-1" }
          : { recoveryCodes: ["AAAA-1111", "BBBB-2222"] },
    );
    runtime.get = reads(table);
    runtime.post = post;
    await mount(<AccountSecurityPage />, "en");
    await until(() => expect(text()).toContain("Backup codes"));
    await clickButton("Create new codes");
    await until(() =>
      expect(text()).toContain("Confirm to create new backup codes"),
    );
    await typeInFirstField("123456");
    await clickButton("Confirm");
    await until(() => expect(text()).toContain("AAAA-1111"));
    expect(post).toHaveBeenCalledWith("/auth/security/step-up/requirement", {
      purpose: "RegenerateRecoveryCodes",
    });
    expect(post).toHaveBeenCalledWith("/auth/mfa/recovery-codes/regenerate", {
      stepUpToken: "proof-1",
    });
  });

  it("shows the localized error with a retry when the settings cannot load", async () => {
    runtime.get = reads({
      ...table,
      "/auth/security/summary": new Error("boom"),
    });
    await mount(<AccountSecurityPage />, "vi");
    await until(() =>
      expect(text()).toContain("Không thể tải cài đặt bảo mật của bạn."),
    );
    runtime.get = reads(table);
    await clickButton("Thử lại");
    await until(() => expect(text()).toContain("Xác thực hai yếu tố"));
  });
});

describe("AccountNotificationsPage container", () => {
  const prefsOf = {
    version: 3,
    rows: [
      { type: "assigned", inApp: true, email: true },
      { type: "mention", inApp: true, email: true },
      { type: "approval", inApp: true, email: true },
      { type: "overdue", inApp: true, email: false },
      { type: "watching", inApp: true, email: false },
      { type: "invitation", inApp: true, email: true },
      { type: "digest", inApp: false, email: false },
    ],
  };
  const url = "/workspaces/w-1/notification-preferences";
  const table = { "/auth/me": profile, [url]: prefsOf };
  const cell = (name: string) =>
    host?.querySelector<HTMLElement>(`[role="checkbox"][aria-label="${name}"]`);

  it("passes the Vietnamese copy and the saved choices of the route's workspace to the surface", async () => {
    runtime.get = reads(table);
    await mount(<AccountNotificationsPage />, "vi");
    await until(() => expect(text()).toContain("workspace này"));
    expect(text()).toContain("@Ada Lovelace");
    // Mobile and quiet hours are not offered.
    expect(text()).not.toContain("Di động");
    expect(text()).not.toContain("Giờ yên lặng");
    expect(
      cell("Được giao việc: Trong ứng dụng")?.getAttribute("aria-checked"),
    ).toBe("true");
    expect(
      cell("Tóm tắt hằng ngày: Trong ứng dụng")?.getAttribute("aria-checked"),
    ).toBe("false");
  });

  it("saves only the changed cell with the version it was read at", async () => {
    const patch = vi.fn(() => ({ ...prefsOf, version: 4 }));
    runtime.get = reads(table);
    runtime.patch = patch;
    await mount(<AccountNotificationsPage />, "en");
    await until(() => expect(text()).toContain("Daily digest"));
    await act(async () => cell("Daily digest: In the app")?.click());
    await clickButton("Save preferences");
    await until(() =>
      expect(text()).toContain("Notification preferences saved."),
    );
    expect(patch).toHaveBeenCalledWith(url, {
      expectedVersion: 3,
      rows: [{ type: "digest", inApp: true }],
    });
  });

  it("announces a failed save without losing the cells and refetches after a 412", async () => {
    let reads412 = 0;
    runtime.get = (u) => {
      if (u === url) reads412 += 1;
      return (table as Record<string, unknown>)[u];
    };
    runtime.patch = () => {
      throw Object.assign(new Error("stale"), { status: 412 });
    };
    await mount(<AccountNotificationsPage />, "vi");
    await until(() => expect(text()).toContain("workspace này"));
    await act(async () => cell("Tóm tắt hằng ngày: Email")?.click());
    await clickButton("Lưu tùy chọn");
    await until(() =>
      expect(text()).toContain("Không thể lưu tùy chọn thông báo của bạn."),
    );
    await until(() => expect(reads412).toBeGreaterThan(1));
  });

  it("explains a failed load and retries", async () => {
    runtime.get = reads({ ...table, [url]: new Error("boom") });
    await mount(<AccountNotificationsPage />, "vi");
    await until(() =>
      expect(text()).toContain("Không thể tải tùy chọn thông báo của bạn."),
    );
    runtime.get = reads(table);
    await clickButton("Thử lại");
    await until(() => expect(text()).toContain("workspace này"));
  });
});

describe("AccountWorkspacesPage container", () => {
  const list = {
    workspaces: [
      {
        id: "w-1",
        name: "Minh An Group",
        role: "admin",
        memberCount: 86,
        departmentCount: 5,
        unreadCount: null,
        guestBoard: null,
        otherOwnerName: "Trịnh Quốc Hưng",
        isDefault: true,
        canLeave: true,
      },
      {
        id: "w-2",
        name: "Minh An Đà Nẵng",
        role: "member",
        memberCount: 18,
        departmentCount: null,
        unreadCount: 2,
        guestBoard: null,
        otherOwnerName: null,
        isDefault: false,
        canLeave: true,
      },
    ],
  };

  it("lists the workspaces in Vietnamese and makes another one the default", async () => {
    const put = vi.fn(() => list);
    runtime.get = reads({ "/account/workspaces": list });
    runtime.put = put;
    await mount(<AccountWorkspacesPage />, "vi");
    await until(() => expect(text()).toContain("Mặc định khi mở ứng dụng"));
    expect(text()).toContain("Quản trị workspace · 86 người · 5 phòng ban");
    expect(text()).toContain("Thành viên · 18 người · 2 thông báo chưa đọc");
    await clickButton("Đặt làm mặc định");
    await until(() =>
      expect(text()).toContain("Minh An Đà Nẵng sẽ là nơi mở mặc định."),
    );
    expect(put).toHaveBeenCalledWith("/account/default-workspace", {
      workspaceId: "w-2",
    });
  });
});
