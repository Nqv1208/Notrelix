import { defineCatalog } from "@notrelix/i18n";

/**
 * Theme and language picker copy (WX-FND-002/006). Owned by the web host
 * because the appearance settings surface is host-composed today; moves with
 * the surface if it is extracted into a feature package.
 */
export const appearanceCatalog = defineCatalog("appearance", {
  vi: {
    title: "Giao diện",
    description: "Tùy chỉnh cách Notrelix hiển thị trên thiết bị này.",
    mode: {
      legend: "Chế độ hiển thị",
      light: {
        label: "Sáng",
        description: "Nền sáng cho môi trường nhiều ánh sáng",
      },
      dark: {
        label: "Tối",
        description: "Nền tối, dịu mắt khi thiếu sáng",
      },
      system: {
        label: "Theo hệ thống",
        description: "Tự động theo cài đặt của thiết bị",
      },
      highContrast: {
        label: "Tương phản cao",
        description: "Chữ và viền rõ nét tối đa",
      },
    },
    accent: {
      legend: "Màu nhấn",
      blue: { label: "Xanh Notrelix", description: "Màu thương hiệu mặc định" },
      coral: { label: "San hô", description: "Cam ấm" },
      ember: { label: "Than hồng", description: "Đỏ hồng đậm" },
      sage: { label: "Xô thơm", description: "Xanh lục dịu" },
      amber: {
        label: "Hổ phách",
        description: "Vàng ấm, chữ tối trên nền màu",
      },
    },
    language: {
      legend: "Ngôn ngữ",
      vi: "Tiếng Việt",
      en: "English",
    },
    quick: {
      label: "Giao diện",
      light: "Giao diện sáng",
      dark: "Giao diện tối",
      system: "Giao diện theo hệ thống",
      toggle: "Chuyển giao diện sáng/tối",
    },
  },
  en: {
    title: "Appearance",
    description: "Customize how Notrelix looks on this device.",
    mode: {
      legend: "Mode",
      light: {
        label: "Light",
        description: "Light surfaces for bright environments",
      },
      dark: {
        label: "Dark",
        description: "Dark surfaces that are easy on the eyes in low light",
      },
      system: {
        label: "System",
        description: "Follows your device setting",
      },
      highContrast: {
        label: "High contrast",
        description: "Maximum legibility for text and borders",
      },
    },
    accent: {
      legend: "Accent color",
      blue: { label: "Notrelix Blue", description: "The default brand color" },
      coral: { label: "Coral", description: "Warm orange" },
      ember: { label: "Ember", description: "Deep rose red" },
      sage: { label: "Sage", description: "Muted green" },
      amber: {
        label: "Amber",
        description: "Warm yellow with dark text on fills",
      },
    },
    language: {
      legend: "Language",
      vi: "Tiếng Việt",
      en: "English",
    },
    quick: {
      label: "Theme",
      light: "Light theme",
      dark: "Dark theme",
      system: "System theme",
      toggle: "Toggle light/dark theme",
    },
  },
});
