import { endpoints } from "@notrelix/contracts";
import type { UserProfile } from "@notrelix/features-account";
import type { MockHandler } from "../transport/mock-handler";

export const accountHandlers: readonly MockHandler[] = [
  {
    id: "account.profile.update",
    matches: (request) => request.method === "PATCH" && request.url === endpoints.users.updateProfile,
    async handle(request, context) {
      const current = context.store.getCurrentUser();
      const patch = request.body as Partial<UserProfile>;
      context.store.update((draft) => {
        const user = draft.users.find(({ id }) => id === current.id);
        if (user) Object.assign(user, { name: patch.name ?? user.name, email: patch.email ?? user.email, avatarUrl: patch.avatarUrl ?? user.avatarUrl });
      });
      return { ...context.store.getCurrentUser(), timezone: patch.timezone ?? "Asia/Ho_Chi_Minh", locale: patch.locale ?? "en", createdAt: "2026-08-01T09:00:00.000Z" } satisfies UserProfile;
    },
  },
];
