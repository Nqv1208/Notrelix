import { useQuery } from '@tanstack/react-query';
import { createAccountService, type AccountApiClient, type AccountEndpoints } from '../../api/account.service';
import { accountQueryKeys } from '../keys';

interface UseSecuritySettingsDeps {
  api: AccountApiClient;
  endpoints: AccountEndpoints;
  options?: {
    mockMode?: boolean;
  };

}

export function createUseSecuritySettings({ api, endpoints, options }: UseSecuritySettingsDeps) {
  const service = createAccountService(api, endpoints, options);

  return function useSecuritySettings() {
    return useQuery({
      queryKey: accountQueryKeys.security,
      queryFn: () => service.getSecuritySettings(),
    });
  };
}
