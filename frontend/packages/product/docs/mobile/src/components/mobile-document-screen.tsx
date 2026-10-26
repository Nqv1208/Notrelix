import React from "react";
import { View, Text, ScrollView, StyleSheet } from "react-native";
import type { Block } from "@notrelix/docs-core";
import {
  MobileDocRenderer,
  type MobileDocRendererProps,
} from "./mobile-doc-renderer";

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
}

export function MobileDocumentScreen({
  workspaceId,
  documentId,
  blocks,
  title,
  reader,
}: MobileDocumentScreenProps): React.ReactNode {
  if (blocks) {
    return (
      <ScrollView style={styles.container}>
        {title ? (
          <Text style={styles.title} accessibilityRole="header">
            {title}
          </Text>
        ) : null}
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

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: 16,
    backgroundColor: "#ffffff",
  },
  title: {
    fontSize: 20,
    fontWeight: "bold",
    marginBottom: 8,
  },
  detail: {
    fontSize: 14,
    color: "#6b7280",
    marginTop: 4,
  },
});
