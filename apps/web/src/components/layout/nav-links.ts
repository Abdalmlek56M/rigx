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
