import React from "react";
import { View, Text, StyleSheet } from "react-native";

export interface MobileAutomationScreenProps {
  readonly workspaceId: string;
}

export function MobileAutomationScreen({
  workspaceId,
}: MobileAutomationScreenProps): React.ReactNode {
  return (
    <View style={styles.container}>
      <Text style={styles.title}>Automation Rules</Text>
      <Text style={styles.detail}>Workspace: {workspaceId}</Text>
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
