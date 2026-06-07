import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useTranslations } from "next-intl"
import { accountService, UpdateProfileRequest } from "../api/account.service"
import { toast } from "sonner"

export function useUpdateProfile() {
  const queryClient = useQueryClient()
  const t = useTranslations("account.profile")

  return useMutation({
    mutationFn: (data: UpdateProfileRequest) => accountService.updateProfile(data),
    onSuccess: (updatedUser) => {
      queryClient.setQueryData(["auth", "profile"], updatedUser)
      toast.success(t("updateSuccess"))
    },
    onError: (error: any) => {
      toast.error(error.message || t("updateError"))
    },
  })
}
