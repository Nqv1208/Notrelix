import type { Metadata } from "next";
import { NextIntlClientProvider } from "next-intl";
import { getMessages } from "next-intl/server";

import { MarketingFooter } from "../components/marketing-footer";
import { MarketingHeader } from "../components/marketing-header";
import { env } from "../config/env";
import { messages } from "../messages/en";
import "../styles/globals.css";

export const metadata: Metadata = {
  metadataBase: new URL(env.siteUrl),
  title: {
    default: messages.layout.title,
    template: "%s | Notrelix",
  },
  description: messages.layout.description,
  keywords: [...messages.layout.keywords],
  alternates: { canonical: "/" },
  openGraph: {
    title: messages.layout.ogTitle,
    description: messages.layout.description,
    type: "website",
    siteName: "Notrelix",
    url: env.siteUrl,
    locale: "en_US",
  },
  twitter: {
    card: "summary_large_image",
    title: messages.layout.ogTitle,
    description: messages.layout.description,
  },
};

const structuredData = {
  "@context": "https://schema.org",
  "@type": "SoftwareApplication",
  name: "Notrelix",
  applicationCategory: "BusinessApplication",
  operatingSystem: "Web",
  description: messages.layout.description,
  url: env.siteUrl,
  offers: { "@type": "Offer", price: "0", priceCurrency: "USD" },
};

export default async function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  const localeMessages = await getMessages();

  return (
    <html lang="en">
      <body>
        <NextIntlClientProvider messages={localeMessages}>
          <script
            type="application/ld+json"
            dangerouslySetInnerHTML={{ __html: JSON.stringify(structuredData) }}
          />
          <MarketingHeader />
          {children}
          <MarketingFooter />
        </NextIntlClientProvider>
      </body>
    </html>
  );
}
