import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { AccountScopeProvider } from "@notrelix/query";
import { I18nProvider, type Locale } from "@notrelix/i18n";
import { fireEvent, render, screen, waitFor } from "@notrelix/testing";
import type { NotificationsApiClient } from "../../../core/api/notifications.service";
import type { Notification } from "../../../core/types/notifications";
import { notification } from "../../../verification/notifications-ui-fixtures";
import { notificationBellCatalog } from "../notification-bell.catalog";
import { en, vi as viCopy } from "../notification-bell.copy";
import { createNotificationBell } from "../notification-bell";

const endpoints = {
  notifications: {
    list: "/notifications",
    read: (id: string) => `/notifications/${id}/read`,
    readAll: "/notifications/read-all",
    unreadCount: "/notifications/unread-count",
    archive: (id: string) => `/notifications/${id}/archive`,
  },
};

const THREE_DAYS_AGO = new Date(
  Date.now() - 3 * 24 * 60 * 60 * 1000,
).toISOString();

function mount(locale: Locale, items: Notification[], count: number) {
  const post = vi.fn(async () => undefined);
  const api = {
    get: vi.fn(async (url: string) =>
      url === endpoints.notifications.unreadCount ? { count } : items,
    ),
    post,
    put: vi.fn(),
    patch: vi.fn(),
    delete: vi.fn(),
  } as unknown as NotificationsApiClient;
  const NotificationBell = createNotificationBell({ api, endpoints });
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>
      <AccountScopeProvider accountId="acc-test">
        <I18nProvider
          catalogs={[notificationBellCatalog]}
          initialLocale={locale}
        >
          {children}
        </I18nProvider>
      </AccountScopeProvider>
    </QueryClientProvider>
  );
  render(<NotificationBell />, { wrapper });
  return { post };
}

describe("NotificationBell container", () => {
  it("resolves the Vietnamese copy and formatters for the vi locale", async () => {
    const { post } = mount(
      "vi",
      [
        notification({
          id: "n-1",
          title: "Ada nhắc đến bạn",
          createdAt: THREE_DAYS_AGO,
        }),
      ],
      101,
    );

    const bell = await screen.findByRole("button", {
      name: "Thông báo, 101 chưa đọc",
    });
    expect(bell.textContent).toBe("99+");
    fireEvent.click(bell);

    expect(await screen.findByText("Ada nhắc đến bạn")).toBeTruthy();
    // Relative time comes from the injected locale formatters.
    expect(screen.getByText(/ngày trước/)).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: viCopy.markAllRead }));
    await waitFor(() =>
      expect(post).toHaveBeenCalledWith(endpoints.notifications.readAll, {}),
    );
  });

  it("resolves the English copy for the en locale", async () => {
    mount(
      "en",
      [
        notification({
          id: "n-1",
          title: "Ada mentioned you",
          createdAt: THREE_DAYS_AGO,
        }),
      ],
      1,
    );

    fireEvent.click(
      await screen.findByRole("button", { name: "Notifications, 1 unread" }),
    );

    expect(await screen.findByText("Ada mentioned you")).toBeTruthy();
    expect(screen.getByText(/days ago/)).toBeTruthy();
    expect(screen.getByRole("button", { name: en.markAllRead })).toBeTruthy();
  });
});
