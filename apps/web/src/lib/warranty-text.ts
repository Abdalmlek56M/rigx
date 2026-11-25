/**
 * The warranty as a customer reads it beside an item: "ضمان 3 شهور", not a bare "3 شهور". The owner writes the labels in the settings, so
 * a label that already names the warranty ("ضمان الوكيل", "Agent warranty") is kept as written; `withWord` adds the word to any other.
 * An empty label is no warranty line at all (`null`).
 */
export function warrantyText(label: string | null | undefined, withWord: (label: string) => string): string | null {
  const clean = label?.trim();
  if (!clean) return null;
  return /ضمان|warranty/i.test(clean) ? clean : withWord(clean);
}
