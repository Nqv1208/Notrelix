/**
 * Copy of the session bootstrap screens of the auth guard (reading the signed-in
 * person fails or is pending), vi and en. Registered as the `auth-guard` catalog.
 */
export const en = {
  loading: {
    title: "Loading",
    description: "Verifying session...",
  },
  unavailable: {
    title: "Could not check your session",
    description:
      "Notrelix could not reach the server. You are still signed in. Try again.",
    retry: "Try again",
  },
};

export const vi: typeof en = {
  loading: {
    title: "Đang tải",
    description: "Đang xác minh phiên đăng nhập...",
  },
  unavailable: {
    title: "Chưa kiểm tra được phiên đăng nhập",
    description:
      "Notrelix chưa kết nối được máy chủ. Bạn vẫn đang đăng nhập. Bạn thử lại nhé.",
    retry: "Thử lại",
  },
};
