import { useCallback, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  AccountProfileSurface,
  checkAvatarFile,
  createAccountService,
  createUsePreferences,
  createUseProfile,
  createUseProfilePhoto,
  createUseUpdateProfile,
  photoFailureOf,
  useAccountProfileCopy,
  accountQueryKeys,
  type AccountProfileData,
  type AccountPhotoFailure,
  type AccountProfileValues,
  type AccountSaveStatus,
  type AccountSection,
} from "@notrelix/features-account";
import { authQueryKeys, type User } from "@notrelix/features-auth";
import { useFormatters } from "@notrelix/i18n";
import { useAccountId } from "@notrelix/query";
import { useAppRuntime } from "@notrelix/runtime-web";
import {
  useRouteWorkspaceId,
  useRouteWorkspaceName,
} from "./use-account-pages";
import { useClientPreference } from "./use-client-preference";

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
  const useProfilePhoto = useMemo(() => createUseProfilePhoto(deps), [deps]);

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
  const [photoError, setPhotoError] = useState<AccountPhotoFailure | null>(
    null,
  );
  const photo = useProfilePhoto({
    // The signed-in session shows the person too (rail, sidebar).
    onChanged: (avatarUrl) =>
      queryClient.setQueryData<User>(authQueryKeys.profile, (user) =>
        user ? { ...user, avatarUrl } : user,
      ),
  });

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
      const refused = checkAvatarFile(file);
      if (refused) {
        setPhotoError(refused);
        return;
      }
      try {
        await photo.upload(file);
        setPhotoError(null);
      } catch (error) {
        setPhotoError(photoFailureOf(error));
      }
    },
    [photo],
  );

  const handlePhotoRemove = useCallback(async () => {
    try {
      await photo.remove();
      setPhotoError(null);
    } catch (error) {
      setPhotoError(photoFailureOf(error));
    }
  }, [photo]);

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
      onPhotoRemove={() => void handlePhotoRemove()}
    />
  );
}
