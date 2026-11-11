import { useEffect, useMemo, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useLocation } from "@tanstack/react-router";
import { useAppRuntime } from "@notrelix/runtime-web";
import { useAuth } from "@notrelix/features-auth";
import { workspaceRealtimeAdapter } from "@notrelix/features-workspace";
import { workManagementRealtimeAdapter } from "@notrelix/work-management-state";
import {
  createChatRealtimeAdapter,
  createChatService,
} from "@notrelix/features-chat";
import { ModuleAdapterRegistry } from "../realtime/module-adapter-registry";
import { getActiveWorkspaceIdFromPathname } from "../realtime/active-workspace";
import { getActiveBoardIdFromPathname } from "../realtime/active-board";
import { getActiveChannelIdFromPathname } from "../realtime/active-channel";
import { createDragGate } from "../realtime/drag-gate";
import { handleWorkspaceRecovery } from "../realtime/workspace-recovery-policy";

export function RealtimeLifecycle({ children }: { children: React.ReactNode }) {
  const runtime = useAppRuntime();
  const queryClient = useQueryClient();
  const location = useLocation();
  const { isAuthenticated, sessionGeneration, user } = useAuth();
  // Account = the signed-in principal (same source as the AccountScopeBridge).
  const accountId = user?.id ?? null;
  const workspaceId = getActiveWorkspaceIdFromPathname(location.pathname);
  const boardId = getActiveBoardIdFromPathname(location.pathname);
  const channelId = getActiveChannelIdFromPathname(location.pathname);
  // Bumped when the person's access changed: the open channel is subscribed again.
  const [accessEpoch, setAccessEpoch] = useState(0);

  const chatAdapter = useMemo(
    () =>
      createChatRealtimeAdapter({
        queryClient,
        service: createChatService(runtime.api.api, runtime.api.endpoints),
        viewerId: accountId ?? undefined,
      }),
    [queryClient, runtime.api, accountId],
  );

  const registry = useMemo(() => {
    const next = new ModuleAdapterRegistry();
    next.register(workspaceRealtimeAdapter);
    next.register(workManagementRealtimeAdapter);
    next.register(chatAdapter);
    return next;
  }, [chatAdapter]);

  useEffect(() => {
    if (!isAuthenticated || !sessionGeneration) {
      runtime.realtime.disconnect();
      return;
    }

    void runtime.realtime.connect({ sessionGeneration });
  }, [isAuthenticated, runtime.realtime, sessionGeneration, workspaceId]);

  useEffect(() => {
    if (!isAuthenticated || !sessionGeneration || !workspaceId) return;

    // Set to false on cleanup (Workspace/principal transition). Late async
    // work of the previous scope must not touch the new one (NRX-014).
    let active = true;

    const invalidateQueries = async (keys: readonly unknown[][]) => {
      if (!active) return;
      await Promise.all(
        keys.map((queryKey) => queryClient.invalidateQueries({ queryKey })),
      );
    };

    const unsubscribeEvents = runtime.realtime.subscribe(
      { workspaceId },
      (envelope) => {
        if (!active) return;
        void registry
          .dispatch(envelope, { workspaceId, invalidateQueries })
          .then((result) => {
            if (!result.handled) {
              runtime.telemetry.track("realtime.unknown_event_type", {
                eventType: envelope.eventType,
                workspaceId: envelope.workspaceId,
              });
            }
          })
          .catch((error) => {
            runtime.telemetry.reportError(error, {
              context: "realtime.module_dispatch",
              eventType: envelope.eventType,
              eventId: envelope.eventId,
            });
          });
      },
    );

    // Subscription ack, stream gap/duplicate/reset and permission loss all
    // mean "incremental history is unreliable": refetch authoritative state
    // over REST (FE-RT-024/025). Permission loss is not retried; the refetch
    // surfaces the forbidden state through the normal query error pathway.
    const unsubscribeRecovery = runtime.realtime.subscribeRecovery((signal) => {
      if (!active || !accountId || signal.workspaceId !== workspaceId) return;
      if (signal.reason === "permission-lost") {
        runtime.telemetry.track("realtime.permission_lost", {
          workspaceId,
          code: signal.code,
        });
      }
      // Chat streams recover on their own: the channel backfills after its last
      // known sequence and the counters are read again (UC-CHT-C04, C05).
      if (signal.scope.kind === "channel" || signal.scope.kind === "user") {
        void chatAdapter.recover(signal).catch((error) => {
          runtime.telemetry.reportError(error, {
            context: "realtime.chat_recovery",
            workspaceId,
            reason: signal.reason,
          });
        });
        return;
      }
      if (signal.reason === "access-changed")
        setAccessEpoch((epoch) => epoch + 1);
      void handleWorkspaceRecovery({
        workspaceId,
        accountId,
        invalidateQueries,
      }).catch((error) => {
        runtime.telemetry.reportError(error, {
          context: "realtime.workspace_recovery",
          workspaceId,
          reason: signal.reason,
          expected: signal.expected,
          received: signal.received,
        });
      });
    });

    return () => {
      active = false;
      unsubscribeEvents();
      unsubscribeRecovery();
    };
  }, [
    accountId,
    chatAdapter,
    isAuthenticated,
    queryClient,
    registry,
    runtime.realtime,
    runtime.telemetry,
    sessionGeneration,
    workspaceId,
  ]);

  // The per-user stream carries the caller's unread and mention counters (UC-CHT-C09).
  useEffect(() => {
    if (!isAuthenticated || !sessionGeneration || !workspaceId || !accountId)
      return;

    let active = true;
    const invalidateQueries = async (keys: readonly unknown[][]) => {
      if (!active) return;
      await Promise.all(
        keys.map((queryKey) => queryClient.invalidateQueries({ queryKey })),
      );
    };

    const unsubscribe = runtime.realtime.subscribe(
      { workspaceId, userId: accountId },
      (envelope) => {
        if (!active) return;
        void registry
          .dispatch(envelope, { workspaceId, invalidateQueries })
          .catch((error) => {
            runtime.telemetry.reportError(error, {
              context: "realtime.user_dispatch",
              eventType: envelope.eventType,
              eventId: envelope.eventId,
            });
          });
      },
    );

    return () => {
      active = false;
      unsubscribe();
    };
  }, [
    accountId,
    isAuthenticated,
    queryClient,
    registry,
    runtime.realtime,
    runtime.telemetry,
    sessionGeneration,
    workspaceId,
  ]);

  // The open Chat channel streams its messages (UC-CHT-C01). One channel at a
  // time: switching or leaving the channel unsubscribes it (never every channel).
  useEffect(() => {
    if (!isAuthenticated || !sessionGeneration || !workspaceId || !channelId)
      return;

    let active = true;
    const invalidateQueries = async (keys: readonly unknown[][]) => {
      if (!active) return;
      await Promise.all(
        keys.map((queryKey) => queryClient.invalidateQueries({ queryKey })),
      );
    };

    const unsubscribe = runtime.realtime.subscribe(
      { workspaceId, channelId },
      (envelope) => {
        if (!active) return;
        void registry
          .dispatch(envelope, { workspaceId, channelId, invalidateQueries })
          .catch((error) => {
            runtime.telemetry.reportError(error, {
              context: "realtime.channel_dispatch",
              eventType: envelope.eventType,
              eventId: envelope.eventId,
            });
          });
      },
    );

    return () => {
      active = false;
      unsubscribe();
    };
  }, [
    accessEpoch,
    channelId,
    isAuthenticated,
    queryClient,
    registry,
    runtime.realtime,
    runtime.telemetry,
    sessionGeneration,
    workspaceId,
  ]);

  // The open board streams its own changes (authorized per board by the
  // gateway). Gap, reset and permission loss of this stream arrive on the
  // recovery listener above, which revalidates the whole Workspace scope.
  useEffect(() => {
    if (!isAuthenticated || !sessionGeneration || !workspaceId || !boardId)
      return;

    let active = true;

    // A refetch waits for a drag in flight: no card reorders under the pointer.
    const gate = createDragGate(document, async (keys) => {
      if (!active) return;
      await Promise.all(
        keys.map((queryKey) => queryClient.invalidateQueries({ queryKey })),
      );
    });

    const unsubscribe = runtime.realtime.subscribe(
      { workspaceId, boardId },
      (envelope) => {
        if (!active) return;
        void registry
          .dispatch(envelope, {
            workspaceId,
            boardId,
            invalidateQueries: gate.invalidate,
          })
          .catch((error) => {
            runtime.telemetry.reportError(error, {
              context: "realtime.board_dispatch",
              eventType: envelope.eventType,
              eventId: envelope.eventId,
            });
          });
      },
    );

    return () => {
      active = false;
      gate.dispose();
      unsubscribe();
    };
  }, [
    boardId,
    isAuthenticated,
    queryClient,
    registry,
    runtime.realtime,
    runtime.telemetry,
    sessionGeneration,
    workspaceId,
  ]);

  return <>{children}</>;
}
