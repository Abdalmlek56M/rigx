export interface NavLink {
  key: "used" | "new" | "builds" | "builder" | "services";
  href: string;
  emphasis?: boolean;
}

/** Header links after "All categories", in display order. */
export const NAV_LINKS: readonly NavLink[] = [
  { key: "used", href: "/products?condition=Used" },
  { key: "new", href: "/products?condition=New" },
  { key: "builds", href: "/builds" },
  { key: "builder", href: "/builder" },
  { key: "services", href: "/services", emphasis: true }
];

export function categoryHref(slug: string): string {
  return `/products?category=${encodeURIComponent(slug)}`;
}

/** The header's controls on the end side: notifications (signed in), account and cart, evenly spaced. */
export const HEADER_ICON_CLUSTER = "flex shrink-0 items-center gap-1.5";

/** One header control (44 px touch target, 20 px icon); a label may follow the icon on wide screens. */
export const HEADER_ICON_BUTTON = "flex h-11 min-w-11 items-center justify-center gap-2 rounded-full text-ink transition-[background-color,color] hover:bg-paper hover:text-orange-text";

/** A small control on the dark top strip (theme, language, WhatsApp). */
export const STRIP_BUTTON = "flex h-8 min-w-8 items-center justify-center gap-1.5 rounded-full px-2 text-[13px] font-medium text-footer-text transition-colors hover:bg-white/10 hover:text-on-carbon";
