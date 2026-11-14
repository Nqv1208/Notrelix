/**
 * The Workspace retention rule (UC-CHT-H05) and the export of a person's chat data (UC-CHT-H06). The mock mirrors the backend's
 * order of checks; the background work is the helpers `runRetention` and `runExport` (the export also runs on the first status read,
 * so a client sees the job finish without a worker). Audit facts have no mock surface.
 */

import { defineMockOperation } from "../../operations/types";
import { ok } from "../../transport/create-response";
import {
  callerIn,
  fail,
  guarded,
  notFoundProblem,
  parseVersion,
  requireAction,
  requireVersion,
  validationProblem,
} from "./chat-access";
import { CHAT_ERROR, type ChatExportJobRecord } from "./chat-model";
import { requireWithinLimit } from "./chat-limits";
import {
  RETENTION_MAX_DAYS,
  RETENTION_MIN_DAYS,
  runExport,
  sweepExport,
  virtualNowIso,
} from "./chat-retention";
import type { ChatCaller } from "./chat-access";
import type { MockChatState } from "./chat-state";

type WorkspaceParams = { workspaceId: string };
type ExportParams = { workspaceId: string; exportId: string };
type SetBody = {
  retentionDays?: unknown;
  mode?: unknown;
  expectedVersion?: unknown;
};
type CreateBody = {
  subjectUserId?: unknown;
  rangeFrom?: unknown;
  rangeTo?: unknown;
};

function retentionView(
  state: MockChatState,
  workspaceId: string,
  caller: ChatCaller,
) {
  const row = state.retentionPolicies.get(workspaceId);
  return {
    retentionDays: row?.retentionDays ?? null,
    mode: row?.mode ?? "tombstone",
    version: row?.version ?? 1,
    updatedAt: row?.updatedAt ?? null,
    canChange: caller.role === "Owner",
  };
}

function exportView(
  workspaceId: string,
  job: ChatExportJobRecord,
  now: string,
) {
  const downloadable =
    job.status === "ready" && job.expiresAt !== null && now < job.expiresAt;
  return {
    id: job.id,
    subjectUserId: job.subjectUserId,
    rangeFrom: job.rangeFrom,
    rangeTo: job.rangeTo,
    status: job.status,
    messageCount: job.messageCount,
    spaceCount: job.spaceCount,
    excludedSpaceCount: job.excludedSpaceCount,
    tombstoneCount: job.tombstoneCount,
    sizeBytes: job.sizeBytes,
    failureCode: job.failureCode,
    createdAt: job.createdAt,
    completedAt: job.completedAt,
    expiresAt: job.expiresAt,
    downloadPath: downloadable
      ? `/api/v1/workspaces/${workspaceId}/chat/exports/${job.id}/content`
      : null,
  };
}

/** The export of the caller; another person's job, or one of another Workspace, is a missing one. */
function ownJob(
  state: MockChatState,
  caller: ChatCaller,
  exportId: string,
): ChatExportJobRecord {
  const job = state.exportJobs.get(exportId);
  if (
    !job ||
    job.workspaceId !== caller.workspaceId ||
    job.requestedBy !== caller.userId
  ) {
    notFoundProblem();
  }
  sweepExport(state, job);
  return job;
}

function optionalInstant(value: unknown, name: string): string | null {
  if (value === undefined || value === null) return null;
  if (typeof value !== "string" || Number.isNaN(Date.parse(value))) {
    validationProblem(`${name} is not a valid time.`);
  }
  return value;
}

