import { api } from "@/lib/api/api-client";
import { ForgotPassword, ForgotPasswordRes, LoginRequestApi, LoginResponseApi, LogoutRequest, RefreshRequest, RegisterRequestApi, RegisterResponseApi, User } from "../types/auth.types";
import { endpoints } from "@/lib/api/endpoints";


export const authService = {
  login(data: LoginRequestApi) {
    return api.post<LoginResponseApi>(endpoints.auth.login, data);
  },

  register(data: RegisterRequestApi) {
    return api.post<RegisterResponseApi>(endpoints.auth.register, data);
  },

  logout(data: LogoutRequest) {
    return api.post<void>(endpoints.auth.logout, data);
  },

  refresh(data: RefreshRequest) {
    return api.post<LoginResponseApi>(endpoints.auth.refresh, data);
  },

  forgotpassword(data: ForgotPassword) {
    return api.post<ForgotPasswordRes>(endpoints.auth.forgotpassword, data)
  },

  profile() {
    return api.get<User>(endpoints.auth.profile);
  },
};