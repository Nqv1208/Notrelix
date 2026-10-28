import { useEffect, useMemo } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useLocation } from "@tanstack/react-router";
import { useAppRuntime } from "@notrelix/runtime-web";
import { useAuth } from "@notrelix/features-auth";
import { workspaceRealtimeAdapter } from "@notrelix/features-workspace";
import { workManagementRealtimeAdapter } from "@notrelix/work-management-state";
import { ModuleAdapterRegistry } from "../realtime/module-adapter-registry";
import { getActiveWorkspaceIdFromPathname } from "../realtime/active-workspace";
import { getActiveBoardIdFromPathname } from "../realtime/active-board";
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

  const registry = useMemo(() => {
    const next = new ModuleAdapterRegistry();
    next.register(workspaceRealtimeAdapter);
    next.register(workManagementRealtimeAdapter);
    return next;
  }, []);

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

    const unsubscribe = runtime.realtime.subscribe(
      { workspaceId, boardId },
      (envelope) => {
        if (!active) return;
        void registry
          .dispatch(envelope, {
            workspaceId,
            boardId,
            invalidateQueries: async (keys) => {
              if (!active) return;
              await Promise.all(
                keys.map((queryKey) =>
                  queryClient.invalidateQueries({ queryKey }),
                ),
              );
            },
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
