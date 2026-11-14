const numberFormat = new Intl.NumberFormat("en-EG", { maximumFractionDigits: 2 });

/** Western digits in both languages (prices are read as numbers); the currency label follows the locale. */
export function formatEgp(amount: number, locale: string): string {
  const value = numberFormat.format(amount);
  return locale === "ar" ? `${value} ج.م` : `EGP ${value}`;
}
