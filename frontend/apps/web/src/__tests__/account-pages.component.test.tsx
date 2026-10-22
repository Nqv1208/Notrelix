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
        auth: { profile: "/auth/me" },
        users: {
          updateProfile: "/profile",
          preferences: "/account/preferences",
          security: "/users/security",
        },
        notifications: {
          list: "/notifications",
          read: (id: string) => `/notifications/${id}/read`,
          readAll: "/notifications/read-all",
          preferences: "/notifications/preferences",
        },
      },
    },
  }),
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
  timezone: "Asia/Ho_Chi_Minh",
  locale: "vi",
  createdAt: "2025-03-12T09:00:00.000Z",
};
const details = {
  jobTitle: "Implementation lead",
  phone: "0912 468 135",
  timezone: "Asia/Ho_Chi_Minh",
  dateFormat: "dd/mm/yyyy",
  avatarUrl: null,
  joinedAt: "2024-03-04T00:00:00.000Z",
  managedBy: "Microsoft Entra",
  workspaceName: "Minh An Group",
};

describe("AccountProfilePage container", () => {
  it("passes the Vietnamese copy and locale formatters to the surface", async () => {
    runtime.get = reads({
      "/auth/me": profile,
      "/account/profile-details": details,
    });
    await mount(<AccountProfilePage />, "vi");
    await until(() => expect(text()).toContain("Múi giờ"));
    expect(text()).toContain("Thành phố Hồ Chí Minh");
    // The join date is formatted for vi, not for en.
    expect(text()).toMatch(/Tham gia từ 04\/03\/2024/);
    expect(text()).not.toContain("Member since");
  });

  it("saves the name through the profile contract and the rest through the details", async () => {
    const stored = { ...profile };
    const patch = vi.fn((url: string, body: unknown) =>
      url === "/profile"
        ? Object.assign(stored, body)
        : { ...details, ...(body as object) },
    );
    // A read returns a copy: the cache must see a new object after the write.
    runtime.get = (url) => (url === "/auth/me" ? { ...stored } : details);
    runtime.patch = patch;
    await mount(<AccountProfilePage />, "en");
    await until(() => expect(text()).toContain("Time zone"));

    await typeInFirstField("  Ada King  ");
    await clickButton("Save changes");

    await until(() => expect(text()).toContain("Profile updated."));
    expect(patch).toHaveBeenCalledWith("/profile", { name: "Ada King" });
    expect(patch).toHaveBeenCalledWith("/account/profile-details", {
      jobTitle: "Implementation lead",
      phone: "0912 468 135",
      timezone: "Asia/Ho_Chi_Minh",
      dateFormat: "dd/mm/yyyy",
    });
  });

  it("shows the localized error with a retry when the profile cannot load", async () => {
    runtime.get = reads({
      "/auth/me": new Error("boom"),
      "/account/profile-details": details,
    });
    await mount(<AccountProfilePage />, "vi");
    await until(() => expect(text()).toContain("Không thể tải hồ sơ của bạn."));

    runtime.get = reads({
      "/auth/me": profile,
      "/account/profile-details": details,
    });
    await clickButton("Thử lại");
    await until(() => expect(text()).toContain("Múi giờ"));
  });
});

