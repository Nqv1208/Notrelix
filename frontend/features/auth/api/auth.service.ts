import { api } from "@/lib/api/api-client";
import { LoginRequest, LoginResponse, LogoutRequest, RefreshRequest, RegisterRequest, RegisterResponse, User } from "../types/auth.types";
import { endpoints } from "@/lib/api/endpoints";


export const authService = {
    login(data: LoginRequest) {
        return api.post<LoginResponse>(endpoints.auth.login, data)
    },

    register(data: RegisterRequest) {
        return api.post<RegisterResponse>(endpoints.auth.register, data)
    },

    logout(data: LogoutRequest) {
        return api.post<LogoutRequest>(endpoints.auth.logout, data)
    },

    refresh(data: RefreshRequest) {
        return api.post<RefreshRequest>(endpoints.auth.refresh, data)
    },

    profile() {
        return api.get<User>(endpoints.auth.profile)
    }
}