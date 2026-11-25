/** The line at the foot of every printed page, written into the A4 invoice's page margin (browsers that support margin boxes print it). */

/** A text as a CSS string; `<` and `>` are escaped too, so the text can never close the `<style>` element it travels in. */
export function cssString(text: string): string {
  return `"${text.replace(/[\\"<>\n\r]/g, (character) => `\\${character.charCodeAt(0).toString(16)} `)}"`;
}

/** "صفحة {page} من {pages}" as a CSS `content` value with the browser's page counters in place of the two marks. */
export function pageCounterContent(pattern: string): string {
  return pattern
    .split(/(\{page\}|\{pages\})/)
    .filter(Boolean)
    .map((piece) => (piece === "{page}" ? "counter(page)" : piece === "{pages}" ? "counter(pages)" : cssString(piece)))
    .join(" ");
}

/** The print rule for the invoice's page margin: the label on the reading side, the page counter opposite. */
export function invoicePageCss(label: string, pattern: string, direction: "rtl" | "ltr"): string {
  const [start, end] = direction === "rtl" ? ["right", "left"] : ["left", "right"];
  const box = `font:9px sans-serif;color:#4f535a;direction:${direction}`;
  return `@media print{@page invoice{@bottom-${start}{content:${cssString(label)};${box}}@bottom-${end}{content:${pageCounterContent(pattern)};${box}}}}`;
}
