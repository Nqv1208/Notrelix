import React from "react";

export const View = (props: any) => React.createElement("div", props);
export const Text = (props: any) => React.createElement("span", props);
export const TouchableOpacity = (props: any) =>
  React.createElement("button", props);
export const TextInput = (props: any) => React.createElement("input", props);
export const StyleSheet = {
  create: (styles: any) => styles,
};

export default {
  View,
  Text,
  TouchableOpacity,
  TextInput,
  StyleSheet,
};
