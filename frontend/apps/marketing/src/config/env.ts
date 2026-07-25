export const env = {
  siteUrl: process.env.NEXT_PUBLIC_SITE_URL || 'http://localhost:3000',
  webAppUrl: process.env.NEXT_PUBLIC_WEB_APP_URL || 'http://localhost:5173',
  apiUrl: process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8000',
} as const;
