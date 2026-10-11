import { act } from "react";
import { createRoot, type Root } from "react-dom/client";
import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { I18nProvider, type Locale } from "@notrelix/i18n";
import { ThemeProvider } from "@notrelix/ui-web";
import { webCatalogs } from "../i18n/catalogs";

type Handler = (url: string, body?: unknown) => unknown;

const runtime = vi.hoisted(() => ({
  get: undefined as Handler | undefined,
  patch: undefined as Handler | undefined,
}));

vi.mock("@notrelix/runtime-web", () => ({
  useAppRuntime: () => ({
    api: {
      api: {
        get: async (url: string) => runtime.get?.(url),
        patch: async (url: string, body: unknown) => runtime.patch?.(url, body),
        post: async () => undefined,
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
        <I18nProvider catalogs={webCatalogs} initialLocale={locale}>
          <ThemeProvider target={null}>{page}</ThemeProvider>
        </I18nProvider>
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

/** Clicks the radio or switch with this accessible name. */
async function clickControl(role: "radio" | "switch", name: string) {
  const control = Array.from(
    host?.querySelectorAll<HTMLElement>(`[role="${role}"]`) ?? [],
  ).find((element) => {
    const labelledBy = element.getAttribute("aria-labelledby");
    const label = labelledBy
      ? host?.querySelector(`[id="${labelledBy}"]`)?.textContent
      : (host?.querySelector(`label[for="${element.id}"]`)?.textContent ??
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
  Array.from(host?.querySelectorAll<HTMLElement>(`[role="${role}"]`) ?? [])
    .find((element) => {
      const labelledBy = element.getAttribute("aria-labelledby");
      const label = labelledBy
        ? host?.querySelector(`[id="${labelledBy}"]`)?.textContent
        : host?.querySelector(`label[for="${element.id}"]`)?.textContent;
      return label?.startsWith(name);
    })
    ?.getAttribute("aria-checked");

describe("AccountProfilePage container", () => {
  const profile = {
    id: "u-1",
    name: "Ada Lovelace",
    email: "ada@example.com",
    avatarUrl: null,
    timezone: "Asia/Ho_Chi_Minh",
    locale: "vi",
    createdAt: "2025-03-12T09:00:00.000Z",
  };

  it("passes the Vietnamese copy and locale formatters to the surface", async () => {
    runtime.get = () => profile;
    await mount(<AccountProfilePage />, "vi");
    await until(() => expect(text()).toContain("Múi giờ"));
    expect(text()).toContain("Thành phố Hồ Chí Minh");
    // The join date is formatted for vi ("12 thg 3, 2025"), not for en.
    expect(text()).toMatch(/Tham gia từ .*thg 3/);
    expect(text()).not.toContain("Member since");
  });

  it("passes the English copy and locale formatters to the surface", async () => {
    runtime.get = () => profile;
    await mount(<AccountProfilePage />, "en");
    await until(() => expect(text()).toContain("Time zone"));
    expect(text()).toMatch(/Member since Mar 12, 2025/);
  });

  it("saves the trimmed name through the update hook and reports success", async () => {
    let stored = profile;
    const patch = vi.fn((_url: string, body: unknown) => {
      stored = { ...stored, ...(body as object) };
      return stored;
    });
    runtime.get = () => stored;
    runtime.patch = patch;
    await mount(<AccountProfilePage />, "en");
    await until(() => expect(text()).toContain("Time zone"));

    await typeInFirstField("  Ada King  ");
    await clickButton("Save changes");

    await until(() => expect(text()).toContain("Profile updated."));
    expect(patch).toHaveBeenCalledWith("/profile", {
      name: "Ada King",
      timezone: "Asia/Ho_Chi_Minh",
      locale: "vi",
    });
  });

  it("shows the localized error with a retry when the profile cannot load", async () => {
    runtime.get = () => {
      throw new Error("boom");
    };
    await mount(<AccountProfilePage />, "vi");
    await until(() => expect(text()).toContain("Không thể tải hồ sơ của bạn."));
    expect(text()).toContain("Thử lại");

    // Retry re-reads the profile through the hook.
    runtime.get = () => profile;
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

  it("passes the Vietnamese copy and the current device appearance to the surface", async () => {
    runtime.get = () => preferences;
    await mount(<AccountAppearancePage />, "vi");
    await until(() => expect(text()).toContain("Thu gọn theo mặc định"));
    expect(text()).toContain("Chế độ hiển thị");
    expect(text()).toContain("Màu nhấn");
    expect(text()).not.toContain("Accent color");
    // The theme runtime's defaults are the selected radios.
    expect(checked("radio", "Sáng")).toBe("true");
    expect(checked("radio", "Xanh Notrelix")).toBe("true");
    expect(checked("radio", "Tiếng Việt")).toBe("true");
  });

  it("applies a mode choice to the theme runtime and mirrors light/dark/system to the account", async () => {
    const patch = vi.fn((_url: string, body: unknown) => body);
    runtime.get = () => preferences;
    runtime.patch = patch;
    await mount(<AccountAppearancePage />, "en");
    await until(() => expect(text()).toContain("Accent color"));

    await clickControl("radio", "Dark");
    expect(checked("radio", "Dark")).toBe("true");
    expect(patch).toHaveBeenCalledWith("/account/preferences", {
      theme: "dark",
    });

    patch.mockClear();
    await clickControl("radio", "High contrast");
    expect(checked("radio", "High contrast")).toBe("true");
    expect(patch).not.toHaveBeenCalled();
  });

  it("applies an accent choice to the theme runtime", async () => {
    runtime.get = () => preferences;
    await mount(<AccountAppearancePage />, "en");
    await until(() => expect(text()).toContain("Accent color"));
    await clickControl("radio", "Coral");
    expect(checked("radio", "Coral")).toBe("true");
    expect(checked("radio", "Notrelix Blue")).toBe("false");
  });

  it("switches the interface language through the locale runtime", async () => {
    runtime.get = () => preferences;
    await mount(<AccountAppearancePage />, "vi");
    await until(() => expect(text()).toContain("Chế độ hiển thị"));
    await clickControl("radio", "English");
    await until(() => expect(text()).toContain("Accent color"));
    expect(text()).not.toContain("Màu nhấn");
  });

  it("saves the sidebar default through the preferences hook and reports success", async () => {
    let stored = preferences;
    const patch = vi.fn((_url: string, body: unknown) => {
      stored = { ...stored, ...(body as object) };
      return stored;
    });
    runtime.get = () => stored;
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
    runtime.get = () => {
      throw new Error("boom");
    };
    await mount(<AccountAppearancePage />, "vi");
    await until(() =>
      expect(text()).toContain("Không thể tải tùy chọn thanh bên của bạn."),
    );
    expect(text()).toContain("Thử lại");
    // The device appearance stays usable without the account.
    expect(text()).toContain("Chế độ hiển thị");

    // Retry re-reads the preferences through the hook.
    runtime.get = () => preferences;
    await clickButton("Thử lại");
    await until(() => expect(text()).toContain("Thu gọn theo mặc định"));
  });
});

describe("AccountSecurityPage container", () => {
  const security = {
    userId: "u-1",
    twoFactorEnabled: true,
    lastPasswordChange: "2026-01-15T09:00:00.000Z",
    activeSessions: 3,
  };

  it("passes the Vietnamese copy and locale formatters to the surface", async () => {
    runtime.get = () => security;
    await mount(<AccountSecurityPage />, "vi");
    await until(() => expect(text()).toContain("Xác thực hai yếu tố"));
    expect(text()).toContain("Bảo mật");
    expect(text()).toMatch(/Đổi lần cuối vào .*thg 1/);
    expect(text()).toContain("3 phiên đang hoạt động");
    expect(text()).toContain("Đã bật");
  });

  it("passes the English copy and locale formatters to the surface", async () => {
    runtime.get = () => security;
    await mount(<AccountSecurityPage />, "en");
    await until(() => expect(text()).toContain("Active sessions"));
    expect(text()).toContain("Last changed Jan 15, 2026");
    expect(text()).toContain("3 sessions are currently active");
  });

  it("shows the localized error with a retry when the settings cannot load", async () => {
    runtime.get = () => {
      throw new Error("boom");
    };
    await mount(<AccountSecurityPage />, "vi");
    await until(() =>
      expect(text()).toContain("Không thể tải cài đặt bảo mật của bạn."),
    );
    expect(text()).toContain("Thử lại");

    // Retry re-reads the settings through the hook.
    runtime.get = () => security;
    await clickButton("Thử lại");
    await until(() => expect(text()).toContain("Xác thực hai yếu tố"));
  });
});

describe("AccountNotificationsPage container", () => {
  const preferences = {
    userId: "u-1",
    emailEnabled: false,
    pushEnabled: true,
    mutedTypes: [],
  };

  it("passes the Vietnamese copy and the saved channels to the surface", async () => {
    runtime.get = () => preferences;
    await mount(<AccountNotificationsPage />, "vi");
    await until(() => expect(text()).toContain("Thông báo qua email"));
    expect(text()).toContain("Lưu tùy chọn");
    expect(text()).not.toContain("Email notifications");
    expect(checked("switch", "Thông báo qua email")).toBe("false");
    expect(checked("switch", "Thông báo đẩy trên máy tính")).toBe("true");
  });

  it("saves the changed channels through the preference hook and reports success", async () => {
    let stored = preferences;
    const patch = vi.fn((_url: string, body: unknown) => {
      stored = { ...stored, ...(body as object) };
      return stored;
    });
    runtime.get = () => stored;
    runtime.patch = patch;
    await mount(<AccountNotificationsPage />, "en");
    await until(() => expect(text()).toContain("Email notifications"));

    await clickControl("switch", "Email notifications");
    await clickButton("Save preferences");

    await until(() =>
      expect(text()).toContain("Notification preferences saved."),
    );
    expect(patch).toHaveBeenCalledWith("/notifications/preferences", {
      emailEnabled: true,
      pushEnabled: true,
    });
  });

  it("announces a failed save without losing the switches", async () => {
    runtime.get = () => preferences;
    runtime.patch = () => {
      throw new Error("boom");
    };
    await mount(<AccountNotificationsPage />, "vi");
    await until(() => expect(text()).toContain("Thông báo qua email"));
    await clickControl("switch", "Thông báo qua email");
    await clickButton("Lưu tùy chọn");
    await until(() =>
      expect(text()).toContain("Không thể lưu tùy chọn thông báo của bạn."),
    );
    expect(checked("switch", "Thông báo qua email")).toBe("true");
  });

  it("explains a failed load, keeps the defaults visible and retries through the hook", async () => {
    runtime.get = () => {
      throw new Error("boom");
    };
    await mount(<AccountNotificationsPage />, "vi");
    await until(() =>
      expect(text()).toContain(
        "Không thể tải các tùy chọn thông báo đã lưu của bạn.",
      ),
    );
    expect(checked("switch", "Thông báo qua email")).toBe("true");

    runtime.get = () => preferences;
    await clickButton("Thử lại");
    await until(() =>
      expect(checked("switch", "Thông báo qua email")).toBe("false"),
    );
    expect(text()).not.toContain("Không thể tải các tùy chọn");
  });
});
