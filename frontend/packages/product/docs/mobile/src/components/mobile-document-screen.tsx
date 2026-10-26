import React from "react";
import {
  View,
  Text,
  ScrollView,
  StyleSheet,
  TouchableOpacity,
} from "react-native";
import type { Block } from "@notrelix/docs-core";
import {
  MobileDocRenderer,
  type MobileDocRendererProps,
} from "./mobile-doc-renderer";
import { DEFAULT_DOC_LABELS, type MobileDocLabels } from "../render/doc-labels";
import { DOC_COLORS } from "../render/doc-theme";

/**
 * Where the host's read of the page stands. `forbidden` covers both a page the
 * viewer may not see and a page that does not exist: the screen never says
 * which (the producer answers both the same way).
 */
export type MobileDocumentStatus =
  | { readonly kind: "loading" }
  | { readonly kind: "forbidden" }
  | { readonly kind: "error"; readonly onRetry?: () => void };

export interface MobileDocumentScreenProps {
  readonly workspaceId: string;
  readonly documentId: string;
  /**
   * The page's blocks, read by the host (this package does not own server
   * state). When given the page is shown read-only; without them the screen
   * only names the document.
   */
  readonly blocks?: readonly Block[];
  readonly title?: string;
  readonly reader?: Omit<MobileDocRendererProps, "blocks">;
  /** A read that is not finished or failed; wins over `blocks`. */
  readonly status?: MobileDocumentStatus;
}

export function MobileDocumentScreen({
  workspaceId,
  documentId,
  blocks,
  title,
  reader,
  status,
}: MobileDocumentScreenProps): React.ReactNode {
  const labels: MobileDocLabels = { ...DEFAULT_DOC_LABELS, ...reader?.labels };
  if (status)
    return <MobileDocumentStatusView status={status} labels={labels} />;
  if (blocks) {
    return (
      <ScrollView style={styles.container}>
        <Text style={styles.title} accessibilityRole="header">
          {title || labels.untitled}
        </Text>
        <MobileDocRenderer blocks={blocks} {...reader} />
      </ScrollView>
    );
  }
  return (
    <View style={styles.container}>
      <Text style={styles.title}>Document View</Text>
      <Text style={styles.detail}>Workspace: {workspaceId}</Text>
      <Text style={styles.detail}>Document: {documentId}</Text>
    </View>
  );
}

function MobileDocumentStatusView({
  status,
  labels,
}: {
  status: MobileDocumentStatus;
  labels: MobileDocLabels;
}): React.ReactNode {
  if (status.kind === "loading") {
    return (
      <View
        style={styles.centered}
        accessibilityRole="progressbar"
        accessibilityLabel={labels.loading}
      >
        <Text style={styles.detail}>{labels.loading}</Text>
      </View>
    );
  }
  if (status.kind === "forbidden") {
    return (
      <View style={styles.centered}>
        <Text style={styles.detail} accessibilityRole="alert">
          {labels.noAccess}
        </Text>
      </View>
    );
  }
  return (
    <View style={styles.centered}>
      <Text style={styles.detail} accessibilityRole="alert">
        {labels.loadError}
      </Text>
      {status.onRetry ? (
        <TouchableOpacity
          style={styles.retry}
          onPress={status.onRetry}
          accessibilityRole="button"
        >
          <Text style={styles.retryText}>{labels.retry}</Text>
        </TouchableOpacity>
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: 16,
    backgroundColor: "#ffffff",
  },
  centered: {
    flex: 1,
    padding: 16,
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: "#ffffff",
  },
  title: {
    fontSize: 20,
    fontWeight: "bold",
    marginBottom: 8,
  },
  detail: {
    fontSize: 14,
    color: DOC_COLORS.muted,
    marginTop: 4,
    textAlign: "center",
  },
  retry: {
    marginTop: 12,
    paddingVertical: 8,
    paddingHorizontal: 16,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: DOC_COLORS.line,
  },
  retryText: {
    fontSize: 14,
    color: DOC_COLORS.link,
  },
});
