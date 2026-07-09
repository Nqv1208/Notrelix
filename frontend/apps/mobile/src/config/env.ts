import Constants from 'expo-constants';

export const env = {
  apiUrl: Constants.expoConfig?.extra?.apiUrl || 'https://api.notrelix.com',
  realtimeUrl: Constants.expoConfig?.extra?.realtimeUrl || 'https://api.notrelix.com/realtime',
  webUrl: Constants.expoConfig?.extra?.webUrl || 'https://app.notrelix.com',
} as const;
