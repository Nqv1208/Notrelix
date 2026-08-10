import { useEffect, useMemo } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useLocation } from "@tanstack/react-router";
import { useAppRuntime } from "@notrelix/runtime-web";
import { useAuth } from "@notrelix/features-auth";
import { workspaceRealtimeAdapter } from "@notrelix/features-workspace";
import { ModuleAdapterRegistry } from "../realtime/module-adapter-registry";
import { getActiveWorkspaceIdFromPathname } from "../realtime/active-workspace";
import { handleWorkspaceRecovery } from "../realtime/workspace-recovery-policy";

export function RealtimeLifecycle({ children }: { children: React.ReactNode }) {
  const runtime = useAppRuntime();
  const queryClient = useQueryClient();
  const location = useLocation();
  const { isAuthenticated, sessionGeneration } = useAuth();
  const workspaceId = getActiveWorkspaceIdFromPathname(location.pathname);

  const registry = useMemo(() => {
    const next = new ModuleAdapterRegistry();
    next.register(workspaceRealtimeAdapter);
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
    if (!isAuthenticated || !workspaceId) return;

    const unsubscribeEvents = runtime.realtime.subscribe(
      { workspaceId },
      (envelope) => {
        void registry
          .dispatch(envelope, {
            workspaceId,
            invalidateQueries: async (keys) => {
              await Promise.all(
                keys.map((queryKey) =>
                  queryClient.invalidateQueries({ queryKey }),
                ),
              );
            },
          })
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

    const unsubscribeRecovery = runtime.realtime.subscribeRecovery((gap) => {
      if (gap.workspaceId !== workspaceId) return;
      void handleWorkspaceRecovery({
        workspaceId,
        invalidateQueries: async (keys) => {
          await Promise.all(
            keys.map((queryKey) => queryClient.invalidateQueries({ queryKey })),
          );
        },
      }).catch((error) => {
        runtime.telemetry.reportError(error, {
          context: "realtime.workspace_recovery",
          workspaceId,
          expected: gap.expected,
          received: gap.received,
        });
      });
    });

    return () => {
      unsubscribeEvents();
      unsubscribeRecovery();
    };
  }, [
    isAuthenticated,
    queryClient,
    registry,
    runtime.realtime,
    runtime.telemetry,
    workspaceId,
  ]);

  return <>{children}</>;
}
