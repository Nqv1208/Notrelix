import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { isThrottled, readFlowError, type AuthFlowService } from "../../core";
import { useAuthCopy } from "../copy/use-auth-copy";
import {
  AuthRegisterFormSurface,
  type AuthRegisterStep,
} from "../components/auth-register-form-surface";
import type { AuthFlowNavigation } from "./flow-navigation";

/**
 * Registration: the form, then one neutral "check your email" step. The step
 * is the same for a new address and for one that already has an account, and
 * no session is opened: the person confirms the email and signs in.
 */
export function RegisterFlow({
  service,
  navigation,
}: {
  service: AuthFlowService;
  navigation: AuthFlowNavigation;
}) {
  const copy = useAuthCopy();
  const [step, setStep] = useState<AuthRegisterStep>({ kind: "form" });
  // "Use another email" starts a fresh form: the key remounts it with empty fields.
  const [formKey, setFormKey] = useState(0);

  const register = useMutation({
    mutationFn: (value: { name: string; email: string; password: string }) =>
      service.register(value),
    onSuccess: (_result, value) =>
      setStep({ kind: "sent", email: value.email }),
    onError: (error) => {
      const failure = readFlowError(error);
      if (isThrottled(failure))
        return setStep({ kind: "form", error: "limited" });
      const { email, password, name } = failure.fieldErrors;
      if (email || password || name)
        return setStep({
          kind: "form",
          fieldErrors: { email, password, name },
        });
      setStep({ kind: "form", error: "unreachable" });
    },
  });

  return (
    <AuthRegisterFormSurface
      key={formKey}
      copy={copy}
      step={step}
      pending={register.isPending}
      onSubmit={(data) =>
        register.mutate({
          name: `${data.firstName} ${data.lastName}`,
          email: data.email,
          password: data.password,
        })
      }
      onSignIn={() => navigation.navigate({ to: "/sign-in" })}
      onOtherEmail={() => {
        setStep({ kind: "form" });
        setFormKey((key) => key + 1);
      }}
    />
  );
}
