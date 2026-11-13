import type { Metadata } from "next";
import { cookies } from "next/headers";
import { IBM_Plex_Sans_Arabic, Inter } from "next/font/google";
import { AppProviders } from "@/components/app-providers";
import { AppChrome } from "@/components/app-chrome";
import { SkipToContent } from "@/components/site-chrome";
import "./globals.css";
import "@/styles/rigx/rigx-design-tokens.css";
import "@/styles/rigx/preview.css";
import "@/styles/rigx/v1.2-polish.css";
import "@/styles/rigx/v1.3-refinement.css";
import "@/styles/rigx/v1.4-services.css";
import "@/styles/rigx/v1.5-complete-footer-analytics.css";
import "@/styles/rigx/v1.6-commerce-admin.css";
import "@/styles/rigx/v1.9-complete.css";
import "@/styles/rigx/v1.9.1-owner-refinements.css";
import "./rigx-bridge.css";
import "./rigx-pages.css";

const arabicFont = IBM_Plex_Sans_Arabic({
  subsets: ["arabic", "latin"],
  weight: ["400", "500", "600", "700"],
  display: "swap",
  variable: "--font-arabic",
});

const latinFont = Inter({
  subsets: ["latin"],
  display: "swap",
  variable: "--font-latin",
});

export const metadata: Metadata = {
  metadataBase: new URL(process.env.NEXT_PUBLIC_SITE_URL ?? "http://127.0.0.1:3005"),
  title: "RIGX | PC Components",
  description: "RIGX متجر مكونات كمبيوتر ببيانات ومواصفات وأسعار واضحة.",
  applicationName: "RIGX",
};

export default async function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  const cookieStore = await cookies();
  const localeCookie = cookieStore.get("hardware-store-locale")?.value;
  const initialLocale = localeCookie === "ar" ? "ar" : "en";
  const themeCookie = cookieStore.get("hardware-store-theme")?.value;
  const hasThemePreference = themeCookie === "dark" || themeCookie === "light";
  const initialTheme = themeCookie === "light" ? "light" : "dark";
  const themeBootstrap = `
    (() => {
      const stored = localStorage.getItem("hardware-store-theme");
      const serverTheme = document.documentElement.dataset.theme;
      const theme = stored === "dark" || stored === "light"
        ? stored
        : (document.documentElement.dataset.themeLocked === "true" ? serverTheme : "dark");
      document.documentElement.dataset.theme = theme;
      document.documentElement.style.colorScheme = theme;
    })();
  `;

  return (
    <html
      lang={initialLocale}
      dir={initialLocale === "ar" ? "rtl" : "ltr"}
      data-theme={initialTheme}
      data-theme-locked={hasThemePreference ? "true" : "false"}
      data-scroll-behavior="smooth"
      className={`${arabicFont.variable} ${latinFont.variable}`}
      suppressHydrationWarning
    >
      <head>
        <script dangerouslySetInnerHTML={{ __html: themeBootstrap }} />
      </head>
      <body>
        <AppProviders initialLocale={initialLocale}>
          <SkipToContent />
          <AppChrome>{children}</AppChrome>
        </AppProviders>
      </body>
    </html>
  );
}
