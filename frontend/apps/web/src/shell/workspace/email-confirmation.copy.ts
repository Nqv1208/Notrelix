/**
 * Copy of the "confirm your email first" recovery shown when the producer
 * refuses a request because the member's email is not confirmed, vi and en.
 * Registered as the `email-confirmation` catalog.
 */
export const en = {
  description: "Confirm your email first, then try again.",
  resend: "Resend email",
  sent: "We sent a new confirmation link to {{email}}.",
  sendFailed: "The confirmation email could not be sent. Try again shortly.",
};

export const vi: typeof en = {
  description: "Hãy xác nhận email của bạn trước, rồi thử lại.",
  resend: "Gửi lại email",
  sent: "Đã gửi lại liên kết xác nhận tới {{email}}.",
  sendFailed: "Chưa gửi được email xác nhận. Hãy thử lại sau ít phút.",
};