describe("AccountAppearancePage container", () => {
  const preferences = {
    userId: "u-1",
    theme: "light",
    colorTheme: "default",
    sidebarCollapsed: false,
    defaultView: "list",
  };
  const table = {
    "/account/preferences": preferences,
    "/account/profile-details": details,
  };

  it("passes the Vietnamese copy and the current device appearance to the surface", async () => {
    runtime.get = reads(table);
    await mount(<AccountAppearancePage />, "vi");
    await until(() => expect(text()).toContain("Thu gọn theo mặc định"));
    expect(text()).toContain("Chế độ hiển thị");
    expect(text()).toContain("Màu nhấn");
    expect(text()).not.toContain("Accent color");
    expect(checked("radio", "Sáng")).toBe("true");
    expect(checked("radio", "Xanh Notrelix")).toBe("true");
    expect(checked("radio", "Tiếng Việt")).toBe("true");
    expect(text()).toContain("Minh An Group");
  });

  it("applies a mode choice to the theme runtime and mirrors light/dark/system to the account", async () => {
    const patch = vi.fn((_url: string, body: unknown) => body);
    runtime.get = reads(table);
    runtime.patch = patch;
    await mount(<AccountAppearancePage />, "en");
    await until(() => expect(text()).toContain("Accent color"));

    await clickControl("radio", "Dark");
    expect(checked("radio", "Dark")).toBe("true");
    expect(patch).toHaveBeenCalledWith("/account/preferences", {
      theme: "dark",
    });
    await until(() => expect(text()).toContain("Saved."));

    patch.mockClear();
    await clickControl("radio", "High contrast");
    expect(checked("radio", "High contrast")).toBe("true");
    expect(patch).not.toHaveBeenCalled();
  });

  it("applies an accent choice and switches the language through the runtimes", async () => {
    runtime.get = reads(table);
    await mount(<AccountAppearancePage />, "en");
    await until(() => expect(text()).toContain("Accent color"));
    await clickControl("radio", "Coral");
    expect(checked("radio", "Coral")).toBe("true");
    expect(checked("radio", "Notrelix Blue")).toBe("false");
    await clickControl("radio", "Tiếng Việt");
    await until(() => expect(text()).toContain("Màu nhấn"));
  });

  it("saves the sidebar default through the preferences hook and reports success", async () => {
    let stored = preferences;
    const patch = vi.fn((_url: string, body: unknown) => {
      stored = { ...stored, ...(body as object) };
      return stored;
    });
    runtime.get = (url) => (url === "/account/preferences" ? stored : details);
    runtime.patch = patch;
    await mount(<AccountAppearancePage />, "en");
    await until(() => expect(text()).toContain("Collapsed by default"));

    await clickControl("switch", "Collapsed by default");
    await until(() => expect(text()).toContain("Saved."));
    expect(patch).toHaveBeenCalledWith("/account/preferences", {
      sidebarCollapsed: true,
    });
    expect(checked("switch", "Collapsed by default")).toBe("true");
  });

  it("shows the localized error with a retry when the preferences cannot load", async () => {
    runtime.get = reads({
      "/account/preferences": new Error("boom"),
      "/account/profile-details": details,
    });
    await mount(<AccountAppearancePage />, "vi");
    await until(() =>
      expect(text()).toContain("Không thể tải tùy chọn thanh bên của bạn."),
    );
    expect(text()).toContain("Chế độ hiển thị");

    runtime.get = reads(table);
    await clickButton("Thử lại");
    await until(() => expect(text()).toContain("Thu gọn theo mặc định"));
  });
});

