/**
 * Account notification preferences copy, vi and en, registered as the
 * `account-notifications` catalog (`account-notifications.catalog.ts`). The
 * pure surface takes a `copy` prop; `useAccountNotificationsCopy` resolves
 * it for the active locale in the container.
 */
export const en = {
  title: "Notifications",
  description: "Choose how you want to be notified.",
  loading: "Loading your notification preferences…",
  loadError:
    "We couldn't load your saved notification preferences. These are the defaults.",
  retry: "Try again",
  channels: {
    email: {
      label: "Email notifications",
      description: "Receive notifications by email",
    },
    push: {
      label: "Desktop push notifications",
      description: "Receive push notifications on your desktop",
    },
  },
  save: "Save preferences",
  saving: "Saving…",
  saved: "Notification preferences saved.",
  saveError: "We couldn't save your notification preferences. Try again.",
};

export type AccountNotificationsCopy = typeof en;

export const vi: AccountNotificationsCopy = {
  title: "Thông báo",
  description: "Chọn cách bạn muốn nhận thông báo.",
  loading: "Đang tải tùy chọn thông báo của bạn…",
  loadError:
    "Không thể tải các tùy chọn thông báo đã lưu của bạn. Đây là giá trị mặc định.",
  retry: "Thử lại",
  channels: {
    email: {
      label: "Thông báo qua email",
      description: "Nhận thông báo qua email",
    },
    push: {
      label: "Thông báo đẩy trên máy tính",
      description: "Nhận thông báo đẩy ngay trên máy tính của bạn",
    },
  },
  save: "Lưu tùy chọn",
  saving: "Đang lưu…",
  saved: "Đã lưu tùy chọn thông báo.",
  saveError: "Không thể lưu tùy chọn thông báo của bạn. Hãy thử lại.",
};

export function accountNotificationsCopy(
  locale: "vi" | "en",
): AccountNotificationsCopy {
  return locale === "vi" ? vi : en;
}
