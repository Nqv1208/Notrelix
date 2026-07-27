import { useMemo } from 'react';
import { createUseSecuritySettings } from '@notrelix/features-account';
import { useAppRuntime } from '@notrelix/runtime-web';
import { Button } from '@notrelix/ui-web';
import { Shield, Key, Smartphone } from 'lucide-react';

export function AccountSecurityPage() {
  const { api: runtimeClient } = useAppRuntime();

  const useSecuritySettings = useMemo(
    () => createUseSecuritySettings({ api: runtimeClient.api, endpoints: runtimeClient.endpoints }),
    [runtimeClient],
  );

  const { data: security, isLoading } = useSecuritySettings();

  if (isLoading) {
    return (
      <div className="space-y-4">
        {[1, 2, 3].map((i) => (
          <div key={i} className="h-16 bg-muted rounded-lg animate-pulse" />
        ))}
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h2 className="font-semibold text-sm mb-1">Security</h2>
        <p className="text-xs text-muted-foreground">Manage your password and security settings.</p>
      </div>

      {/* Password */}
      <div className="rounded-lg border border-border p-4">
        <div className="flex items-center gap-3 mb-3">
          <div className="flex size-9 items-center justify-center rounded-lg bg-muted">
            <Key className="size-4 text-muted-foreground" />
          </div>
          <div>
            <p className="font-medium text-sm">Password</p>
            <p className="text-xs text-muted-foreground">
              Last changed {security?.lastPasswordChange ? new Date(security.lastPasswordChange).toLocaleDateString() : 'unknown'}
            </p>
          </div>
        </div>
        <Button variant="outline" size="sm" disabled>
          Change password
        </Button>
      </div>

      {/* Two-Factor Authentication */}
      <div className="rounded-lg border border-border p-4">
        <div className="flex items-center gap-3 mb-3">
          <div className="flex size-9 items-center justify-center rounded-lg bg-muted">
            <Smartphone className="size-4 text-muted-foreground" />
          </div>
          <div>
            <p className="font-medium text-sm">Two-Factor Authentication</p>
            <p className="text-xs text-muted-foreground">
              {security?.twoFactorEnabled ? 'Enabled' : 'Not enabled'}
            </p>
          </div>
        </div>
        <Button variant="outline" size="sm" disabled>
          {security?.twoFactorEnabled ? 'Disable 2FA' : 'Enable 2FA'}
        </Button>
      </div>

      {/* Active Sessions */}
      <div className="rounded-lg border border-border p-4">
        <div className="flex items-center gap-3">
          <div className="flex size-9 items-center justify-center rounded-lg bg-muted">
            <Shield className="size-4 text-muted-foreground" />
          </div>
          <div>
            <p className="font-medium text-sm">Active Sessions</p>
            <p className="text-xs text-muted-foreground">
              {security?.activeSessions ?? 1} session(s) currently active
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
