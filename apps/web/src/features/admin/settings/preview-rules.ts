/**
 * Where the watermark logo lands on a photo, worked out the way the server does it (`WatermarkPlacement` in the Catalog module): the logo is
 * `sizePercent` of the photo's width with its own proportions (never taller than the photo), and sits 3 % of the shorter side from the
 * edges. The settings screen uses it to draw the preview, so a change shows at once without saving (a half pixel may round the other way than the server's).
 */
export interface PlacedLogo {
  x: number;
  y: number;
  width: number;
  height: number;
}

export const WATERMARK_POSITION_KEYS = ["BottomLeft", "BottomRight", "TopLeft", "TopRight", "Center"] as const;
export type WatermarkPositionKey = (typeof WATERMARK_POSITION_KEYS)[number];

const MARGIN_SHARE = 0.03;

export function placeWatermark(imageWidth: number, imageHeight: number, logoWidth: number, logoHeight: number, position: string, sizePercent: number): PlacedLogo {
  let width = Math.max(1, Math.round((imageWidth * sizePercent) / 100));
  let height = Math.max(1, Math.round((width * logoHeight) / logoWidth));
  if (height > imageHeight) {
    width = Math.max(1, Math.round((width * imageHeight) / height));
    height = imageHeight;
  }
  const margin = Math.round(Math.min(imageWidth, imageHeight) * MARGIN_SHARE);
  const right = Math.max(0, imageWidth - width - margin);
  const bottom = Math.max(0, imageHeight - height - margin);
  switch (position) {
    case "TopLeft": return { x: margin, y: margin, width, height };
    case "TopRight": return { x: right, y: margin, width, height };
    case "BottomLeft": return { x: margin, y: bottom, width, height };
    case "Center": return { x: Math.trunc((imageWidth - width) / 2), y: Math.trunc((imageHeight - height) / 2), width, height };
    default: return { x: right, y: bottom, width, height };
  }
}

/** The preview's numbers from what the form holds as text: a blank, a letter or a value outside the allowed range falls back to the nearest allowed one. */
export function previewNumber(text: string, min: number, max: number, fallback: number): number {
  const value = Number(text);
  if (text.trim() === "" || !Number.isFinite(value)) return fallback;
  return Math.min(max, Math.max(min, value));
}

/** "INV-2026-0001": the invoice number the way the server builds it (`InvoiceRules.FormatNumber`), for the sample on the invoice preview. */
export function sampleInvoiceNumber(prefix: string, year: number): string {
  const clean = prefix.trim().toUpperCase();
  return `${clean || "INV"}-${year}-0001`;
}
