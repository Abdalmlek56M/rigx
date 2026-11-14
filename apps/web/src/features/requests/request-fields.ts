const ARABIC_DIGITS = "٠١٢٣٤٥٦٧٨٩";
const EGYPT_MOBILE = /^01[0125]\d{8}$/;
const EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/** Western digits, no spaces or dashes; "+20" / "0020" prefixes become the local leading 0. */
export function normalizePhone(value: string): string {
  const digits = value.replace(/[٠-٩]/g, (d) => String(ARABIC_DIGITS.indexOf(d))).replace(/[^\d+]/g, "");
  return digits.replace(/^(\+|00)20/, "0");
}

export function isEgyptMobile(value: string): boolean {
  return EGYPT_MOBILE.test(normalizePhone(value));
}

export function isEmail(value: string): boolean {
  return EMAIL.test(value.trim());
}
