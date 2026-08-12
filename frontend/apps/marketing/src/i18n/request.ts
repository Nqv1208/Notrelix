import { getRequestConfig } from "next-intl/server";

import { routing } from "./routing";

export default getRequestConfig(async ({ locale = routing.defaultLocale }) => ({
  locale,
  messages: (await import(`../messages/${locale}.ts`)).messages,
  ...routing,
}));
