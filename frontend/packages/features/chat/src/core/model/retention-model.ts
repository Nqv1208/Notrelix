/**
 * Pure rules of the Workspace retention form and the export form (UC-CHT-H05, H06). The server decides; these only keep an
 * impossible request from leaving the form and word what is about to happen.
 */

import {
  CHAT_RETENTION_DAYS,
  type ChatExport,
  type ChatRetentionMode,
  type ChatRetentionPolicy,
} from "../types/chat";

/** What the form holds: an empty `days` is no retention. */
export interface RetentionDraft {
  readonly days: string;
  readonly mode: ChatRetentionMode;
}

export function draftOf(policy: ChatRetentionPolicy): RetentionDraft {
  return {
    days: policy.retentionDays === null ? "" : String(policy.retentionDays),
    mode: policy.mode,
  };
}

/** The window in days, null for no retention, or "invalid" for text that is not a whole number inside the bounds. */
export function parseDays(text: string): number | null | "invalid" {
  const trimmed = text.trim();
  if (trimmed.length === 0) return null;
  if (!/^\d+$/.test(trimmed)) return "invalid";
  const value = Number(trimmed);
  return value >= CHAT_RETENTION_DAYS.min && value <= CHAT_RETENTION_DAYS.max
    ? value
    : "invalid";
}

/** True when the draft differs from the stored rule (a no-op is never sent). */
export function isDirty(
  policy: ChatRetentionPolicy,
  draft: RetentionDraft,
): boolean {
  const days = parseDays(draft.days);
  if (days === "invalid") return true;
  return days !== policy.retentionDays || draft.mode !== policy.mode;
}

/** A purge removes content for good, so turning it on, or keeping it on with a shorter window, needs the confirmation. */
export function needsPurgeConfirmation(
  policy: ChatRetentionPolicy,
  draft: RetentionDraft,
): boolean {
  const days = parseDays(draft.days);
  if (days === null || days === "invalid" || draft.mode !== "purge") {
    return false;
  }
  if (policy.mode !== "purge" || policy.retentionDays === null) return true;
  return days < policy.retentionDays;
}

/** Whether an export can be saved right now. */
export function canDownload(job: ChatExport): boolean {
  return job.status === "ready" && job.downloadPath !== null;
}

/** An export in progress: the status is polled while it is. */
export function isExportPending(job: ChatExport): boolean {
  return job.status === "queued" || job.status === "running";
}

/** The file name the browser saves the export as. */
export function exportFileName(job: ChatExport): string {
  return `chat-export-${job.id}.json`;
}

/** The size as KB or MB with one decimal, for the status line. */
export function formatBytes(bytes: number): string {
  if (!Number.isFinite(bytes) || bytes < 0) return "0 KB";
  if (bytes < 1024 * 1024) return `${Math.max(1, Math.round(bytes / 1024))} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}
