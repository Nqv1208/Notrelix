/**
 * Where a finished sign-in lands when no return address came with it: the home
 * launcher, asked to open the default workspace first (`?open=default`). Every
 * other visit to `/home` ("back to the launcher") leaves the flag off.
 */
export const SIGN_IN_LANDING = {
  to: "/home",
  search: { open: "default" },
} as const;

export const SIGN_IN_LANDING_PATH = "/home?open=default";
