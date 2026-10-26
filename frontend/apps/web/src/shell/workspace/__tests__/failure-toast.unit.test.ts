import { afterEach, describe, expect, it, vi } from "vitest";

const toastError = vi.hoisted(() => vi.fn());
vi.mock("sonner", () => ({ toast: { error: toastError } }));

import { toastFailure } from "../failure-toast";
import { registerEmailConfirmationRecovery } from "../email-confirmation-recovery";

const unconfirmed = {
  kind: "forbidden",
  status: 403,
  details: {
    errorCode: "auth.email_not_confirmed",
    detail: "Email must be confirmed before using this feature.",
  },
};
const plainForbidden = {
  kind: "forbidden",
  status: 403,
  details: { errorCode: "auth.forbidden", detail: "Forbidden access." },
};

afterEach(() => toastError.mockReset());

describe("toastFailure", () => {
  it("offers to confirm the email, not to ask an owner, when the email is not confirmed", () => {
    const resend = vi.fn();
    const unregister = registerEmailConfirmationRecovery({
      description: "Confirm your email first, then try again.",
      actionLabel: "Resend email",
      resend,
    });

    toastFailure("Could not invite", unconfirmed, "Ask a workspace owner.");

    const [title, options] = toastError.mock.calls[0]!;
    expect(title).toBe("Could not invite");
    expect(options.description).toBe(
      "Confirm your email first, then try again.",
    );
    expect(options.action.label).toBe("Resend email");
    options.action.onClick();
    expect(resend).toHaveBeenCalledOnce();
    unregister();
  });

  it("keeps the page's own hint for an ordinary permission denial", () => {
    const unregister = registerEmailConfirmationRecovery({
      description: "Confirm your email first, then try again.",
      actionLabel: "Resend email",
      resend: vi.fn(),
    });

    toastFailure("Could not invite", plainForbidden, "Ask a workspace owner.");

    expect(toastError).toHaveBeenCalledWith("Could not invite", {
      description: "Ask a workspace owner.",
    });
    unregister();
  });

  it("falls back to the page's hint when no recovery is registered", () => {
    toastFailure("Could not invite", unconfirmed, "Ask a workspace owner.");
    expect(toastError).toHaveBeenCalledWith("Could not invite", {
      description: "Ask a workspace owner.",
    });
  });
});
