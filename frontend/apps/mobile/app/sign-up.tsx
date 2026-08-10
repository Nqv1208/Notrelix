import {
  View,
  Text,
  TextInput,
  TouchableOpacity,
  StyleSheet,
} from "react-native";
import { useState } from "react";
import { useRouter } from "expo-router";

export default function SignUpScreen() {
  const router = useRouter();
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Create account</Text>
      <Text style={styles.subtitle}>Start your free trial today</Text>

      <View style={styles.form}>
        <Text style={styles.label}>Name</Text>
        <TextInput
          style={styles.input}
          placeholder="Your name"
          value={name}
          onChangeText={setName}
        />

        <Text style={styles.label}>Email</Text>
        <TextInput
          style={styles.input}
          placeholder="you@company.com"
          value={email}
          onChangeText={setEmail}
          autoCapitalize="none"
          keyboardType="email-address"
        />

        <Text style={styles.label}>Password</Text>
        <TextInput
          style={styles.input}
          placeholder="Create a password"
          value={password}
          onChangeText={setPassword}
          secureTextEntry
        />

        <TouchableOpacity style={styles.button}>
          <Text style={styles.buttonText}>Create account</Text>
        </TouchableOpacity>
      </View>

      <TouchableOpacity onPress={() => router.push("/sign-in")}>
        <Text style={styles.link}>
          Already have an account? <Text style={styles.linkBold}>Sign in</Text>
        </Text>
      </TouchableOpacity>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, justifyContent: "center", padding: 20 },
  title: { fontSize: 24, fontWeight: "bold", marginBottom: 4 },
  subtitle: { fontSize: 14, color: "#666", marginBottom: 32 },
  form: { marginBottom: 24 },
  label: { fontSize: 14, fontWeight: "500", marginBottom: 4 },
  input: {
    borderWidth: 1,
    borderColor: "#d0d4e4",
    borderRadius: 8,
    padding: 12,
    marginBottom: 16,
    fontSize: 16,
  },
  button: {
    backgroundColor: "#6161ff",
    padding: 12,
    borderRadius: 8,
    alignItems: "center",
  },
  buttonText: { color: "white", fontWeight: "600", fontSize: 16 },
  link: { textAlign: "center", color: "#666" },
  linkBold: { fontWeight: "600", color: "#333" },
});
