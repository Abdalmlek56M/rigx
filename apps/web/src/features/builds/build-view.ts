import { buildLine, type CartLine } from "../cart/cart-rules.ts";

/** The parts of a published build the pages read (`AssemblyPublicView`, narrowed). */
export interface BuildView {
  code: string;
  titleAr: string | null;
  titleEn: string | null;
  salePrice: number;
  sellMode: string;
  copies: number;
  warrantyLabel: string | null;
  suitableForAr: string | null;
  suitableForEn: string | null;
  photos: { storageKey: string; thumbnailStorageKey: string }[];
}

const FULL_OR_SPLIT = "FullOrSplit";

/** The text for the visitor's language, falling back to the other one (the owner may fill only one). */
function localized(locale: string, ar: string | null, en: string | null): string | null {
  const [primary, other] = locale === "ar" ? [ar, en] : [en, ar];
  return primary?.trim() || other?.trim() || null;
}

/** Title by locale; a build without any title shows its code. */
export function buildTitle(build: BuildView, locale: string): string {
  return localized(locale, build.titleAr, build.titleEn) ?? build.code;
}

export function buildSuitableFor(build: BuildView, locale: string): string | null {
  return localized(locale, build.suitableForAr, build.suitableForEn);
}

/** Single-part prices are shown only for builds the owner allows to be split. */
export function showPartPrices(build: Pick<BuildView, "sellMode">): boolean {
  return build.sellMode === FULL_OR_SPLIT;
}

export function buildCartLine(build: BuildView, locale: string, quantity = 1): CartLine {
  return buildLine(
    {
      code: build.code,
      title: buildTitle(build, locale),
      warrantyLabel: build.warrantyLabel,
      thumbnailStorageKey: build.photos[0]?.thumbnailStorageKey ?? null,
      salePrice: build.salePrice,
      copies: build.copies
    },
    quantity
  );
}
