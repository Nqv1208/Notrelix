import { z } from "zod";
import { AUTH_ERROR_KEYS } from "./auth-errors";

export const loginSchema = z.object({
  email: z
    .string()
    .trim()
    .min(1, AUTH_ERROR_KEYS.EMAIL_REQUIRED)
    .email(AUTH_ERROR_KEYS.EMAIL_INVALID),
  password: z.string().min(1, AUTH_ERROR_KEYS.PASSWORD_REQUIRED),
});

export type LoginRequest = z.infer<typeof loginSchema>;
