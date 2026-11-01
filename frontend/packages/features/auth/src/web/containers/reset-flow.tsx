import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { isThrottled, readFlowError, type AuthFlowService } from "../../core";
import { useAuthCopy } from "../copy/use-auth-copy";
import {
  AuthResetSurface,
  type AuthResetError,
} from "../components/auth-reset-surface";
import type { AuthFlowNavigation } from "./flow-navigation";

/** The backend's answer when five wrong codes killed the code. */
const TOO_MANY_CODES = "identity.auth.too-many-attempts";

/** Password reset, second half: the code from the email and the new password. */
export function ResetFlow({
  service,
  navigation,
  initialEmail = "",
}: {
  service: AuthFlowService;
  navigation: AuthFlowNavigation;
  initialEmail?: string;
}) {
  const copy = useAuthCopy();
  const [error, setError] = useState<AuthResetError>(null);

  const reset = useMutation({
    mutationFn: (value: { email: string; code: string; newPassword: string }) =>
      service.resetPassword(value),
    onSuccess: () =>
      // Every device signed out: the person signs in again with the new password.
      navigation.navigate({
        to: "/sign-in",
        search: { reason: "password-reset" },
        replace: true,
      }),
    onError: (failure) => {
      const read = readFlowError(failure);
      setError(
        read.code === TOO_MANY_CODES
          ? "too-many"
          : isThrottled(read)
            ? "limited"
            : read.status === null || read.network || (read.status ?? 0) >= 500
              ? "unreachable"
              : "rejected",
      );
    },
  });

  return (
    <AuthResetSurface
      copy={copy}
      initialEmail={initialEmail}
      pending={reset.isPending}
      error={error}
      onSubmit={(value) => {
        setError(null);
        reset.mutate(value);
      }}
      onSendAgain={(email) =>
        navigation.navigate({
          to: "/forgot-password",
          search: email ? { email } : undefined,
        })
      }
      onBack={() => navigation.navigate({ to: "/sign-in" })}
    />
  );
}
