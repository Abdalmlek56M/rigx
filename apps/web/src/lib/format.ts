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

/** "14 نوفمبر 2026" / "14 November 2026", in Cairo time. */
export function formatDate(iso: string, locale: string): string {
  return new Intl.DateTimeFormat(calendarLocale(locale), { day: "numeric", month: "long", year: "numeric", timeZone: CAIRO }).format(new Date(iso));
}

/** "13/11 · 14:22": day/month and 24-hour time in Cairo (Western digits). */
export function formatDateTime(iso: string, locale: string): string {
  const parts = new Intl.DateTimeFormat(calendarLocale(locale), {
    day: "numeric", month: "numeric", hour: "2-digit", minute: "2-digit", hourCycle: "h23", timeZone: CAIRO
  }).formatToParts(new Date(iso));
  const part = (type: string) => parts.find((p) => p.type === type)?.value ?? "";
  return `${part("day")}/${part("month")} · ${part("hour")}:${part("minute")}`;
}

/** "13/11/2026 · 14:22": day/month/year and 24-hour time in Cairo (Western digits), as printed on an invoice. */
export function formatDateTimeFull(iso: string, locale: string): string {
  const parts = new Intl.DateTimeFormat(calendarLocale(locale), {
    day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit", hourCycle: "h23", timeZone: CAIRO
  }).formatToParts(new Date(iso));
  const part = (type: string) => parts.find((p) => p.type === type)?.value ?? "";
  return `${part("day")}/${part("month")}/${part("year")} · ${part("hour")}:${part("minute")}`;
}
