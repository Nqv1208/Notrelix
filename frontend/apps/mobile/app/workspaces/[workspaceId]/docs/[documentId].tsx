import React from "react";
import { useLocalSearchParams } from "expo-router";
import { MobileDocumentScreen } from "@notrelix/docs-mobile";

export default function DocumentRoute() {
  const { workspaceId, documentId } = useLocalSearchParams<{
    workspaceId: string;
    documentId: string;
  }>();

  return (
    <MobileDocumentScreen
      workspaceId={workspaceId ?? ""}
      documentId={documentId ?? ""}
    />
  );
}
