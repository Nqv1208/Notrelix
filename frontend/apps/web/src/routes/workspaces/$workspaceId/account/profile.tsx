import { useCallback, useMemo, useState } from "react";
import {
  AccountProfileSurface,
  createUseProfile,
  createUseUpdateProfile,
  useAccountProfileCopy,
  type AccountProfileData,
  type AccountProfileValues,
  type AccountSaveStatus,
  type AccountSection,
} from "@notrelix/features-account";
import { useFormatters } from "@notrelix/i18n";
import { useAppRuntime } from "@notrelix/runtime-web";

export function AccountProfilePage() {
  const { api: runtimeClient } = useAppRuntime();
  const copy = useAccountProfileCopy();
  const formatters = useFormatters();

  const useProfile = useMemo(
    () =>
      createUseProfile({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
      }),
    [runtimeClient],
  );

  const useUpdateProfile = useMemo(
    () =>
      createUseUpdateProfile({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
      }),
    [runtimeClient],
  );

  const { profile, isError, refetch } = useProfile();
  const updateMutation = useUpdateProfile();
  const [save, setSave] = useState<AccountSaveStatus>({ state: "idle" });

  const section = useMemo<AccountSection<AccountProfileData>>(
    () =>
      profile
        ? {
            status: "ready",
            data: {
              name: profile.name,
              email: profile.email,
              timezone: profile.timezone,
              locale: profile.locale,
              createdAt: profile.createdAt ?? null,
            },
          }
        : isError
          ? { status: "error" }
          : { status: "loading" },
    [profile, isError],
  );

  const handleSave = useCallback(
    async (values: AccountProfileValues) => {
      setSave({ state: "saving" });
      try {
        await updateMutation.mutateAsync(values);
        setSave({ state: "saved" });
      } catch {
        setSave({ state: "error" });
      }
    },
    [updateMutation],
  );

  return (
    <AccountProfileSurface
      copy={copy}
      formatters={formatters}
      profile={section}
      save={save}
      onSave={handleSave}
      onRetry={() => void refetch()}
    />
  );
}
