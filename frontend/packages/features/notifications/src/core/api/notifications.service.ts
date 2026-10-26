import {
  NOTIFICATION_PREFERENCE_TYPES,
  type Notification,
  type NotificationPreferences,
} from "../types/notifications";

export interface NotificationsApiClient {
  get<T>(url: string): Promise<T>;
  post<T>(url: string, body: unknown): Promise<T>;
  put<T>(url: string, body: unknown): Promise<T>;
  patch<T>(url: string, body: unknown): Promise<T>;
  delete<T>(url: string): Promise<T>;
}

export interface NotificationsEndpoints {
  notifications: {
    list: string;
    read: (notificationId: string) => string;
    readAll: string;
    // PENDING BACKEND
    unreadCount?: string;
    archive?: (notificationId: string) => string;
    preferences: (workspaceId: string) => string;
  };
}

export function createNotificationsService(
  api: NotificationsApiClient,
  endpoints: NotificationsEndpoints,
  options?: {
    mockMode?: boolean;
  },
) {
  const mockMode = options?.mockMode === true;

  const mockNotifications: Notification[] = [
    {
      id: "mock-1",
      workspaceId: "w-1",
      userId: "u-1",
      type: "mention",
      title: "John Doe mentioned you",
      body: "Hey @user, check out the board for phase 2 planning.",
      isRead: false,
      isArchived: false,
      createdAt: new Date().toISOString(),
    },
    {
      id: "mock-2",
      workspaceId: "w-1",
      userId: "u-1",
      type: "comment",
      title: "New comment on Page spec",
      body: "Let's split the API into separate endpoints to avoid god-files.",
      isRead: false,
      isArchived: false,
      createdAt: new Date(Date.now() - 3600000).toISOString(),
    },
  ];

  return {
    async getList(): Promise<Notification[]> {
      try {
        return await api.get<Notification[]>(endpoints.notifications.list);
      } catch (err) {
        if (mockMode) {
          return mockNotifications;
        }
        throw err;
      }
    },

    async getUnreadCount(): Promise<{ count: number }> {
      if (!endpoints.notifications.unreadCount) {
        if (mockMode) {
          const list = await this.getList();
          return { count: list.filter((n) => !n.isRead).length };
        }
        throw new Error(
          "Backend contract missing for notifications.unreadCount",
        );
      }
      return api.get<{ count: number }>(endpoints.notifications.unreadCount);
    },

    async markAsRead(id: string): Promise<void> {
      await api.post<void>(endpoints.notifications.read(id), {});
    },

    async markAllAsRead(): Promise<void> {
      await api.post<void>(endpoints.notifications.readAll, {});
    },

    async archive(id: string): Promise<void> {
      if (!endpoints.notifications.archive) {
        if (mockMode) {
          return;
        }
        throw new Error("Backend contract missing for notifications.archive");
      }
      await api.post<void>(endpoints.notifications.archive(id), {});
    },

    /** `Collaboration.Preferences.GetNotificationPreferences`. */
    async getPreferences(
      workspaceId: string,
    ): Promise<NotificationPreferences> {
      return toPreferences(
        await api.get<PreferencesWire>(
          endpoints.notifications.preferences(workspaceId),
        ),
      );
    },

    /**
     * `Collaboration.Preferences.UpdateNotificationPreferences`: only the
     * cells that changed are sent, with the version they were read at. A stale
     * version answers 412; the caller refetches.
     */
    async updatePreferences(
      workspaceId: string,
      input: {
        readonly expectedVersion: number;
        readonly rows: readonly {
          readonly type: string;
          readonly inApp?: boolean;
          readonly email?: boolean;
        }[];
      },
    ): Promise<NotificationPreferences> {
      return toPreferences(
        await api.patch<PreferencesWire>(
          endpoints.notifications.preferences(workspaceId),
          input,
        ),
      );
    },
  };
}

/** `NotificationPreferencesDto`: `{ version, rows: [{ type, inApp, email }] }`. */
interface PreferencesWire {
  readonly version?: number;
  readonly rows?:
    | readonly {
        readonly type?: string | null;
        readonly inApp?: boolean;
        readonly email?: boolean;
      }[]
    | null;
}

/** The one place the producer's rows are read into the page's model. */
export function toPreferences(wire: PreferencesWire): NotificationPreferences {
  const rows = Object.fromEntries(
    NOTIFICATION_PREFERENCE_TYPES.map((type) => {
      const row = wire.rows?.find((candidate) => candidate.type === type);
      return [type, { inApp: row?.inApp ?? true, email: row?.email ?? false }];
    }),
  ) as unknown as NotificationPreferences["rows"];
  return { version: wire.version ?? 0, rows };
}
