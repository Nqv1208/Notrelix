import React from "react";

export const View = (props: Record<string, unknown>) =>
  React.createElement("div", props);
export const Text = (props: Record<string, unknown>) =>
  React.createElement("span", props);
export const TouchableOpacity = (props: Record<string, unknown>) =>
  React.createElement("button", props);
export const TextInput = (props: Record<string, unknown>) =>
  React.createElement("input", props);
export const StyleSheet = {
  create: <T extends Record<string, unknown>>(styles: T): T => styles,
};

export default {
  View,
  Text,
  TouchableOpacity,
  TextInput,
  StyleSheet,
};
