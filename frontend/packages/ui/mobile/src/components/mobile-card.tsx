import React, { type ReactNode } from "react";
import { View, Text, StyleSheet } from "react-native";

export interface MobileCardProps {
  readonly id: string;
  readonly title: string;
  readonly subtitle?: string;
  readonly children?: ReactNode;
}

export function MobileCard({
  title,
  subtitle,
  children,
}: MobileCardProps): React.ReactNode {
  return (
    <View style={styles.card}>
      <Text style={styles.title}>{title}</Text>
      {subtitle ? <Text style={styles.subtitle}>{subtitle}</Text> : null}
      {children ? <View style={styles.content}>{children}</View> : null}
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    padding: 16,
    borderRadius: 12,
    backgroundColor: "#ffffff",
    borderWidth: 1,
    borderColor: "#e5e7eb",
    marginVertical: 6,
  },
  title: {
    fontSize: 18,
    fontWeight: "700",
    color: "#111827",
  },
  subtitle: {
    fontSize: 14,
    color: "#6b7280",
    marginTop: 4,
  },
  content: {
    marginTop: 12,
  },
});
