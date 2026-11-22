/** The pure parts of turning the quote card into a PNG (the browser parts are in `quote-image.ts`). */

/** The first family of a computed `font-family` value, without quotes: `"IBM Plex Sans", sans-serif` → `IBM Plex Sans`. */
export function firstFontFamily(value: string): string {
  const first = value.split(",")[0]?.trim() ?? "";
  return first.replace(/^["']|["']$/g, "");
}

/** The address inside the `src` of a font face (`url("/a.woff2") format("woff2")` → `/a.woff2`); null when there is none. */
export function fontFaceUrl(src: string): string | null {
  return /url\(\s*["']?([^"')]+?)["']?\s*\)/.exec(src)?.[1] ?? null;
}

export interface EmbeddedFace {
  family: string;
  weight: string;
  style: string;
  unicodeRange: string;
  /** The font file as a data address (`data:font/woff2;base64,...`). */
  data: string;
}

/** One `@font-face` rule with its file inside it: an image made from SVG cannot load files, so the fonts must travel with it. */
export function fontFaceCss(face: EmbeddedFace): string {
  const range = face.unicodeRange ? `unicode-range:${face.unicodeRange};` : "";
  return `@font-face{font-family:"${face.family}";font-weight:${face.weight};font-style:${face.style};${range}src:url(${face.data});}`;
}

/** An SVG that draws an HTML fragment (`markup`, serialised as XHTML) at the given size, with `css` for the fonts. */
export function wrapInSvg(markup: string, css: string, width: number, height: number): string {
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}" viewBox="0 0 ${width} ${height}">` +
    `<style>${css}</style><foreignObject x="0" y="0" width="${width}" height="${height}">${markup}</foreignObject></svg>`;
}

/** The file name of the saved picture: `rigx-<kind>-<label>.png` (for example `rigx-invoice-INV-2026-0012.png`); a label with no Latin letters or digits is left out. */
export function imageFileName(kind: string, label: string): string {
  const safe = label.replace(/[^\w-]+/g, "-").replace(/^-+|-+$/g, "").slice(0, 40);
  return `rigx-${kind}${safe ? `-${safe}` : ""}.png`;
}