describe("AccountSecurityPage container", () => {
  const security = {
    serverTime: "2026-10-12T03:40:00.000Z",
    sso: { provider: "Microsoft Entra", email: "ada@example.com" },
    twoFactorEnabled: true,
    backupCodesLeft: 8,
    sessions: [
      {
        id: "s-1",
        device: "MacBook · Chrome",
        kind: "desktop",
        place: "Hà Nội",
        lastActiveAt: null,
      },
      {
        id: "s-2",
        device: "iPhone 15",
        kind: "phone",
        place: "Hà Nội",
        lastActiveAt: "2026-10-12T01:40:00.000Z",
      },
    ],
  };
  const table = {
    "/account/security-details": security,
    "/account/profile-details": details,
  };

  it("passes the Vietnamese copy and the producer's clock to the surface", async () => {
    runtime.get = reads(table);
    await mount(<AccountSecurityPage />, "vi");
    await until(() => expect(text()).toContain("Xác thực hai yếu tố"));
    expect(text()).toContain("Đăng nhập một lần qua Microsoft Entra");
    expect(text()).toContain("2 phiên đang hoạt động");
    expect(text()).toContain("2 giờ trước");
    expect(text()).toContain("Đã bật");
  });

  it("renews the backup codes and shows the toast", async () => {
    const post = vi.fn(() => security);
    runtime.get = reads(table);
    runtime.post = post;
    await mount(<AccountSecurityPage />, "en");
    await until(() => expect(text()).toContain("Backup codes"));
    await clickButton("Create new codes");
    await until(() => expect(text()).toContain("New backup codes created."));
    expect(post).toHaveBeenCalledWith("/account/security/backup-codes", {});
  });

  it("shows the localized error with a retry when the settings cannot load", async () => {
    runtime.get = reads({
      "/account/security-details": new Error("boom"),
      "/account/profile-details": details,
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
  const matrix = {
    rows: {
      assigned: { app: true, email: true, mobile: true },
      mention: { app: true, email: true, mobile: true },
      approval: { app: true, email: true, mobile: true },
      overdue: { app: true, email: true, mobile: false },
      watching: { app: true, email: false, mobile: false },
      invitation: { app: true, email: true, mobile: false },
      digest: { app: false, email: true, mobile: false },
    },
    quietHours: { enabled: true, from: "21:00", to: "07:00", days: "weekdays" },
  };
  const table = {
    "/auth/me": profile,
    "/account/notification-matrix": matrix,
    "/account/profile-details": details,
  };
  const cell = (name: string) =>
    host?.querySelector<HTMLElement>(`[role="checkbox"][aria-label="${name}"]`);

  it("passes the Vietnamese copy and the saved matrix to the surface", async () => {
    runtime.get = reads(table);
    await mount(<AccountNotificationsPage />, "vi");
    await until(() => expect(text()).toContain("Giờ yên lặng"));
    expect(text()).toContain("@Ada Lovelace");
    expect(text()).toContain("GMT+7");
    expect(
      cell("Được giao việc: Trong ứng dụng")?.getAttribute("aria-checked"),
    ).toBe("true");
    expect(
      cell("Tóm tắt hằng ngày: Trong ứng dụng")?.getAttribute("aria-checked"),
    ).toBe("false");
  });

  it("saves the changed matrix and reports success", async () => {
    const put = vi.fn((_url: string, body: unknown) => body);
    runtime.get = reads(table);
    runtime.put = put;
    await mount(<AccountNotificationsPage />, "en");
    await until(() => expect(text()).toContain("Quiet hours"));
    await act(async () => cell("Daily digest: In the app")?.click());
    await clickButton("Save preferences");
    await until(() =>
      expect(text()).toContain("Notification preferences saved."),
    );
    const sent = put.mock.calls[0]?.[1] as typeof matrix;
    expect(sent.rows.digest.app).toBe(true);
    expect(sent.quietHours.from).toBe("21:00");
  });

  it("announces a failed save without losing the cells", async () => {
    runtime.get = reads(table);
    runtime.put = () => {
      throw new Error("boom");
    };
    await mount(<AccountNotificationsPage />, "vi");
    await until(() => expect(text()).toContain("Giờ yên lặng"));
    await clickButton("Lưu tùy chọn");
    await until(() =>
      expect(text()).toContain("Không thể lưu tùy chọn thông báo của bạn."),
    );
  });

  it("explains a failed load and retries", async () => {
    runtime.get = reads({
      ...table,
      "/account/notification-matrix": new Error("boom"),
    });
    await mount(<AccountNotificationsPage />, "vi");
    await until(() =>
      expect(text()).toContain("Không thể tải tùy chọn thông báo của bạn."),
    );
    runtime.get = reads(table);
    await clickButton("Thử lại");
    await until(() => expect(text()).toContain("Giờ yên lặng"));
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
