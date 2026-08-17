import { endpoints } from "@notrelix/contracts";
import type { MockHandler } from "../transport/mock-handler";

export const notificationHandlers: readonly MockHandler[] = [
  {
    id: "notifications.list",
    matches: (request) => request.method === "GET" && request.url === endpoints.notifications.list,
    async handle(_request, context) { const userId = context.store.getCurrentUser().id; return context.store.getSnapshot().notifications.filter((notification) => notification.userId === userId && !notification.isArchived); },
  },
  {
    id: "notifications.read",
    matches: (request) => request.method === "POST" && /^\/notifications\/[^/]+\/read$/.test(request.url),
    async handle(request, context) { const id = request.url.split("/")[2]; context.store.update((draft) => { const notification = draft.notifications.find((candidate) => candidate.id === id); if (notification) notification.isRead = true; }); },
  },
  {
    id: "notifications.read-all",
    matches: (request) => request.method === "POST" && request.url === endpoints.notifications.readAll,
    async handle(_request, context) { const userId = context.store.getCurrentUser().id; context.store.update((draft) => { for (const notification of draft.notifications) if (notification.userId === userId) notification.isRead = true; }); },
  },
];
