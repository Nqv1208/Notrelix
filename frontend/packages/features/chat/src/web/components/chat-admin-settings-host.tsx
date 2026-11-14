import { useMemo, useState } from "react";
import { chatFailureOf } from "../../core/api/chat-failure";
import type { ChatExport } from "../../core/types/chat";
import type { ChatHooks } from "../query/create-chat-hooks";
import type { ChatCopy } from "./chat.copy";
import type { ChatViewerRole } from "./chat-workspace";
import { saveBlobAs } from "./chat-attachments";
import { exportFileName } from "../../core/model/retention-model";
import { ChatAdminSettingsDialog } from "./chat-admin-settings";

export interface ChatAdminSettingsProps {
  readonly open: boolean;
  readonly onOpenChange: (open: boolean) => void;
  readonly hooks: ChatHooks;
  readonly workspaceId: string;
  readonly viewerRole: ChatViewerRole | null;
  readonly people: ReadonlyMap<string, string>;
  readonly locale: string;
  readonly copy: ChatCopy;
}

/**
 * Wires the Chat admin settings (UC-CHT-H05, H06) to the hooks: the retention rule (Owner edits, Admin reads) and the export of a
 * person's chat data (Owner and Admin). The queries only run while the dialog is open.
 */
export function ChatAdminSettings({
  open,
  onOpenChange,
  hooks,
  workspaceId,
  viewerRole,
  people,
  locale,
  copy,
}: ChatAdminSettingsProps) {
  const canExport = viewerRole === "owner" || viewerRole === "admin";
  const policy = hooks.useRetention(workspaceId, open && canExport);
  const writer = hooks.useSetRetention(workspaceId);
  const creator = hooks.useCreateExport(workspaceId);
  const downloader = hooks.useDownloadExport(workspaceId);
  const [message, setMessage] = useState<{
    tone: "error" | "ok";
    text: string;
  } | null>(null);
  const [jobId, setJobId] = useState<string | null>(null);
  const [exportError, setExportError] = useState<string | null>(null);
  const status = hooks.useExport(workspaceId, jobId);
  const job: ChatExport | null = status.data ?? null;
  const rows = useMemo(
    () =>
      [...people.entries()]
        .map(([id, name]) => ({ id, name }))
        .sort((a, b) => a.name.localeCompare(b.name)),
    [people],
  );
  const time = useMemo(
    () =>
      new Intl.DateTimeFormat(locale, {
        dateStyle: "medium",
        timeStyle: "short",
      }),
    [locale],
  );

  if (!canExport) return null;
  const retentionText = copy.admin.retention;
  const exportText = copy.admin.export;

  return (
    <ChatAdminSettingsDialog
      open={open}
      copy={copy}
      onOpenChange={(next) => {
        if (!next) {
          setMessage(null);
          setExportError(null);
        }
        onOpenChange(next);
      }}
      retention={{
        copy,
        policy: policy.data,
        loading: policy.isPending,
        loadFailed: policy.isError,
        busy: writer.isPending,
        message,
        onRetry: () => void policy.refetch(),
        onSave: (input) => {
          setMessage(null);
          writer.mutate(input, {
            onSuccess: () =>
              setMessage({ tone: "ok", text: retentionText.saved }),
            onError: (error) => {
              const kind = chatFailureOf(error).kind;
              setMessage({
                tone: "error",
                text:
                  kind === "stale"
                    ? retentionText.stale
                    : kind === "forbidden"
                      ? retentionText.forbidden
                      : retentionText.failed,
              });
            },
          });
        },
      }}
      exporter={{
        copy,
        people: rows,
        job,
        busy: creator.isPending,
        downloading: downloader.isPending,
        error: exportError,
        formatTime: (iso) => time.format(new Date(iso)),
        onRequest: (input) => {
          setExportError(null);
          creator.mutate(input, {
            onSuccess: (created) => setJobId(created.id),
            onError: (error) => {
              const kind = chatFailureOf(error).kind;
              setExportError(
                kind === "rate-limited"
                  ? exportText.rateLimited
                  : kind === "forbidden"
                    ? exportText.forbidden
                    : exportText.createFailed,
              );
            },
          });
        },
        onDownload: (target) => {
          setExportError(null);
          downloader.mutate(target.id, {
            onSuccess: (blob) => saveBlobAs(blob, exportFileName(target)),
            onError: () => setExportError(exportText.downloadFailed),
          });
        },
      }}
    />
  );
}
