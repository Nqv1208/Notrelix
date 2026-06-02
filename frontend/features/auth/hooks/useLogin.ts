import { authService } from "../api/auth.service";
import { useRouter } from "next/navigation";
import { useMutation } from "@tanstack/react-query";
import { routes } from "@/lib/routes";


export const useLogin = () => {
  const router = useRouter();

  return useMutation({
    mutationFn: authService.login,
    onSuccess: () => {
      router.push(routes.home);
      router.refresh();
    },
  });
};