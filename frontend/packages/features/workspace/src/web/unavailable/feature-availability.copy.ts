/** Copy of the pages whose backend is not available yet and of the billing capabilities page, vi and en; catalog `workspace-feature-availability`. */
export const en = {
  badge: "Not available yet",
  integrations: {
    title: "Integrations",
    lede: "Connections to the tools the company already uses.",
    heading: "Integrations are not available yet",
    body: "The service that lists, connects and monitors integrations is not live. Nothing is lost: connections you add later will appear here.",
  },
  reports: {
    title: "Reports",
    lede: "Dashboards over the boards of the workspace.",
    heading: "Reports are not available yet",
    body: "The service that builds and shares reports is not live. Your boards and items are not affected.",
  },
  billing: {
    title: "Plans and billing",
    lede: "What the workspace's billing grants, and how much of it is used.",
    loading: "Loading what the workspace's billing grants",
    error: "What the workspace's billing grants could not be loaded.",
    retry: "Try again",
    forbiddenTitle: "Only workspace admins can see billing",
    forbiddenBody:
      "Ask a workspace admin if you need to know what the workspace's billing grants.",
    listLabel: "Capabilities granted by billing",
    used: "{{used}} of {{limit}} used",
    usedUnlimited: "{{used}} used",
    unlimited: "Unlimited",
    notGranted: "Not granted",
    remaining: "{{count}} left",
    full: "Limit reached",
    capabilities: {
      AUTOMATION_RULE: "Automation rules",
    } as Readonly<Record<string, string>>,
    unavailableTitle: "Not available yet",
    unavailableBody:
      "Buying or changing a plan, invoices and payment methods are not available yet.",
  },
};

export type FeatureAvailabilityCopy = typeof en;

export const vi: FeatureAvailabilityCopy = {
  badge: "Chưa khả dụng",
  integrations: {
    title: "Tích hợp",
    lede: "Kết nối với các công cụ công ty đang dùng.",
    heading: "Tích hợp chưa khả dụng",
    body: "Dịch vụ liệt kê, kết nối và theo dõi tích hợp chưa hoạt động. Không có gì bị mất: các kết nối bạn thêm sau này sẽ hiện ở đây.",
  },
  reports: {
    title: "Báo cáo",
    lede: "Bảng thống kê trên các bảng của workspace.",
    heading: "Báo cáo chưa khả dụng",
    body: "Dịch vụ tạo và chia sẻ báo cáo chưa hoạt động. Các bảng và hạng mục của bạn không bị ảnh hưởng.",
  },
  billing: {
    title: "Gói và thanh toán",
    lede: "Những gì thanh toán của workspace cấp, và đã dùng bao nhiêu.",
    loading: "Đang tải những gì thanh toán của workspace cấp",
    error: "Không tải được những gì thanh toán của workspace cấp.",
    retry: "Thử lại",
    forbiddenTitle: "Chỉ quản trị workspace xem được thanh toán",
    forbiddenBody:
      "Hãy hỏi quản trị workspace nếu bạn cần biết thanh toán của workspace cấp những gì.",
    listLabel: "Quyền do thanh toán cấp",
    used: "Đã dùng {{used}} trên {{limit}}",
    usedUnlimited: "Đã dùng {{used}}",
    unlimited: "Không giới hạn",
    notGranted: "Chưa được cấp",
    remaining: "Còn {{count}}",
    full: "Đã hết hạn mức",
    capabilities: {
      AUTOMATION_RULE: "Quy tắc tự động hoá",
    },
    unavailableTitle: "Chưa khả dụng",
    unavailableBody:
      "Mua hoặc đổi gói, hoá đơn và phương thức thanh toán chưa khả dụng.",
  },
};

export function featureAvailabilityCopy(
  locale: "vi" | "en",
): FeatureAvailabilityCopy {
  return locale === "en" ? en : vi;
}
