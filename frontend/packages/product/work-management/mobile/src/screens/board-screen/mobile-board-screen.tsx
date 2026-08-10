import React from "react";
import { View, Text, StyleSheet } from "react-native";

export interface MobileBoardScreenProps {
  boardId: string;
  workspaceId: string;
}

export function MobileBoardScreen({
  boardId,
  workspaceId,
}: MobileBoardScreenProps) {
  return (
    <View style={styles.container}>
      <Text style={styles.title}>Board: {boardId}</Text>
      <Text style={styles.subtitle}>Workspace: {workspaceId}</Text>
      {/* TODO: Implement mobile board list view */}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, padding: 16 },
  title: { fontSize: 18, fontWeight: "bold" },
  subtitle: { fontSize: 14, color: "#6b7280", marginTop: 4 },
});
