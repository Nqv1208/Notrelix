import { useState, useMemo } from 'react';
import { useParams } from '@tanstack/react-router';
import { useWorkspaceContext } from '@/providers/workspace-provider';
import { createUseUpdateWorkspace } from '@notrelix/features-workspace';
import type { UpdateWorkspaceInput } from '@notrelix/features-workspace/core';
import { useAppRuntime } from '@notrelix/runtime-web';
import { Button, Input } from '@notrelix/ui-web';
import { toast } from 'sonner';

export function SettingsPage() {
  const { workspaceId } = useParams({ from: '/workspaces/$workspaceId' });
  const { api: runtimeClient } = useAppRuntime();
  const { workspace, refetch } = useWorkspaceContext();

  const useUpdateWorkspace = useMemo(
    () => createUseUpdateWorkspace({ api: runtimeClient.api, endpoints: runtimeClient.endpoints }),
    [runtimeClient],
  );

  const updateMutation = useUpdateWorkspace(workspaceId);

  const [name, setName] = useState(workspace?.name ?? '');
  const [description, setDescription] = useState(workspace?.description ?? '');
  const [isSaving, setIsSaving] = useState(false);

  const handleSave = async () => {
    if (!name.trim()) {
      toast.error('Workspace name is required');
      return;
    }

    setIsSaving(true);
    try {
      const input: UpdateWorkspaceInput = {
        name: name.trim(),
      };
      await updateMutation.mutateAsync(input);
      await refetch();
      toast.success('Workspace updated');
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to update workspace');
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div className="p-8 max-w-2xl">
      <div className="mb-8">
        <h1 className="text-2xl font-bold tracking-tight mb-1">Settings</h1>
        <p className="text-sm text-muted-foreground">Manage your workspace settings.</p>
      </div>

      <div className="space-y-8">
        <div className="space-y-4">
          <h2 className="font-semibold text-sm">General</h2>
          <div className="space-y-3">
            <div className="space-y-1.5">
              <label className="text-sm font-medium text-foreground">Name</label>
              <Input
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Workspace name"
              />
            </div>
            <div className="space-y-1.5">
              <label className="text-sm font-medium text-foreground">Description</label>
              <textarea
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Optional description for your workspace"
                rows={3}
                className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50 resize-none"
              />
            </div>
          </div>
          <Button onClick={handleSave} disabled={isSaving || !name.trim()}>
            {isSaving ? 'Saving...' : 'Save changes'}
          </Button>
        </div>

        <div className="space-y-4">
          <h2 className="font-semibold text-sm">Danger Zone</h2>
          <div className="rounded-lg border border-destructive/20 p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="font-medium text-sm">Delete workspace</p>
                <p className="text-xs text-muted-foreground">
                  Permanently delete this workspace and all its data.
                </p>
              </div>
              <Button variant="destructive" size="sm" disabled>
                Delete
              </Button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
