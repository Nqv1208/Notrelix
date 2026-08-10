import { View, Text, TouchableOpacity } from "react-native";
import { useRouter } from "expo-router";

export default function IndexScreen() {
  const router = useRouter();

  return (
    <View
      style={{
        flex: 1,
        justifyContent: "center",
        alignItems: "center",
        padding: 20,
      }}
    >
      <Text style={{ fontSize: 32, fontWeight: "bold", marginBottom: 8 }}>
        Notrelix
      </Text>
      <Text
        style={{
          fontSize: 16,
          color: "#666",
          marginBottom: 32,
          textAlign: "center",
        }}
      >
        Write like Notion. Plan like Trello. Ship like a pro.
      </Text>
      <TouchableOpacity
        onPress={() => router.push("/sign-in")}
        style={{
          backgroundColor: "#6161ff",
          paddingHorizontal: 32,
          paddingVertical: 12,
          borderRadius: 8,
          marginBottom: 12,
        }}
      >
        <Text style={{ color: "white", fontWeight: "600" }}>Sign In</Text>
      </TouchableOpacity>
      <TouchableOpacity
        onPress={() => router.push("/sign-up")}
        style={{
          borderWidth: 1,
          borderColor: "#d0d4e4",
          paddingHorizontal: 32,
          paddingVertical: 12,
          borderRadius: 8,
        }}
      >
        <Text style={{ fontWeight: "600" }}>Create Account</Text>
      </TouchableOpacity>
    </View>
  );
}
