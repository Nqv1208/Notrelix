import React from "react";
import { View, Text, StyleSheet } from "react-native";

export function MobileApp() {
  return (
    <View style={styles.container}>
      <Text style={styles.title}>Notrelix Mobile</Text>
      <Text style={styles.description}>
        Mobile app placeholder — implemented with Expo/React Native primitives.
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    padding: 16,
    backgroundColor: "#ffffff",
  },
  title: {
    fontSize: 20,
    fontWeight: "bold",
    marginBottom: 8,
  },
  description: {
    fontSize: 14,
    color: "#4b5563",
  },
});
