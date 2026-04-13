import { z } from "zod";
import { AUTH_ERROR_KEYS } from "@/features/auth/i18n/auth-error-keys";

export const registerSchema = z.object({
  email: z
    .string()
    .trim()
    .min(1, AUTH_ERROR_KEYS.EMAIL_REQUIRED)
    .email(AUTH_ERROR_KEYS.EMAIL_INVALID),

  password: z
    .string()
    .trim()
    .min(8, AUTH_ERROR_KEYS.PASSWORD_MIN),

  confirmPassword: z
    .string()
    .min(1, AUTH_ERROR_KEYS.CONFIRM_PASSWORD_REQUIRED),

  firstName: z
    .string()
    .trim()
    .min(1, AUTH_ERROR_KEYS.FIRST_NAME_REQUIRED)
    .max(50, AUTH_ERROR_KEYS.FIRST_NAME_MAX)
    .transform((v) => v.replace(/\s+/g, " ")),

  lastName: z
    .string()
    .trim()
    .min(1, AUTH_ERROR_KEYS.LAST_NAME_REQUIRED)
    .max(50, AUTH_ERROR_KEYS.LAST_NAME_MAX)
    .transform((v) => v.replace(/\s+/g, " "))
}).refine((data) => data.password === data.confirmPassword, {
  message: AUTH_ERROR_KEYS.CONFIRM_PASSWORD_MISMATCH,
  path: ["confirmPassword"]
});

export type RegisterRequest = z.infer<typeof registerSchema>;