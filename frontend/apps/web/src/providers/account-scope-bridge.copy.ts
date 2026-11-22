/**
 * Copy of the screens shown while the signed-in person's Account is not known
 * yet (reading it is pending, failed, or the person belongs to no Account), vi
 * and en. Registered as the `account-scope` catalog.
 */
export const en = {
  loading: {
    title: "Loading",
    description: "Opening your account...",
  },
  unavailable: {
    title: "Could not open your account",
    description:
      "Notrelix could not reach the server. You are still signed in. Try again.",
    retry: "Try again",
  },
  noAccount: {
    title: "No account to open",
    description:
      "You are signed in, but you do not belong to an account yet. Try again, or sign out and use another sign-in.",
    retry: "Try again",
    signOut: "Sign out",
    signingOut: "Signing out...",
  },
};

export const vi: typeof en = {
  loading: {
    title: "Đang tải",
    description: "Đang mở tài khoản của bạn...",
  },
  unavailable: {
    title: "Chưa mở được tài khoản của bạn",
    description:
      "Notrelix chưa kết nối được máy chủ. Bạn vẫn đang đăng nhập. Bạn thử lại nhé.",
    retry: "Thử lại",
  },
  noAccount: {
    title: "Chưa có tài khoản để mở",
    description:
      "Bạn đã đăng nhập nhưng chưa thuộc tài khoản nào. Bạn thử lại, hoặc đăng xuất rồi đăng nhập bằng cách khác nhé.",
    retry: "Thử lại",
    signOut: "Đăng xuất",
    signingOut: "Đang đăng xuất...",
  },
};
