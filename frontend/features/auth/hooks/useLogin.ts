import { authService } from "../api/auth.service";
import { useRouter } from "next/navigation";
import { useMutation } from "@tanstack/react-query";
import { tokenStorage } from "@/lib/auth/token-storage";


export const useLogin = () => {

    const router = useRouter()

    return useMutation({
        mutationFn: authService.login,

        onSuccess: (data) => {
            tokenStorage.setTokens(data.accessToken, data.refreshToken)
            router.push("/dashboard")
        },

        onError: () => {
            alert("Login failed...")
        }
    })

}