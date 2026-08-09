import React from "react";
import { View, Text, StyleSheet } from "react-native";
import { useLocalSearchParams } from "expo-router";

export default function BoardRoute() {
  const { workspaceId, boardId } = useLocalSearchParams<{
    workspaceId: string;
    boardId: string;
  }>();

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Board Route</Text>
      <Text style={styles.detail}>Workspace: {workspaceId}</Text>
      <Text style={styles.detail}>Board: {boardId}</Text>
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
  },
  detail: {
    fontSize: 14,
    color: "#6b7280",
    marginTop: 4,
  },
});
