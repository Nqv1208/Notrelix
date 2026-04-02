import { authService } from "../api/auth.service";
import { useRouter } from "next/navigation";
import { useMutation } from "@tanstack/react-query";
import { tokenStorage } from "@/lib/auth/token-storage";


export const useRegister = () => {
  const router = useRouter();

  return useMutation({
    mutationFn: authService.register,
    onSuccess: (data) => {
      tokenStorage.setTokens(data.accessToken, data.refreshToken);
      router.push("/");
      router.refresh();
    },
  });
};