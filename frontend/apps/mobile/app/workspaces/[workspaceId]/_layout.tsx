import React, { useEffect } from "react";
import { Stack, useLocalSearchParams } from "expo-router";
import { useMobileRuntime } from "@notrelix/runtime-mobile";

export default function WorkspaceLayout() {
  const { workspaceId } = useLocalSearchParams<{ workspaceId: string }>();
  const runtime = useMobileRuntime();

  useEffect(() => {
    if (workspaceId) {
      runtime.realtime.connect({ sessionGeneration: workspaceId });
    }
  }, [workspaceId, runtime]);

  return (
    <Stack>
      <Stack.Screen name="index" options={{ title: "Workspace" }} />
      <Stack.Screen name="boards/[boardId]" options={{ title: "Board" }} />
      <Stack.Screen name="docs/[documentId]" options={{ title: "Document" }} />
      <Stack.Screen name="automation/index" options={{ title: "Automation" }} />
    </Stack>
  );
}
