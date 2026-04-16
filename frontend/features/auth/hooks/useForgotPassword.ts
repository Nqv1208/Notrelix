import { useMutation } from "@tanstack/react-query"
import { authService } from "@/features/auth/api/auth.service"
import type { ForgotPassword, ForgotPasswordRes } from "@/features/auth/types/auth.types"

export function useForgotPassword() {
  return useMutation<ForgotPasswordRes, Error, ForgotPassword>({
    mutationFn: (data) => authService.forgotpassword(data),
  })
}
