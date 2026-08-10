import React from "react";
import { View, Text, StyleSheet } from "react-native";

export interface MobileWorkspaceHomeProps {
  workspaceId: string;
}

export function MobileWorkspaceHome({ workspaceId }: MobileWorkspaceHomeProps) {
  return (
    <View style={styles.container}>
      <Text style={styles.title}>Workspace Home</Text>
      <Text style={styles.subtitle}>Workspace: {workspaceId}</Text>
      {/* TODO: Implement mobile workspace home with board list */}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, padding: 16 },
  title: { fontSize: 22, fontWeight: "bold" },
  subtitle: { fontSize: 14, color: "#6b7280", marginTop: 4 },
});
