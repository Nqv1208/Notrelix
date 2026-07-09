import { useQuery } from '@tanstack/react-query';
import { createAccountService, type AccountApiClient, type AccountEndpoints } from '../../api/account.service';
import { accountQueryKeys } from '../keys';

interface UseSecuritySettingsDeps {
  api: AccountApiClient;
  endpoints: AccountEndpoints;
}

export function createUseSecuritySettings({ api, endpoints }: UseSecuritySettingsDeps) {
  const service = createAccountService(api, endpoints);

  return function useSecuritySettings() {
    return useQuery({
      queryKey: accountQueryKeys.security,
      queryFn: () => service.getSecuritySettings(),
    });
  };
}
