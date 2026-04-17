import { useMutation } from "@tanstack/react-query"
import { authService } from "@/features/auth/api/auth.service"
import type { ResetPasswordRequest } from "@/features/auth/types/auth.types"

export function useResetPassword() {
  return useMutation<void, Error, ResetPasswordRequest>({
    mutationFn: (data) => authService.resetPassword(data),
  })
}
