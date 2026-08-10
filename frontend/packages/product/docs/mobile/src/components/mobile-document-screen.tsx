import React from "react";
import { View, Text, StyleSheet } from "react-native";

export interface MobileDocumentScreenProps {
  readonly workspaceId: string;
  readonly documentId: string;
}

export function MobileDocumentScreen({
  workspaceId,
  documentId,
}: MobileDocumentScreenProps): React.ReactNode {
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
