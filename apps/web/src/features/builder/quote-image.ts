import { firstFontFamily, fontFaceCss, fontFaceUrl, wrapInSvg, type EmbeddedFace } from "./quote-image-svg";

export type PngDelivery = "copied" | "saved";

/**
 * Copies the computed style of every element of `source` onto its twin in `clone`, so the picture needs none of the page's stylesheets
 * (custom properties are resolved already and left out). The font families met on the way are added to `families`.
 */
function inlineStyles(source: Element, clone: Element, families: Set<string>): void {
  const computed = getComputedStyle(source);
  const target = (clone as HTMLElement).style;
  for (const name of Array.from(computed)) {
    if (!name.startsWith("--")) target.setProperty(name, computed.getPropertyValue(name), computed.getPropertyPriority(name));
  }
  families.add(firstFontFamily(computed.fontFamily));
  Array.from(source.children).forEach((child, index) => {
    const twin = clone.children[index];
    if (twin) inlineStyles(child, twin, families);
  });
}

function readAsDataUrl(blob: Blob): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(String(reader.result));
    reader.onerror = () => reject(reader.error);
    reader.readAsDataURL(blob);
  });
}

/** The page's own font files for these families, read into data addresses (an SVG picture cannot load files itself). */
async function embeddedFaces(families: ReadonlySet<string>): Promise<EmbeddedFace[]> {
  const loading: Promise<EmbeddedFace>[] = [];
  for (const sheet of Array.from(document.styleSheets)) {
    let rules: CSSRuleList;
    try {
      rules = sheet.cssRules;
    } catch {
      continue;
    }
    for (const rule of Array.from(rules)) {
      if (!(rule instanceof CSSFontFaceRule)) continue;
      const family = firstFontFamily(rule.style.getPropertyValue("font-family"));
      const url = fontFaceUrl(rule.style.getPropertyValue("src"));
      if (!url || !families.has(family)) continue;
      loading.push(
        fetch(new URL(url, sheet.href ?? window.location.href))
          .then((response) => response.blob())
          .then(readAsDataUrl)
          .then((data) => ({
            family,
            weight: rule.style.getPropertyValue("font-weight") || "400",
            style: rule.style.getPropertyValue("font-style") || "normal",
            unicodeRange: rule.style.getPropertyValue("unicode-range"),
            data
          }))
      );
    }
  }
  return Promise.all(loading);
}

function loadImage(source: string): Promise<HTMLImageElement> {
  return new Promise((resolve, reject) => {
    const image = new Image();
    image.onload = () => resolve(image);
    image.onerror = () => reject(new Error("The picture could not be drawn."));
    image.src = source;
  });
}

/**
 * Draws `card` (at its own size, whatever scale it is shown at) into a PNG without any library: the element is cloned with its styles
 * written inline, wrapped in an SVG `foreignObject` together with its fonts, and that SVG is painted on a canvas.
 */
export async function renderCardToPng(card: HTMLElement, width: number, height: number): Promise<Blob> {
  const clone = card.cloneNode(true) as HTMLElement;
  const families = new Set<string>();
  inlineStyles(card, clone, families);
  Object.assign(clone.style, { position: "relative", left: "0px", top: "0px", transform: "none" });
  const css = (await embeddedFaces(families)).map(fontFaceCss).join("");
  const svg = wrapInSvg(new XMLSerializer().serializeToString(clone), css, width, height);
  const image = await loadImage(`data:image/svg+xml;charset=utf-8,${encodeURIComponent(svg)}`);
  const canvas = document.createElement("canvas");
  canvas.width = width;
  canvas.height = height;
  canvas.getContext("2d")?.drawImage(image, 0, 0, width, height);
  return new Promise((resolve, reject) => canvas.toBlob((blob) => (blob ? resolve(blob) : reject(new Error("The PNG could not be made."))), "image/png"));
}

function download(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  link.click();
  window.setTimeout(() => URL.revokeObjectURL(url), 1000);
}

/**
 * Puts the PNG on the clipboard (the browser asks for the picture while the click is still fresh, so it is handed over as a promise);
 * where the clipboard cannot take a picture it is saved as a file instead. A rendering that fails fails both ways.
 */
export async function deliverPng(png: Promise<Blob>, fileName: string): Promise<PngDelivery> {
  try {
    if (typeof ClipboardItem === "undefined" || !navigator.clipboard?.write) throw new Error("No clipboard for pictures.");
    await navigator.clipboard.write([new ClipboardItem({ "image/png": png })]);
    return "copied";
  } catch {
    download(await png, fileName);
    return "saved";
  }
}
