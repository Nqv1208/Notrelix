import "@/styles/globals.css"
import { ThemeProvider } from "@/components/theme-provider"
import { Poppins, Inter } from "next/font/google"
import { QueryProvider } from "./providers"
import { NextIntlClientProvider } from "next-intl"
import { getLocale, getMessages } from "next-intl/server"
import { colorThemeScript } from "@/lib/theme"

const poppins = Poppins({
  subsets: ["latin", "latin-ext"],
  weight: ["400", "500", "600", "700"],
  variable: "--font-poppins",
  display: "swap",
})

const inter = Inter({
  subsets: ["latin", "latin-ext"],
  weight: ["400", "500", "600"],
  variable: "--font-inter",
  display: "swap",
})


export default async function RootLayout({
  children,
}: {
  children: React.ReactNode
}) {
  const [locale, messages] = await Promise.all([getLocale(), getMessages()])

  return (
    <html lang={locale} suppressHydrationWarning className={`${poppins.variable} ${inter.variable}`}>
      <head>
        <script dangerouslySetInnerHTML={{ __html: colorThemeScript }} />
      </head>
      <body className={inter.className}>
        <NextIntlClientProvider locale={locale} messages={messages}>
          <QueryProvider>
            <ThemeProvider
              attribute="class"
              defaultTheme="system"
              enableSystem
            >
              {children}
            </ThemeProvider>
          </QueryProvider>
        </NextIntlClientProvider>
      </body>
    </html>
  )
}
