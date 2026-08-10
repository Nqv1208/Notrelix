import React from "react";
import { TextInput, StyleSheet } from "react-native";

export interface MobileInputProps {
  readonly value: string;
  readonly onChangeText?: (text: string) => void;
  readonly placeholder?: string;
  readonly disabled?: boolean;
}

export function MobileInput({
  value,
  onChangeText,
  placeholder,
  disabled = false,
}: MobileInputProps): React.ReactNode {
  return (
    <TextInput
      value={value}
      onChangeText={onChangeText}
      placeholder={placeholder}
      editable={!disabled}
      style={[styles.input, disabled && styles.disabled]}
    />
  );
}

const styles = StyleSheet.create({
  input: {
    height: 44,
    paddingHorizontal: 12,
    borderWidth: 1,
    borderColor: "#d1d5db",
    borderRadius: 8,
    backgroundColor: "#ffffff",
    fontSize: 16,
    color: "#111827",
  },
  disabled: {
    backgroundColor: "#f3f4f6",
    color: "#9ca3af",
  },
});
