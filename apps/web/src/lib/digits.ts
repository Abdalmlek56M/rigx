const ARABIC_ZERO = 0x0660;
const PERSIAN_ZERO = 0x06f0;

/**
 * A typed number as the browser and API read it: Arabic-Indic (٠-٩) and Persian (۰-۹) digits become 0-9, the Arabic decimal mark (٫)
 * becomes a dot, and anything else that cannot be part of a number is dropped. An Arabic keyboard otherwise types digits a number field ignores.
 */
export function latinNumber(value: string): string {
  return value
    .replace(/[٠-٩]/g, (digit) => String(digit.charCodeAt(0) - ARABIC_ZERO))
    .replace(/[۰-۹]/g, (digit) => String(digit.charCodeAt(0) - PERSIAN_ZERO))
    .replace(/[٫,]/g, ".")
    .replace(/[^0-9.-]/g, "");
}
