const numberFormat = new Intl.NumberFormat("en-EG", { maximumFractionDigits: 2 });

/** Western digits in both languages (prices are read as numbers); the currency label follows the locale. */
export function formatEgp(amount: number, locale: string): string {
  const value = numberFormat.format(amount);
  return locale === "ar" ? `${value} ج.م` : `EGP ${value}`;
}

const CAIRO = "Africa/Cairo";
const calendarLocale = (locale: string) => (locale === "ar" ? "ar-EG-u-nu-latn" : "en-GB");

/** "نوفمبر" / "November": the month name only, in Cairo time. */
export function formatMonth(iso: string, locale: string): string {
  return new Intl.DateTimeFormat(calendarLocale(locale), { month: "long", timeZone: CAIRO }).format(new Date(iso));
}

/** "نوفمبر 2026" / "November 2026", in Cairo time. */
export function formatMonthYear(iso: string, locale: string): string {
  return new Intl.DateTimeFormat(calendarLocale(locale), { month: "long", year: "numeric", timeZone: CAIRO }).format(new Date(iso));
}

/** "الأحد" / "Sunday", in Cairo time. */
export function formatWeekday(iso: string, locale: string): string {
  return new Intl.DateTimeFormat(calendarLocale(locale), { weekday: "long", timeZone: CAIRO }).format(new Date(iso));
}

/** "14 نوفمبر 2026" / "14 November 2026", in Cairo time. */
export function formatDate(iso: string, locale: string): string {
  return new Intl.DateTimeFormat(calendarLocale(locale), { day: "numeric", month: "long", year: "numeric", timeZone: CAIRO }).format(new Date(iso));
}

type Parts = Intl.DateTimeFormatPart[];
const part = (parts: Parts, type: Intl.DateTimeFormatPartTypes) => parts.find((p) => p.type === type)?.value ?? "";

/** "ص" / "م" in Arabic, "AM" / "PM" in English. */
const dayPeriod = (parts: Parts, locale: string) => (locale === "ar" ? part(parts, "dayPeriod") : part(parts, "dayPeriod").toUpperCase());

function cairoParts(iso: string, locale: string, date: Intl.DateTimeFormatOptions): Parts {
  return new Intl.DateTimeFormat(calendarLocale(locale), {
    ...date, hour: "numeric", minute: "2-digit", hour12: true, timeZone: CAIRO
  }).formatToParts(new Date(iso));
}

const clock = (parts: Parts, locale: string) => `${part(parts, "hour")}:${part(parts, "minute")} ${dayPeriod(parts, locale)}`;

/**
 * The one time format for admin and storefront: 12-hour Cairo time with Western digits, "2:22 م" / "2:22 PM".
 * The date-time helpers below build on it, so every time on the site reads the same.
 */
export function formatTime(iso: string, locale: string): string {
  return clock(cairoParts(iso, locale, {}), locale);
}

/** "13/11 · 2:22 م" / "13/11 · 2:22 PM": day/month and 12-hour Cairo time. */
export function formatDateTime(iso: string, locale: string): string {
  const parts = cairoParts(iso, locale, { day: "numeric", month: "numeric" });
  return `${part(parts, "day")}/${part(parts, "month")} · ${clock(parts, locale)}`;
}

/** "13/11/2026 · 2:22 م": day/month/year and 12-hour Cairo time, as printed on an invoice. */
export function formatDateTimeFull(iso: string, locale: string): string {
  const parts = cairoParts(iso, locale, { day: "2-digit", month: "2-digit", year: "numeric" });
  return `${part(parts, "day")}/${part(parts, "month")}/${part(parts, "year")} · ${clock(parts, locale)}`;
}
