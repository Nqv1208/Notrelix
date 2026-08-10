import type { Metadata } from "next";

import { MarketingFooter } from "../components/marketing-footer";
import { MarketingHeader } from "../components/marketing-header";
import { env } from "../config/env";
import "../styles/globals.css";

const description =
  "Notrelix kết nối tài liệu, Board, quy trình và dữ liệu trong một workspace duy nhất để đội ngũ biết việc gì cần làm tiếp theo.";

export const metadata: Metadata = {
  metadataBase: new URL(env.siteUrl),
  title: {
    default: "Notrelix | Không gian làm việc cho đội ngũ hiện đại",
    template: "%s | Notrelix",
  },
  description,
  keywords: [
    "work OS",
    "quản lý công việc",
    "workspace",
    "board",
    "tài liệu đội ngũ",
    "tự động hóa quy trình",
  ],
  alternates: { canonical: "/" },
  openGraph: {
    title: "Notrelix | Từ ý tưởng đến kết quả rõ ràng",
    description,
    type: "website",
    siteName: "Notrelix",
    url: env.siteUrl,
    locale: "vi_VN",
  },
  twitter: {
    card: "summary_large_image",
    title: "Notrelix | Từ ý tưởng đến kết quả rõ ràng",
    description,
  },
};

const structuredData = {
  "@context": "https://schema.org",
  "@type": "SoftwareApplication",
  name: "Notrelix",
  applicationCategory: "BusinessApplication",
  operatingSystem: "Web",
  description,
  url: env.siteUrl,
  offers: { "@type": "Offer", price: "0", priceCurrency: "USD" },
};

export default function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="vi">
      <body>
        <script
          type="application/ld+json"
          dangerouslySetInnerHTML={{ __html: JSON.stringify(structuredData) }}
        />
        <MarketingHeader />
        {children}
        <MarketingFooter />
      </body>
    </html>
  );
}
