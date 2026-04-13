import { authService } from "../api/auth.service";
import { useRouter } from "next/navigation";
import { useMutation } from "@tanstack/react-query";
import { tokenStorage } from "@/lib/auth/token-storage";
import { routes } from "@/lib/routes";


export const useLogin = () => {
  const router = useRouter();

  return useMutation({
    mutationFn: authService.login,
    onSuccess: (data) => {
      tokenStorage.setTokens(data.accessToken, data.refreshToken);
      router.push(routes.home);
      router.refresh();
    },
  });
};