import { defineRouting } from "next-intl/routing";

export const routing = defineRouting({
  locales: ["ar", "en"],
  defaultLocale: "ar",
  localePrefix: "as-needed",
  // Arabic first: the browser language never redirects; English only through the EN switch (when enabled in settings).
  localeDetection: false
});

export type Locale = (typeof routing.locales)[number];

export const localeDirection: Record<Locale, "rtl" | "ltr"> = { ar: "rtl", en: "ltr" };