export const retentionOperations = [
  defineMockOperation<WorkspaceParams>({
    id: "chat.retention.get",
    contract: { kind: "openapi", operationId: "Chat.Retention.Get" },
    method: "GET",
    route: "/workspaces/:workspaceId/chat/retention",
    async handle({ params, store }) {
      return guarded(async () => {
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "viewWorkspace");
        return ok(retentionView(store.chat, params.workspaceId, caller));
      });
    },
  }),

  defineMockOperation<WorkspaceParams, SetBody>({
    id: "chat.retention.set",
    contract: { kind: "openapi", operationId: "Chat.Retention.Set" },
    method: "PUT",
    route: "/workspaces/:workspaceId/chat/retention",
    async handle({ params, body, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "manageWorkspace");
        const expected = parseVersion(body?.expectedVersion, "expectedVersion");
        const days = body?.retentionDays ?? null;
        if (
          days !== null &&
          (typeof days !== "number" ||
            !Number.isInteger(days) ||
            days < RETENTION_MIN_DAYS ||
            days > RETENTION_MAX_DAYS)
        ) {
          validationProblem(
            `retentionDays must be ${RETENTION_MIN_DAYS} to ${RETENTION_MAX_DAYS}, or empty.`,
          );
        }
        if (body?.mode !== "tombstone" && body?.mode !== "purge") {
          validationProblem("mode must be tombstone or purge.");
        }
        const current = state.retentionPolicies.get(params.workspaceId);
        requireVersion(expected, current?.version ?? 1);
        const next = {
          retentionDays: days as number | null,
          mode: body.mode as "tombstone" | "purge",
        };
        // Nothing changes when the rule is the same (the version stays).
        if (
          !current ||
          current.retentionDays !== next.retentionDays ||
          current.mode !== next.mode
        ) {
          state.retentionPolicies.set(params.workspaceId, {
            ...next,
            version: (current?.version ?? 1) + 1,
            updatedAt: state.stampWrite(),
          });
        }
        return ok(retentionView(state, params.workspaceId, caller));
      });
    },
  }),

  defineMockOperation<WorkspaceParams, CreateBody>({
    id: "chat.exports.create",
    contract: { kind: "openapi", operationId: "Chat.Exports.Create" },
    method: "POST",
    route: "/workspaces/:workspaceId/chat/exports",
    async handle({ params, body, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "manageWorkspaceSettings");
        if (typeof body?.subjectUserId !== "string" || !body.subjectUserId) {
          validationProblem("subjectUserId is required.");
        }
        const from = optionalInstant(body.rangeFrom, "rangeFrom");
        const to = optionalInstant(body.rangeTo, "rangeTo");
        if (
          from !== null &&
          to !== null &&
          Date.parse(to) <= Date.parse(from)
        ) {
          validationProblem("The range must end after it starts.");
        }
        const subject = body.subjectUserId;
        const member = store
          .getWorkspaceMembers(params.workspaceId)
          .some((row) => row.userId === subject);
        if (!member) notFoundProblem();
        requireWithinLimit(state, "export", params.workspaceId, caller.userId);
        state.exportCounter += 1;
        const job: ChatExportJobRecord = {
          id: `c7000000-0000-4000-8000-${String(state.exportCounter).padStart(12, "0")}`,
          workspaceId: params.workspaceId,
          requestedBy: caller.userId,
          subjectUserId: subject,
          rangeFrom: from,
          rangeTo: to,
          status: "queued",
          messageCount: 0,
          spaceCount: 0,
          excludedSpaceCount: 0,
          tombstoneCount: 0,
          sizeBytes: 0,
          failureCode: null,
          createdAt: state.stampWrite(),
          completedAt: null,
          expiresAt: null,
          file: null,
        };
        state.exportJobs.set(job.id, job);
        return {
          status: 202,
          body: exportView(params.workspaceId, job, virtualNowIso(state)),
        };
      });
    },
  }),

  defineMockOperation<ExportParams>({
    id: "chat.exports.get",
    contract: { kind: "openapi", operationId: "Chat.Exports.Get" },
    method: "GET",
    route: "/workspaces/:workspaceId/chat/exports/:exportId",
    async handle({ params, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "manageWorkspaceSettings");
        const job = ownJob(state, caller, params.exportId);
        // The worker of the backend picks the job up; the mock runs it on the first read.
        runExport(state, job);
        return ok(exportView(params.workspaceId, job, virtualNowIso(state)));
      });
    },
  }),

  defineMockOperation<ExportParams>({
    id: "chat.exports.content",
    contract: { kind: "openapi", operationId: "Chat.Exports.GetContent" },
    method: "GET",
    route: "/workspaces/:workspaceId/chat/exports/:exportId/content",
    async handle({ params, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "manageWorkspaceSettings");
        const job = ownJob(state, caller, params.exportId);
        if (job.status !== "ready" || job.file === null) {
          fail(
            404,
            CHAT_ERROR.notFound,
            "Not found",
            "The export is not available.",
          );
        }
        return {
          status: 200,
          headers: {
            "Content-Type": "application/json",
            "Content-Disposition": `attachment; filename*=UTF-8''chat-export-${job.id}.json`,
            "X-Content-Type-Options": "nosniff",
            "Cache-Control": "private, no-store",
          },
          rawBody: new TextEncoder().encode(job.file),
        };
      });
    },
  }),
];
