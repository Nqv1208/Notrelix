import { parseEnv } from "@notrelix/runtime-mobile";

export const env = parseEnv({
  EXPO_PUBLIC_API_URL: process.env.EXPO_PUBLIC_API_URL,
  EXPO_PUBLIC_REALTIME_URL: process.env.EXPO_PUBLIC_REALTIME_URL,
  EXPO_PUBLIC_APP_URL: process.env.EXPO_PUBLIC_APP_URL,
  EXPO_PUBLIC_RELEASE_SHA: process.env.EXPO_PUBLIC_RELEASE_SHA,
});
