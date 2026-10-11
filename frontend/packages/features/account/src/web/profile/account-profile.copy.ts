/**
 * Account profile copy, vi and en, registered as the `account-profile`
 * catalog (`account-profile.catalog.ts`). The pure surface takes a `copy`
 * prop; `useAccountProfileCopy` resolves it for the active locale in the
 * container. Placeholders use the catalog `{{name}}` form and are filled by
 * `fillCopy`.
 */
export const en = {
  title: "Profile",
  description: "Update your personal information.",
  loading: "Loading your profile…",
  loadError: "We couldn't load your profile.",
  retry: "Try again",
  nameLabel: "Name",
  namePlaceholder: "Your name",
  nameRequired: "Enter your name.",
  emailLabel: "Email",
  emailHelp: "Contact support to change your email.",
  timezoneLabel: "Time zone",
  localeLabel: "Language",
  memberSince: "Member since {{date}}",
  save: "Save changes",
  saving: "Saving…",
  saved: "Profile updated.",
  saveError: "We couldn't save your changes. Try again.",
  discard: "Discard changes",
  timezones: {
    UTC: "UTC",
    "America/New_York": "Eastern Time (New York)",
    "America/Chicago": "Central Time (Chicago)",
    "America/Denver": "Mountain Time (Denver)",
    "America/Los_Angeles": "Pacific Time (Los Angeles)",
    "Europe/London": "London",
    "Europe/Paris": "Paris",
    "Asia/Tokyo": "Tokyo",
    "Asia/Ho_Chi_Minh": "Ho Chi Minh City",
  },
  locales: {
    en: "English",
    vi: "Tiếng Việt",
  },
};

export type AccountProfileCopy = typeof en;

export const vi: AccountProfileCopy = {
  title: "Hồ sơ",
  description: "Cập nhật thông tin cá nhân của bạn.",
  loading: "Đang tải hồ sơ của bạn…",
  loadError: "Không thể tải hồ sơ của bạn.",
  retry: "Thử lại",
  nameLabel: "Tên",
  namePlaceholder: "Tên của bạn",
  nameRequired: "Hãy nhập tên của bạn.",
  emailLabel: "Email",
  emailHelp: "Liên hệ bộ phận hỗ trợ để đổi địa chỉ email.",
  timezoneLabel: "Múi giờ",
  localeLabel: "Ngôn ngữ",
  memberSince: "Tham gia từ {{date}}",
  save: "Lưu thay đổi",
  saving: "Đang lưu…",
  saved: "Đã cập nhật hồ sơ.",
  saveError: "Không thể lưu thay đổi của bạn. Hãy thử lại.",
  discard: "Hủy thay đổi",
  timezones: {
    UTC: "UTC",
    "America/New_York": "Giờ miền Đông Hoa Kỳ (New York)",
    "America/Chicago": "Giờ miền Trung Hoa Kỳ (Chicago)",
    "America/Denver": "Giờ miền núi Hoa Kỳ (Denver)",
    "America/Los_Angeles": "Giờ Thái Bình Dương Hoa Kỳ (Los Angeles)",
    "Europe/London": "Luân Đôn",
    "Europe/Paris": "Pa-ri",
    "Asia/Tokyo": "Tô-ky-ô",
    "Asia/Ho_Chi_Minh": "Thành phố Hồ Chí Minh",
  },
  locales: {
    en: "English",
    vi: "Tiếng Việt",
  },
};

export function accountProfileCopy(locale: "vi" | "en"): AccountProfileCopy {
  return locale === "vi" ? vi : en;
}
