/**
 * Account profile copy, vi and en, registered as the `account-profile`
 * catalog (`account-profile.catalog.ts`). The pure surface takes a `copy`
 * prop; `useAccountProfileCopy` resolves it for the active locale in the
 * container. Placeholders use the catalog `{{name}}` form and are filled by
 * `fillCopy`.
 */
export const en = {
  title: "Profile",
  description:
    "Update your personal information. Your profile is shared by every workspace you belong to.",
  loading: "Loading your profile…",
  loadError: "We couldn't load your profile.",
  retry: "Try again",
  changePhoto: "Change photo",
  changePhotoLabel: "Change profile photo",
  removePhoto: "Remove photo",
  removePhotoLabel: "Remove profile photo",
  photoUnsupported: "We couldn't use that photo. Choose a PNG, JPEG or WebP.",
  photoTooLarge: "That photo is too large. Choose one under 1.5 MB.",
  photoFailed: "We couldn't change your photo. Try again.",
  memberSince: "Member since {{date}}",
  nameLabel: "Name",
  namePlaceholder: "Your name",
  nameRequired: "Enter your name.",
  jobTitleLabel: "Job title",
  emailLabel: "Email",
  emailManaged:
    "Managed by {{workspace}} through {{provider}}. It can't be changed here.",
  emailHelp: "Contact support to change your email.",
  phoneLabel: "Phone number",
  phonePlaceholder: "+84 912 345 678",
  phoneHelp:
    "Start with the country code, for example +84 for Vietnam. Spaces and dashes are fine.",
  phoneCountryCode:
    "Start the number with its country code, for example +84 912 345 678.",
  phoneVisibleLabel: "Visible to people in my workspaces",
  phoneVisibleHint:
    "Off by default: only you see your phone number. This applies to every workspace you belong to.",
  phoneInvalid:
    "That doesn't look like a phone number. Use a + and 8 to 15 digits, or leave it empty.",
  timezoneLabel: "Time zone",
  timezoneHelp: "Due dates and quiet hours follow this time zone.",
  dateFormatLabel: "Date format",
  edited: "Edited",
  unsaved: {
    one: "You have 1 unsaved change",
    other: "You have {{count}} unsaved changes",
  },
  unsavedRegion: "Unsaved changes",
  save: "Save changes",
  saving: "Saving…",
  saved: "Profile updated.",
  saveError: "We couldn't save your changes. Try again.",
  discard: "Discard changes",
  timezones: {
    UTC: "UTC",
    "Asia/Ho_Chi_Minh": "(GMT+7) Ho Chi Minh City",
    "Asia/Tokyo": "(GMT+9) Tokyo",
    "Europe/Paris": "(GMT+1) Paris",
  },
  dateFormats: {
    "dd/mm/yyyy": "dd/mm/yyyy · 12/10/2026",
    "yyyy-mm-dd": "yyyy-mm-dd · 2026-10-12",
  },
};

export type AccountProfileCopy = typeof en;

export const vi: AccountProfileCopy = {
  title: "Hồ sơ",
  description:
    "Cập nhật thông tin cá nhân của bạn. Hồ sơ dùng chung cho mọi workspace bạn tham gia.",
  loading: "Đang tải hồ sơ của bạn…",
  loadError: "Không thể tải hồ sơ của bạn.",
  retry: "Thử lại",
  changePhoto: "Đổi ảnh",
  changePhotoLabel: "Đổi ảnh đại diện",
  removePhoto: "Gỡ ảnh",
  removePhotoLabel: "Gỡ ảnh đại diện",
  photoUnsupported:
    "Không dùng được ảnh này. Hãy chọn ảnh PNG, JPEG hoặc WebP.",
  photoTooLarge: "Ảnh quá lớn. Hãy chọn ảnh dưới 1,5 MB.",
  photoFailed: "Chưa đổi được ảnh đại diện. Hãy thử lại.",
  memberSince: "Tham gia từ {{date}}",
  nameLabel: "Tên",
  namePlaceholder: "Tên của bạn",
  nameRequired: "Hãy nhập tên của bạn.",
  jobTitleLabel: "Chức danh",
  emailLabel: "Email",
  emailManaged:
    "Do {{workspace}} quản lý qua {{provider}}. Không đổi được ở đây.",
  emailHelp: "Liên hệ bộ phận hỗ trợ để đổi địa chỉ email.",
  phoneLabel: "Số điện thoại",
  phonePlaceholder: "+84 912 345 678",
  phoneHelp:
    "Bắt đầu bằng mã quốc gia, ví dụ +84 cho Việt Nam. Có thể dùng dấu cách và dấu gạch ngang.",
  phoneCountryCode: "Hãy bắt đầu số bằng mã quốc gia, ví dụ +84 912 345 678.",
  phoneVisibleLabel: "Hiển thị cho người trong các workspace của tôi",
  phoneVisibleHint:
    "Mặc định tắt: chỉ mình bạn thấy số điện thoại. Áp dụng cho mọi workspace bạn tham gia.",
  phoneInvalid:
    "Số này chưa đúng dạng số điện thoại. Hãy dùng dấu + và từ 8 đến 15 chữ số, hoặc để trống.",
  timezoneLabel: "Múi giờ",
  timezoneHelp: "Hạn việc và giờ yên lặng tính theo múi giờ này.",
  dateFormatLabel: "Định dạng ngày",
  edited: "Đã sửa",
  unsaved: {
    one: "Bạn có 1 thay đổi chưa lưu",
    other: "Bạn có {{count}} thay đổi chưa lưu",
  },
  unsavedRegion: "Thay đổi chưa lưu",
  save: "Lưu thay đổi",
  saving: "Đang lưu…",
  saved: "Đã cập nhật hồ sơ.",
  saveError: "Không thể lưu thay đổi của bạn. Hãy thử lại.",
  discard: "Hủy thay đổi",
  timezones: {
    UTC: "UTC",
    "Asia/Ho_Chi_Minh": "(GMT+7) Thành phố Hồ Chí Minh",
    "Asia/Tokyo": "(GMT+9) Tô-ky-ô",
    "Europe/Paris": "(GMT+1) Pa-ri",
  },
  dateFormats: {
    "dd/mm/yyyy": "dd/mm/yyyy · 12/10/2026",
    "yyyy-mm-dd": "yyyy-mm-dd · 2026-10-12",
  },
};

export function accountProfileCopy(locale: "vi" | "en"): AccountProfileCopy {
  return locale === "vi" ? vi : en;
}
