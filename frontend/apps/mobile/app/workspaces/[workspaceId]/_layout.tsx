import React, { useEffect } from "react";
import { Stack, useLocalSearchParams } from "expo-router";
import { useMobileApplicationServices } from "@notrelix/runtime-mobile";

export default function WorkspaceLayout() {
  const { workspaceId } = useLocalSearchParams<{ workspaceId: string }>();
  const { workspaceLifecycle } = useMobileApplicationServices();

  useEffect(() => {
    if (workspaceId) {
      workspaceLifecycle.prepareWorkspaceTransition(workspaceId);
    }
  }, [workspaceId, workspaceLifecycle]);

  return (
    <Stack>
      <Stack.Screen name="index" options={{ title: "Workspace" }} />
      <Stack.Screen name="boards/[boardId]" options={{ title: "Board" }} />
      <Stack.Screen name="docs/[documentId]" options={{ title: "Document" }} />
      <Stack.Screen name="automation/index" options={{ title: "Automation" }} />
    </Stack>
  );
}
