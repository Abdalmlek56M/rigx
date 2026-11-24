import type { ReactNode } from "react";

/**
 * Re-mounts on every navigation, so each storefront page gets the short fade-in (`.page-in`, off with reduced motion).
 * Next scrolls the first element of a new page into view; the margins keep that element (the page header) below the sticky
 * site header (129 px on a phone, 79 px from `md`) for every page, instead of one page at a time.
 */
export default function StoreTemplate({ children }: { children: ReactNode }) {
  return <div className="page-in scroll-mt-36 *:scroll-mt-36 md:scroll-mt-24 md:*:scroll-mt-24">{children}</div>;
}
