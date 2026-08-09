import { z } from "zod";
import { AUTH_ERROR_KEYS } from "./auth-errors";

export const forgotPasswordSchema = z.object({
  email: z
    .string()
    .trim()
    .min(1, AUTH_ERROR_KEYS.EMAIL_REQUIRED)
    .email(AUTH_ERROR_KEYS.EMAIL_INVALID),
});

export type ForgotPasswordRequest = z.infer<typeof forgotPasswordSchema>;
