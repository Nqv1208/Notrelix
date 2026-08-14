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
    <html lang="en" suppressHydrationWarning>
      <body>
        <script
          dangerouslySetInnerHTML={{
            __html: `(function(){
              try {
                var stored = localStorage.getItem("theme");
                var theme = stored === "dark" || stored === "light" ? stored : "light";
                var root = document.documentElement;
                root.classList.remove("light", "dark");
                root.classList.add(theme);
                root.style.colorScheme = theme;
              } catch (e) {
                var root = document.documentElement;
                root.classList.add("light");
                root.style.colorScheme = "light";
              }
            })();`,
          }}
        />
        <NextIntlClientProvider messages={localeMessages}>
          <script
            type="application/ld+json"
            dangerouslySetInnerHTML={{ __html: JSON.stringify(structuredData) }}
          />
          <div
            id="header-scroll-sentinel"
            aria-hidden="true"
            className="absolute top-0 left-0 h-10 w-full pointer-events-none"
          />
          <MarketingHeader />
          {children}
          <MarketingFooter />
        </NextIntlClientProvider>
      </body>
    </html>
  );
}
