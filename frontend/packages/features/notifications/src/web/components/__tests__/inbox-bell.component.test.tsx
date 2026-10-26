import { describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";
import { fireEvent, render, screen } from "@notrelix/testing";
import { InboxBell } from "../inbox-bell";
import { notificationBellCatalog } from "../notification-bell.catalog";

function mount(unreadCount: number, onOpen = vi.fn()) {
  render(
    <I18nProvider catalogs={[notificationBellCatalog]} initialLocale="en">
      <InboxBell unreadCount={unreadCount} onOpen={onOpen} />
    </I18nProvider>,
  );
  return onOpen;
}

describe("InboxBell", () => {
  it("names the unread count and opens the Inbox on a tap", () => {
    const onOpen = mount(3);
    fireEvent.click(
      screen.getByRole("button", { name: "Notifications, 3 unread" }),
    );
    expect(onOpen).toHaveBeenCalledTimes(1);
  });

  it("has a plain name and no badge when nothing is unread", () => {
    mount(0);
    expect(screen.getByRole("button", { name: "Notifications" })).toBeTruthy();
    expect(screen.queryByText("0")).toBeNull();
  });

  it("caps the badge at 99+", () => {
    mount(250);
    expect(screen.getByText("99+")).toBeTruthy();
  });
});
