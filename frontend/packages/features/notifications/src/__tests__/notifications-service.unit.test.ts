import { describe, expect, it, vi } from "vitest";
import {
  createNotificationsService,
  type NotificationsApiClient,
  type NotificationsEndpoints,
} from "../core/api/notifications.service";
import { endpoints as contractEndpoints } from "@notrelix/contracts";

const endpoints: NotificationsEndpoints = {
  notifications: {
    list: "/notifications",
    read: (id) => `/notifications/${id}/read`,
    readAll: (workspaceId) => `/workspaces/${workspaceId}/inbox/read-all`,
    preferences: (workspaceId) =>
      `/workspaces/${workspaceId}/notification-preferences`,
  },
};

function clientWithPost() {
  const post = vi.fn(async () => undefined);
  const api = {
    get: vi.fn(),
    post,
    put: vi.fn(),
    patch: vi.fn(),
    delete: vi.fn(),
  } as unknown as NotificationsApiClient;
  return { api, post };
}

describe("notifications service markAllAsRead", () => {
  it("posts to the workspace Inbox read-all route the backend serves", async () => {
    const { api, post } = clientWithPost();

    await createNotificationsService(api, endpoints).markAllAsRead("ws-1");

    expect(post).toHaveBeenCalledWith("/workspaces/ws-1/inbox/read-all", {});
  });

  it("never posts to the account-level /notifications/read-all route, which the backend does not have", async () => {
    const { api, post } = clientWithPost();

    await createNotificationsService(api, endpoints).markAllAsRead("ws-1");

    expect(post).not.toHaveBeenCalledWith("/notifications/read-all", {});
  });

  it("the shared contract names the same workspace Inbox route", () => {
    expect(contractEndpoints.notifications.readAll("ws-1")).toBe(
      "/workspaces/ws-1/inbox/read-all",
    );
  });
});
