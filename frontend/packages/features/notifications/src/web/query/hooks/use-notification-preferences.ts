import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { createNotificationsService, type NotificationsApiClient, type NotificationsEndpoints } from '../../../core/api/notifications.service';
import { notificationsQueryKeys } from '../../../core/query/keys';
import type { NotificationPreferences } from '../../../core/types/notifications';

interface UseNotificationPreferencesDeps {
  api: NotificationsApiClient;
  endpoints: NotificationsEndpoints;
  options?: {
    mockMode?: boolean;
  };

}

export function createUseNotificationPreferences({ api, endpoints, options }: UseNotificationPreferencesDeps) {
  const service = createNotificationsService(api, endpoints, options);

  return function useNotificationPreferences() {
    const queryClient = useQueryClient();

    const query = useQuery({
      queryKey: notificationsQueryKeys.preferences,
      queryFn: () => service.getPreferences(),
    });

    const mutation = useMutation({
      mutationFn: (prefs: Partial<NotificationPreferences>) => service.updatePreferences(prefs),
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: notificationsQueryKeys.preferences });
      },
    });

    return {
      preferences: query.data,
      isLoading: query.isLoading,
      isError: query.isError,
      updatePreferences: mutation.mutate,
      isUpdating: mutation.isPending,
    };
  };
}
