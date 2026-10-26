/**
 * What a failure toast offers when the producer refused a request because the
 * member's email is not confirmed (`auth.email_not_confirmed`): the message
 * and the way to ask for the confirmation link again.
 *
 * Failure toasts are raised outside React, so the shell registers the current
 * recovery here (`EmailConfirmationToastBridge`); without one the toast keeps
 * the page's own hint.
 */
export interface EmailConfirmationRecovery {
  readonly description: string;
  readonly actionLabel: string;
  readonly resend: () => void;
}

let current: EmailConfirmationRecovery | null = null;

export function registerEmailConfirmationRecovery(
  recovery: EmailConfirmationRecovery,
): () => void {
  current = recovery;
  return () => {
    if (current === recovery) current = null;
  };
}

export function currentEmailConfirmationRecovery(): EmailConfirmationRecovery | null {
  return current;
}
