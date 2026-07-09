import { z } from 'zod';
import { AUTH_ERROR_KEYS } from './auth-errors';

export const registerSchema = z.object({
  name: z.string().min(1, AUTH_ERROR_KEYS.NAME_REQUIRED),
  email: z
    .string()
    .trim()
    .min(1, AUTH_ERROR_KEYS.EMAIL_REQUIRED)
    .email(AUTH_ERROR_KEYS.EMAIL_INVALID),
  password: z
    .string()
    .min(1, AUTH_ERROR_KEYS.PASSWORD_REQUIRED)
    .min(8, AUTH_ERROR_KEYS.PASSWORD_TOO_SHORT),
});

export type RegisterRequest = z.infer<typeof registerSchema>;
