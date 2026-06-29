import { api } from "@/lib/api/api-client"
import { endpoints } from "@/lib/api/endpoints"
import type { User } from "@/features/auth"

export interface UpdateProfileRequest {
  name: string
  avatar?: string | null
}

export const accountService = {
  updateProfile(data: UpdateProfileRequest) {
    return api.patch<User>(endpoints.users.updateProfile, data)
  },
}
