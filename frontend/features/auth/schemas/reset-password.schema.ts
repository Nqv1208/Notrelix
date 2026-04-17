import { z } from "zod";
import { AUTH_ERROR_KEYS } from "@/features/auth/i18n/auth-error-keys";

export const resetPasswordSchema = z.object({
  email: z
    .string()
    .trim()
    .min(1, AUTH_ERROR_KEYS.EMAIL_REQUIRED)
    .email(AUTH_ERROR_KEYS.EMAIL_INVALID),

  code: z
    .string()
    .trim()
    .length(6, "Code must be 6 digits")
    .regex(/^\d{6}$/, "Code must contain only digits"),

  newPassword: z
    .string()
    .trim()
    .min(8, AUTH_ERROR_KEYS.PASSWORD_MIN),

  confirmPassword: z
    .string()
    .min(1, AUTH_ERROR_KEYS.CONFIRM_PASSWORD_REQUIRED),
}).refine((data) => data.newPassword === data.confirmPassword, {
  message: AUTH_ERROR_KEYS.CONFIRM_PASSWORD_MISMATCH,
  path: ["confirmPassword"],
});

export type ResetPasswordFormData = z.infer<typeof resetPasswordSchema>;
