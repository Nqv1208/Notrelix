import React from "react";
import { useLocalSearchParams } from "expo-router";
import { MobileBoardScreen } from "@notrelix/work-management-mobile";

export default function BoardRoute() {
  const { workspaceId, boardId } = useLocalSearchParams<{
    workspaceId: string;
    boardId: string;
  }>();

  return (
    <MobileBoardScreen
      boardId={boardId ?? ""}
      workspaceId={workspaceId ?? ""}
    />
  );
}
