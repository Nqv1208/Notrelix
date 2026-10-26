import { useEffect, useMemo } from "react";
import { toast } from "sonner";
import {
  createAuthFlowService,
  useCurrentUser,
  type AuthFlowApiClient,
} from "@notrelix/features-auth";
import { formatCopy } from "@notrelix/features-workspace/web";
import { useLocale } from "@notrelix/i18n";
import { useAppRuntime } from "@notrelix/runtime-web";
import { en, vi } from "./email-confirmation.copy";
import { registerEmailConfirmationRecovery } from "./email-confirmation-recovery";

/**
 * Gives failure toasts the way out of "confirm your email first": the message
 * in the member's language and an action that asks for the confirmation link
 * again (`Identity.Auth.ResendEmailVerification`). Renders nothing.
 */
export function EmailConfirmationToastBridge() {
  const { locale } = useLocale();
  const { api: runtimeClient } = useAppRuntime();
  const email = useCurrentUser()?.email ?? null;
  const service = useMemo(
    () =>
      createAuthFlowService(runtimeClient.api as unknown as AuthFlowApiClient),
    [runtimeClient],
  );

  useEffect(() => {
    if (!email) return;
    const copy = locale === "vi" ? vi : en;
    return registerEmailConfirmationRecovery({
      description: copy.description,
      actionLabel: copy.resend,
      resend: () => {
        service.resendEmailVerification(email).then(
          () => toast.success(formatCopy(copy.sent, { email })),
          () => toast.error(copy.sendFailed),
        );
      },
    });
  }, [email, locale, service]);

  return null;
}
