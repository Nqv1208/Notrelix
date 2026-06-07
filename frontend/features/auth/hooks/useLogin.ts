import { authService } from "../api/auth.service";
import { useRouter, useSearchParams } from "next/navigation";
import { useMutation } from "@tanstack/react-query";
import { routes } from "@/lib/routes";


export const useLogin = () => {
  const router = useRouter();
  const searchParams = useSearchParams();
  const redirect = searchParams.get("redirect");

  return useMutation({
    mutationFn: authService.login,
    onSuccess: () => {
      router.push((redirect || routes.home) as never);
      router.refresh();
    },
  });
};