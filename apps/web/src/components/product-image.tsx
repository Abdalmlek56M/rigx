"use client";

import { resolveDemoProductImage } from "@/lib/demo-media";
import { Icon, type IconName } from "@/components/icons";

const categoryIcons: Record<string, IconName> = {
  processors: "cpu",
  "amd-processors": "cpu",
  "intel-processors": "cpu",
  "graphics-cards": "gpu",
  motherboards: "motherboard",
  memory: "ram",
  storage: "hardDrive",
  cooling: "fan",
  "power-supplies": "power",
  cases: "pcCase",
  peripherals: "peripherals",
};

const categoryLabels: Record<string, { ar: string; en: string }> = {
  processors: { ar: "معالجات", en: "PROCESSORS" },
  "amd-processors": { ar: "معالجات", en: "PROCESSORS" },
  "intel-processors": { ar: "معالجات", en: "PROCESSORS" },
  "graphics-cards": { ar: "كروت شاشة", en: "GRAPHICS" },
  motherboards: { ar: "لوحات أم", en: "MOTHERBOARDS" },
  memory: { ar: "ذاكرة", en: "MEMORY" },
  storage: { ar: "تخزين", en: "STORAGE" },
  cooling: { ar: "تبريد", en: "COOLING" },
  "power-supplies": { ar: "مزودات طاقة", en: "POWER" },
  cases: { ar: "صناديق حاسوب", en: "CASES" },
  peripherals: { ar: "ملحقات", en: "PERIPHERALS" },
};

type ProductImageProps = {
  product: {
    slug?: string | null;
    sku?: string | null;
    categorySlug?: string | null;
    name?: string | null;
  };
  alt: string;
  className?: string;
  imageClassName?: string;
  compact?: boolean;
  loading?: "lazy" | "eager";
};

/**
 * The one product-image surface used by storefront cards, search results,
 * cart lines, galleries, related products, and admin thumbnails.
 *
 * Verified local product photos win. Otherwise the component renders a calm
 * category mark using the same card surface tokens, so a missing asset never
 * looks like a broken image or an unrelated product.
 */
export function ProductImage({
  product,
  alt,
  className = "",
  imageClassName = "",
  compact = false,
  loading = "lazy",
}: ProductImageProps) {
  const source = resolveDemoProductImage(product);

  if (source) {
    return (
      <img
        src={source}
        alt={alt}
        loading={loading}
        decoding="async"
        className={imageClassName}
        draggable={false}
      />
    );
  }

  const category = product.categorySlug ?? "";
  const label = categoryLabels[category];

  return (
    <span
      className={`rigx-image-fallback${compact ? " is-compact" : ""} ${className}`.trim()}
      role="img"
      aria-label={alt}
    >
      <Icon name={categoryIcons[category] ?? "box"} />
      {!compact && label ? <small dir="ltr">RIGX / {label.en}</small> : null}
    </span>
  );
}
