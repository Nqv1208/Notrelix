import { useState, useEffect, useMemo } from "react";
import {
  createUseProfile,
  createUseUpdateProfile,
} from "@notrelix/features-account";
import { useAppRuntime } from "@notrelix/runtime-web";
import { Button, Input } from "@notrelix/ui-web";
import { toast } from "sonner";

export function AccountProfilePage() {
  const { api: runtimeClient } = useAppRuntime();

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

  const { profile, isLoading } = useProfile();
  const updateMutation = useUpdateProfile();

  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [timezone, setTimezone] = useState("");
  const [locale, setLocale] = useState("");

  useEffect(() => {
    if (profile) {
      setName(profile.name);
      setEmail(profile.email);
      setTimezone(profile.timezone);
      setLocale(profile.locale);
    }
  }, [profile]);

  const handleSave = async () => {
    if (!name.trim()) {
      toast.error("Name is required");
      return;
    }

    try {
      await updateMutation.mutateAsync({
        name: name.trim(),
        timezone,
        locale,
      });
      toast.success("Profile updated");
    } catch (err) {
      toast.error(
        err instanceof Error ? err.message : "Failed to update profile",
      );
    }
  };

  if (isLoading) {
    return (
      <div className="space-y-4">
        {[1, 2, 3].map((i) => (
          <div key={i} className="h-10 bg-muted rounded-lg animate-pulse" />
        ))}
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h2 className="font-semibold text-sm mb-1">Profile</h2>
        <p className="text-xs text-muted-foreground">
          Update your personal information.
        </p>
      </div>

      <div className="space-y-4">
        <div className="space-y-1.5">
          <label className="text-sm font-medium text-foreground">Name</label>
          <Input
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="Your name"
          />
        </div>

        <div className="space-y-1.5">
          <label className="text-sm font-medium text-foreground">Email</label>
          <Input value={email} disabled className="opacity-60" />
          <p className="text-xs text-muted-foreground">
            Contact support to change your email.
          </p>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-1.5">
            <label className="text-sm font-medium text-foreground">
              Timezone
            </label>
            <select
              value={timezone}
              onChange={(e) => setTimezone(e.target.value)}
              className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
            >
              <option value="UTC">UTC</option>
              <option value="America/New_York">Eastern Time</option>
              <option value="America/Chicago">Central Time</option>
              <option value="America/Denver">Mountain Time</option>
              <option value="America/Los_Angeles">Pacific Time</option>
              <option value="Europe/London">London</option>
              <option value="Europe/Paris">Paris</option>
              <option value="Asia/Tokyo">Tokyo</option>
              <option value="Asia/Ho_Chi_Minh">Ho Chi Minh</option>
            </select>
          </div>

          <div className="space-y-1.5">
            <label className="text-sm font-medium text-foreground">
              Language
            </label>
            <select
              value={locale}
              onChange={(e) => setLocale(e.target.value)}
              className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
            >
              <option value="en">English</option>
              <option value="vi">Tiếng Việt</option>
            </select>
          </div>
        </div>

        <Button
          onClick={handleSave}
          disabled={updateMutation.isPending || !name.trim()}
        >
          {updateMutation.isPending ? "Saving..." : "Save changes"}
        </Button>
      </div>
    </div>
  );
}
