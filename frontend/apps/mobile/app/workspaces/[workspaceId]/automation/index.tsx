import React from "react";
import { useLocalSearchParams } from "expo-router";
import { MobileAutomationScreen } from "@notrelix/automation-mobile";

export default function AutomationRoute() {
  const { workspaceId } = useLocalSearchParams<{ workspaceId: string }>();

  return <MobileAutomationScreen workspaceId={workspaceId ?? ""} />;
}
