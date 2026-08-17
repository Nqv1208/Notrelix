import { endpoints } from "@notrelix/contracts";
import { AppError } from "@notrelix/kernel";
import type { MockHandler } from "../transport/mock-handler";

export const authHandlers: readonly MockHandler[] = [
  {
    id: "auth.profile",
    matches: (request) => request.method === "GET" && request.url === endpoints.auth.profile,
    async handle(_request, context) {
      if (!context.store.isAuthenticated()) {
        throw new AppError({
          kind: "auth",
          status: 401,
          message: "Mock session is signed out.",
        });
      }
      return {
        ...context.store.getCurrentUser(),
        timezone: "Asia/Ho_Chi_Minh",
        locale: "en",
        createdAt: context.now().toISOString(),
      };
    },
  },
  {
    id: "auth.logout",
    matches: (request) =>
      request.method === "POST" && request.url === endpoints.auth.logout,
    async handle(_request, context) {
      context.store.signOut();
      return undefined;
    },
  },
];
