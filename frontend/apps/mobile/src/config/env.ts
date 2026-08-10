import { parseEnv } from "@notrelix/runtime-mobile";

const mode =
  process.env.NODE_ENV === "production"
    ? "production"
    : process.env.NODE_ENV === "test"
      ? "test"
      : "development";

export const env = parseEnv({
  mode,
  apiUrl: process.env.EXPO_PUBLIC_API_URL,
  realtimeUrl: process.env.EXPO_PUBLIC_REALTIME_URL,
  appUrl: process.env.EXPO_PUBLIC_APP_URL,
  releaseSha: process.env.EXPO_PUBLIC_RELEASE_SHA,
});
