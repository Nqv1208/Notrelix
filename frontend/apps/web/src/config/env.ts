export const env = {
  apiUrl: import.meta.env.VITE_API_URL || 'http://localhost:8000/api/v1',
  realtimeUrl: import.meta.env.VITE_REALTIME_URL || 'http://localhost:8000/realtime',
  appUrl: import.meta.env.VITE_APP_URL || 'http://localhost:5173',
  marketingUrl: import.meta.env.VITE_MARKETING_URL || 'http://localhost:3001',
} as const;
