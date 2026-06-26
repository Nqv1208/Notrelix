// Public API for the account feature slice.
// Explicit exports only.

export { accountService } from "./api/account.service"
export type { UpdateProfileRequest } from "./api/account.service"
export { useUpdateProfile } from "./hooks/use-update-profile"
