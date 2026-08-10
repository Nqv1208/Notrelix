import React from "react";
import { useLocalSearchParams } from "expo-router";
import { MobileWorkspaceHome } from "@notrelix/work-management-mobile";

export default function WorkspaceIndexRoute() {
  const { workspaceId } = useLocalSearchParams<{ workspaceId: string }>();

  return <MobileWorkspaceHome workspaceId={workspaceId ?? ""} />;
}
