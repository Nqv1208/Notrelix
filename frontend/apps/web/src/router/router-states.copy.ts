/**
 * Copy of the router's default screens (loading, route error, page not found),
 * vi and en. Registered as the `router-states` catalog.
 */
export const en = {
  loading: {
    title: "Loading",
    description: "Preparing the workspace…",
  },
  error: {
    title: "This page could not open",
    description: "Something went wrong while opening this page. Try again.",
    retry: "Try again",
    home: "Back to Home",
  },
  notFound: {
    title: "Page not found",
    description: "This page does not exist or you can no longer open it.",
    home: "Back to Home",
  },
};

export const vi: typeof en = {
  loading: {
    title: "Đang tải",
    description: "Đang chuẩn bị không gian làm việc…",
  },
  error: {
    title: "Không mở được trang này",
    description: "Đã có lỗi khi mở trang này. Bạn thử lại nhé.",
    retry: "Thử lại",
    home: "Về Trang chủ",
  },
  notFound: {
    title: "Không tìm thấy trang",
    description: "Trang này không tồn tại hoặc bạn không còn quyền mở.",
    home: "Về Trang chủ",
  },
};
