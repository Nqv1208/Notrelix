import { useMemo } from "react";
import {
  AccountSecuritySurface,
  createUseSecuritySettings,
  useAccountSecurityCopy,
  type AccountSecurityData,
  type AccountSection,
} from "@notrelix/features-account";
import { useFormatters } from "@notrelix/i18n";
import { useAppRuntime } from "@notrelix/runtime-web";

export function AccountSecurityPage() {
  const { api: runtimeClient } = useAppRuntime();
  const copy = useAccountSecurityCopy();
  const formatters = useFormatters();

  const useSecuritySettings = useMemo(
    () =>
      createUseSecuritySettings({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
      }),
    [runtimeClient],
  );

  const { data: security, isError, refetch } = useSecuritySettings();

  const section = useMemo<AccountSection<AccountSecurityData>>(
    () =>
      security
        ? {
            status: "ready",
            data: {
              twoFactorEnabled: security.twoFactorEnabled,
              lastPasswordChange: security.lastPasswordChange || null,
              activeSessions: security.activeSessions ?? 1,
            },
          }
        : isError
          ? { status: "error" }
          : { status: "loading" },
    [security, isError],
  );

  return (
    <AccountSecuritySurface
      copy={copy}
      formatters={formatters}
      security={section}
      onRetry={() => void refetch()}
    />
  );
}
