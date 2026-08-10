import { Stack } from "expo-router";
import { MobileAppProviders } from "../src/providers/mobile-app-providers";

export default function RootLayout() {
  return (
    <MobileAppProviders>
      <Stack>
        <Stack.Screen name="index" options={{ title: "Notrelix" }} />
        <Stack.Screen name="sign-in" options={{ title: "Sign In" }} />
        <Stack.Screen name="sign-up" options={{ title: "Sign Up" }} />
        <Stack.Screen
          name="workspaces/[workspaceId]"
          options={{ headerShown: false }}
        />
      </Stack>
    </MobileAppProviders>
  );
}
