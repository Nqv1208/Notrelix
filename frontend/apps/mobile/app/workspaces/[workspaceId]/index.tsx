import React from "react";
import { View, Text, StyleSheet } from "react-native";
import { useLocalSearchParams } from "expo-router";

export default function WorkspaceIndexRoute() {
  const { workspaceId } = useLocalSearchParams<{ workspaceId: string }>();

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Workspace Overview</Text>
      <Text style={styles.subtitle}>ID: {workspaceId}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: 16,
    backgroundColor: "#f9fafb",
  },
  title: {
    fontSize: 22,
    fontWeight: "bold",
  },
  subtitle: {
    fontSize: 14,
    color: "#6b7280",
    marginTop: 4,
  },
});
