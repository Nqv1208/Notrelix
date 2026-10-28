import React from "react";
import { Linking } from "react-native";
import { useLocalSearchParams, useRouter } from "expo-router";
import { MobileDocumentScreen } from "@notrelix/docs-mobile";
import { useDocument } from "../../../../src/docs/use-document";
import { createDocumentLinkHandlers } from "../../../../src/docs/document-links";

export default function DocumentRoute() {
  const { workspaceId = "", documentId = "" } = useLocalSearchParams<{
    workspaceId: string;
    documentId: string;
  }>();
  const router = useRouter();
  const doc = useDocument(workspaceId, documentId);
  const links = createDocumentLinkHandlers({
    workspaceId,
    openUrl: (url) => Linking.openURL(url),
    push: (route) => router.push(route as never),
  });

  const status = doc.isLoading
    ? ({ kind: "loading" } as const)
    : doc.noAccess
      ? ({ kind: "forbidden" } as const)
      : doc.failed
        ? ({ kind: "error", onRetry: doc.retry } as const)
        : undefined;

  return (
    <MobileDocumentScreen
      workspaceId={workspaceId}
      documentId={documentId}
      blocks={doc.blocks}
      title={doc.title}
      status={status}
      reader={links}
    />
  );
}
