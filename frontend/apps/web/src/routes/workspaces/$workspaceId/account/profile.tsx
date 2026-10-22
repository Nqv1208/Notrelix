import { useCallback, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
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
import { useAccountPagesKeys, useAccountPagesApi } from "./use-account-pages";

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

export function AccountProfilePage() {
  const accountPagesKeys = useAccountPagesKeys();
  const { api: runtimeClient } = useAppRuntime();
  const copy = useAccountProfileCopy();
  const formatters = useFormatters();
  const queryClient = useQueryClient();
  const api = useAccountPagesApi();

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
  const details = useQuery({
    queryKey: accountPagesKeys.details,
    queryFn: () => api.getDetails(),
    retry: false,
  });
  const updateName = useUpdateProfile();
  const updateDetails = useMutation({
    mutationFn: (input: Parameters<typeof api.updateDetails>[0]) =>
      api.updateDetails(input),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: accountPagesKeys.details }),
  });
  const [save, setSave] = useState<AccountSaveStatus>({ state: "idle" });
  const [photoError, setPhotoError] = useState(false);

  const section = useMemo<AccountSection<AccountProfileData>>(
    () =>
      profile && details.data
        ? {
            status: "ready",
            data: {
              name: profile.name,
              email: profile.email,
              jobTitle: details.data.jobTitle,
              phone: details.data.phone,
              timezone: details.data.timezone,
              dateFormat: details.data.dateFormat,
              joinedAt: details.data.joinedAt,
              avatarUrl: details.data.avatarUrl,
              managedBy: details.data.managedBy,
              workspaceName: details.data.workspaceName,
            },
          }
        : isError || details.isError
          ? { status: "error" }
          : { status: "loading" },
    [profile, details.data, details.isError, isError],
  );

  const handleSave = useCallback(
    async (values: AccountProfileValues) => {
      setSave({ state: "saving" });
      try {
        if (values.name !== profile?.name)
          await updateName.mutateAsync({ name: values.name });
        await updateDetails.mutateAsync({
          jobTitle: values.jobTitle,
          phone: values.phone,
          timezone: values.timezone,
          dateFormat: values.dateFormat,
        });
        setSave({ state: "saved" });
      } catch {
        setSave({ state: "error" });
      }
    },
    [profile?.name, updateName, updateDetails],
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
          queryKey: accountPagesKeys.details,
        });
      } catch {
        setPhotoError(true);
      }
    },
    [api, queryClient],
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
        void details.refetch();
      }}
      onPhotoChange={(file) => void handlePhoto(file)}
    />
  );
}
