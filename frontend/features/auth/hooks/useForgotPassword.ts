import { useMutation } from "@tanstack/react-query"
import { authService } from "@/features/auth/api/auth.service"
import type { ForgotPasswordRequest } from "@/features/auth/types/auth.types"

export function useForgotPassword() {
  return useMutation<void, Error, ForgotPasswordRequest>({
    mutationFn: (data) => authService.forgotPassword(data),
  })
}
