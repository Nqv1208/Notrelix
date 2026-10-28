export const env = {
  // Local defaults point at the dev nginx gateway (3080), which serves both
  // the marketing site and the web app; deployments set the env vars.
  siteUrl: process.env.NEXT_PUBLIC_SITE_URL || "http://localhost:3080",
  webAppUrl: process.env.NEXT_PUBLIC_WEB_APP_URL || "http://localhost:3080",
  apiUrl: process.env.NEXT_PUBLIC_API_URL || "http://localhost:8000",
} as const;
