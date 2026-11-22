import { useEffect, useMemo, useState } from "react";
import { MailWarning } from "lucide-react";
import { toast } from "sonner";
import {
  createAuthFlowService,
  useCurrentUser,
  type AuthFlowApiClient,
} from "@notrelix/features-auth";
import { formatCopy } from "@notrelix/features-workspace/web";
import { useLocale } from "@notrelix/i18n";
import { useAppRuntime } from "@notrelix/runtime-web";
import { Button } from "@notrelix/ui-web";
import { en, vi } from "./email-confirmation.copy";

/** Seconds before another link can be asked for (the producer throttles the same address). */
const RESEND_SECONDS = 60;

/**
 * A slim bar over the workspace for as long as the signed-in person's email is not confirmed
 * (`emailConfirmed === false` on the current user): it names the address and offers to send the
 * link again. It cannot be dismissed; it goes away when the profile is read again as confirmed
 * (the confirmation page does that). Many writes are refused with `auth.email_not_confirmed`, so
 * the way out must not hide in a failure toast.
 */
export function EmailNotConfirmedBannerHost() {
  const { locale } = useLocale();
  const { api: runtimeClient } = useAppRuntime();
  const user = useCurrentUser();
  const service = useMemo(
    () =>
      createAuthFlowService(runtimeClient.api as unknown as AuthFlowApiClient),
    [runtimeClient],
  );
  const [pending, setPending] = useState(false);
  const [deadline, setDeadline] = useState<number | null>(null);
  const [left, setLeft] = useState(0);

  useEffect(() => {
    if (deadline === null) return;
    const tick = () => {
      const seconds = Math.max(0, Math.ceil((deadline - Date.now()) / 1000));
      setLeft(seconds);
      if (seconds === 0) setDeadline(null);
    };
    tick();
    const timer = setInterval(tick, 1000);
    return () => clearInterval(timer);
  }, [deadline]);

  if (!user || user.emailConfirmed !== false) return null;
  const email = user.email;
  const copy = locale === "vi" ? vi : en;

  const resend = () => {
    if (pending || left > 0) return;
    setPending(true);
    service
      .resendEmailVerification(email)
      .then(
        () => {
          toast.success(formatCopy(copy.sent, { email }));
          setDeadline(Date.now() + RESEND_SECONDS * 1000);
          setLeft(RESEND_SECONDS);
        },
        () => toast.error(copy.sendFailed),
      )
      .finally(() => setPending(false));
  };

  return (
    <div
      role="status"
      data-f="email-not-confirmed-banner"
      className="mx-4 mt-3 flex flex-wrap items-center gap-x-3 gap-y-2 rounded-lg bg-warning-subtle px-3.5 py-2.5 text-compact text-warning-subtle-foreground"
    >
      <MailWarning aria-hidden="true" className="size-4 shrink-0" />
      <span className="min-w-0 flex-1 break-words">
        <b className="font-semibold">{copy.bannerTitle}</b>{" "}
        {formatCopy(copy.bannerText, { email })}
      </span>
      <Button
        size="sm"
        variant="outline"
        disabled={pending || left > 0}
        onClick={resend}
      >
        {pending
          ? copy.sending
          : left > 0
            ? formatCopy(copy.resendIn, { seconds: String(left) })
            : copy.resend}
      </Button>
    </div>
  );
}
