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
