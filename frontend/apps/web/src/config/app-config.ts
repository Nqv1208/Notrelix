import { env } from './env';

export const appConfig = {
  api: {
    baseUrl: env.apiUrl,
  },
  realtime: {
    url: env.wsUrl,
  },
  urls: {
    app: env.appUrl,
    marketing: env.marketingUrl,
  },
} as const;
