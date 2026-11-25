/** The owner's description of an item (the item form): shown on its page in the page's language and used for the page's meta description. */

/** About the length search results show. */
const META_LENGTH = 160;

/** The description written in this language, or `null`; the other language's text is never shown in its place. */
export function productDescription(item: { description?: string | null; descriptionEn?: string | null }, locale: string): string | null {
  const text = (locale === "en" ? item.descriptionEn : item.description)?.trim();
  return text ? text : null;
}

/** A blank line starts a new paragraph; single line breaks stay inside one. */
export const descriptionParagraphs = (text: string): string[] =>
  text.split(/\n\s*\n/).map((part) => part.trim()).filter(Boolean);

/** The text on one line, cut at a whole word when it is longer than a search result shows. */
export function metaDescription(text: string, length = META_LENGTH): string {
  const line = text.replace(/\s+/g, " ").trim();
  if (line.length <= length) return line;
  const cut = line.slice(0, length);
  const space = cut.lastIndexOf(" ");
  return `${(space > length / 2 ? cut.slice(0, space) : cut.slice(0, length - 1)).replace(/[\s،,.؛;:-]+$/, "")}…`;
}
