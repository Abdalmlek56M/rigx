import type { Metadata, Viewport } from "next";
import { Alexandria, IBM_Plex_Mono, IBM_Plex_Sans_Arabic } from "next/font/google";
import { cookies } from "next/headers";
import { notFound } from "next/navigation";
import { hasLocale, NextIntlClientProvider } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import type { ReactNode } from "react";
import { Analytics } from "@/components/analytics/Analytics";
import { THEME_COOKIE, THEME_INIT_SCRIPT, parseTheme } from "@/components/layout/theme-script";
import { localeDirection, routing } from "@/i18n/routing";
import { openGraphBase } from "@/lib/seo/page-seo";
import { siteOrigin } from "@/lib/seo/urls";
import "../globals.css";
import "../blueprint.css";

const sans = IBM_Plex_Sans_Arabic({
  subsets: ["arabic", "latin"],
  weight: ["400", "500", "700"],
  variable: "--font-plex-sans",
  display: "swap"
});

/** Display face for page and section headings (h1/h2): geometric Arabic + Latin that matches the wordmark. */
const display = Alexandria({
  subsets: ["arabic", "latin"],
  weight: ["600", "700"],
  variable: "--font-display-face",
  display: "swap"
});

const mono = IBM_Plex_Mono({
  subsets: ["latin"],
  weight: ["500", "600"],
  variable: "--font-plex-mono",
  display: "swap"
});

/** `viewportFit: "cover"` lets sticky bars pad themselves with the safe-area insets (notch, home bar). */
export const viewport: Viewport = {
  themeColor: [
    { media: "(prefers-color-scheme: light)", color: "#FFFFFF" },
    { media: "(prefers-color-scheme: dark)", color: "#17191D" }
  ],
  viewportFit: "cover"
};

export function generateStaticParams() {
  return routing.locales.map((locale) => ({ locale }));
}

export async function generateMetadata({ params }: { params: Promise<{ locale: string }> }): Promise<Metadata> {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) return {};
  const t = await getTranslations({ locale, namespace: "meta" });
  return {
    metadataBase: new URL(siteOrigin()),
    title: { default: t("title"), template: "%s · RIGX" },
    description: t("description"),
    openGraph: openGraphBase(locale),
    twitter: { card: "summary_large_image" }
  };
}

export default async function LocaleLayout({ children, params }: { children: ReactNode; params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) notFound();
  setRequestLocale(locale);
  // The theme is written by the server (from its cookie), so a page that is swapped in by a language switch is never light for a moment;
  // the head script stays as the fallback for a first visit that only has the old local-storage choice.
  const theme = parseTheme((await cookies()).get(THEME_COOKIE)?.value);

  return (
    <html lang={locale} dir={localeDirection[locale]} data-theme={theme} className={`${sans.variable} ${display.variable} ${mono.variable}`} suppressHydrationWarning>
      <head>
        <script dangerouslySetInnerHTML={{ __html: THEME_INIT_SCRIPT }} />
      </head>
      <body>
        <NextIntlClientProvider>
          {children}
        </NextIntlClientProvider>
        <Analytics />
      </body>
    </html>
  );
}
