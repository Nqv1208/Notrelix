import { useCallback, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  AccountProfileSurface,
  createAccountService,
  createUsePreferences,
  createUseProfile,
  createUseUpdateProfile,
  useAccountProfileCopy,
  accountQueryKeys,
  type AccountProfileData,
  type AccountProfileValues,
  type AccountSaveStatus,
  type AccountSection,
} from "@notrelix/features-account";
import { useFormatters } from "@notrelix/i18n";
import { useAccountId } from "@notrelix/query";
import { useAppRuntime } from "@notrelix/runtime-web";
import {
  useAccountPagesApi,
  useRouteWorkspaceId,
  useRouteWorkspaceName,
} from "./use-account-pages";
import { useClientPreference } from "./use-client-preference";

/** A photo is sent as a data URL; anything bigger is refused here. */
const MAX_PHOTO_BYTES = 1_500_000;
const PHOTO_TYPES = ["image/png", "image/jpeg", "image/webp"];

function readAsDataUrl(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(String(reader.result));
    reader.onerror = () => reject(reader.error);
    reader.readAsDataURL(file);
  });
}

/**
 * The profile: the name and email are the account's (`/auth/me`, `/profile`),
 * the phone and time zone its preferences, the job title the member's own row
 * in this workspace (saved with its version), the date format this browser's.
 */
export function AccountProfilePage() {
  const accountId = useAccountId();
  const workspaceId = useRouteWorkspaceId();
  const workspaceName = useRouteWorkspaceName();
  const { api: runtimeClient } = useAppRuntime();
  const copy = useAccountProfileCopy();
  const formatters = useFormatters();
  const queryClient = useQueryClient();
  const api = useAccountPagesApi();
  const deps = useMemo(
    () => ({ api: runtimeClient.api, endpoints: runtimeClient.endpoints }),
    [runtimeClient],
  );
  const service = useMemo(
    () => createAccountService(deps.api, deps.endpoints),
    [deps],
  );
  const useProfile = useMemo(() => createUseProfile(deps), [deps]);
  const useUpdateProfile = useMemo(() => createUseUpdateProfile(deps), [deps]);
  const usePreferences = useMemo(() => createUsePreferences(deps), [deps]);

  const { profile, isError, refetch } = useProfile();
  const updateName = useUpdateProfile();
  const preferences = usePreferences();
  const memberKey = accountQueryKeys.memberProfile(accountId, workspaceId);
  const member = useQuery({
    queryKey: memberKey,
    queryFn: () => service.getMyMemberProfile(workspaceId),
    enabled: workspaceId !== "",
    retry: false,
  });
  const [dateFormat, setDateFormat] = useClientPreference<string>(
    "notrelix-date-format",
    "dd/mm/yyyy",
  );
  const updateJobTitle = useMutation({
    mutationFn: (jobTitle: string) =>
      service.updateMyJobTitle(
        workspaceId,
        jobTitle,
        member.data?.version ?? 0,
      ),
    onSettled: () => queryClient.invalidateQueries({ queryKey: memberKey }),
  });
  const [save, setSave] = useState<AccountSaveStatus>({ state: "idle" });
  const [photoError, setPhotoError] = useState(false);

  const section = useMemo<AccountSection<AccountProfileData>>(
    () =>
      profile && preferences.preferences && member.data
        ? {
            status: "ready",
            data: {
              name: profile.name,
              email: profile.email,
              jobTitle: member.data.jobTitle,
              phone: preferences.preferences.phone,
              timezone: preferences.preferences.timezone,
              dateFormat,
              joinedAt: member.data.joinedAt,
              avatarUrl: profile.avatarUrl,
              managedBy: null,
              workspaceName,
            },
          }
        : isError || preferences.isError || member.isError
          ? { status: "error" }
          : { status: "loading" },
    [
      profile,
      preferences.preferences,
      preferences.isError,
      member.data,
      member.isError,
      isError,
      dateFormat,
      workspaceName,
    ],
  );

  const handleSave = useCallback(
    async (values: AccountProfileValues) => {
      setSave({ state: "saving" });
      try {
        if (values.name !== profile?.name)
          await updateName.mutateAsync({ name: values.name });
        if (
          values.phone !== preferences.preferences?.phone ||
          values.timezone !== preferences.preferences?.timezone
        )
          await preferences.save({
            phone: values.phone,
            timezone: values.timezone,
          });
        if (values.jobTitle !== member.data?.jobTitle)
          await updateJobTitle.mutateAsync(values.jobTitle);
        setDateFormat(values.dateFormat);
        setSave({ state: "saved" });
      } catch {
        // A stale version has refetched the authoritative values.
        setSave({ state: "error" });
      }
    },
    [
      profile?.name,
      updateName,
      preferences,
      member.data?.jobTitle,
      updateJobTitle,
      setDateFormat,
    ],
  );

  const handlePhoto = useCallback(
    async (file: File) => {
      if (!PHOTO_TYPES.includes(file.type) || file.size > MAX_PHOTO_BYTES) {
        setPhotoError(true);
        return;
      }
      try {
        await api.setAvatar(await readAsDataUrl(file));
        setPhotoError(false);
        await queryClient.invalidateQueries({
          queryKey: accountQueryKeys.profile(accountId),
        });
      } catch {
        setPhotoError(true);
      }
    },
    [api, queryClient, accountId],
  );

  return (
    <AccountProfileSurface
      copy={copy}
      formatters={formatters}
      profile={section}
      save={save}
      photoError={photoError}
      onSave={(values) => void handleSave(values)}
      onRetry={() => {
        void refetch();
        void preferences.refetch();
        void member.refetch();
      }}
      onPhotoChange={(file) => void handlePhoto(file)}
    />
  );
}
