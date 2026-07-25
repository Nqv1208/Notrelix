import { z } from 'zod';
import { AUTH_ERROR_KEYS } from './auth-errors';

export const resetPasswordSchema = z.object({
  email: z
    .string()
    .trim()
    .min(1, AUTH_ERROR_KEYS.EMAIL_REQUIRED)
    .email(AUTH_ERROR_KEYS.EMAIL_INVALID),
  code: z.string().min(1, AUTH_ERROR_KEYS.CODE_REQUIRED),
  newPassword: z
    .string()
    .min(1, AUTH_ERROR_KEYS.NEW_PASSWORD_REQUIRED)
    .min(8, AUTH_ERROR_KEYS.PASSWORD_TOO_SHORT),
});

export type ResetPasswordRequest = z.infer<typeof resetPasswordSchema>;
