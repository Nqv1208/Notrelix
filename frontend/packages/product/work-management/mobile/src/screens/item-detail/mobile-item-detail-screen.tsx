import React from "react";
import { View, Text, StyleSheet } from "react-native";

export interface MobileItemDetailScreenProps {
  itemId: string;
  boardId: string;
}

export function MobileItemDetailScreen({
  itemId,
  boardId,
}: MobileItemDetailScreenProps) {
  return (
    <View style={styles.container}>
      <Text style={styles.title}>Item: {itemId}</Text>
      <Text style={styles.subtitle}>Board: {boardId}</Text>
      {/* TODO: Implement mobile item detail with bottom-sheet editors */}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, padding: 16 },
  title: { fontSize: 18, fontWeight: "bold" },
  subtitle: { fontSize: 14, color: "#6b7280", marginTop: 4 },
});
