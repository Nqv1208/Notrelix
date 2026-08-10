import { z } from "zod";
import { AUTH_ERROR_KEYS } from "./auth-errors";

export const registerSchema = z
  .object({
    firstName: z.string().min(1, AUTH_ERROR_KEYS.NAME_REQUIRED),
    lastName: z.string().min(1, AUTH_ERROR_KEYS.NAME_REQUIRED),
    email: z
      .string()
      .trim()
      .min(1, AUTH_ERROR_KEYS.EMAIL_REQUIRED)
      .email(AUTH_ERROR_KEYS.EMAIL_INVALID),
    password: z
      .string()
      .min(1, AUTH_ERROR_KEYS.PASSWORD_REQUIRED)
      .min(8, AUTH_ERROR_KEYS.PASSWORD_TOO_SHORT),
    confirmPassword: z.string().min(1, "Please confirm your password"),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: "Passwords don't match",
    path: ["confirmPassword"],
  });

export type RegisterRequest = z.infer<typeof registerSchema>;
