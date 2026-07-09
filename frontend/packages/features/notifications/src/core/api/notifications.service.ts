import type { Notification, NotificationPreferences } from '../types/notifications';

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
    preferences?: string;
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
      id: 'mock-1',
      workspaceId: 'w-1',
      userId: 'u-1',
      type: 'mention',
      title: 'John Doe mentioned you',
      body: 'Hey @user, check out the board for phase 2 planning.',
      isRead: false,
      isArchived: false,
      createdAt: new Date().toISOString(),
    },
    {
      id: 'mock-2',
      workspaceId: 'w-1',
      userId: 'u-1',
      type: 'comment',
      title: 'New comment on Page spec',
      body: 'Let\'s split the API into separate endpoints to avoid god-files.',
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
        throw new Error('Backend contract missing for notifications.unreadCount');
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
        throw new Error('Backend contract missing for notifications.archive');
      }
      await api.post<void>(endpoints.notifications.archive(id), {});
    },

    async getPreferences(): Promise<NotificationPreferences> {
      if (!endpoints.notifications.preferences) {
        if (mockMode) {
          return {
            userId: 'current-user',
            emailEnabled: true,
            pushEnabled: true,
            mutedTypes: [],
          };
        }
        throw new Error('Backend contract missing for notifications.preferences');
      }
      return api.get<NotificationPreferences>(endpoints.notifications.preferences);
    },

    async updatePreferences(preferences: Partial<NotificationPreferences>): Promise<NotificationPreferences> {
      if (!endpoints.notifications.preferences) {
        if (mockMode) {
          return {
            userId: 'current-user',
            emailEnabled: true,
            pushEnabled: true,
            mutedTypes: [],
            ...preferences,
          };
        }
        throw new Error('Backend contract missing for notifications.preferences');
      }
      return api.patch<NotificationPreferences>(endpoints.notifications.preferences, preferences);
    },
  };
}
export type NotificationsService = ReturnType<typeof createNotificationsService>;
