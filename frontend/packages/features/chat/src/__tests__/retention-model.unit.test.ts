import { describe, expect, it } from "vitest";
import {
  canDownload,
  draftOf,
  exportFileName,
  formatBytes,
  isDirty,
  isExportPending,
  needsPurgeConfirmation,
  parseDays,
} from "../core/model/retention-model";
import type { ChatExport, ChatRetentionPolicy } from "../core/types/chat";

const policy = (
  over: Partial<ChatRetentionPolicy> = {},
): ChatRetentionPolicy => ({
  retentionDays: null,
  mode: "tombstone",
  version: 1,
  updatedAt: null,
  canChange: true,
  ...over,
});

const job = (over: Partial<ChatExport> = {}): ChatExport => ({
  id: "e1",
  subjectUserId: "u1",
  rangeFrom: null,
  rangeTo: null,
  status: "ready",
  messageCount: 1,
  spaceCount: 1,
  excludedSpaceCount: 0,
  tombstoneCount: 0,
  sizeBytes: 2048,
  failureCode: null,
  createdAt: "2026-11-12T09:00:00Z",
  completedAt: null,
  expiresAt: null,
  downloadPath: "/x",
  ...over,
});

describe("retention form rules (UC-CHT-H05)", () => {
  it("parses a window of 7 to 3650 days, an empty field as no retention, anything else as invalid", () => {
    expect(parseDays("")).toBeNull();
    expect(parseDays("  ")).toBeNull();
    expect(parseDays("7")).toBe(7);
    expect(parseDays("3650")).toBe(3650);
    for (const bad of ["6", "3651", "1.5", "-3", "abc", "30d"]) {
      expect(parseDays(bad)).toBe("invalid");
    }
  });

  it("is dirty only when the window or the mode differs from the stored rule", () => {
    const stored = policy({ retentionDays: 90, mode: "purge" });
    expect(isDirty(stored, draftOf(stored))).toBe(false);
    expect(isDirty(stored, { days: "91", mode: "purge" })).toBe(true);
    expect(isDirty(stored, { days: "90", mode: "tombstone" })).toBe(true);
    expect(isDirty(stored, { days: "x", mode: "purge" })).toBe(true);
  });

  it("asks for the confirmation when a purge starts or its window shrinks, never for a tombstone or turning it off", () => {
    const none = policy();
    expect(needsPurgeConfirmation(none, { days: "30", mode: "purge" })).toBe(
      true,
    );
    expect(
      needsPurgeConfirmation(none, { days: "30", mode: "tombstone" }),
    ).toBe(false);
    expect(needsPurgeConfirmation(none, { days: "", mode: "purge" })).toBe(
      false,
    );
    const purging = policy({ retentionDays: 90, mode: "purge" });
    expect(needsPurgeConfirmation(purging, { days: "60", mode: "purge" })).toBe(
      true,
    );
    expect(
      needsPurgeConfirmation(purging, { days: "120", mode: "purge" }),
    ).toBe(false);
    const tomb = policy({ retentionDays: 90, mode: "tombstone" });
    expect(needsPurgeConfirmation(tomb, { days: "90", mode: "purge" })).toBe(
      true,
    );
  });
});

describe("export status rules (UC-CHT-H06)", () => {
  it("downloads only a ready export that has a path, and polls only while queued or running", () => {
    expect(canDownload(job())).toBe(true);
    expect(canDownload(job({ downloadPath: null }))).toBe(false);
    expect(canDownload(job({ status: "expired" }))).toBe(false);
    expect(isExportPending(job({ status: "queued" }))).toBe(true);
    expect(isExportPending(job({ status: "running" }))).toBe(true);
    expect(isExportPending(job())).toBe(false);
  });

  it("names the file after the job and words the size", () => {
    expect(exportFileName(job())).toBe("chat-export-e1.json");
    expect(formatBytes(10)).toBe("1 KB");
    expect(formatBytes(2048)).toBe("2 KB");
    expect(formatBytes(3 * 1024 * 1024)).toBe("3.0 MB");
    expect(formatBytes(-1)).toBe("0 KB");
  });
});
