/** The name to show for a store category or governorate in the visitor's language. */
export function localizedName(item: { name: string; nameEn?: string | null }, locale: string): string {
  if (locale !== "en") return item.name;
  const english = item.nameEn?.trim();
  return english || item.name;
}
