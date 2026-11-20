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

/** The header's icon cluster: one pill holding the theme, language, notifications (signed in), account and cart controls. */
export const HEADER_ICON_CLUSTER = "flex items-center gap-0.5 rounded-full border border-line bg-paper p-1";

/** One round control inside the header icon cluster (same size and icon size for all of them). */
export const HEADER_ICON_BUTTON = "flex h-10 min-w-10 items-center justify-center rounded-full text-ink transition-[background-color,color,box-shadow] hover:bg-surface hover:text-orange-text hover:shadow-soft";
