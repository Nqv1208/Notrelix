/**
 * Minimum password length for creating or resetting a password.
 *
 * The backend is the single source of truth (`PasswordPolicy.MinimumLength` in the Identity
 * domain); this constant only lets forms reject a short password before the request. Keep the
 * two equal, and change the backend first.
 */
export const PASSWORD_MIN_LENGTH = 8;
